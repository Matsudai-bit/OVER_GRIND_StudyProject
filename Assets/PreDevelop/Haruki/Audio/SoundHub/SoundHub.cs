using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// サウンド再生の中枢です。シーンに 1 つ配置して使います。
/// カテゴリー別ミキサー、再生ノードのプール管理、2D / 3D 再生、停止、一時停止、切り替えを担当します。
/// </summary>
[AddComponentMenu("Sound/Sound Hub")]
[DisallowMultipleComponent]
public sealed class SoundHub : MonoBehaviour
{
    /// <summary>
    /// 再生に失敗した場合に返るハンドル ID です。
    /// </summary>
    public const int INVALID_HANDLE = -1;

    // Resources から読み込む SoundDatabase のアセット名
    private const string DATABASE_RESOURCE_NAME = "SoundDatabase";

    // 自動生成するハブのオブジェクト名
    private const string AUTO_OBJECT_NAME = "SoundHub (Auto)";

    // 再生ノードのオブジェクト名
    private const string VOICE_OBJECT_NAME = "SoundVoice";

    // PlayerPrefs のキー（マスター音量）
    private const string PREFS_KEY_MASTER = "SoundHub.Master";

    // PlayerPrefs のキー接頭辞（カテゴリー音量）
    private const string PREFS_KEY_CATEGORY_PREFIX = "SoundHub.Category.";

    // PlayerPrefs のキー接頭辞（カテゴリーミュート）
    private const string PREFS_KEY_MUTE_PREFIX = "SoundHub.Mute.";

    // 同時発音数の上限で古い音を止めるときのフェード秒数
    private const float STEAL_FADE_SECONDS = 0.03f;

    // 音量の最小値
    private const float MIN_VOLUME = 0.0f;

    // 音量の最大値
    private const float MAX_VOLUME = 1.0f;

    // シングルトンのインスタンス
    private static SoundHub m_instance;

    // アプリケーション終了中か（終了中の自動生成を防ぐ）
    private static bool m_isQuitting = false;

    // カテゴリー値の一覧
    private static readonly SoundCategory[] CATEGORY_VALUES = (SoundCategory[])Enum.GetValues(typeof(SoundCategory));

    /// <summary>
    /// SoundHub のインスタンスを取得します。
    /// シーンに無い場合は自動生成します。再生中でない場合や終了中は null です。
    /// </summary>
    public static SoundHub Instance
    {
        get
        {
            if (m_instance != null)
            {
                return m_instance;
            }

            // 再生中でないとき、終了処理中は生成しない
            if (!Application.isPlaying || m_isQuitting)
            {
                return null;
            }

            // シーン上に配置済みならそれを使う
            m_instance = FindFirstObjectByType<SoundHub>();

            if (m_instance == null)
            {
                m_instance = CreateAutoHub();
            }

            m_instance.EnsureInitialized();

            return m_instance;
        }
    }

    /// <summary>
    /// すでに存在する場合だけ SoundHub を取得します。存在しなくても生成しません。
    /// </summary>
    public static SoundHub InstanceIfExists => m_instance;

    [Header("サウンドデータベース（未設定時は Resources/SoundDatabase を探します）")]
    // 音声データを管理するデータベース
    [SerializeField] private SoundDatabase m_database;

    [Header("ミキサー")]
    // 全体のマスター音量
    [SerializeField, Range(0.0f, 1.0f)] private float m_masterVolume = 1.0f;

    // カテゴリー別の音量・ミュート設定
    [SerializeField] private List<SoundCategoryMix> m_categoryMixes = new List<SoundCategoryMix>();

    [Header("再生設定")]
    // 起動時に先に作っておく再生ノードの数
    [SerializeField, Min(0)] private int m_prewarmVoiceCount = 8;

    // 同じ ID の最大同時発音数（0 で無制限）
    [SerializeField, Min(0)] private int m_maxVoicesPerId = 8;

    [Header("その他")]
    // シーンをまたいで残すか
    [SerializeField] private bool m_isPersistent = true;

    // 起動時に保存済みのミキサー設定を読み込むか
    [SerializeField] private bool m_isLoadSavedMixOnAwake = false;

    // 再生中のノード
    private readonly List<SoundVoice> m_activeVoices = new List<SoundVoice>();

    // 再生中のノード（ハンドルごと）
    private readonly Dictionary<int, SoundVoice> m_handleDict = new Dictionary<int, SoundVoice>();

    // 再利用待ちのノード
    private readonly Queue<SoundVoice> m_pool = new Queue<SoundVoice>();

    // サウンド設定の検索用キャッシュ
    private readonly Dictionary<AudioID, SoundDatabase.SoundData> m_dataCache = new Dictionary<AudioID, SoundDatabase.SoundData>();

    // 一括停止時に使う作業用リスト
    private readonly List<SoundVoice> m_workList = new List<SoundVoice>();

    // 警告を出し済みの ID（3D なのに位置指定が無い場合）
    private readonly HashSet<AudioID> m_warnedIds = new HashSet<AudioID>();

    // カテゴリー別の実効音量（マスター込み・カテゴリー値で引く）
    private float[] m_categoryGains = new float[0];

    // 再生終了のコールバック（毎回の生成を避けるため保持）
    private Action<SoundVoice> m_onVoiceFinished;

    // ハンドル ID の採番カウンター
    private int m_handleCounter = 0;

    // 初期化済みか
    private bool m_isInitialized = false;

    /// <summary>
    /// 使用中の SoundDatabase を取得します。
    /// </summary>
    public SoundDatabase Database => m_database;

    /// <summary>
    /// 再生中のノード一覧を取得します（Inspector 表示用）。
    /// </summary>
    public IReadOnlyList<SoundVoice> ActiveVoices => m_activeVoices;

    /// <summary>
    /// カテゴリー別のミキサー設定一覧を取得します。
    /// </summary>
    public IReadOnlyList<SoundCategoryMix> CategoryMixes => m_categoryMixes;

    /// <summary>
    /// 再生中のサウンド数を取得します。
    /// </summary>
    public int ActiveVoiceCount => m_activeVoices.Count;

    /// <summary>
    /// マスター音量（0～1）を取得または設定します。
    /// </summary>
    public float MasterVolume
    {
        get => m_masterVolume;
        set => m_masterVolume = Mathf.Clamp(value, MIN_VOLUME, MAX_VOLUME);
    }

    /// <summary>
    /// ドメインリロード無効時に残る静的な状態を初期化します。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        m_instance = null;
        m_isQuitting = false;
    }

    /// <summary>
    /// 自動生成用のハブを作成します。
    /// </summary>
    /// <returns>生成した SoundHub。</returns>
    private static SoundHub CreateAutoHub()
    {
        Debug.LogWarning("[SoundHub] シーン内に SoundHub が無いため自動生成しました。SoundDatabase は Resources から読み込みます。シーンへの配置を推奨します。");

        GameObject hubObject = new GameObject(AUTO_OBJECT_NAME);
        return hubObject.AddComponent<SoundHub>();
    }

    private void Reset()
    {
        // 追加直後にカテゴリー一覧を用意する
        EnsureCategoryMixes();

#if UNITY_EDITOR
        // プロジェクト内の SoundDatabase を自動で割り当てる
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:SoundDatabase");

        if (guids.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            m_database = UnityEditor.AssetDatabase.LoadAssetAtPath<SoundDatabase>(path);
        }
#endif
    }

    private void OnValidate()
    {
        // カテゴリーの追加に追従し、値を範囲内に収める
        EnsureCategoryMixes();
        m_masterVolume = Mathf.Clamp(m_masterVolume, MIN_VOLUME, MAX_VOLUME);
    }

    private void Awake()
    {
        // 重複したハブは破棄する
        if (m_instance != null && m_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        m_instance = this;

        // シーンをまたいで残す場合はルートに移して保持する
        if (m_isPersistent)
        {
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }

            DontDestroyOnLoad(gameObject);
        }

        EnsureInitialized();
    }

    private void LateUpdate()
    {
        if (m_activeVoices.Count == 0)
        {
            return;
        }

        // Inspector での変更も反映するため、毎フレーム実効音量を更新する
        RefreshCategoryGains();

        // 再生が終わったノードは途中で取り除かれるため、後ろから処理する
        float deltaTime = Time.unscaledDeltaTime;

        for (int i = m_activeVoices.Count - 1; i >= 0; i--)
        {
            if (i >= m_activeVoices.Count)
            {
                continue;
            }

            SoundVoice voice = m_activeVoices[i];
            voice.Tick(deltaTime, GetGain(voice.Category));
        }
    }

    private void OnApplicationQuit()
    {
        // 終了処理中に再生成されないようにする
        m_isQuitting = true;
    }

    private void OnDestroy()
    {
        // 自分自身が登録されている場合のみ解除する
        if (m_instance == this)
        {
            m_instance = null;
        }
    }

    /// <summary>
    /// サウンドを再生します。3D の場合は追従先のオブジェクトから発音します。
    /// </summary>
    /// <param name="id">再生するサウンド ID。</param>
    /// <param name="followTarget">3D 再生時に追従するオブジェクト。</param>
    /// <param name="volumeScale">音量倍率（0～1）。</param>
    /// <param name="pitchScale">ピッチ倍率。</param>
    /// <param name="fadeInSeconds">フェードイン秒数。</param>
    /// <param name="spatialMode">2D / 3D の指定。</param>
    /// <returns>停止や確認に使うハンドル ID。失敗時は INVALID_HANDLE。</returns>
    public int Play(
        AudioID id,
        Transform followTarget = null,
        float volumeScale = 1.0f,
        float pitchScale = 1.0f,
        float fadeInSeconds = 0.0f,
        SoundSpatialMode spatialMode = SoundSpatialMode.USE_DATABASE)
    {
        SoundPlayRequest request = new SoundPlayRequest(
            followTarget,
            false,
            Vector3.zero,
            volumeScale,
            pitchScale,
            fadeInSeconds,
            spatialMode);

        return StartVoice(id, request);
    }

    /// <summary>
    /// 指定した位置でサウンドを再生します（追従しません）。
    /// </summary>
    /// <param name="id">再生するサウンド ID。</param>
    /// <param name="position">発音位置（ワールド座標）。</param>
    /// <param name="volumeScale">音量倍率（0～1）。</param>
    /// <param name="pitchScale">ピッチ倍率。</param>
    /// <param name="fadeInSeconds">フェードイン秒数。</param>
    /// <param name="spatialMode">2D / 3D の指定。</param>
    /// <returns>停止や確認に使うハンドル ID。失敗時は INVALID_HANDLE。</returns>
    public int PlayAt(
        AudioID id,
        Vector3 position,
        float volumeScale = 1.0f,
        float pitchScale = 1.0f,
        float fadeInSeconds = 0.0f,
        SoundSpatialMode spatialMode = SoundSpatialMode.USE_DATABASE)
    {
        SoundPlayRequest request = new SoundPlayRequest(
            null,
            true,
            position,
            volumeScale,
            pitchScale,
            fadeInSeconds,
            spatialMode);

        return StartVoice(id, request);
    }

    /// <summary>
    /// 再生中のサウンドを止め、別のサウンドへ切り替えます（クロスフェード対応）。
    /// </summary>
    /// <param name="currentHandle">現在再生中のハンドル ID。</param>
    /// <param name="nextId">切り替え先のサウンド ID。</param>
    /// <param name="crossFadeSeconds">クロスフェード秒数。0 で即時切り替え。</param>
    /// <param name="followTarget">3D 再生時に追従するオブジェクト。</param>
    /// <param name="volumeScale">音量倍率（0～1）。</param>
    /// <param name="pitchScale">ピッチ倍率。</param>
    /// <param name="spatialMode">2D / 3D の指定。</param>
    /// <returns>切り替え先のハンドル ID。失敗時は INVALID_HANDLE。</returns>
    public int Switch(
        int currentHandle,
        AudioID nextId,
        float crossFadeSeconds = 0.3f,
        Transform followTarget = null,
        float volumeScale = 1.0f,
        float pitchScale = 1.0f,
        SoundSpatialMode spatialMode = SoundSpatialMode.USE_DATABASE)
    {
        Stop(currentHandle, crossFadeSeconds);

        return Play(nextId, followTarget, volumeScale, pitchScale, crossFadeSeconds, spatialMode);
    }

    /// <summary>
    /// ハンドル ID を指定して個別に停止します。
    /// </summary>
    /// <param name="handle">Play が返したハンドル ID。</param>
    /// <param name="fadeOutSeconds">フェードアウト秒数。0 で即時停止。</param>
    public void Stop(int handle, float fadeOutSeconds = 0.0f)
    {
        if (m_handleDict.TryGetValue(handle, out SoundVoice voice))
        {
            voice.Stop(fadeOutSeconds);
        }
    }

    /// <summary>
    /// 指定 ID で再生中のサウンドをすべて停止します。
    /// </summary>
    /// <param name="id">停止するサウンド ID。</param>
    /// <param name="fadeOutSeconds">フェードアウト秒数。0 で即時停止。</param>
    public void Stop(AudioID id, float fadeOutSeconds = 0.0f)
    {
        m_workList.Clear();

        foreach (SoundVoice voice in m_activeVoices)
        {
            if (voice.SoundId == id)
            {
                m_workList.Add(voice);
            }
        }

        StopWorkList(fadeOutSeconds);
    }

    /// <summary>
    /// 指定カテゴリーのサウンドをすべて停止します。
    /// </summary>
    /// <param name="category">停止するカテゴリー。</param>
    /// <param name="fadeOutSeconds">フェードアウト秒数。0 で即時停止。</param>
    public void StopCategory(SoundCategory category, float fadeOutSeconds = 0.0f)
    {
        m_workList.Clear();

        foreach (SoundVoice voice in m_activeVoices)
        {
            if (voice.Category == category)
            {
                m_workList.Add(voice);
            }
        }

        StopWorkList(fadeOutSeconds);
    }

    /// <summary>
    /// 再生中のサウンドをすべて停止します。
    /// </summary>
    /// <param name="fadeOutSeconds">フェードアウト秒数。0 で即時停止。</param>
    public void StopAll(float fadeOutSeconds = 0.0f)
    {
        m_workList.Clear();
        m_workList.AddRange(m_activeVoices);

        StopWorkList(fadeOutSeconds);
    }

    /// <summary>
    /// 指定カテゴリーのサウンドを一時停止します（ゲームのポーズ用）。
    /// </summary>
    /// <param name="category">一時停止するカテゴリー。</param>
    public void PauseCategory(SoundCategory category)
    {
        SetPaused(true, true, category);
    }

    /// <summary>
    /// 指定カテゴリーの一時停止を解除します。
    /// </summary>
    /// <param name="category">再開するカテゴリー。</param>
    public void ResumeCategory(SoundCategory category)
    {
        SetPaused(false, true, category);
    }

    /// <summary>
    /// 再生中のサウンドをすべて一時停止します。
    /// </summary>
    public void PauseAll()
    {
        SetPaused(true, false, SoundCategory.BGM);
    }

    /// <summary>
    /// 一時停止中のサウンドをすべて再開します。
    /// </summary>
    public void ResumeAll()
    {
        SetPaused(false, false, SoundCategory.BGM);
    }

    /// <summary>
    /// 指定ハンドルのサウンドが再生中かを確認します。
    /// </summary>
    /// <param name="handle">Play が返したハンドル ID。</param>
    /// <returns>
    /// true：再生中（一時停止中を含む）です。
    /// false：再生していません。
    /// </returns>
    public bool IsPlaying(int handle)
    {
        return m_handleDict.ContainsKey(handle);
    }

    /// <summary>
    /// 指定 ID のサウンドが再生中かを確認します。
    /// </summary>
    /// <param name="id">確認するサウンド ID。</param>
    /// <returns>
    /// true：再生中です。
    /// false：再生していません。
    /// </returns>
    public bool IsPlaying(AudioID id)
    {
        foreach (SoundVoice voice in m_activeVoices)
        {
            if (voice.SoundId == id)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 再生中のサウンドに音量倍率を設定します。
    /// </summary>
    /// <param name="handle">Play が返したハンドル ID。</param>
    /// <param name="volumeScale">音量倍率（0～1）。</param>
    public void SetVolumeScale(int handle, float volumeScale)
    {
        if (m_handleDict.TryGetValue(handle, out SoundVoice voice))
        {
            voice.SetVolumeScale(volumeScale);
        }
    }

    /// <summary>
    /// カテゴリー音量を設定します。
    /// </summary>
    /// <param name="category">対象のカテゴリー。</param>
    /// <param name="volume">音量（0～1）。</param>
    public void SetCategoryVolume(SoundCategory category, float volume)
    {
        SoundCategoryMix mix = FindMix(category);

        if (mix != null)
        {
            mix.Volume = volume;
        }
    }

    /// <summary>
    /// カテゴリー音量を取得します。
    /// </summary>
    /// <param name="category">対象のカテゴリー。</param>
    /// <returns>カテゴリー音量（0～1）。</returns>
    public float GetCategoryVolume(SoundCategory category)
    {
        SoundCategoryMix mix = FindMix(category);

        return mix != null ? mix.Volume : MAX_VOLUME;
    }

    /// <summary>
    /// カテゴリーのミュートを設定します。
    /// </summary>
    /// <param name="category">対象のカテゴリー。</param>
    /// <param name="isMuted">ミュートするか。</param>
    public void SetCategoryMuted(SoundCategory category, bool isMuted)
    {
        SoundCategoryMix mix = FindMix(category);

        if (mix != null)
        {
            mix.IsMuted = isMuted;
        }
    }

    /// <summary>
    /// カテゴリーがミュート中かを確認します。
    /// </summary>
    /// <param name="category">対象のカテゴリー。</param>
    /// <returns>
    /// true：ミュート中です。
    /// false：ミュートしていません。
    /// </returns>
    public bool IsCategoryMuted(SoundCategory category)
    {
        SoundCategoryMix mix = FindMix(category);

        return mix != null && mix.IsMuted;
    }

    /// <summary>
    /// マスターとカテゴリーを合わせた実効音量を取得します。
    /// </summary>
    /// <param name="category">対象のカテゴリー。</param>
    /// <returns>実効音量（0～1）。</returns>
    public float GetCategoryGain(SoundCategory category)
    {
        SoundCategoryMix mix = FindMix(category);

        return m_masterVolume * (mix != null ? mix.Gain : MAX_VOLUME);
    }

    /// <summary>
    /// ミキサー設定を PlayerPrefs へ保存します（オプション画面用）。
    /// </summary>
    public void SaveMix()
    {
        PlayerPrefs.SetFloat(PREFS_KEY_MASTER, m_masterVolume);

        foreach (SoundCategoryMix mix in m_categoryMixes)
        {
            string categoryName = mix.Category.ToString();

            PlayerPrefs.SetFloat(PREFS_KEY_CATEGORY_PREFIX + categoryName, mix.Volume);
            PlayerPrefs.SetInt(PREFS_KEY_MUTE_PREFIX + categoryName, mix.IsMuted ? 1 : 0);
        }

        PlayerPrefs.Save();
    }

    /// <summary>
    /// 保存済みのミキサー設定を PlayerPrefs から読み込みます。
    /// </summary>
    public void LoadMix()
    {
        EnsureCategoryMixes();

        m_masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PREFS_KEY_MASTER, m_masterVolume));

        foreach (SoundCategoryMix mix in m_categoryMixes)
        {
            string categoryName = mix.Category.ToString();

            mix.Volume = PlayerPrefs.GetFloat(PREFS_KEY_CATEGORY_PREFIX + categoryName, mix.Volume);
            mix.IsMuted = PlayerPrefs.GetInt(PREFS_KEY_MUTE_PREFIX + categoryName, mix.IsMuted ? 1 : 0) != 0;
        }
    }

    /// <summary>
    /// 初期化を行います。何度呼んでも 1 回だけ実行されます。
    /// </summary>
    private void EnsureInitialized()
    {
        if (m_isInitialized)
        {
            return;
        }

        m_isInitialized = true;

        m_onVoiceFinished = OnVoiceFinished;

        EnsureCategoryMixes();
        BuildGainTable();
        RebuildCache();
        PrewarmVoices();

        if (m_isLoadSavedMixOnAwake)
        {
            LoadMix();
        }
    }

    /// <summary>
    /// すべてのカテゴリーにミキサー設定があるようにします。
    /// </summary>
    private void EnsureCategoryMixes()
    {
        if (m_categoryMixes == null)
        {
            m_categoryMixes = new List<SoundCategoryMix>();
        }

        foreach (SoundCategory category in CATEGORY_VALUES)
        {
            if (FindMix(category) == null)
            {
                m_categoryMixes.Add(new SoundCategoryMix(category));
            }
        }
    }

    /// <summary>
    /// カテゴリーのミキサー設定を検索します。
    /// </summary>
    /// <param name="category">対象のカテゴリー。</param>
    /// <returns>ミキサー設定。見つからない場合は null。</returns>
    private SoundCategoryMix FindMix(SoundCategory category)
    {
        if (m_categoryMixes == null)
        {
            return null;
        }

        foreach (SoundCategoryMix mix in m_categoryMixes)
        {
            if (mix != null && mix.Category == category)
            {
                return mix;
            }
        }

        return null;
    }

    /// <summary>
    /// カテゴリー別の実効音量テーブルを作成します。
    /// </summary>
    private void BuildGainTable()
    {
        int maxValue = 0;

        foreach (SoundCategory category in CATEGORY_VALUES)
        {
            maxValue = Mathf.Max(maxValue, (int)category);
        }

        m_categoryGains = new float[maxValue + 1];

        for (int i = 0; i < m_categoryGains.Length; i++)
        {
            m_categoryGains[i] = MAX_VOLUME;
        }
    }

    /// <summary>
    /// 実効音量テーブルを最新のミキサー設定に更新します。
    /// </summary>
    private void RefreshCategoryGains()
    {
        foreach (SoundCategoryMix mix in m_categoryMixes)
        {
            int index = (int)mix.Category;

            if (index >= 0 && index < m_categoryGains.Length)
            {
                m_categoryGains[index] = m_masterVolume * mix.Gain;
            }
        }
    }

    /// <summary>
    /// カテゴリーの実効音量をテーブルから取得します。
    /// </summary>
    /// <param name="category">対象のカテゴリー。</param>
    /// <returns>実効音量（0～1）。</returns>
    private float GetGain(SoundCategory category)
    {
        int index = (int)category;

        if (index < 0 || index >= m_categoryGains.Length)
        {
            return m_masterVolume;
        }

        return m_categoryGains[index];
    }

    /// <summary>
    /// データベースが利用できる状態にします。
    /// </summary>
    /// <returns>
    /// true：データベースを利用できます。
    /// false：データベースが見つかりません。
    /// </returns>
    private bool EnsureDatabase()
    {
        if (m_database != null)
        {
            return true;
        }

        // 未設定の場合は Resources から読み込む
        m_database = Resources.Load<SoundDatabase>(DATABASE_RESOURCE_NAME);

        if (m_database == null)
        {
            Debug.LogError("[SoundHub] SoundDatabase が見つかりません。Inspector で割り当てるか、Resources フォルダに配置してください。");
            return false;
        }

        return true;
    }

    /// <summary>
    /// サウンド設定の検索用キャッシュを作り直します。
    /// </summary>
    private void RebuildCache()
    {
        m_dataCache.Clear();

        if (!EnsureDatabase())
        {
            return;
        }

        foreach (SoundDatabase.SoundData data in m_database.m_soundList)
        {
            if (data != null && !m_dataCache.ContainsKey(data.m_id))
            {
                m_dataCache.Add(data.m_id, data);
            }
        }
    }

    /// <summary>
    /// サウンド設定を取得します。
    /// </summary>
    /// <param name="id">サウンド ID。</param>
    /// <param name="data">取得したサウンド設定。</param>
    /// <returns>
    /// true：再生可能な設定が見つかりました。
    /// false：設定が無い、または AudioClip が未設定です。
    /// </returns>
    private bool TryGetSoundData(AudioID id, out SoundDatabase.SoundData data)
    {
        if (m_dataCache.TryGetValue(id, out data) && data.m_clip != null)
        {
            return true;
        }

#if UNITY_EDITOR
        // Tools でデータを追加した直後にも対応できるよう、エディタでは作り直して再確認する
        RebuildCache();

        if (m_dataCache.TryGetValue(id, out data) && data.m_clip != null)
        {
            return true;
        }
#endif

        Debug.LogWarning($"[SoundHub] {id} の再生データ（AudioClip）が見つかりません。");
        data = null;
        return false;
    }

    /// <summary>
    /// 起動時に再生ノードを先に作成します。
    /// </summary>
    private void PrewarmVoices()
    {
        for (int i = 0; i < m_prewarmVoiceCount; i++)
        {
            m_pool.Enqueue(CreateVoice());
        }
    }

    /// <summary>
    /// 再生リクエストからノードを起動して再生します。
    /// </summary>
    /// <param name="id">再生するサウンド ID。</param>
    /// <param name="request">再生リクエスト。</param>
    /// <returns>ハンドル ID。失敗時は INVALID_HANDLE。</returns>
    private int StartVoice(AudioID id, SoundPlayRequest request)
    {
        if (id == AudioID.None)
        {
            return INVALID_HANDLE;
        }

        EnsureInitialized();

        if (!TryGetSoundData(id, out SoundDatabase.SoundData data))
        {
            return INVALID_HANDLE;
        }

        // 2D / 3D を決め、同時発音数を制限する
        bool is3D = ResolveIs3D(id, data, request);
        LimitVoices(id);

        // ノードを用意して初期設定を行う
        SoundVoice voice = GetVoiceFromPool();
        int handle = ++m_handleCounter;

        if (!voice.Init(handle, data, is3D, request, m_onVoiceFinished))
        {
            PushVoiceToPool(voice);
            return INVALID_HANDLE;
        }

#if UNITY_EDITOR
        // Hierarchy で何の音か分かるようにする
        voice.gameObject.name = $"{VOICE_OBJECT_NAME}_{id}";
#endif

        // 有効化して管理対象に登録し、再生する
        voice.gameObject.SetActive(true);
        m_activeVoices.Add(voice);
        m_handleDict[handle] = voice;

        RefreshCategoryGains();
        voice.Play(GetGain(data.m_category));

        return handle;
    }

    /// <summary>
    /// 3D で再生するかを決定します。
    /// </summary>
    /// <param name="id">サウンド ID。</param>
    /// <param name="data">サウンド設定。</param>
    /// <param name="request">再生リクエスト。</param>
    /// <returns>
    /// true：3D で再生します。
    /// false：2D で再生します。
    /// </returns>
    private bool ResolveIs3D(AudioID id, SoundDatabase.SoundData data, SoundPlayRequest request)
    {
        // 指定とデータベース設定から 3D かを決める
        bool is3D = request.SpatialMode == SoundSpatialMode.FORCE_3D
            || (request.SpatialMode == SoundSpatialMode.USE_DATABASE && data.m_is3D);

        if (!is3D)
        {
            return false;
        }

        // 発音位置が無い 3D 再生は聞こえなくなるため 2D にする
        bool hasSource = request.HasPosition || request.FollowTarget != null;

        if (!hasSource)
        {
            if (m_warnedIds.Add(id))
            {
                Debug.LogWarning($"[SoundHub] {id} は 3D ですが発音位置・追従先が指定されていないため 2D で再生します。");
            }

            return false;
        }

        return true;
    }

    /// <summary>
    /// 同じ ID の同時発音数が上限に達していれば、最も古い音を止めます。
    /// </summary>
    /// <param name="id">これから再生するサウンド ID。</param>
    private void LimitVoices(AudioID id)
    {
        if (m_maxVoicesPerId <= 0)
        {
            return;
        }

        // 同じ ID の数と、最も古いノードを調べる
        int count = 0;
        SoundVoice oldest = null;

        foreach (SoundVoice voice in m_activeVoices)
        {
            if (voice.SoundId != id || voice.IsStopping)
            {
                continue;
            }

            count++;

            if (oldest == null || voice.HandleId < oldest.HandleId)
            {
                oldest = voice;
            }
        }

        if (count >= m_maxVoicesPerId && oldest != null)
        {
            oldest.Stop(STEAL_FADE_SECONDS);
        }
    }

    /// <summary>
    /// 作業用リストのノードをまとめて停止します。
    /// </summary>
    /// <param name="fadeOutSeconds">フェードアウト秒数。</param>
    private void StopWorkList(float fadeOutSeconds)
    {
        for (int i = 0; i < m_workList.Count; i++)
        {
            m_workList[i].Stop(fadeOutSeconds);
        }

        m_workList.Clear();
    }

    /// <summary>
    /// 条件に合うノードの一時停止状態を切り替えます。
    /// </summary>
    /// <param name="isPaused">一時停止するか（false で再開）。</param>
    /// <param name="isFilterByCategory">カテゴリーで絞り込むか。</param>
    /// <param name="category">絞り込むカテゴリー。</param>
    private void SetPaused(bool isPaused, bool isFilterByCategory, SoundCategory category)
    {
        foreach (SoundVoice voice in m_activeVoices)
        {
            if (isFilterByCategory && voice.Category != category)
            {
                continue;
            }

            if (isPaused)
            {
                voice.Pause();
            }
            else
            {
                voice.Resume();
            }
        }
    }

    /// <summary>
    /// 再生が終わったノードを管理対象から外し、プールへ戻します。
    /// </summary>
    /// <param name="voice">終了したノード。</param>
    private void OnVoiceFinished(SoundVoice voice)
    {
        m_activeVoices.Remove(voice);
        m_handleDict.Remove(voice.HandleId);

        // 破棄されたノードはプールへ戻さない
        if (voice.IsDestroyed)
        {
            return;
        }

        PushVoiceToPool(voice);
    }

    /// <summary>
    /// プールからノードを取得します。無ければ新規生成します。
    /// </summary>
    /// <returns>利用可能なノード。</returns>
    private SoundVoice GetVoiceFromPool()
    {
        // 破棄されたノードを読み飛ばす
        while (m_pool.Count > 0)
        {
            SoundVoice pooledVoice = m_pool.Dequeue();

            if (pooledVoice != null)
            {
                return pooledVoice;
            }
        }

        return CreateVoice();
    }

    /// <summary>
    /// 再生ノードを新規生成します（無効状態で作成）。
    /// </summary>
    /// <returns>生成したノード。</returns>
    private SoundVoice CreateVoice()
    {
        GameObject voiceObject = new GameObject(VOICE_OBJECT_NAME);
        voiceObject.transform.SetParent(transform);
        voiceObject.SetActive(false);

        return voiceObject.AddComponent<SoundVoice>();
    }

    /// <summary>
    /// ノードを無効化してプールへ戻します。
    /// </summary>
    /// <param name="voice">戻すノード。</param>
    private void PushVoiceToPool(SoundVoice voice)
    {
        voice.gameObject.SetActive(false);
        voice.transform.SetParent(transform);

        m_pool.Enqueue(voice);
    }
}