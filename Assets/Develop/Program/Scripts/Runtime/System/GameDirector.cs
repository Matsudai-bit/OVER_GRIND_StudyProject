using System.Collections;
using UnityEngine;

/// <summary>
/// ゲーム全体の進行を統括します。
/// </summary>
[DisallowMultipleComponent]
public sealed class GameDirector : MonoBehaviour
{
    [SerializeField, Header("BGMPlayer")]
    private BGMPlayer m_bgmPlayer;
    // セーブデータ管理
    [SerializeField, Header("リザルト")]
    private SaveDataController m_saveDataController;

    // リザルトタイム
    [SerializeField]
    private ResultTimeModel m_resultTimeModel;

    // リザルトランク
    [SerializeField]
    private ResultRankModel m_resultRankModel;

    // リザルトリトライ回数
    [SerializeField]
    private ResultRetryModel m_resultRetryModel;

    // リザルトステージ情報
    [SerializeField]
    private ResultStageModel m_resultStageModel;

    // ゲーム経過時間Timer
    [SerializeField, Header("ゲーム進行")]
    private ElapsedTimer m_gameProgressElapsedTimer;

    // ステージ制限時間管理
    [SerializeField]
    private StageTimeLimitController m_stageTimeLimitController;

    // 現在のステージ固有設定
    [SerializeField]
    private StageSettingsAsset m_stageSettings;

    // ゲーム進行状態管理
    [SerializeField]
    private GameFlowController m_gameFlowController;

    // ゲームプレイモード管理
    [SerializeField]
    private GameplayModeController m_gameplayModeController;

    // シーケンス管理
    [SerializeField]
    private GameplaySequenceController m_sequenceController;

    // イントロシーケンス
    [SerializeField, Header("イントロシーケンス_ボス")]
    private GameplaySequenceAsset m_introSequenceAsset_BossSpawn;

    [SerializeField, Header("イントロシーケンス_プレイヤー")]
    private GameplaySequenceAsset m_introSequenceAsset_PlayerSpawn;

    // ゲーム進行状態変更イベント
    [SerializeField, Header("イベント")]
    private GameFlowStateChangedEvent m_gameFlowStateChangedEvent;

    // シーケンス状態変更イベント
    [SerializeField]
    private GameplaySequenceChangedEvent m_gameplaySequenceChangedEvent;

    // プレイヤーHP
    [SerializeField, Header("ゲームオブジェクト")]
    private Health m_playerHealth;

    // ボスフェーズ管理
    [SerializeField]
    private BossPhaseController m_bossPhaseController;

    // ステージシーン開始Context
    [SerializeField, Header("チェックポイント")]
    private StageSceneContext m_stageSceneContext;

    // チェックポイント管理
    [SerializeField]
    private StageCheckpointController m_stageCheckpointController;

    // ステージScene遷移管理
    [SerializeField, Header("遷移関連")]
    private StageSceneTransitionController
        m_stageSceneTransitionController;

    // 次のステージ定義
    [SerializeField]
    private StageDefinitionAsset m_nextStageDefinition;

    // ポーズ画面表示用データ
    [SerializeField, Header("ポーズ")]
    private PauseStatusModel m_pauseStatusModel;

    // ポーズ状態変更イベント
    [SerializeField]
    private PauseChangedEvent m_pauseChangedEvent;

    private void Awake()
    {
        Application.targetFrameRate = 60;

        ConfigureStage();
    }

    private void OnEnable()
    {
        RegisterEvents();
    }

    private void Start()
    {
        // GameFlowの初期化はGameFlowController側で行う
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        UnregisterEvents();
    }

    /// <summary>
    /// ステージ固有設定をRuntimeシステムへ適用します。
    /// </summary>
    private void ConfigureStage()
    {
        if (m_stageSettings == null)
        {
            Debug.LogError(
                $"{nameof(StageSettingsAsset)} が設定されていません。",
                this);

            return;
        }

        if (m_stageTimeLimitController == null)
        {
            Debug.LogWarning(
                $"{nameof(StageTimeLimitController)} が設定されていません。",
                this);

            return;
        }

        if (!m_stageTimeLimitController.Configure(
            m_stageSettings.TimeLimitSeconds))
        {
            Debug.LogError(
                "ステージ制限時間の設定に失敗しました。",
                this);
        }
    }

    /// <summary>
    /// 必要なイベントを登録します。
    /// </summary>
    private void RegisterEvents()
    {
        if (m_gameFlowStateChangedEvent != null)
        {
            m_gameFlowStateChangedEvent.RegisterListener(
                OnGameFlowStateChanged);
        }
        else
        {
            Debug.LogWarning(
                $"{nameof(GameFlowStateChangedEvent)} が設定されていません。",
                this);
        }

        if (m_gameplaySequenceChangedEvent != null)
        {
            m_gameplaySequenceChangedEvent.RegisterListener(
                OnGameplaySequenceChanged);
        }
        else
        {
            Debug.LogWarning(
                $"{nameof(GameplaySequenceChangedEvent)} が設定されていません。",
                this);
        }

        if (m_playerHealth != null)
        {
            m_playerHealth.HealthChanged +=
                OnPlayerHealthChanged;
        }
        else
        {
            Debug.LogWarning(
                $"{nameof(Health)} が設定されていません。",
                this);
        }

        if (m_stageTimeLimitController != null)
        {
            m_stageTimeLimitController.TimeExpired +=
                OnStageTimeExpired;
        }

        if (m_bossPhaseController != null)
        {
            m_bossPhaseController.PhaseCompletionRequested +=
                OnBossPhaseCompletionRequested;

            m_bossPhaseController.PhaseChanged +=
                OnBossPhaseChanged;
        }
        else
        {
            Debug.LogWarning(
                $"{nameof(BossPhaseController)} が設定されていません。",
                this);
        }

        if (m_pauseChangedEvent != null)
        {
            m_pauseChangedEvent.RegisterListener(
                OnPauseChanged);
        }
        else
        {
            Debug.LogWarning(
                $"{nameof(PauseChangedEvent)} が設定されていません。",
                this);
        }
    }

    /// <summary>
    /// 登録したイベントを解除します。
    /// </summary>
    private void UnregisterEvents()
    {
        if (m_gameFlowStateChangedEvent != null)
        {
            m_gameFlowStateChangedEvent.UnregisterListener(
                OnGameFlowStateChanged);
        }

        if (m_gameplaySequenceChangedEvent != null)
        {
            m_gameplaySequenceChangedEvent.UnregisterListener(
                OnGameplaySequenceChanged);
        }

        if (m_playerHealth != null)
        {
            m_playerHealth.HealthChanged -=
                OnPlayerHealthChanged;
        }

        if (m_stageTimeLimitController != null)
        {
            m_stageTimeLimitController.TimeExpired -=
                OnStageTimeExpired;
        }

        if (m_bossPhaseController != null)
        {
            m_bossPhaseController.PhaseCompletionRequested -=
                OnBossPhaseCompletionRequested;

            m_bossPhaseController.PhaseChanged -=
                OnBossPhaseChanged;
        }

        if (m_pauseChangedEvent != null)
        {
            m_pauseChangedEvent.UnregisterListener(
                OnPauseChanged);
        }
    }
    private void OnGameFlowStateChanged(
    GameFlowStateChangedEventData eventData)
    {
        switch (eventData.CurrentState)
        {
            case GameFlowStateType.INTRO:
                StopGameProgressTimer();
                HandleStageEntry();
                break;

            case GameFlowStateType.PLAYING:
                HandlePlayingStarted();
                break;

            case GameFlowStateType.ENDING:
                StopGameProgressTimer();
                break;

            case GameFlowStateType.RESULT:
                HandleResultStarted();
                break;

            case GameFlowStateType.GAME_OVER:
                StopGameProgressTimer();
                break;
        }
    }

    /// <summary>
    /// ステージSceneの開始方法に応じて初期処理を行います。
    /// </summary>
    private void HandleStageEntry()
    {
        StageSceneRequest request =
            m_stageSceneContext != null
                ? m_stageSceneContext.ConsumeRequest()
                : StageSceneRequest.NewGame;

        ApplyCarriedElapsedTime(
            request.CarriedElapsedSeconds);

        switch (request.EntryType)
        {
            case StageEntryType.NEW_GAME:
                StartIntroSequence();
                break;

            case StageEntryType.RESTART:
                StartRestart();
                break;

            case StageEntryType.CHECKPOINT:
                StartFromCheckpoint(
                    request.CheckpointSnapshot);
                break;
        }
    }

    /// <summary>
    /// Scene遷移Contextから累計経過時間を復元します。
    /// </summary>
    /// <param name="elapsedSeconds">
    /// 復元する累計経過時間。
    /// </param>
    private void ApplyCarriedElapsedTime(
        double elapsedSeconds)
    {
        if (m_gameProgressElapsedTimer == null)
        {
            return;
        }

        m_gameProgressElapsedTimer.SetElapsedTime(
            elapsedSeconds);
    }

    /// <summary>
    /// ステージ最初からのリスタートを開始します。
    /// </summary>
    private void StartRestart()
    {
        StartCoroutine(
            StartPlayingDeferred());
    }

    /// <summary>
    /// チェックポイント状態を復元してゲームプレイを開始します。
    /// </summary>
    /// <param name="snapshot">
    /// 復元するチェックポイント。
    /// </param>
    private void StartFromCheckpoint(
        StageCheckpointSnapshot snapshot)
    {
        if (m_stageCheckpointController == null
            || snapshot == null)
        {
            Debug.LogWarning(
                "チェックポイントを復元できないため、"
                + "ステージ最初から開始します。",
                this);

            StartRestart();

            return;
        }

        if (!m_stageCheckpointController.RestoreCheckpoint(
            snapshot))
        {
            Debug.LogWarning(
                "一部のチェックポイント状態を復元できませんでした。",
                this);
        }

        StartCoroutine(
            StartPlayingDeferred());
    }

    /// <summary>
    /// プレイヤー登場シーケンスを開始します。
    /// </summary>
    private void StartIntroSequence()
    {
        if (m_sequenceController == null)
        {
            Debug.LogWarning(
                $"{nameof(GameplaySequenceController)} が設定されていません。",
                this);

            return;
        }

        if (m_introSequenceAsset_PlayerSpawn == null)
        {
            Debug.LogWarning(
                "プレイヤー登場シーケンスが設定されていません。",
                this);

            return;
        }

        if (!m_sequenceController.StartSequence(
            m_introSequenceAsset_PlayerSpawn))
        {
            Debug.LogWarning(
                $"{m_introSequenceAsset_PlayerSpawn.name} "
                + "の開始に失敗しました。",
                this);
        }
    }

    /// <summary>
    /// ボス登場シーケンスを開始します。
    /// </summary>
    private void StartIntroBossSequence()
    {
        if (m_sequenceController == null)
        {
            Debug.LogWarning(
                $"{nameof(GameplaySequenceController)} が設定されていません。",
                this);

            return;
        }

        if (m_introSequenceAsset_BossSpawn == null)
        {
            Debug.LogWarning(
                "ボス登場シーケンスが設定されていません。",
                this);

            return;
        }

        if (!m_sequenceController.StartSequence(
            m_introSequenceAsset_BossSpawn))
        {
            Debug.LogWarning(
                $"{m_introSequenceAsset_BossSpawn.name} "
                + "の開始に失敗しました。",
                this);
        }
    }

    /// <summary>
    /// シーケンス状態変更を受け取ります。
    /// </summary>
    /// <param name="eventData">シーケンス状態変更情報。</param>
    private void OnGameplaySequenceChanged(
      GameplaySequenceChangedEventData eventData)
    {
        switch (eventData.EventType)
        {
            case GameplaySequenceEventType.STARTED:
                StopGameProgressTimer();
                break;

            case GameplaySequenceEventType.FINISHED:
            case GameplaySequenceEventType.CANCELED:
                ResumeGameProgressTimerIfPlaying();
                break;
        }

        // 以下、既存のイントロ固有処理
        if (eventData.Sequence ==
            m_introSequenceAsset_PlayerSpawn)
        {
            if (eventData.EventType ==
                GameplaySequenceEventType.FINISHED)
            {
                StartIntroBossSequence();
            }
        }

        if (eventData.Sequence !=
            m_introSequenceAsset_BossSpawn)
        {
            return;
        }

        if (eventData.EventType !=
            GameplaySequenceEventType.FINISHED)
        {
            return;
        }

        // 初回プレイ開始時のフェーズをチェックポイントとして記録
        m_stageCheckpointController?
            .CaptureCurrentPhaseCheckpoint();

        StartPlaying();
    }
    /// <summary>
    /// 通常ゲームプレイ中であればゲーム進行Timerを再開します。
    /// </summary>
    private void ResumeGameProgressTimerIfPlaying()
    {
        if (m_gameFlowController == null)
        {
            return;
        }

        if (m_gameFlowController.CurrentState !=
            GameFlowStateType.PLAYING)
        {
            return;
        }

        /*
         * 別Sequenceが続けて開始されている場合は
         * Timerを再開しません。
         */
        if (m_sequenceController != null
            && m_sequenceController.IsRunning)
        {
            return;
        }

        StartGameProgressTimer();
    }
    /// <summary>
    /// 現在のGameFlow状態変更イベントの通知完了後に
    /// 通常ゲームプレイ状態へ遷移します。
    /// </summary>
    /// <returns>次フレームまで待機するCoroutine。</returns>
    private IEnumerator StartPlayingDeferred()
    {
        /*
         * INTROのGameFlowStateChangedEvent通知中に
         * PLAYINGへ再遷移すると、イベント通知が入れ子になります。
         *
         * PLAYING通知後に古いINTRO通知を受け取るリスナーが存在すると、
         * UIや入力がINTRO状態へ上書きされる可能性があるため、
         * 次フレームへ遅延させてからPLAYINGへ遷移します。
         */
        yield return null;

        StartPlaying();
    }

    /// <summary>
    /// 通常ゲームプレイ状態へ遷移します。
    /// </summary>
    private void StartPlaying()
    {
        if (m_gameFlowController == null)
        {
            Debug.LogWarning(
                $"{nameof(GameFlowController)} が設定されていません。",
                this);

            return;
        }
        m_gameFlowController.EnterPlaying();
    }

    /// <summary>
    /// ボスフェーズ変更を受け取り、
    /// 新しいフェーズをチェックポイントとして記録します。
    /// </summary>
    /// <param name="phaseID">変更後のボスフェーズ。</param>
    private void OnBossPhaseChanged(
        BossPhaseID phaseID)
    {
        if (m_stageCheckpointController == null)
        {
            return;
        }

        /*
         * RestoreCheckpoint() 内の SetPhase() でも
         * PhaseChanged は通知されます。
         *
         * 復元中にチェックポイントを上書きしないよう、
         * StageCheckpointController の復元状態を確認します。
         */
        if (m_stageCheckpointController.IsRestoring)
        {
            return;
        }

        if (!m_stageCheckpointController
            .CaptureCurrentPhaseCheckpoint())
        {
            Debug.LogWarning(
                $"{phaseID} のチェックポイント記録に失敗しました。",
                this);
        }
    }

    /// <summary>
    /// ボスフェーズ完了要求を受け取ります。
    /// </summary>
    /// <param name="phaseID">完了したボスフェーズ。</param>
    private void OnBossPhaseCompletionRequested(
        BossPhaseID phaseID)
    {
        if (phaseID != BossPhaseID.PHASE_3)
        {
            return;
        }

        if (m_gameFlowController == null)
        {
            return;
        }

        m_gameFlowController.ChangeState(
            GameFlowStateType.ENDING);

        m_gameFlowController.ChangeState(
            GameFlowStateType.RESULT);
    }

    /// <summary>
    /// プレイヤーHP変更を受け取ります。
    /// </summary>
    /// <param name="currentHealth">現在のHP。</param>
    /// <param name="previousHealth">変更前のHP。</param>
    private void OnPlayerHealthChanged(
        int currentHealth,
        int previousHealth)
    {
        if (currentHealth > 0)
        {
            return;
        }

        if (m_gameFlowController == null)
        {
            return;
        }

        m_gameFlowController.ChangeState(
            GameFlowStateType.GAME_OVER);
    }

    /// <summary>
    /// ステージ制限時間切れを処理します。
    /// </summary>
    private void OnStageTimeExpired()
    {
        if (m_gameFlowController == null)
        {
            return;
        }

        m_gameFlowController.ChangeState(
            GameFlowStateType.GAME_OVER);
    }

    /// <summary>
    /// 現在の累計経過時間を取得します。
    /// </summary>
    /// <returns>累計経過時間。</returns>
    private double GetCurrentElapsedSeconds()
    {
        return m_gameProgressElapsedTimer != null
            ? m_gameProgressElapsedTimer.ElapsedSeconds
            : 0.0;
    }

    /// <summary>
    /// 現在のステージ番号を取得します。
    /// </summary>
    /// <returns>現在のステージ番号。</returns>
    private int GetCurrentStageNumber()
    {
        return m_stageSceneContext != null
            ? m_stageSceneContext.StageNumber
            : 0;
    }
    /// <summary>
    /// ゲーム進行時間の計測を開始します。
    /// </summary>
    private void StartGameProgressTimer()
    {
        if (m_gameProgressElapsedTimer == null)
        {
            return;
        }

        m_gameProgressElapsedTimer.StartTimer();
    }
    /// <summary>
    /// PLAYING状態へ入った際の通常ゲーム開始処理を行います。
    /// </summary>
    private void HandlePlayingStarted()
    {
        if (m_gameplayModeController != null)
        {
            m_gameplayModeController.ActivateGameplay();
        }

        /*
         * PLAYINGへ入った時点でSequenceが実行中なら、
         * Sequence終了までゲーム時間は進めません。
         */
        if (m_sequenceController != null
            && m_sequenceController.IsRunning)
        {
            StopGameProgressTimer();

            return;
        }
        m_bgmPlayer.Play();

        StartGameProgressTimer();
    }
    /// <summary>
    /// リザルト開始時の結果確定とセーブ処理を行います。
    /// </summary>
    private void HandleResultStarted()
    {
        StopGameProgressTimer();

        if (m_saveDataController == null
            || m_resultTimeModel == null
            || m_resultRankModel == null
            || m_stageSettings == null
            || m_gameProgressElapsedTimer == null)
        {
            Debug.LogError(
                "リザルト確定に必要な参照が不足しています。",
                this);

            return;
        }

        int stageNumber =
            GetCurrentStageNumber();

        if (stageNumber <= 0)
        {
            Debug.LogError(
                "ステージ番号がContextへ設定されていません。",
                this);

            return;
        }

        /*
         * SaveDataを書き換える前に、
         * 更新前の自己ベストをResultTimeModelへ読み込む。
         */
        if (!m_resultTimeModel.SetTimeData(
            m_saveDataController.CurrentData,
            m_gameProgressElapsedTimer,
            stageNumber))
        {
            Debug.LogError(
                "リザルトタイムの設定に失敗しました。",
                this);

            return;
        }

        // ランク判定はStageSettingsAssetへ委譲する
        if (!m_resultRankModel.EvaluateRank(
            m_resultTimeModel,
            m_stageSettings))
        {
            Debug.LogError(
                "リザルトランクの設定に失敗しました。",
                this);

            return;
        }

        ApplyResultContextData();

        SaveResultData();
    }

    /// <summary>
    /// 現在のリザルトデータをセーブします。
    /// </summary>
    /// <returns>
    /// true：セーブに成功しました。
    /// false：必要な情報が不足しているか、セーブに失敗しました。
    /// </returns>
    public bool SaveResultData()
    {
        if (m_saveDataController == null
            || m_resultTimeModel == null
            || m_stageSettings == null)
        {
            Debug.LogError(
                "リザルトデータの保存に必要な参照が不足しています。",
                this);

            return false;
        }

        int stageNumber =
            GetCurrentStageNumber();

        if (stageNumber <= 0)
        {
            Debug.LogError(
                "ステージ番号がContextへ設定されていません。",
                this);

            return false;
        }

        StageRank stageRank =
            m_stageSettings.EvaluateRank(
                m_resultTimeModel.ClearTime);

        if (stageRank == StageRank.NONE)
        {
            Debug.LogWarning(
                "保存可能なステージランクを取得できませんでした。",
                this);

            return false;
        }

        int clearTimeSeconds =
            Mathf.FloorToInt(
                m_resultTimeModel.ClearTime);

        if (!m_saveDataController.SaveStageResult(
            stageNumber,
            (int)stageRank,
            clearTimeSeconds))
        {
            Debug.LogError(
                "ステージリザルトの保存に失敗しました。",
                this);

            return false;
        }

        return true;
    }

    /// <summary>
    /// UnityEventからリザルトデータをセーブします。
    /// </summary>
    public void SaveResultDataFromEvent()
    {
        SaveResultData();
    }

    /// <summary>
    /// StageSceneContextの情報をリザルト表示Modelへ反映します。
    /// </summary>
    private void ApplyResultContextData()
    {
        if (m_stageSceneContext == null)
        {
            return;
        }

        if (m_resultRetryModel != null)
        {
            m_resultRetryModel.SetRetryCount(
                m_stageSceneContext);
        }

        if (m_resultStageModel != null)
        {
            m_resultStageModel.SetStageData(
                m_stageSceneContext);
        }
    }

    /// <summary>
    /// ポーズ状態変更を受け取ります。
    /// ポーズ開始時に表示用データを更新します。
    /// </summary>
    /// <param name="eventData">ポーズ状態変更情報。</param>
    private void OnPauseChanged(
        PauseChangedEventData eventData)
    {
        if (!eventData.IsPaused)
        {
            return;
        }

        ApplyPauseStatusData();
    }

    /// <summary>
    /// 現在の残り時間とリトライ回数を
    /// ポーズ画面表示用Modelへ反映します。
    /// </summary>
    public void ApplyPauseStatusData()
    {
        if (m_pauseStatusModel == null)
        {
            Debug.LogWarning(
                $"{nameof(PauseStatusModel)} が設定されていません。",
                this);

            return;
        }

        double remainingSeconds =
            m_stageTimeLimitController != null
                ? m_stageTimeLimitController.RemainingSeconds
                : 0.0;

        int retryCount =
            m_stageSceneContext != null
                ? m_stageSceneContext.RetryCount
                : 0;

        m_pauseStatusModel.SetStatus(
            remainingSeconds,
            retryCount);
    }

    /// <summary>
    /// ゲーム経過時間の計測を停止します。
    /// </summary>
    private void StopGameProgressTimer()
    {
        if (m_gameProgressElapsedTimer == null)
        {
            return;
        }

        m_gameProgressElapsedTimer.StopTimer();
    }

    /// <summary>
    /// ステージ最初からリスタートするContextを設定します。
    /// Scene再読み込みは別途ReloadInGameScene()で行います。
    /// </summary>
    public void RestartInGame()
    {
        if (m_stageSceneContext == null)
        {
            Debug.LogWarning(
                $"{nameof(StageSceneContext)} が設定されていません。",
                this);

            return;
        }

        m_stageSceneContext.SetRestart();
    }

    /// <summary>
    /// 最新のチェックポイントから再開するContextを設定します。
    /// Scene再読み込みは別途ReloadInGameScene()で行います。
    /// </summary>
    public void RestartFromCheckpoint()
    {
        if (m_stageSceneContext == null)
        {
            Debug.LogWarning(
                $"{nameof(StageSceneContext)} が設定されていません。",
                this);

            return;
        }

        if (m_stageCheckpointController == null)
        {
            Debug.LogWarning(
                $"{nameof(StageCheckpointController)} が設定されていません。",
                this);

            return;
        }

        if (!m_stageCheckpointController.TryGetCurrentSnapshot(
            out StageCheckpointSnapshot snapshot))
        {
            Debug.LogWarning(
                "再開できるチェックポイントがありません。",
                this);

            return;
        }

        if (!m_stageSceneContext.SetCheckpoint(
            snapshot,
            GetCurrentElapsedSeconds()))
        {
            Debug.LogWarning(
                "チェックポイントContextの設定に失敗しました。",
                this);
        }
    }

    /// <summary>
    /// 現在のインゲームSceneを再読み込みします。
    /// </summary>
    public void ReloadInGameScene()
    {
        if (m_stageSceneTransitionController == null)
        {
            Debug.LogWarning(
                $"{nameof(StageSceneTransitionController)} "
                + "が設定されていません。",
                this);

            return;
        }

        m_stageSceneTransitionController
            .ReloadCurrentStage();
    }

    /// <summary>
    /// 次のステージへ遷移します。
    /// </summary>
    public void LoadNextStage()
    {
        if (m_stageSceneTransitionController == null)
        {
            Debug.LogWarning(
                $"{nameof(StageSceneTransitionController)} "
                + "が設定されていません。",
                this);

            return;
        }

        if (m_nextStageDefinition == null)
        {
            Debug.LogWarning(
                "次のステージ定義が設定されていません。",
                this);

            return;
        }
        m_bgmPlayer.Stop();

        m_stageSceneTransitionController.LoadNewGame(
            m_nextStageDefinition);
    }

    /// <summary>
    /// ステージ選択画面へ遷移します。
    /// </summary>
    public void LoadStageSelect()
    {
        if (m_stageSceneTransitionController == null)
        {
            Debug.LogWarning(
                $"{nameof(StageSceneTransitionController)} "
                + "が設定されていません。",
                this);

            return;
        }
        m_bgmPlayer.Stop();
        m_stageSceneTransitionController
            .LoadStageSelect();
    }

    /// <summary>
    /// 現在のステージを最初からリスタートするContextを設定します。
    /// </summary>
    public void LoadRestartScene()
    {
        m_bgmPlayer.Stop();

        RestartInGame();
    }
}
