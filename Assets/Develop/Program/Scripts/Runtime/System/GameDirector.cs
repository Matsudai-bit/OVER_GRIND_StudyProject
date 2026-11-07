using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲーム全体の進行を統括します。
/// </summary>
[DisallowMultipleComponent]
public sealed class GameDirector : MonoBehaviour
{
    [SerializeField]
    private SaveDataController m_saveDataController;
    [SerializeField]
    private ResultTimeModel m_resultTimeModel;
    [SerializeField]
    private ResultRankModel m_resultRankModel;
    [SerializeField]
    private ResultRetryModel m_resultRetryModel;
    [SerializeField]
    private ResultStageModel m_resultStageModel;

    // ゲーム経過時間Timer
    [SerializeField, Header("ゲーム進行")]
    private ElapsedTimer m_gameProgressElapsedTimer;

    // ステージ制限時間管理
    [SerializeField]
    private StageTimeLimitController m_stageTimeLimitController;

    // ステージ固有設定
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

    // 次のステージシーン名
    [SerializeField, Header("遷移関連")]
    private string m_nextStageSceneName;

    // ステージ選択画面シーン名
    [SerializeField]
    private string m_stageSelectSceneName;

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
    /// ステージ固有設定を各Runtimeシステムへ適用します。
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
            Debug.LogError(
                $"{nameof(StageTimeLimitController)} が設定されていません。",
                this);

            return;
        }

        if (m_stageSceneContext != null)
        {
            m_stageSceneContext.SetStageName(
                m_stageSettings.StageName);
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
        }
        else
        {
            Debug.LogWarning(
                $"{nameof(BossPhaseController)} が設定されていません。",
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
        }
    }

    /// <summary>
    /// ゲーム進行状態変更を受け取ります。
    /// </summary>
    /// <param name="eventData">ゲーム進行状態変更情報。</param>
    private void OnGameFlowStateChanged(
        GameFlowStateChangedEventData eventData)
    {
        switch (eventData.CurrentState)
        {
            case GameFlowStateType.INTRO:
                HandleStageEntry();
                break;

            case GameFlowStateType.ENDING:
                StopGameProgressTimer();
                break;

            case GameFlowStateType.RESULT:
                StopGameProgressTimer();
                ApplyResultContextData();
                break;

            case GameFlowStateType.GAME_OVER:
                StopGameProgressTimer();
                break;
        }
    }

    /// <summary>
    /// ステージシーンの開始方法に応じて初期処理を行います。
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

        /*
         * NEW_GAME / RESTARTは0秒、
         * CHECKPOINTは死亡時点までの累計時間が渡される。
         *
         * この時点ではElapsedTimerは停止中なので、
         * StageTimeLimitController側では
         * 制限時間の消費として扱われない。
         */
        m_gameProgressElapsedTimer.SetElapsedTime(
            elapsedSeconds);
    }

    /// <summary>
    /// ステージ最初からのリスタートを開始します。
    /// </summary>
    private void StartRestart()
    {
        // Scene再読み込み後の初期状態を使用し、
        // 登場シーケンスのみ飛ばしてゲームプレイへ入る
        StartPlaying();
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

        StartPlaying();
    }

    /// <summary>
    /// イントロシーケンスを開始します。
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
                "イントロシーケンスが設定されていません。",
                this);

            return;
        }

        if (!m_sequenceController.StartSequence(
            m_introSequenceAsset_PlayerSpawn))
        {
            Debug.LogWarning(
                $"{m_introSequenceAsset_PlayerSpawn.name} の開始に失敗しました。",
                this);
        }
    }

    /// <summary>
    /// イントロシーケンスを開始します。
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
                "イントロシーケンスが設定されていません。",
                this);

            return;
        }

        if (!m_sequenceController.StartSequence(
            m_introSequenceAsset_BossSpawn))
        {
            Debug.LogWarning(
                $"{m_introSequenceAsset_BossSpawn.name} の開始に失敗しました。",
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
        // イントロシーケンス以外は対象外
        if (eventData.Sequence == m_introSequenceAsset_PlayerSpawn)
        {
            // 完了時のみゲームプレイを開始
            if (eventData.EventType ==
                GameplaySequenceEventType.FINISHED)
            {
                StartIntroBossSequence();
            }
        }

        // イントロシーケンス以外は対象外
        if (eventData.Sequence != m_introSequenceAsset_BossSpawn)
        {
            return;
        }

        // イントロ完了時のみゲームプレイを開始
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
    /// 通常ゲームプレイを開始します。
    /// </summary>
    private void StartPlaying()
    {
        Debug.Log("ゲーム開始");
        if (m_gameFlowController != null)
        {
            m_gameFlowController.EnterPlaying();
        }

        if (m_gameplayModeController != null)
        {
            m_gameplayModeController.ActivateGameplay();
        }

        if (m_gameProgressElapsedTimer != null)
        {
            m_gameProgressElapsedTimer.StartTimer();
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
    /// StageSceneContextの実行情報をリザルト表示Modelへ反映します。
    /// </summary>
    private void ApplyResultContextData()
    {
        if (m_stageSceneContext == null)
        {
            Debug.LogWarning(
                $"{nameof(StageSceneContext)} が設定されていません。",
                this);

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
                1,
                m_stageSceneContext);
        }
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
        m_resultTimeModel.SetTimeData(m_saveDataController.CurrentData, m_gameProgressElapsedTimer, 1);
        m_resultRankModel.EvaluateRank(m_resultTimeModel, m_stageSettings);
    }

    /// <summary>
    /// ステージ最初からリスタートします。
    /// UI Buttonから呼び出すことを想定しています。
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

        // リスタート時は累計プレイ時間も0秒へ戻し、
        // イントロを飛ばしてステージ最初から開始
        m_stageSceneContext.SetRestart();

    }

    /// <summary>
    /// 最新のチェックポイントから再開します。
    /// UI Buttonから呼び出すことを想定しています。
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

            return;
        }

    }

    /// <summary>
    /// 現在のインゲームSceneを再読み込みします。
    /// </summary>
    public void ReloadInGameScene()
    {
        // Pause等でTimeScaleが0のままSceneを跨がないよう戻す
        Time.timeScale = 1.0f;

        Scene currentScene =
            SceneManager.GetActiveScene();

        if (!currentScene.IsValid()
            || string.IsNullOrEmpty(currentScene.name))
        {
            Debug.LogWarning(
                "現在のSceneを取得できませんでした。",
                this);

            return;
        }

        SceneManager.LoadScene(
            currentScene.name);
    }

    /// <summary>
    /// 次のステージへ遷移します。
    /// </summary>
    public void LoadNextStage()
    {
        if (string.IsNullOrEmpty(
            m_nextStageSceneName))
        {
            Debug.LogWarning(
                "次のステージシーン名が設定されていません。",
                this);

            return;
        }

        m_stageSceneContext?.SetNewGame();

        SceneManager.LoadScene(
            m_nextStageSceneName);
    }

    /// <summary>
    /// ステージ選択画面へ遷移します。
    /// </summary>
    public void LoadStageSelect()
    {
        if (string.IsNullOrEmpty(
            m_stageSelectSceneName))
        {
            Debug.LogWarning(
                "ステージ選択画面シーン名が設定されていません。",
                this);

            return;
        }

        m_stageSceneContext?.Clear();

        SceneManager.LoadScene(
            m_stageSelectSceneName);
    }

    /// <summary>
    /// 現在のステージを最初からリスタートします。
    /// </summary>
    public void LoadRestartScene()
    {
        RestartInGame();
    }
}