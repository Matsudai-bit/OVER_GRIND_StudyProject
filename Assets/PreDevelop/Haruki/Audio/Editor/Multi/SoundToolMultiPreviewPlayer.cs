#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 2D 音源（BGM・プレイヤー音など）と 3D 音源（仮想距離・パン付き）を
/// 複数トラック同時に再生・ミキシング・解析する試聴プレイヤーです。
/// </summary>
public static class SoundToolMultiPreviewPlayer
{
    // 非表示オブジェクトのプレフィックス名
    private const string TRACK_OBJECT_PREFIX = "SoundToolMultiTrack_";

    // 最小距離の下限値
    private const float MIN_DISTANCE_LIMIT = 0.01f;

    // 最大距離の余白
    private const float DISTANCE_MARGIN = 0.01f;

    // リピート間隔の最小値（秒）
    private const float MIN_REPEAT_INTERVAL = 0.1f;

    // メーター減衰速度
    private const float TRACK_METER_FALL_SPEED = 2.6f;

    // 再生ステージ定数：停止
    public const int STAGE_STOPPED = 0;

    // 再生ステージ定数：単体
    public const int STAGE_SINGLE = 1;

    // 再生ステージ定数：Intro (_I)
    public const int STAGE_INTRO = 2;

    // 再生ステージ定数：Loop (_L)
    public const int STAGE_LOOP = 3;

    // 再生ステージ定数：Outro (_O)
    public const int STAGE_OUTRO = 4;

    /// <summary>
    /// 同時再生用の1トラック分の再生状態と空間シミュレーション設定です。
    /// </summary>
    public sealed class MixTrack
    {
        // トラックの対象 AudioID
        private AudioID m_audioId = AudioID.None;

        // ミュート中か
        private bool m_isMuted = false;

        // ソロ指定中か
        private bool m_isSolo = false;

        // I/L/O の場合に一連再生するか
        private bool m_playAsSequence = true;

        // ループ1周で自動 Outro 移行するか
        private bool m_autoOutro = false;

        // 単発音などを間隔リピート再生するか
        private bool m_autoRepeat = false;

        // リピート間隔（秒）
        private float m_repeatInterval = 1.5f;

        // 3D 音源の仮想リスナー距離（m）
        private float m_virtualDistance = 3.0f;

        // 3D 音源の仮想ステレオパン（-1:左 ～ +1:右）
        private float m_virtualPan = 0.0f;

        // トラック専用の非表示オブジェクト
        private GameObject m_hostObject;

        // トラック専用の AudioSource
        private AudioSource m_audioSource;

        // 現在の再生ステージ
        private int m_stage = STAGE_STOPPED;

        // リピート待機中か
        private bool m_isWaitingRepeat = false;

        // 次のリピート開始時刻
        private double m_nextRepeatTime = 0.0;

        // 単体データ
        private SoundDatabase.SoundData m_singleData;

        // 開始データ（_I）
        private SoundDatabase.SoundData m_introData;

        // ループデータ（_L）
        private SoundDatabase.SoundData m_loopData;

        // 終了データ（_O）
        private SoundDatabase.SoundData m_outroData;

        // トラック個別の正規化レベルメーター値（0～1）
        private float m_meterLevel = 0.0f;

        // 直近の 3D 減衰率（0～1）
        private float m_lastAttenuation = 1.0f;

        // 直近の実効出力音量
        private float m_lastEffectiveVolume = 0.0f;

        /// <summary>
        /// 対象の AudioID を取得または設定します。
        /// </summary>
        public AudioID AudioId
        {
            get => m_audioId;
            set => m_audioId = value;
        }

        /// <summary>
        /// ミュート状態を取得または設定します。
        /// </summary>
        public bool IsMuted
        {
            get => m_isMuted;
            set => m_isMuted = value;
        }

        /// <summary>
        /// ソロ状態を取得または設定します。
        /// </summary>
        public bool IsSolo
        {
            get => m_isSolo;
            set => m_isSolo = value;
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
        /// 自動 Outro 移行するかを取得または設定します。
        /// </summary>
        public bool AutoOutro
        {
            get => m_autoOutro;
            set => m_autoOutro = value;
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
        /// リピート間隔（秒）を取得または設定します。
        /// </summary>
        public float RepeatInterval
        {
            get => m_repeatInterval;
            set => m_repeatInterval = Mathf.Max(MIN_REPEAT_INTERVAL, value);
        }

        /// <summary>
        /// 3D 音源の仮想距離（m）を取得または設定します。
        /// </summary>
        public float VirtualDistance
        {
            get => m_virtualDistance;
            set => m_virtualDistance = Mathf.Max(0.0f, value);
        }

        /// <summary>
        /// 3D 音源の仮想パン（-1～+1）を取得または設定します。
        /// </summary>
        public float VirtualPan
        {
            get => m_virtualPan;
            set => m_virtualPan = Mathf.Clamp(value, -1.0f, 1.0f);
        }

        /// <summary>
        /// 現在の再生ステージを取得します。
        /// </summary>
        public int Stage => m_stage;

        /// <summary>
        /// トラック個別のレベルメーター値（0～1）を取得します。
        /// </summary>
        public float MeterLevel => m_meterLevel;

        /// <summary>
        /// 現在の 3D 距離減衰率（0～1）を取得します。
        /// </summary>
        public float LastAttenuation => m_lastAttenuation;

        /// <summary>
        /// 現在の実効出力音量を取得します。
        /// </summary>
        public float LastEffectiveVolume => m_lastEffectiveVolume;

        /// <summary>
        /// トラックが再生中（またはリピート待機中）かを確認します。
        /// </summary>
        /// <returns>
        /// true：再生中またはリピート待機中です。
        /// false：停止しています。
        /// </returns>
        public bool IsPlaying()
        {
            if (m_stage == STAGE_STOPPED)
            {
                return false;
            }

            return m_isWaitingRepeat || (m_audioSource != null && m_audioSource.isPlaying);
        }

        /// <summary>
        /// 現在アクティブな SoundData を取得します。
        /// </summary>
        /// <param name="database">参照する SoundDatabase。</param>
        /// <returns>アクティブな SoundData。</returns>
        public SoundDatabase.SoundData GetActiveData(SoundDatabase database)
        {
            switch (m_stage)
            {
                case STAGE_INTRO:
                    return m_introData;
                case STAGE_LOOP:
                    return m_loopData;
                case STAGE_OUTRO:
                    return m_outroData;
                default:
                    return database != null ? database.GetSoundData(m_audioId) : null;
            }
        }

        /// <summary>
        /// 内部 AudioSource を取得します。
        /// </summary>
        internal AudioSource GetAudioSource()
        {
            return m_audioSource;
        }

        /// <summary>
        /// このトラックの再生を開始します。
        /// </summary>
        internal void StartTrack(SoundDatabase database)
        {
            StopTrack();
            if (database == null || m_audioId == AudioID.None)
            {
                return;
            }

            ResolveTargets(database);

            if (m_playAsSequence && (m_introData != null || m_loopData != null || m_outroData != null))
            {
                if (HasClip(m_introData))
                {
                    StartStage(STAGE_INTRO, m_introData, false);
                }
                else if (HasClip(m_loopData))
                {
                    StartStage(STAGE_LOOP, m_loopData, !m_autoOutro);
                }
                else if (HasClip(m_outroData))
                {
                    StartStage(STAGE_OUTRO, m_outroData, false);
                }
            }
            else if (HasClip(m_singleData))
            {
                StartStage(STAGE_SINGLE, m_singleData, m_singleData.m_loop);
            }
        }

        /// <summary>
        /// Outro（_O）パートへ切り替えます。
        /// </summary>
        internal void TriggerTrackOutro()
        {
            if (m_stage == STAGE_STOPPED || !HasClip(m_outroData))
            {
                StopTrack();
                return;
            }

            StartStage(STAGE_OUTRO, m_outroData, false);
        }

        /// <summary>
        /// このトラックの再生を停止します。
        /// </summary>
        internal void StopTrack()
        {
            if (m_audioSource != null)
            {
                m_audioSource.Stop();
            }

            m_stage = STAGE_STOPPED;
            m_isWaitingRepeat = false;
            m_lastEffectiveVolume = 0.0f;
        }

        /// <summary>
        /// 毎フレームの音量同期・減衰適用・シーケンス遷移を行います。
        /// </summary>
        internal void UpdateTrack(SoundDatabase database, bool anySoloActive)
        {
            if (m_stage == STAGE_STOPPED)
            {
                m_lastEffectiveVolume = 0.0f;
                return;
            }

            SoundDatabase.SoundData activeData = GetActiveData(database);
            bool isAudible = !m_isMuted && (!anySoloActive || m_isSolo);

            if (activeData != null && m_audioSource != null)
            {
                m_lastAttenuation = EvaluateAttenuation(activeData, m_virtualDistance);
                float effectiveVol = isAudible ? (activeData.m_volume * m_lastAttenuation) : 0.0f;
                m_lastEffectiveVolume = effectiveVol;

                m_audioSource.volume = effectiveVol;
                m_audioSource.pitch = activeData.m_pitch;
                m_audioSource.panStereo = activeData.m_is3D ? m_virtualPan : 0.0f;
                m_audioSource.spatialBlend = 0.0f;
            }

            if (m_isWaitingRepeat)
            {
                if (EditorApplication.timeSinceStartup >= m_nextRepeatTime)
                {
                    m_isWaitingRepeat = false;
                    StartTrack(database);
                }
                return;
            }

            if (m_audioSource != null && m_audioSource.isPlaying)
            {
                return;
            }

            // 次パート遷移またはリピート処理
            switch (m_stage)
            {
                case STAGE_INTRO:
                    if (HasClip(m_loopData))
                    {
                        StartStage(STAGE_LOOP, m_loopData, !m_autoOutro);
                    }
                    else if (HasClip(m_outroData))
                    {
                        StartStage(STAGE_OUTRO, m_outroData, false);
                    }
                    else
                    {
                        FinishTrackCycle();
                    }
                    break;

                case STAGE_LOOP:
                    if (m_autoOutro && HasClip(m_outroData))
                    {
                        StartStage(STAGE_OUTRO, m_outroData, false);
                    }
                    else
                    {
                        FinishTrackCycle();
                    }
                    break;

                default:
                    FinishTrackCycle();
                    break;
            }
        }

        /// <summary>
        /// 解析結果のレベル値をスムーズに反映します。
        /// </summary>
        internal void UpdateMeterVisual(float targetNormalizedLevel, float deltaTime)
        {
            if (targetNormalizedLevel >= m_meterLevel)
            {
                m_meterLevel = targetNormalizedLevel;
            }
            else
            {
                m_meterLevel = Mathf.Max(targetNormalizedLevel, m_meterLevel - (TRACK_METER_FALL_SPEED * deltaTime));
            }
        }

        /// <summary>
        /// 非表示の再生オブジェクトを破棄します。
        /// </summary>
        internal void DisposeHost()
        {
            StopTrack();
            if (m_hostObject != null)
            {
                UnityEngine.Object.DestroyImmediate(m_hostObject);
            }
            m_hostObject = null;
            m_audioSource = null;
        }

        /// <summary>
        /// 1サイクル完了時のリピートまたは停止を行います。
        /// </summary>
        private void FinishTrackCycle()
        {
            if (m_autoRepeat)
            {
                m_isWaitingRepeat = true;
                m_nextRepeatTime = EditorApplication.timeSinceStartup + Mathf.Max(MIN_REPEAT_INTERVAL, m_repeatInterval);
                return;
            }

            StopTrack();
        }

        /// <summary>
        /// 指定ステージの再生を開始します。
        /// </summary>
        private void StartStage(int stage, SoundDatabase.SoundData data, bool loop)
        {
            if (!HasClip(data) || !EnsureSource())
            {
                StopTrack();
                return;
            }

            m_stage = stage;
            m_isWaitingRepeat = false;

            data.m_clip.LoadAudioData();
            m_audioSource.Stop();
            m_audioSource.clip = data.m_clip;
            m_audioSource.loop = loop;
            m_audioSource.pitch = data.m_pitch;
            m_audioSource.spatialBlend = 0.0f;
            m_audioSource.time = 0.0f;
            m_audioSource.Play();
        }

        /// <summary>
        /// データベースから単体および I/L/O パートデータを解決します。
        /// </summary>
        private void ResolveTargets(SoundDatabase database)
        {
            m_singleData = database.GetSoundData(m_audioId);
            m_introData = null;
            m_loopData = null;
            m_outroData = null;

            if (m_singleData == null)
            {
                return;
            }

            SoundToolSequenceHelper.ParseSoundName(m_audioId.ToString(), out string baseName, out SoundPartType partType);
            if (partType == SoundPartType.PART_SINGLE)
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

                SoundToolSequenceHelper.ParseSoundName(item.m_id.ToString(), out string itemBase, out SoundPartType itemPart);
                if (!string.Equals(baseName, itemBase, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (itemPart == SoundPartType.PART_INTRO)
                {
                    m_introData = item;
                }
                else if (itemPart == SoundPartType.PART_LOOP)
                {
                    m_loopData = item;
                }
                else if (itemPart == SoundPartType.PART_OUTRO)
                {
                    m_outroData = item;
                }
            }
        }

        /// <summary>
        /// 試聴用 AudioSource を用意します。
        /// </summary>
        private bool EnsureSource()
        {
            if (m_audioSource != null)
            {
                return true;
            }

            m_hostObject = EditorUtility.CreateGameObjectWithHideFlags(
                TRACK_OBJECT_PREFIX + m_audioId,
                HideFlags.HideAndDontSave,
                typeof(AudioSource));

            m_audioSource = m_hostObject.GetComponent<AudioSource>();
            if (m_audioSource == null)
            {
                return false;
            }

            m_audioSource.playOnAwake = false;
            return true;
        }

        /// <summary>
        /// クリップが有効かを確認します。
        /// </summary>
        private static bool HasClip(SoundDatabase.SoundData data)
        {
            return data != null && data.m_clip != null;
        }
    }

    // 登録されている同時再生トラック一覧
    private static readonly List<MixTrack> m_tracks = new List<MixTrack>();

    // アナライザーへ渡すワークリスト
    private static readonly List<SoundToolAudioAnalyzer.TrackAnalysisInput> m_analysisInputs = new List<SoundToolAudioAnalyzer.TrackAnalysisInput>();

    // 各トラックの解析結果ピーク受け取り用リスト
    private static readonly List<float> m_analysisTrackPeaks = new List<float>();

    // シーン上の SoundScenePreviewEmitter と同時再生・停止を連動するか
    private static bool m_syncSceneEmitters = true;

    // EditorApplication.update を購読中か
    private static bool m_isUpdating = false;

    // 前回の更新時刻
    private static double m_lastTime = 0.0;

    /// <summary>
    /// マルチトラック再生状態やメーターが更新された際に通知されます。
    /// </summary>
    public static event Action OnStateChanged;

    /// <summary>
    /// 登録されているトラック一覧を取得します。
    /// </summary>
    public static IReadOnlyList<MixTrack> Tracks => m_tracks;

    /// <summary>
    /// シーン上の 3D 試聴エミッターと同時再生・停止を連動するかを取得または設定します。
    /// </summary>
    public static bool SyncSceneEmitters
    {
        get => m_syncSceneEmitters;
        set => m_syncSceneEmitters = value;
    }

    static SoundToolMultiPreviewPlayer()
    {
        AssemblyReloadEvents.beforeAssemblyReload += DisposeAll;
    }

    /// <summary>
    /// 新しい試聴トラックを追加します。
    /// </summary>
    /// <param name="audioId">追加する AudioID。</param>
    /// <param name="database">対象の SoundDatabase。</param>
    /// <returns>作成したトラック。</returns>
    public static MixTrack AddTrack(AudioID audioId, SoundDatabase database)
    {
        MixTrack track = new MixTrack
        {
            AudioId = audioId
        };

        // 単発 SE の場合はテストしやすいよう初期状態で間隔リピートを ON にする
        SoundDatabase.SoundData data = database != null ? database.GetSoundData(audioId) : null;
        if (data != null)
        {
            SoundToolSequenceHelper.ParseSoundName(audioId.ToString(), out string _, out SoundPartType partType);
            bool isSingleNonLoop = partType == SoundPartType.PART_SINGLE && !data.m_loop && data.m_category != SoundCategory.BGM;
            track.AutoRepeat = isSingleNonLoop;

            if (data.m_is3D)
            {
                track.VirtualDistance = Mathf.Max(MIN_DISTANCE_LIMIT, data.m_minDistance);
            }
        }

        m_tracks.Add(track);
        OnStateChanged?.Invoke();
        return track;
    }

    /// <summary>
    /// 指定トラックを削除します。
    /// </summary>
    /// <param name="track">削除するトラック。</param>
    public static void RemoveTrack(MixTrack track)
    {
        if (track == null)
        {
            return;
        }

        track.DisposeHost();
        m_tracks.Remove(track);
        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// すべてのトラックをクリアします。
    /// </summary>
    public static void ClearAllTracks()
    {
        StopAll();
        for (int i = 0; i < m_tracks.Count; i++)
        {
            m_tracks[i].DisposeHost();
        }
        m_tracks.Clear();
        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// すべてのトラック（および連動 ON ならシーンエミッター）を同時に再生開始します。
    /// </summary>
    /// <param name="database">対象の SoundDatabase。</param>
    public static void PlayAll(SoundDatabase database)
    {
        SoundToolPreviewPlayer.Stop();
        SoundToolAudioAnalyzer.ResetSessionPeakAndClip();

        for (int i = 0; i < m_tracks.Count; i++)
        {
            m_tracks[i].StartTrack(database);
        }

        if (m_syncSceneEmitters)
        {
            SoundScenePreviewEmitter[] emitters = UnityEngine.Object.FindObjectsByType<SoundScenePreviewEmitter>(FindObjectsSortMode.None);
            for (int i = 0; i < emitters.Length; i++)
            {
                emitters[i].PlayPreview();
            }
        }

        StartUpdateLoop();
        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// 指定の1トラックを再生開始します（他トラックは止めずに同時再生します）。
    /// </summary>
    /// <param name="track">再生するトラック。</param>
    /// <param name="database">対象の SoundDatabase。</param>
    public static void PlayTrack(MixTrack track, SoundDatabase database)
    {
        if (track == null)
        {
            return;
        }

        SoundToolPreviewPlayer.Stop();
        track.StartTrack(database);
        StartUpdateLoop();
        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// 指定トラックを停止します。
    /// </summary>
    /// <param name="track">停止するトラック。</param>
    public static void StopTrack(MixTrack track)
    {
        if (track == null)
        {
            return;
        }

        track.StopTrack();
        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// 指定トラックの Outro へ移行します。
    /// </summary>
    /// <param name="track">対象のトラック。</param>
    public static void TriggerTrackOutro(MixTrack track)
    {
        if (track == null)
        {
            return;
        }

        track.TriggerTrackOutro();
        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// すべてのトラック（および連動 ON ならシーンエミッター）を停止します。
    /// </summary>
    public static void StopAll()
    {
        for (int i = 0; i < m_tracks.Count; i++)
        {
            m_tracks[i].StopTrack();
        }

        if (m_syncSceneEmitters)
        {
            SoundScenePreviewEmitter[] emitters = UnityEngine.Object.FindObjectsByType<SoundScenePreviewEmitter>(FindObjectsSortMode.None);
            for (int i = 0; i < emitters.Length; i++)
            {
                emitters[i].StopPreview();
            }
        }

        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// いずれかのトラックが再生中かを確認します。
    /// </summary>
    /// <returns>
    /// true：1つ以上のトラックが再生中です。
    /// false：すべて停止しています。
    /// </returns>
    public static bool HasAnyPlaying()
    {
        for (int i = 0; i < m_tracks.Count; i++)
        {
            if (m_tracks[i].IsPlaying())
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 指定された SoundData と仮想距離から 3D 減衰率（0～1）を算出します。
    /// </summary>
    /// <param name="data">サウンド設定。</param>
    /// <param name="distance">仮想距離（m）。</param>
    /// <returns>減衰率（0.0～1.0）。</returns>
    public static float EvaluateAttenuation(SoundDatabase.SoundData data, float distance)
    {
        if (data == null || !data.m_is3D)
        {
            return 1.0f;
        }

        float minDist = Mathf.Max(MIN_DISTANCE_LIMIT, data.m_minDistance);
        float maxDist = Mathf.Max(minDist + DISTANCE_MARGIN, data.m_maxDistance);

        switch (data.m_rolloffMode)
        {
            case AudioRolloffMode.Linear:
                if (distance <= minDist)
                {
                    return 1.0f;
                }
                if (distance >= maxDist)
                {
                    return 0.0f;
                }
                return 1.0f - ((distance - minDist) / (maxDist - minDist));

            case AudioRolloffMode.Custom:
                if (data.m_customRolloffCurve == null)
                {
                    return 1.0f;
                }
                return Mathf.Clamp01(data.m_customRolloffCurve.Evaluate(Mathf.Clamp01(distance / maxDist)));

            default:
                if (distance <= minDist)
                {
                    return 1.0f;
                }
                return Mathf.Clamp01(minDist / distance);
        }
    }

    /// <summary>
    /// すべてのトラックの状態更新とマスター出力の合成解析を行います。
    /// </summary>
    private static void UpdateAllTracks()
    {
        double now = EditorApplication.timeSinceStartup;
        float deltaTime = m_lastTime > 0.0 ? Mathf.Clamp((float)(now - m_lastTime), 0.0f, 0.1f) : 0.016f;
        m_lastTime = now;

        SoundDatabase database = SoundToolDatabaseProvider.GetDatabase();

        // いずれかのトラックがソロ指定されているか確認する
        bool anySoloActive = false;
        for (int i = 0; i < m_tracks.Count; i++)
        {
            if (m_tracks[i].IsSolo)
            {
                anySoloActive = true;
                break;
            }
        }

        m_analysisInputs.Clear();
        bool anyActive = false;

        for (int i = 0; i < m_tracks.Count; i++)
        {
            MixTrack track = m_tracks[i];
            track.UpdateTrack(database, anySoloActive);

            bool playing = track.IsPlaying();
            if (playing)
            {
                anyActive = true;
            }

            AudioSource src = track.GetAudioSource();
            m_analysisInputs.Add(new SoundToolAudioAnalyzer.TrackAnalysisInput
            {
                m_source = src,
                m_effectiveVolume = track.LastEffectiveVolume,
                m_panStereo = src != null ? src.panStereo : 0.0f,
                m_isActive = playing && track.LastEffectiveVolume > 0.0001f
            });
        }

        // シーン上のエミッターが鳴っている場合もマスター合成解析へ含める
        if (m_syncSceneEmitters)
        {
            SoundScenePreviewEmitter[] emitters = UnityEngine.Object.FindObjectsByType<SoundScenePreviewEmitter>(FindObjectsSortMode.None);
            for (int i = 0; i < emitters.Length; i++)
            {
                SoundScenePreviewEmitter emitter = emitters[i];
                if (!emitter.IsPlaying())
                {
                    continue;
                }

                AudioSource emitterSource = emitter.GetComponent<AudioSource>();
                if (emitterSource != null && emitterSource.isPlaying)
                {
                    anyActive = true;
                    m_analysisInputs.Add(new SoundToolAudioAnalyzer.TrackAnalysisInput
                    {
                        m_source = emitterSource,
                        m_effectiveVolume = emitterSource.volume,
                        m_panStereo = emitterSource.panStereo,
                        m_isActive = true
                    });
                }
            }
        }

        // マスター出力と各トラック個別メーターを同時に解析更新する
        SoundToolAudioAnalyzer.UpdateMultiTrackAnalysis(m_analysisInputs, m_analysisTrackPeaks);

        bool anyTrackMeterVisible = false;
        for (int i = 0; i < m_tracks.Count; i++)
        {
            float targetPeak = i < m_analysisTrackPeaks.Count ? m_analysisTrackPeaks[i] : 0.0f;
            m_tracks[i].UpdateMeterVisual(targetPeak, deltaTime);

            if (m_tracks[i].MeterLevel > 0.002f)
            {
                anyTrackMeterVisible = true;
            }
        }

        OnStateChanged?.Invoke();

        if (!anyActive && !anyTrackMeterVisible && !SoundToolAudioAnalyzer.HasVisualActivity())
        {
            StopUpdateLoop();
        }
    }

    /// <summary>
    /// EditorApplication.update 監視を開始します。
    /// </summary>
    private static void StartUpdateLoop()
    {
        if (m_isUpdating)
        {
            return;
        }

        m_isUpdating = true;
        m_lastTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += UpdateAllTracks;
    }

    /// <summary>
    /// EditorApplication.update 監視を停止します。
    /// </summary>
    private static void StopUpdateLoop()
    {
        if (!m_isUpdating)
        {
            return;
        }

        m_isUpdating = false;
        EditorApplication.update -= UpdateAllTracks;
    }

    /// <summary>
    /// スクリプトリロード時にすべての試聴用オブジェクトを破棄します。
    /// </summary>
    private static void DisposeAll()
    {
        StopUpdateLoop();
        for (int i = 0; i < m_tracks.Count; i++)
        {
            m_tracks[i].DisposeHost();
        }
        m_tracks.Clear();
    }
}

#endif