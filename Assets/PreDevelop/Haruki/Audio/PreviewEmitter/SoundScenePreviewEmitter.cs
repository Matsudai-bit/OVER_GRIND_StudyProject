using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// シーン上に配置して SoundDatabase のサウンド（3D減衰・I/L/O一連再生）を試聴するためのエミッターです。
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class SoundScenePreviewEmitter : MonoBehaviour
{
    /// <summary>
    /// エミッター内部で判定するパート種別です。
    /// </summary>
    private enum EmitterPartType
    {
        PART_SINGLE,
        PART_INTRO,
        PART_LOOP,
        PART_OUTRO
    }

    // SoundDatabase を検索するフィルター
    private const string DATABASE_SEARCH_FILTER = "t:SoundDatabase";

    // Resources から読み込む際の既定パス
    private const string DEFAULT_RESOURCE_PATH = "SoundDatabase";

    // 開始パートのサフィックス
    private const string SUFFIX_INTRO = "_I";

    // ループパートのサフィックス
    private const string SUFFIX_LOOP = "_L";

    // 終了パートのサフィックス
    private const string SUFFIX_OUTRO = "_O";

    // サフィックス判定の最小文字数
    private const int MIN_SUFFIXED_LENGTH = 3;

    // サフィックスの文字数
    private const int SUFFIX_LENGTH = 2;

    // 最小距離の下限値
    private const float MIN_DISTANCE_LIMIT = 0.01f;

    // 最大距離の余白
    private const float DISTANCE_MARGIN = 0.01f;

    // 末尾ジャンプの最小残り時間
    private const float MIN_JUMP_REMAINING = 0.1f;

    // シーク終端の余白時間
    private const float SEEK_END_MARGIN = 0.05f;

    // リピート間隔の最小値（秒）
    private const float MIN_REPEAT_INTERVAL = 0.1f;

    // ギズモ球の半径
    private const float GIZMO_CENTER_RADIUS = 0.35f;

    // ステレオパンの最大係数
    private const float STEREO_PAN_FACTOR = 0.85f;

    // 再生ステージ：停止中
    private const int STAGE_STOPPED = 0;

    // 再生ステージ：単体再生
    private const int STAGE_SINGLE = 1;

    // 再生ステージ：開始パート（_I）
    private const int STAGE_INTRO = 2;

    // 再生ステージ：ループパート（_L）
    private const int STAGE_LOOP = 3;

    // 再生ステージ：終了パート（_O）
    private const int STAGE_OUTRO = 4;

    // 最小距離ギズモの色
    private static readonly Color MIN_DISTANCE_GIZMO_COLOR = new Color(0.20f, 0.85f, 0.55f, 0.90f);

    // 最大距離ギズモの色
    private static readonly Color MAX_DISTANCE_GIZMO_COLOR = new Color(1.00f, 0.65f, 0.20f, 0.70f);

    // リスナー接続線の色
    private static readonly Color LISTENER_LINE_COLOR = new Color(0.35f, 0.80f, 1.00f, 0.85f);

    // 参照する SoundDatabase
    [SerializeField] private SoundDatabase m_database;

    // 選択中の AudioID
    [SerializeField] private AudioID m_audioId = AudioID.None;

    // I/L/O グループの場合に一連再生するか
    [SerializeField] private bool m_playAsSequence = true;

    // ループを1周したら自動で Outro へ移行するか
    [SerializeField] private bool m_autoOutro = false;

    // 単発音（SEなど）を一定間隔でリピート再生するか
    [SerializeField] private bool m_autoRepeat = false;

    // リピート再生の間隔（秒）
    [SerializeField] private float m_repeatInterval = 1.2f;

    // Edit Mode 時に Scene カメラ位置をリスナー基準にするか
    [SerializeField] private bool m_useSceneCameraAsListener = true;

    // 試聴に使用する AudioSource
    private AudioSource m_audioSource;

    // 現在のシーケンス再生状態（0:停止, 1:単体, 2:Intro, 3:Loop, 4:Outro）
    private int m_sequenceStage = STAGE_STOPPED;

    // 再生中の単体データ
    private SoundDatabase.SoundData m_singleData;

    // 再生中の開始データ（_I）
    private SoundDatabase.SoundData m_introData;

    // 再生中のループデータ（_L）
    private SoundDatabase.SoundData m_loopData;

    // 再生中の終了データ（_O）
    private SoundDatabase.SoundData m_outroData;

    // リピート待機中か
    private bool m_isWaitingRepeat = false;

    // 次のリピート再生を開始する時刻
    private double m_nextRepeatTime = 0.0;

    /// <summary>
    /// 参照する SoundDatabase を取得または設定します。
    /// </summary>
    public SoundDatabase Database
    {
        get => EnsureDatabase();
        set => m_database = value;
    }

    /// <summary>
    /// 選択中の AudioID を取得または設定します。
    /// </summary>
    public AudioID SelectedAudioId
    {
        get => m_audioId;
        set => m_audioId = value;
    }

    /// <summary>
    /// I/L/O 一連再生を行うかを取得または設定します。
    /// </summary>
    public bool PlayAsSequence
    {
        get => m_playAsSequence;
        set => m_playAsSequence = value;
    }

    /// <summary>
    /// 自動 Outro 移行を行うかを取得または設定します。
    /// </summary>
    public bool AutoOutro
    {
        get => m_autoOutro;
        set
        {
            m_autoOutro = value;
            if (m_sequenceStage == STAGE_LOOP && m_audioSource != null)
            {
                m_audioSource.loop = !m_autoOutro;
            }
        }
    }

    /// <summary>
    /// 間隔リピート再生を行うかを取得または設定します。
    /// </summary>
    public bool AutoRepeat
    {
        get => m_autoRepeat;
        set => m_autoRepeat = value;
    }

    /// <summary>
    /// リピート再生の間隔（秒）を取得または設定します。
    /// </summary>
    public float RepeatInterval
    {
        get => m_repeatInterval;
        set => m_repeatInterval = Mathf.Max(MIN_REPEAT_INTERVAL, value);
    }

    /// <summary>
    /// Scene カメラをリスナー基準にするかを取得または設定します。
    /// </summary>
    public bool UseSceneCameraAsListener
    {
        get => m_useSceneCameraAsListener;
        set => m_useSceneCameraAsListener = value;
    }

    /// <summary>
    /// 現在再生している段階（0:停止, 1:単体, 2:Intro, 3:Loop, 4:Outro）を取得します。
    /// </summary>
    public int SequenceStage => m_sequenceStage;

    private void OnEnable()
    {
        EnsureDatabase();
#if UNITY_EDITOR
        EditorApplication.update += UpdateEmitterState;
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.update -= UpdateEmitterState;
#endif
        StopPreview();
    }

    private void Update()
    {
        // Play モード実行時のみ Update で状態更新を行う（Edit モード時は EditorApplication.update で駆動）
        if (Application.isPlaying)
        {
            UpdateEmitterState();
        }
    }

    /// <summary>
    /// 現在選択されている AudioID（または I/L/O シーケンス）の再生を開始します。
    /// </summary>
    public void PlayPreview()
    {
        StopPreview();

        SoundDatabase database = EnsureDatabase();
        if (database == null || m_audioId == AudioID.None)
        {
            return;
        }

        ResolvePlaybackTargets(database);

        if (m_playAsSequence && (m_introData != null || m_loopData != null || m_outroData != null))
        {
            if (HasClip(m_introData))
            {
                StartClipStage(STAGE_INTRO, m_introData, false);
            }
            else if (HasClip(m_loopData))
            {
                StartClipStage(STAGE_LOOP, m_loopData, !m_autoOutro);
            }
            else if (HasClip(m_outroData))
            {
                StartClipStage(STAGE_OUTRO, m_outroData, false);
            }
        }
        else if (HasClip(m_singleData))
        {
            StartClipStage(STAGE_SINGLE, m_singleData, m_singleData.m_loop);
        }
    }

    /// <summary>
    /// シーケンス再生中に Outro（_O）へ移行します。
    /// </summary>
    public void TriggerOutro()
    {
        if (m_sequenceStage == STAGE_STOPPED || !HasClip(m_outroData))
        {
            StopPreview();
            return;
        }

        StartClipStage(STAGE_OUTRO, m_outroData, false);
    }

    /// <summary>
    /// 現在のクリップの末尾付近へジャンプします。
    /// </summary>
    /// <param name="remainingSeconds">末尾からの残り秒数。</param>
    public void JumpNearEnd(float remainingSeconds)
    {
        if (m_audioSource == null || m_audioSource.clip == null)
        {
            return;
        }

        float safeRemaining = Mathf.Max(MIN_JUMP_REMAINING, remainingSeconds);
        float targetTime = Mathf.Max(0.0f, m_audioSource.clip.length - safeRemaining);
        m_audioSource.time = Mathf.Min(targetTime, Mathf.Max(0.0f, m_audioSource.clip.length - SEEK_END_MARGIN));
    }

    /// <summary>
    /// 再生を停止します。
    /// </summary>
    public void StopPreview()
    {
        if (m_audioSource != null)
        {
            m_audioSource.Stop();
        }

        m_sequenceStage = STAGE_STOPPED;
        m_isWaitingRepeat = false;
    }

    /// <summary>
    /// 現在再生中（またはリピート待機中）かを確認します。
    /// </summary>
    /// <returns>
    /// true：再生中またはリピート待機中です。
    /// false：停止しています。
    /// </returns>
    public bool IsPlaying()
    {
        if (m_sequenceStage == STAGE_STOPPED)
        {
            return false;
        }

        if (m_isWaitingRepeat)
        {
            return true;
        }

        return m_audioSource != null && m_audioSource.isPlaying;
    }

    /// <summary>
    /// 現在再生対象となっている SoundData を取得します。
    /// </summary>
    /// <returns>現在アクティブな SoundData。</returns>
    public SoundDatabase.SoundData GetActiveSoundData()
    {
        switch (m_sequenceStage)
        {
            case STAGE_INTRO:
                return m_introData;
            case STAGE_LOOP:
                return m_loopData;
            case STAGE_OUTRO:
                return m_outroData;
            default:
                SoundDatabase database = EnsureDatabase();
                return database != null ? database.GetSoundData(m_audioId) : null;
        }
    }

    /// <summary>
    /// 現在のリスナー位置との距離（メートル）を取得します。
    /// </summary>
    /// <returns>リスナーまでの距離。</returns>
    public float GetDistanceToListener()
    {
        if (TryGetListenerVectors(out Vector3 listenerPos, out Vector3 _))
        {
            return Vector3.Distance(listenerPos, transform.position);
        }

        return 0.0f;
    }

    /// <summary>
    /// 指定された SoundData と距離から、3D 距離減衰率（0～1）を算出します。
    /// </summary>
    /// <param name="data">サウンド設定データ。</param>
    /// <param name="distance">リスナーからの距離。</param>
    /// <returns>減衰率（0.0～1.0）。</returns>
    public float EvaluateAttenuation(SoundDatabase.SoundData data, float distance)
    {
        if (data == null || !data.m_is3D)
        {
            return 1.0f;
        }

        float minDistance = Mathf.Max(MIN_DISTANCE_LIMIT, data.m_minDistance);
        float maxDistance = Mathf.Max(minDistance + DISTANCE_MARGIN, data.m_maxDistance);

        switch (data.m_rolloffMode)
        {
            case AudioRolloffMode.Linear:
                if (distance <= minDistance)
                {
                    return 1.0f;
                }
                if (distance >= maxDistance)
                {
                    return 0.0f;
                }
                return 1.0f - ((distance - minDistance) / (maxDistance - minDistance));

            case AudioRolloffMode.Custom:
                if (data.m_customRolloffCurve == null)
                {
                    return 1.0f;
                }
                float normalizedDist = Mathf.Clamp01(distance / maxDistance);
                return Mathf.Clamp01(data.m_customRolloffCurve.Evaluate(normalizedDist));

            default:
                if (distance <= minDistance)
                {
                    return 1.0f;
                }
                return Mathf.Clamp01(minDistance / distance);
        }
    }

    /// <summary>
    /// 現在のリスナー位置と右方向ベクトルを取得します。
    /// </summary>
    /// <param name="listenerPosition">リスナーの座標。</param>
    /// <param name="listenerRight">リスナーの右方向ベクトル。</param>
    /// <returns>
    /// true：取得に成功しました。
    /// false：リスナーが見つかりません。
    /// </returns>
    public bool TryGetListenerVectors(out Vector3 listenerPosition, out Vector3 listenerRight)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && m_useSceneCameraAsListener)
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null && sceneView.camera != null)
            {
                Transform camTransform = sceneView.camera.transform;
                listenerPosition = camTransform.position;
                listenerRight = camTransform.right;
                return true;
            }
        }
#endif
        AudioListener listener = FindFirstObjectByType<AudioListener>();
        if (listener != null)
        {
            Transform listenerTransform = listener.transform;
            listenerPosition = listenerTransform.position;
            listenerRight = listenerTransform.right;
            return true;
        }

        listenerPosition = transform.position;
        listenerRight = Vector3.right;
        return false;
    }

    /// <summary>
    /// リアルタイムで SoundDatabase の値を AudioSource に同期し、シーケンス遷移を管理します。
    /// </summary>
    private void UpdateEmitterState()
    {
        if (m_sequenceStage == STAGE_STOPPED)
        {
            return;
        }

        SoundDatabase.SoundData activeData = GetActiveSoundData();
        if (activeData != null && m_audioSource != null)
        {
            ApplySoundDataToAudioSource(activeData);
        }

        // リピート待機中の判定
        if (m_isWaitingRepeat)
        {
            if (GetCurrentTimeSeconds() >= m_nextRepeatTime)
            {
                m_isWaitingRepeat = false;
                PlayPreview();
            }
            return;
        }

        if (m_audioSource != null && m_audioSource.isPlaying)
        {
            return;
        }

        // クリップ再生終了時の次ステージ遷移
        switch (m_sequenceStage)
        {
            case STAGE_INTRO:
                if (HasClip(m_loopData))
                {
                    StartClipStage(STAGE_LOOP, m_loopData, !m_autoOutro);
                }
                else if (HasClip(m_outroData))
                {
                    StartClipStage(STAGE_OUTRO, m_outroData, false);
                }
                else
                {
                    HandlePlaybackFinished();
                }
                break;

            case STAGE_LOOP:
                if (m_autoOutro && HasClip(m_outroData))
                {
                    StartClipStage(STAGE_OUTRO, m_outroData, false);
                }
                else
                {
                    HandlePlaybackFinished();
                }
                break;

            default:
                HandlePlaybackFinished();
                break;
        }
    }

    /// <summary>
    /// 一通りの再生が完了した際のリピートまたは停止処理を行います。
    /// </summary>
    private void HandlePlaybackFinished()
    {
        if (m_autoRepeat)
        {
            m_isWaitingRepeat = true;
            m_nextRepeatTime = GetCurrentTimeSeconds() + Mathf.Max(MIN_REPEAT_INTERVAL, m_repeatInterval);
            return;
        }

        StopPreview();
    }

    /// <summary>
    /// 指定ステージのクリップ再生を開始します。
    /// </summary>
    private void StartClipStage(int stage, SoundDatabase.SoundData data, bool loop)
    {
        if (!HasClip(data) || !EnsureAudioSource())
        {
            StopPreview();
            return;
        }

        m_sequenceStage = stage;
        m_isWaitingRepeat = false;

        data.m_clip.LoadAudioData();
        m_audioSource.Stop();
        m_audioSource.clip = data.m_clip;
        m_audioSource.loop = loop;
        ApplySoundDataToAudioSource(data);
        m_audioSource.time = 0.0f;
        m_audioSource.Play();
    }

    /// <summary>
    /// SoundDatabase の設定値と 3D 減衰を AudioSource へ即時反映します。
    /// </summary>
    private void ApplySoundDataToAudioSource(SoundDatabase.SoundData data)
    {
        if (m_audioSource == null || data == null)
        {
            return;
        }

        m_audioSource.pitch = data.m_pitch;
        float minDistance = Mathf.Max(MIN_DISTANCE_LIMIT, data.m_minDistance);
        float maxDistance = Mathf.Max(minDistance + DISTANCE_MARGIN, data.m_maxDistance);

        m_audioSource.minDistance = minDistance;
        m_audioSource.maxDistance = maxDistance;
        m_audioSource.rolloffMode = data.m_rolloffMode;

        if (data.m_rolloffMode == AudioRolloffMode.Custom && data.m_customRolloffCurve != null)
        {
            m_audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, data.m_customRolloffCurve);
        }

#if UNITY_EDITOR
        // Edit Mode で Scene カメラをリスナーにする場合は、カメラ距離減衰と左右パンを直接計算して適用する
        if (!Application.isPlaying && m_useSceneCameraAsListener && data.m_is3D)
        {
            float distance = GetDistanceToListener();
            float attenuation = EvaluateAttenuation(data, distance);
            m_audioSource.spatialBlend = 0.0f;
            m_audioSource.volume = data.m_volume * attenuation;

            if (TryGetListenerVectors(out Vector3 listenerPos, out Vector3 listenerRight))
            {
                Vector3 toEmitter = transform.position - listenerPos;
                float pan = toEmitter.sqrMagnitude > 0.001f ? Vector3.Dot(listenerRight.normalized, toEmitter.normalized) : 0.0f;
                m_audioSource.panStereo = Mathf.Clamp(pan * STEREO_PAN_FACTOR, -1.0f, 1.0f);
            }
            return;
        }
#endif
        m_audioSource.panStereo = 0.0f;
        m_audioSource.spatialBlend = data.m_is3D ? 1.0f : 0.0f;
        m_audioSource.volume = data.m_volume;
    }

    /// <summary>
    /// 選択中 AudioID から単体および I/L/O パートデータを解決します。
    /// </summary>
    private void ResolvePlaybackTargets(SoundDatabase database)
    {
        m_singleData = database.GetSoundData(m_audioId);
        m_introData = null;
        m_loopData = null;
        m_outroData = null;

        if (m_singleData == null)
        {
            return;
        }

        ParseSoundNameInternal(m_audioId.ToString(), out string baseName, out EmitterPartType partType);
        if (partType == EmitterPartType.PART_SINGLE)
        {
            return;
        }

        for (int i = 0; i < database.m_soundList.Count; i++)
        {
            SoundDatabase.SoundData item = database.m_soundList[i];
            if (item == null || item.m_id == AudioID.None)
            {
                continue;
            }

            ParseSoundNameInternal(item.m_id.ToString(), out string itemBase, out EmitterPartType itemPart);
            if (!string.Equals(baseName, itemBase, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (itemPart == EmitterPartType.PART_INTRO)
            {
                m_introData = item;
            }
            else if (itemPart == EmitterPartType.PART_LOOP)
            {
                m_loopData = item;
            }
            else if (itemPart == EmitterPartType.PART_OUTRO)
            {
                m_outroData = item;
            }
        }
    }

    /// <summary>
    /// AudioID 名からベース名とパート種別（_I / _L / _O）を解析します。
    /// </summary>
    private static void ParseSoundNameInternal(string idName, out string baseName, out EmitterPartType partType)
    {
        if (string.IsNullOrEmpty(idName) || idName.Length < MIN_SUFFIXED_LENGTH)
        {
            baseName = idName ?? "";
            partType = EmitterPartType.PART_SINGLE;
            return;
        }

        if (idName.EndsWith(SUFFIX_INTRO, StringComparison.OrdinalIgnoreCase))
        {
            baseName = idName.Substring(0, idName.Length - SUFFIX_LENGTH);
            partType = EmitterPartType.PART_INTRO;
            return;
        }

        if (idName.EndsWith(SUFFIX_LOOP, StringComparison.OrdinalIgnoreCase))
        {
            baseName = idName.Substring(0, idName.Length - SUFFIX_LENGTH);
            partType = EmitterPartType.PART_LOOP;
            return;
        }

        if (idName.EndsWith(SUFFIX_OUTRO, StringComparison.OrdinalIgnoreCase))
        {
            baseName = idName.Substring(0, idName.Length - SUFFIX_LENGTH);
            partType = EmitterPartType.PART_OUTRO;
            return;
        }

        baseName = idName;
        partType = EmitterPartType.PART_SINGLE;
    }

    /// <summary>
    /// SoundDatabase を取得します。未設定時は自動検索します。
    /// </summary>
    private SoundDatabase EnsureDatabase()
    {
        if (m_database != null)
        {
            return m_database;
        }

#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets(DATABASE_SEARCH_FILTER);
        if (guids.Length > 0)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            m_database = AssetDatabase.LoadAssetAtPath<SoundDatabase>(assetPath);
            if (m_database != null)
            {
                return m_database;
            }
        }
#endif
        m_database = Resources.Load<SoundDatabase>(DEFAULT_RESOURCE_PATH);
        return m_database;
    }

    /// <summary>
    /// 試聴用 AudioSource を準備します。
    /// </summary>
    private bool EnsureAudioSource()
    {
        if (m_audioSource == null)
        {
            m_audioSource = GetComponent<AudioSource>();
        }

        if (m_audioSource == null)
        {
            m_audioSource = gameObject.AddComponent<AudioSource>();
        }

        m_audioSource.playOnAwake = false;
        return m_audioSource != null;
    }

    /// <summary>
    /// データに有効な AudioClip があるかを確認します。
    /// </summary>
    private static bool HasClip(SoundDatabase.SoundData data)
    {
        return data != null && data.m_clip != null;
    }

    /// <summary>
    /// 現在の経過秒数を取得します。
    /// </summary>
    private static double GetCurrentTimeSeconds()
    {
#if UNITY_EDITOR
        return EditorApplication.timeSinceStartup;
#else
        return Time.realtimeSinceStartupAsDouble;
#endif
    }

    private void OnDrawGizmos()
    {
        SoundDatabase database = EnsureDatabase();
        SoundDatabase.SoundData data = database != null ? database.GetSoundData(m_audioId) : null;

        Color centerColor = IsPlaying() ? new Color(0.25f, 0.95f, 0.45f, 0.9f) : new Color(0.30f, 0.65f, 1.0f, 0.8f);
        Gizmos.color = centerColor;
        Gizmos.DrawSphere(transform.position, GIZMO_CENTER_RADIUS);

        if (data != null && data.m_is3D)
        {
            Gizmos.color = MIN_DISTANCE_GIZMO_COLOR;
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(MIN_DISTANCE_LIMIT, data.m_minDistance));
        }
    }

    private void OnDrawGizmosSelected()
    {
        SoundDatabase database = EnsureDatabase();
        SoundDatabase.SoundData data = database != null ? database.GetSoundData(m_audioId) : null;

        if (data == null || !data.m_is3D)
        {
            return;
        }

        float minDistance = Mathf.Max(MIN_DISTANCE_LIMIT, data.m_minDistance);
        float maxDistance = Mathf.Max(minDistance + DISTANCE_MARGIN, data.m_maxDistance);

        Gizmos.color = MAX_DISTANCE_GIZMO_COLOR;
        Gizmos.DrawWireSphere(transform.position, maxDistance);

        if (TryGetListenerVectors(out Vector3 listenerPos, out Vector3 _))
        {
            Gizmos.color = LISTENER_LINE_COLOR;
            Gizmos.DrawLine(transform.position, listenerPos);
        }
    }
}