using System;
using UnityEngine;

/// <summary>
/// 1回分のサウンド再生を担当する再生ノードです。
/// 毎フレームの更新は SoundHub がまとめて呼び出します。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public sealed class SoundVoice : MonoBehaviour
{
    // 3D 距離の下限値
    private const float MIN_DISTANCE_LIMIT = 0.01f;

    // 最大距離を最小距離より大きく保つための余白
    private const float DISTANCE_MARGIN = 0.01f;

    // 2D 再生時の空間ブレンド値
    private const float SPATIAL_BLEND_2D = 0.0f;

    // 3D 再生時の空間ブレンド値
    private const float SPATIAL_BLEND_3D = 1.0f;

    // ドップラー効果の強さ（追従移動によるピッチ変化を防ぐため無効）
    private const float DOPPLER_LEVEL = 0.0f;

    // 追従先が破棄されたときのフェードアウト秒数
    private const float TARGET_LOST_FADE_SECONDS = 0.15f;

    // この秒数以下のフェードは即時扱いにする
    private const float MIN_FADE_SECONDS = 0.001f;

    // ピッチの下限値
    private const float MIN_PITCH = 0.1f;

    // ピッチの上限値
    private const float MAX_PITCH = 3.0f;

    // 再生開始直後に終了判定を行わないフレーム数
    private const int START_GRACE_FRAMES = 2;

    // Unity の再生優先度（小さいほど優先される）
    private const int PRIORITY_BGM = 0;
    private const int PRIORITY_VOICE = 32;
    private const int PRIORITY_SE_SYSTEM = 64;
    private const int PRIORITY_SE_PLAYER = 96;
    private const int PRIORITY_SE_INGAME = 128;

    // 再生を行う AudioSource
    private AudioSource m_audioSource;

    // 再生中のサウンド設定（音量・ピッチの変更をリアルタイムに反映するため参照を保持）
    private SoundDatabase.SoundData m_data;

    // 再生終了時に呼ばれるコールバック
    private Action<SoundVoice> m_onFinished;

    // 追従先
    private Transform m_followTarget;

    // 追従中か
    private bool m_hasFollowTarget = false;

    // 再生中か（一時停止中も true）
    private bool m_isActive = false;

    // 一時停止中か
    private bool m_isPaused = false;

    // フェードアウトして停止する途中か
    private bool m_isStopping = false;

    // 破棄済みか
    private bool m_isDestroyed = false;

    // この再生に掛ける音量倍率
    private float m_volumeScale = 1.0f;

    // この再生に掛けるピッチ倍率
    private float m_pitchScale = 1.0f;

    // 現在のフェード値（0～1）
    private float m_fadeCurrent = 1.0f;

    // フェードの目標値（0～1）
    private float m_fadeTarget = 1.0f;

    // フェードの変化速度（1秒あたり）
    private float m_fadeSpeed = 0.0f;

    // 再生を開始したフレーム
    private int m_playStartFrame = 0;

    /// <summary>
    /// 割り当てられたサウンド ID を取得します。
    /// </summary>
    public AudioID SoundId { get; private set; }

    /// <summary>
    /// 割り当てられたカテゴリーを取得します。
    /// </summary>
    public SoundCategory Category { get; private set; }

    /// <summary>
    /// 個別に操作するためのハンドル ID を取得します。
    /// </summary>
    public int HandleId { get; private set; }

    /// <summary>
    /// 3D 再生中かを取得します。
    /// </summary>
    public bool Is3D { get; private set; }

    /// <summary>
    /// AudioSource に設定している最終音量を取得します。
    /// </summary>
    public float FinalVolume { get; private set; }

    /// <summary>
    /// 再生中か（一時停止中も含む）を取得します。
    /// </summary>
    public bool IsActive => m_isActive;

    /// <summary>
    /// 一時停止中かを取得します。
    /// </summary>
    public bool IsPaused => m_isPaused;

    /// <summary>
    /// フェードアウトして停止する途中かを取得します。
    /// </summary>
    public bool IsStopping => m_isStopping;

    /// <summary>
    /// 破棄済みかを取得します。
    /// </summary>
    public bool IsDestroyed => m_isDestroyed;

    private void OnDestroy()
    {
        // 破棄されたことを記録し、管理側へ終了を通知する
        m_isDestroyed = true;
        m_isActive = false;
        NotifyFinished();
    }

    /// <summary>
    /// 再生前の初期設定を行います。
    /// </summary>
    /// <param name="handleId">割り当てるハンドル ID。</param>
    /// <param name="data">再生するサウンドの設定。</param>
    /// <param name="is3D">3D で再生するか。</param>
    /// <param name="request">再生リクエスト。</param>
    /// <param name="onFinished">再生終了時に呼ばれるコールバック。</param>
    /// <returns>
    /// true：初期設定に成功しました。
    /// false：設定データまたは AudioSource が不正です。
    /// </returns>
    public bool Init(
        int handleId,
        SoundDatabase.SoundData data,
        bool is3D,
        SoundPlayRequest request,
        Action<SoundVoice> onFinished)
    {
        // 不正なデータでは初期化しない
        if (data == null || data.m_clip == null || !CacheAudioSource())
        {
            return false;
        }

        // 識別情報を設定する
        HandleId = handleId;
        SoundId = data.m_id;
        Category = data.m_category;
        Is3D = is3D;
        m_data = data;
        m_onFinished = onFinished;

        // 状態を初期化する
        m_isActive = false;
        m_isPaused = false;
        m_isStopping = false;
        m_volumeScale = Mathf.Clamp01(request.VolumeScale);
        m_pitchScale = Mathf.Max(MIN_PITCH, request.PitchScale);

        // 位置・追従・フェード・AudioSource の順に設定する
        SetupPosition(is3D, request);
        SetupFadeIn(request.FadeInSeconds);
        ApplyAudioSettings(data, is3D);

        return true;
    }

    /// <summary>
    /// 再生を開始します。
    /// </summary>
    /// <param name="categoryGain">カテゴリーとマスターを合わせた音量。</param>
    public void Play(float categoryGain)
    {
        // 再生できない状態なら終了扱いにして管理側へ返す
        if (m_audioSource == null || m_audioSource.clip == null)
        {
            Finish();
            return;
        }

        m_isActive = true;
        m_isPaused = false;
        m_playStartFrame = Time.frameCount;

        // 再生開始前に音量を確定させ、前回の音量での発音を防ぐ
        ApplyParameters(categoryGain);
        m_audioSource.Play();
    }

    /// <summary>
    /// 毎フレームの更新を行います（SoundHub から呼ばれます）。
    /// </summary>
    /// <param name="deltaTime">経過時間（秒）。</param>
    /// <param name="categoryGain">カテゴリーとマスターを合わせた音量。</param>
    public void Tick(float deltaTime, float categoryGain)
    {
        if (!m_isActive)
        {
            return;
        }

        // 追従先の位置へ移動する
        UpdateFollow();

        if (!m_isActive)
        {
            return;
        }

        // 一時停止中はフェードを止める
        if (!m_isPaused)
        {
            UpdateFade(deltaTime);

            if (!m_isActive)
            {
                return;
            }
        }

        // 音量・ピッチを最新の設定に合わせる
        ApplyParameters(categoryGain);

        // 一時停止中は終了判定をしない
        if (!m_isPaused)
        {
            CheckFinished();
        }
    }

    /// <summary>
    /// 再生を停止します。
    /// </summary>
    /// <param name="fadeOutSeconds">フェードアウト秒数。0 以下で即時停止。</param>
    public void Stop(float fadeOutSeconds = 0.0f)
    {
        if (!m_isActive)
        {
            return;
        }

        // 即時停止、または一時停止中（フェードが進まない）は直ちに終了する
        if (fadeOutSeconds <= MIN_FADE_SECONDS || m_isPaused)
        {
            Finish();
            return;
        }

        // 現在値から指定秒数で 0 になる速度を求める
        float newSpeed = m_fadeCurrent / fadeOutSeconds;

        // すでにフェードアウト中なら、より速い方を採用する
        m_fadeSpeed = m_isStopping ? Mathf.Max(m_fadeSpeed, newSpeed) : newSpeed;
        m_fadeTarget = 0.0f;
        m_isStopping = true;
    }

    /// <summary>
    /// 再生を一時停止します。
    /// </summary>
    public void Pause()
    {
        if (!m_isActive || m_isPaused)
        {
            return;
        }

        m_audioSource.Pause();
        m_isPaused = true;
    }

    /// <summary>
    /// 一時停止中の再生を再開します。
    /// </summary>
    public void Resume()
    {
        if (!m_isActive || !m_isPaused)
        {
            return;
        }

        m_audioSource.UnPause();
        m_isPaused = false;
    }

    /// <summary>
    /// この再生に掛ける音量倍率を変更します。
    /// </summary>
    /// <param name="volumeScale">音量倍率（0～1）。</param>
    public void SetVolumeScale(float volumeScale)
    {
        m_volumeScale = Mathf.Clamp01(volumeScale);
    }

    /// <summary>
    /// AudioSource を取得し、起動時再生を無効にします。
    /// </summary>
    /// <returns>
    /// true：AudioSource を利用できます。
    /// false：AudioSource が見つかりません。
    /// </returns>
    private bool CacheAudioSource()
    {
        if (m_audioSource == null)
        {
            m_audioSource = GetComponent<AudioSource>();

            if (m_audioSource != null)
            {
                m_audioSource.playOnAwake = false;
            }
        }

        return m_audioSource != null;
    }

    /// <summary>
    /// 再生位置と追従先を設定します。
    /// </summary>
    /// <param name="is3D">3D で再生するか。</param>
    /// <param name="request">再生リクエスト。</param>
    private void SetupPosition(bool is3D, SoundPlayRequest request)
    {
        m_followTarget = null;
        m_hasFollowTarget = false;

        // 初期位置を設定する
        if (request.HasPosition)
        {
            transform.position = request.Position;
        }
        else if (request.FollowTarget != null)
        {
            transform.position = request.FollowTarget.position;
        }

        // 追従は 3D のときだけ行う
        if (is3D && request.FollowTarget != null)
        {
            m_followTarget = request.FollowTarget;
            m_hasFollowTarget = true;
        }
    }

    /// <summary>
    /// フェードインの初期状態を設定します。
    /// </summary>
    /// <param name="fadeInSeconds">フェードイン秒数。</param>
    private void SetupFadeIn(float fadeInSeconds)
    {
        if (fadeInSeconds > MIN_FADE_SECONDS)
        {
            m_fadeCurrent = 0.0f;
            m_fadeTarget = 1.0f;
            m_fadeSpeed = 1.0f / fadeInSeconds;
            return;
        }

        m_fadeCurrent = 1.0f;
        m_fadeTarget = 1.0f;
        m_fadeSpeed = 0.0f;
    }

    /// <summary>
    /// サウンド設定を AudioSource へ反映します。
    /// </summary>
    /// <param name="data">反映するサウンド設定。</param>
    /// <param name="is3D">3D で再生するか。</param>
    private void ApplyAudioSettings(SoundDatabase.SoundData data, bool is3D)
    {
        // 基本設定
        m_audioSource.clip = data.m_clip;
        m_audioSource.loop = data.m_loop;
        m_audioSource.priority = GetPriority(data.m_category);
        m_audioSource.dopplerLevel = DOPPLER_LEVEL;

        // 2D / 3D の切り替え
        m_audioSource.spatialBlend = is3D ? SPATIAL_BLEND_3D : SPATIAL_BLEND_2D;

        // 3D のときだけ距離減衰を設定する
        if (is3D)
        {
            ApplyRolloffSettings(data);
        }
    }

    /// <summary>
    /// 3D 距離減衰の設定を AudioSource へ反映します。
    /// </summary>
    /// <param name="data">反映するサウンド設定。</param>
    private void ApplyRolloffSettings(SoundDatabase.SoundData data)
    {
        // 最小距離・最大距離の整合性を保つ
        float minDistance = Mathf.Max(MIN_DISTANCE_LIMIT, data.m_minDistance);
        float maxDistance = Mathf.Max(minDistance + DISTANCE_MARGIN, data.m_maxDistance);

        m_audioSource.minDistance = minDistance;
        m_audioSource.maxDistance = maxDistance;
        m_audioSource.rolloffMode = data.m_rolloffMode;

        // カスタムカーブのときだけカーブを設定する
        if (data.m_rolloffMode == AudioRolloffMode.Custom && data.m_customRolloffCurve != null)
        {
            m_audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, data.m_customRolloffCurve);
        }
    }

    /// <summary>
    /// カテゴリーに応じた再生優先度を取得します。
    /// </summary>
    /// <param name="category">サウンドのカテゴリー。</param>
    /// <returns>再生優先度（小さいほど優先）。</returns>
    private static int GetPriority(SoundCategory category)
    {
        switch (category)
        {
            case SoundCategory.BGM:
                return PRIORITY_BGM;
            case SoundCategory.Voice:
                return PRIORITY_VOICE;
            case SoundCategory.SE_System:
                return PRIORITY_SE_SYSTEM;
            case SoundCategory.SE_InGame_Player:
                return PRIORITY_SE_PLAYER;
            default:
                return PRIORITY_SE_INGAME;
        }
    }

    /// <summary>
    /// 追従先の位置へ移動します。追従先が破棄された場合の処理も行います。
    /// </summary>
    private void UpdateFollow()
    {
        if (!m_hasFollowTarget)
        {
            return;
        }

        // 追従先が生きていれば位置を合わせる
        if (m_followTarget != null)
        {
            transform.position = m_followTarget.position;
            return;
        }

        // 追従先が破棄された。ループ音は止め、単発音は最後の位置で鳴らし切る
        m_hasFollowTarget = false;

        if (m_audioSource.loop)
        {
            Stop(TARGET_LOST_FADE_SECONDS);
        }
    }

    /// <summary>
    /// フェードを進め、フェードアウトが完了したら停止します。
    /// </summary>
    /// <param name="deltaTime">経過時間（秒）。</param>
    private void UpdateFade(float deltaTime)
    {
        // 目標値へ近づける
        if (!Mathf.Approximately(m_fadeCurrent, m_fadeTarget))
        {
            m_fadeCurrent = Mathf.MoveTowards(m_fadeCurrent, m_fadeTarget, m_fadeSpeed * deltaTime);
        }

        // フェードアウト完了で停止する
        if (m_isStopping && m_fadeCurrent <= 0.0f)
        {
            Finish();
        }
    }

    /// <summary>
    /// 音量とピッチを AudioSource へ反映します。
    /// </summary>
    /// <param name="categoryGain">カテゴリーとマスターを合わせた音量。</param>
    private void ApplyParameters(float categoryGain)
    {
        float baseVolume = m_data != null ? m_data.m_volume : 1.0f;
        float basePitch = m_data != null ? m_data.m_pitch : 1.0f;

        FinalVolume = Mathf.Clamp01(baseVolume * m_volumeScale * m_fadeCurrent * categoryGain);

        m_audioSource.volume = FinalVolume;
        m_audioSource.pitch = Mathf.Clamp(basePitch * m_pitchScale, MIN_PITCH, MAX_PITCH);
    }

    /// <summary>
    /// 再生が自然終了したかを確認します。
    /// </summary>
    private void CheckFinished()
    {
        // ループ再生は自然終了しない
        if (m_audioSource.loop)
        {
            return;
        }

        // 再生開始直後は判定しない
        if (Time.frameCount - m_playStartFrame <= START_GRACE_FRAMES)
        {
            return;
        }

        if (!m_audioSource.isPlaying)
        {
            Finish();
        }
    }

    /// <summary>
    /// 再生を終了し、管理側へ通知します。
    /// </summary>
    private void Finish()
    {
        if (m_audioSource != null)
        {
            m_audioSource.Stop();
        }

        m_isActive = false;
        m_isPaused = false;
        m_isStopping = false;
        m_hasFollowTarget = false;
        m_followTarget = null;

        NotifyFinished();
    }

    /// <summary>
    /// 終了を管理側へ通知します。二重通知は行いません。
    /// </summary>
    private void NotifyFinished()
    {
        if (m_onFinished == null)
        {
            return;
        }

        // コールバック内の再入を防ぐため、先に参照を外してから呼ぶ
        Action<SoundVoice> callback = m_onFinished;
        m_onFinished = null;
        callback.Invoke(this);
    }
}