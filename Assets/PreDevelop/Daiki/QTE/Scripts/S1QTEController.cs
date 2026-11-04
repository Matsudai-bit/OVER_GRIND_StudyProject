using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

/// <summary>
/// ステージ1のボスコア破壊QTEを制御します。
/// </summary>
[DisallowMultipleComponent]
public sealed class S1QTEController : MonoBehaviour
{
    // Timeline時間比較用の許容誤差
    private const double TIME_EPSILON = 0.001;

    /// <summary>
    /// QTEの進行状態です。
    /// </summary>
    private enum QTEState
    {
        INACTIVE,
        WAITING_A,
        A_ATTACKING,
        B_ATTACKING,
        REVERSING_TO_A,
        REVERSING_TO_INITIAL,
        SUCCEEDED,
        FAILED
    }

    // Timelineを制御するPlayableDirector
    [SerializeField, Header("Timeline")]
    private PlayableDirector m_playableDirector;

    // A攻撃入力
    [SerializeField, Header("QTE入力")]
    private InputActionReference m_aAttackActionReference;

    // B攻撃入力
    [SerializeField]
    private InputActionReference m_bAttackActionReference;

    // A入力表示UI
    [SerializeField, Header("QTE UI")]
    private GameObject m_aInputUI;

    // B入力表示UI
    [SerializeField]
    private GameObject m_bInputUI;

    // QTE全体の制限時間
    [SerializeField, Header("QTE設定"), Min(0.1f)]
    private float m_timeLimit = 10.0f;

    // QTE成功に必要なB攻撃の連続時間
    [SerializeField, Min(0.1f)]
    private float m_requiredBAttackDuration = 3.0f;

    // Timelineの逆再生速度
    [SerializeField, Min(0.01f)]
    private float m_reverseSpeed = 1.0f;

    // A攻撃ループ開始位置
    [SerializeField, Header("Timeline位置"), Min(0.0f)]
    private double m_aLoopStartTime;

    // A攻撃ループ終了位置
    [SerializeField, Min(0.0f)]
    private double m_aLoopEndTime;

    // B入力時にジャンプするB攻撃開始位置
    [SerializeField, Min(0.0f)]
    private double m_bAttackStartTime;

    // B攻撃の2周目以降のループ開始位置
    [SerializeField, Min(0.0f)]
    private double m_bLoopStartTime;

    // QTE成功時のTimeline位置
    [SerializeField, Min(0.0f)]
    private double m_successTime;

    // QTE失敗時のTimeline位置
    [SerializeField, Min(0.0f)]
    private double m_failureTime;

    // 現在のQTE状態
    private QTEState m_currentState = QTEState.INACTIVE;

    // QTEが実行中か
    private bool m_isQteActive;

    // A入力を受付中か
    private bool m_isAInputReceptionEnabled;

    // B入力を受付中か
    private bool m_isBInputReceptionEnabled;

    // 初期地点への逆再生中にA再入力で復帰できるか
    private bool m_canResumeAFromReverse;

    // A入力を保持しているか
    private bool m_isAHeld;

    // B入力を保持しているか
    private bool m_isBHeld;

    // B攻撃ループ区間に到達しているか
    private bool m_isBLoopActive;

    // QTE制限時間に到達したか
    private bool m_isTimeLimitReached;

    // 初期地点への逆再生完了後に失敗させるか
    private bool m_shouldFailAfterReverse;

    // 手動逆再生中か
    private bool m_isManualReverseActive;

    // QTE残り時間
    private float m_remainingTime;

    // B攻撃の連続継続時間
    private float m_bAttackDuration;

    // QTE初期地点
    private double m_initialTime;

    // 逆再生開始前のTimeline更新方式
    private DirectorUpdateMode m_previousUpdateMode;

    private void OnEnable()
    {
        RegisterInputCallbacks();
    }

    private void OnDisable()
    {
        UnregisterInputCallbacks();
        DisableInputActions();
        RestoreTimelineUpdateMode();
    }

    private void Update()
    {
  
        if (!m_isQteActive)
        {
            return;
        }

        float deltaTime = Time.unscaledDeltaTime;

        UpdateBAttackDuration(deltaTime);

        if (!m_isQteActive)
        {
            return;
        }

        UpdateTimeLimit(deltaTime);

        if (!m_isQteActive)
        {
            return;
        }

        UpdateReversePlayback(deltaTime);
    }

    /// <summary>
    /// QTEを開始します。
    /// TimelineのSignalから呼び出します。
    /// </summary>
    public void StartQTE()
    {
        if (m_isQteActive)
        {
            return;
        }

        if (!ValidateReferences())
        {
            return;
        }

        m_isQteActive = true;

        m_currentState = QTEState.INACTIVE;

        m_isAInputReceptionEnabled = false;
        m_isBInputReceptionEnabled = false;

        m_isAHeld = false;
        m_isBHeld = false;

        m_isBLoopActive = false;
        m_isTimeLimitReached = false;
        m_shouldFailAfterReverse = false;
        m_isManualReverseActive = false;

        m_remainingTime = m_timeLimit;
        m_bAttackDuration = 0.0f;

        SetAInputUIActive(false);
        SetBInputUIActive(false);

        EnableInputActions();
    }

    /// <summary>
    /// QTE初期地点を登録してA入力待ちを開始します。
    /// TimelineのSignalから最初の1回だけ呼び出します。
    /// </summary>
    public void BeginAInputWait()
    {
        if (!m_isQteActive)
        {
            return;
        }

        m_initialTime = m_playableDirector.time;

        m_currentState = QTEState.WAITING_A;

        m_isAInputReceptionEnabled = true;
        m_isBInputReceptionEnabled = false;

        // A入力を待つためTimelineを停止
        SetTimelineSpeed(0.0);
    }

    /// <summary>
    /// A入力受付を有効にします。
    /// </summary>
    public void EnableAInputReception()
    {
        if (!m_isQteActive)
        {
            return;
        }

        m_isAInputReceptionEnabled = true;
    }

    /// <summary>
    /// A入力受付を無効にします。
    /// </summary>
    public void DisableAInputReception()
    {
        m_isAInputReceptionEnabled = false;
    }

    /// <summary>
    /// B入力受付を有効にします。
    /// TimelineのA攻撃区間から呼び出します。
    /// </summary>
    public void EnableBInputReception()
    {
        if (!m_isQteActive ||
            !m_isAHeld)
        {
            return;
        }

        m_isBInputReceptionEnabled = true;
    }

    /// <summary>
    /// B入力受付を無効にします。
    /// </summary>
    public void DisableBInputReception()
    {
        m_isBInputReceptionEnabled = false;
    }

    /// <summary>
    /// A入力UIを表示します。
    /// </summary>
    public void ShowAInputUI()
    {
        SetAInputUIActive(true);
    }

    /// <summary>
    /// A入力UIを非表示にします。
    /// </summary>
    public void HideAInputUI()
    {
        SetAInputUIActive(false);
    }

    /// <summary>
    /// B入力UIを表示します。
    /// </summary>
    public void ShowBInputUI()
    {
        SetBInputUIActive(true);
    }

    /// <summary>
    /// B入力UIを非表示にします。
    /// </summary>
    public void HideBInputUI()
    {
        SetBInputUIActive(false);
    }

    /// <summary>
    /// A攻撃ループ終端を処理します。
    /// TimelineのSignalから呼び出します。
    /// </summary>
    public void HandleALoopEnd()
    {
        if (!m_isQteActive ||
            m_currentState != QTEState.A_ATTACKING)
        {
            return;
        }

        // Aが解除されている場合は初期地点まで戻す
        if (!m_isAHeld)
        {
            BeginReverseToInitial(false);
            return;
        }

        /*
         * Aループ内でBが既に入力されている場合。
         *
         * 主にB→Aの逆再生中にBを再入力し、
         * Aループから再開したケースで使用します。
         */
        if (m_isBHeld)
        {
            StartBAttack();
            return;
        }

        // B未入力なのでA攻撃を継続
        JumpTimeline(m_aLoopStartTime);
    }

    /// <summary>
    /// B攻撃ループ開始地点へ到達したことを通知します。
    /// TimelineのSignalから呼び出します。
    /// </summary>
    public void NotifyBLoopStarted()
    {
        if (!m_isQteActive ||
            m_currentState != QTEState.B_ATTACKING)
        {
            return;
        }

        m_isBLoopActive = true;
    }

    /// <summary>
    /// B攻撃ループ終端を処理します。
    /// TimelineのSignalから呼び出します。
    /// </summary>
    public void HandleBLoopEnd()
    {
        if (!m_isQteActive ||
            m_currentState != QTEState.B_ATTACKING)
        {
            return;
        }

        // Aが解除されていれば初期地点まで戻す
        if (!m_isAHeld)
        {
            BeginReverseToInitial(false);
            return;
        }

        // Bが解除されていればAループまで戻す
        if (!m_isBHeld)
        {
            BeginReverseToA();
            return;
        }

        // A+B保持中なのでB攻撃を継続
        JumpTimeline(m_bLoopStartTime);
    }

    /// <summary>
    /// QTEを強制的に失敗させます。
    /// </summary>
    public void FailQTE()
    {
        if (!m_isQteActive)
        {
            return;
        }

        BeginReverseToInitial(true);
    }

    /// <summary>
    /// A入力開始を処理します。
    /// </summary>
    /// <param name="context">入力情報。</param>
    private void OnAInputPerformed(
        InputAction.CallbackContext context)
    {
        if (!m_isQteActive ||
            m_isAHeld)
        {
            return;
        }

        /*
         * A解除後の逆再生中にAが再入力された場合は、
         * 現在位置から通常方向へ復帰します。
         */
        if (m_currentState == QTEState.REVERSING_TO_INITIAL &&
            m_canResumeAFromReverse)
        {
            m_isAHeld = true;

            ResumeFromReverseByAInput();
            return;
        }

        // 通常のA入力待ち
        if (!m_isAInputReceptionEnabled ||
            m_currentState != QTEState.WAITING_A)
        {
            return;
        }

        m_isAHeld = true;

        m_isAInputReceptionEnabled = false;

        m_currentState = QTEState.A_ATTACKING;

        SetAInputUIActive(false);

        // 初期地点からA攻撃を開始
        SetTimelineSpeed(1.0);
    }

    /// <summary>
    /// A入力解除を処理します。
    /// </summary>
    /// <param name="context">入力情報。</param>
    private void OnAInputCanceled(
        InputAction.CallbackContext context)
    {
        if (!m_isQteActive ||
            !m_isAHeld)
        {
            return;
        }

        m_isAHeld = false;
        m_isBHeld = false;

        m_bAttackDuration = 0.0f;
        m_isBLoopActive = false;

        switch (m_currentState)
        {
            case QTEState.A_ATTACKING:
            case QTEState.B_ATTACKING:
                BeginReverseToInitial(false);
                break;

            case QTEState.REVERSING_TO_A:
                /*
                 * B→Aへの逆再生途中でAも解除された場合、
                 * 戻り先をAではなく初期地点へ変更します。
                 */
                BeginReverseToInitial(false);
                break;
        }
    }

    /// <summary>
    /// B入力開始を処理します。
    /// </summary>
    /// <param name="context">入力情報。</param>
    private void OnBInputPerformed(
        InputAction.CallbackContext context)
    {
        if (!m_isQteActive ||
            !m_isAHeld ||
            m_isBHeld)
        {
            return;
        }

        /*
         * B解除後の逆再生中は、
         * Timeline側の受付フラグに関係なく
         * B再入力を許可します。
         */
        if (m_currentState ==
            QTEState.REVERSING_TO_A)
        {
            m_isBHeld = true;

            ResumeFromReverseByBInput();
            return;
        }

        if (!m_isBInputReceptionEnabled ||
            m_currentState != QTEState.A_ATTACKING)
        {
            return;
        }

        m_isBHeld = true;

        StartBAttack();
    }

    /// <summary>
    /// B入力解除を処理します。
    /// </summary>
    /// <param name="context">入力情報。</param>
    private void OnBInputCanceled(
        InputAction.CallbackContext context)
    {
        if (!m_isQteActive ||
            !m_isBHeld)
        {
            return;
        }

        m_isBHeld = false;

        // 連続強攻撃時間をリセット
        m_bAttackDuration = 0.0f;
        m_isBLoopActive = false;

        /*
         * Aループ内でBを離した場合は、
         * 既にA状態なのでそのままB入力待ちを続けます。
         */
        if (m_currentState == QTEState.A_ATTACKING)
        {
            return;
        }

        /*
         * B攻撃中ならAループへ逆再生します。
         */
        if (m_currentState == QTEState.B_ATTACKING)
        {
            BeginReverseToA();
        }
    }

    /// <summary>
    /// B攻撃を開始します。
    /// </summary>
    private void StartBAttack()
    {
        if (!m_isQteActive ||
            !m_isAHeld ||
            !m_isBHeld)
        {
            return;
        }

        m_currentState = QTEState.B_ATTACKING;

        m_isBInputReceptionEnabled = false;

        m_bAttackDuration = 0.0f;
        m_isBLoopActive = false;

        SetBInputUIActive(false);

        /*
         * B入力時はBループ開始地点ではなく、
         * B攻撃の初回開始地点へジャンプします。
         */
        JumpTimeline(m_bAttackStartTime);

        SetTimelineSpeed(1.0);
    }

    /// <summary>
    /// B攻撃からAループへの逆再生を開始します。
    /// </summary>
    private void BeginReverseToA()
    {
        if (!m_isQteActive)
        {
            return;
        }

        if (!m_isAHeld)
        {
            BeginReverseToInitial(false);
            return;
        }

        if (m_currentState ==
            QTEState.REVERSING_TO_A)
        {
            return;
        }

        m_currentState = QTEState.REVERSING_TO_A;

        m_bAttackDuration = 0.0f;
        m_isBLoopActive = false;

        /*
         * B→A逆再生中でもB再入力を可能にします。
         */
        m_isBInputReceptionEnabled = true;

        SetBInputUIActive(true);

        BeginManualReverse();
    }

    /// <summary>
    /// 現在地点からQTE初期地点への逆再生を開始します。
    /// </summary>
    /// <param name="shouldFailAfterReverse">
    /// 初期地点到達後にQTEを失敗させるか。
    /// </param>
    private void BeginReverseToInitial(
     bool shouldFailAfterReverse)
    {
        if (!m_isQteActive)
        {
            return;
        }

        if (shouldFailAfterReverse)
        {
            m_shouldFailAfterReverse = true;
        }

        bool isAlreadyReversing =
            m_currentState == QTEState.REVERSING_TO_A ||
            m_currentState == QTEState.REVERSING_TO_INITIAL;

        m_currentState =
            QTEState.REVERSING_TO_INITIAL;

        /*
         * 通常の入力解除なら逆再生中のA再入力を許可。
         * 時間切れなどの失敗確定後は許可しない。
         */
        m_canResumeAFromReverse =
            !m_shouldFailAfterReverse;

        m_isAInputReceptionEnabled =
            m_canResumeAFromReverse;

        m_isBInputReceptionEnabled = false;

        m_isAHeld = false;
        m_isBHeld = false;

        m_bAttackDuration = 0.0f;
        m_isBLoopActive = false;

        SetAInputUIActive(
            m_canResumeAFromReverse);

        SetBInputUIActive(false);

        if (isAlreadyReversing)
        {
            return;
        }

        BeginManualReverse();
    }

    /// <summary>
    /// Timelineの手動逆再生を開始します。
    /// </summary>
    /// <summary>
    /// Timelineの手動逆再生を開始します。
    /// </summary>
    private void BeginManualReverse()
    {
        if (m_isManualReverseActive)
        {
            return;
        }

        m_playableDirector.timeUpdateMode =
            DirectorUpdateMode.Manual;

        m_isManualReverseActive = true;
    }
    /// <summary>
    /// A再入力によって初期地点への逆再生から復帰します。
    /// </summary>
    private void ResumeFromReverseByAInput()
    {
        double currentTime =
            m_playableDirector.time;

        m_canResumeAFromReverse = false;

        /*
         * まだB側の区間にいる場合、
         * AだけではB攻撃を継続できないため、
         * Aループまで逆再生を続けます。
         */
        if (currentTime >
            m_aLoopEndTime + TIME_EPSILON)
        {
            m_currentState =
                QTEState.REVERSING_TO_A;

            m_isBInputReceptionEnabled = true;

            SetAInputUIActive(false);
            SetBInputUIActive(true);

            return;
        }

        /*
         * A区間まで戻っている場合は、
         * その現在位置から通常方向へ再生します。
         */
        RestoreTimelineUpdateMode();

        m_currentState =
            QTEState.A_ATTACKING;

        m_isAInputReceptionEnabled = false;

        /*
         * Aループ開始位置より後ろなら、
         * B入力も再び受け付けます。
         *
         * Aループより前なら、Timeline上の
         * EnableBInputReception Signalに任せます。
         */
        m_isBInputReceptionEnabled =
            currentTime >=
            m_aLoopStartTime - TIME_EPSILON;

        SetAInputUIActive(false);

        SetBInputUIActive(
            m_isBInputReceptionEnabled);

        // 通常再生へ復帰
        SetTimelineSpeed(1.0);
    }
    /// <summary>
    /// B再入力によって逆再生途中から通常再生へ復帰します。
    /// </summary>
    private void ResumeFromReverseByBInput()
    {
        double currentTime =
            m_playableDirector.time;

        RestoreTimelineUpdateMode();

        // 通常再生速度を確実に復元
        SetTimelineSpeed(1.0);

        m_bAttackDuration = 0.0f;

        /*
         * Aループ終了地点より後ろにいる場合は、
         * B側の演出区間です。
         *
         * 現在地点からB方向へ通常再生します。
         */
        if (currentTime >
            m_aLoopEndTime + TIME_EPSILON)
        {
            m_currentState =
                QTEState.B_ATTACKING;

            m_isBInputReceptionEnabled = false;

            /*
             * Bループ開始地点より後ろなら、
             * 強攻撃区間へ既に入っているため
             * 成功時間の計測を再開します。
             */
            m_isBLoopActive =
                currentTime >=
                m_bLoopStartTime - TIME_EPSILON;

            SetBInputUIActive(false);

            return;
        }

        /*
         * Aループ内まで戻っている場合。
         *
         * 現在のAループ位置から前方向へ再生します。
         * Bは押された状態なので、
         * HandleALoopEnd()でB攻撃へ移ります。
         */
        m_currentState =
            QTEState.A_ATTACKING;

        m_isBInputReceptionEnabled = true;
        m_isBLoopActive = false;

        SetBInputUIActive(false);
    }

    /// <summary>
    /// B攻撃の連続継続時間を更新します。
    /// </summary>
    /// <param name="deltaTime">経過時間。</param>
    private void UpdateBAttackDuration(float deltaTime)
    {
        if (m_currentState != QTEState.B_ATTACKING ||
            !m_isBLoopActive ||
            !m_isAHeld ||
            !m_isBHeld)
        {
            return;
        }

        m_bAttackDuration += deltaTime;

        if (m_bAttackDuration <
            m_requiredBAttackDuration)
        {
            return;
        }

        FinishQTE(true);
    }

    /// <summary>
    /// QTE全体の制限時間を更新します。
    /// </summary>
    /// <param name="deltaTime">経過時間。</param>
    private void UpdateTimeLimit(float deltaTime)
    {
        if (m_isTimeLimitReached)
        {
            return;
        }

        m_remainingTime -= deltaTime;

        if (m_remainingTime > 0.0f)
        {
            return;
        }

        m_isTimeLimitReached = true;

        /*
         * 初期地点でA入力待ちなら、
         * 逆再生する必要がありません。
         */
        if (m_currentState ==
            QTEState.WAITING_A)
        {
            FinishQTE(false);
            return;
        }

        /*
         * A/B攻撃途中の場合は、
         * 初期地点まで逆再生してから失敗します。
         */
        BeginReverseToInitial(true);
    }

    /// <summary>
    /// Timelineの逆再生を更新します。
    /// </summary>
    /// <param name="deltaTime">経過時間。</param>
    private void UpdateReversePlayback(float deltaTime)
    {
        double targetTime;

        switch (m_currentState)
        {
            case QTEState.REVERSING_TO_A:
                targetTime = m_aLoopStartTime;
                break;

            case QTEState.REVERSING_TO_INITIAL:
                targetTime = m_initialTime;
                break;

            default:
                return;
        }

        double nextTime =
            m_playableDirector.time -
            (m_reverseSpeed * deltaTime);

        if (nextTime <= targetTime)
        {
            nextTime = targetTime;
        }

        m_playableDirector.time = nextTime;
        m_playableDirector.Evaluate();

        if (nextTime >
            targetTime + TIME_EPSILON)
        {
            return;
        }

        if (m_currentState ==
            QTEState.REVERSING_TO_A)
        {
            CompleteReverseToA();
            return;
        }

        CompleteReverseToInitial();
    }

    /// <summary>
    /// Aループへの逆再生終了処理を行います。
    /// </summary>
    private void CompleteReverseToA()
    {
        m_playableDirector.time =
            m_aLoopStartTime;

        m_playableDirector.Evaluate();

        RestoreTimelineUpdateMode();

        /*
         * 逆再生中にAまで解除されていた場合は、
         * 初期地点まで戻ります。
         */
        if (!m_isAHeld)
        {
            BeginReverseToInitial(false);
            return;
        }

        m_currentState =
            QTEState.A_ATTACKING;

        m_isBHeld = false;
        m_isBLoopActive = false;

        m_bAttackDuration = 0.0f;

        // Aループへ戻ったのでB入力待ちを再開
        m_isBInputReceptionEnabled = true;

        SetBInputUIActive(true);

        // Aループを通常方向へ再開
        SetTimelineSpeed(1.0);
    }

    /// <summary>
    /// 初期地点への逆再生終了処理を行います。
    /// </summary>
    private void CompleteReverseToInitial()
    {
        m_playableDirector.time =
            m_initialTime;

        m_playableDirector.Evaluate();

        RestoreTimelineUpdateMode();

        if (m_shouldFailAfterReverse)
        {
            FinishQTE(false);
            return;
        }

        m_currentState =
            QTEState.WAITING_A;

        m_isAHeld = false;
        m_isBHeld = false;

        m_isBLoopActive = false;

        m_bAttackDuration = 0.0f;

        // 初期地点へ戻ったのでA入力を再受付
        m_isAInputReceptionEnabled = true;
        m_isBInputReceptionEnabled = false;

        SetAInputUIActive(true);
        SetBInputUIActive(false);

        /*
         * A入力待ち状態なのでTimelineを停止します。
         *
         * 次のA入力で必ずSpeed = 1へ戻ります。
         */
        SetTimelineSpeed(0.0);
    }

    /// <summary>
    /// QTEを終了します。
    /// </summary>
    /// <param name="isSuccess">QTEに成功したか。</param>
    private void FinishQTE(bool isSuccess)
    {
        if (!m_isQteActive)
        {
            return;
        }

        RestoreTimelineUpdateMode();

        m_isQteActive = false;

        m_currentState = isSuccess
            ? QTEState.SUCCEEDED
            : QTEState.FAILED;

        m_isAInputReceptionEnabled = false;
        m_isBInputReceptionEnabled = false;

        m_isAHeld = false;
        m_isBHeld = false;

        m_isBLoopActive = false;

        SetAInputUIActive(false);
        SetBInputUIActive(false);

        DisableInputActions();

        double targetTime = isSuccess
            ? m_successTime
            : m_failureTime;

        JumpTimeline(targetTime);

        SetTimelineSpeed(1.0);
    }

    /// <summary>
    /// Timelineを通常の更新方式へ戻します。
    /// </summary>
    private void RestoreTimelineUpdateMode()
    {
        if (m_playableDirector == null)
        {
            return;
        }

        m_playableDirector.timeUpdateMode =
            DirectorUpdateMode.GameTime;

        m_isManualReverseActive = false;
    }

    /// <summary>
    /// Timelineを指定位置へ移動します。
    /// </summary>
    /// <param name="targetTime">移動先の時間。</param>
    private void JumpTimeline(double targetTime)
    {
        if (m_playableDirector == null)
        {
            return;
        }

        m_playableDirector.time = targetTime;
        m_playableDirector.Evaluate();
    }

    /// <summary>
    /// Timelineの再生速度を変更します。
    /// </summary>
    /// <param name="speed">再生速度。</param>
    private void SetTimelineSpeed(double speed)
    {
        if (m_playableDirector == null ||
            !m_playableDirector.playableGraph.IsValid() ||
            m_playableDirector.playableGraph.GetRootPlayableCount() <= 0)
        {
            return;
        }

        /*
         * Director自体がPause状態の場合、
         * Speedを1にするだけでは再生されないためResumeする。
         */
        if (speed > 0.0 &&
            m_playableDirector.state == PlayState.Paused)
        {
            m_playableDirector.Resume();
        }

        m_playableDirector.playableGraph
            .GetRootPlayable(0)
            .SetSpeed(speed);

        Debug.Log(
            $"[QTE Timeline] " +
            $"Speed = {speed}, " +
            $"State = {m_playableDirector.state}, " +
            $"UpdateMode = {m_playableDirector.timeUpdateMode}, " +
            $"Time = {m_playableDirector.time:F3}",
            this);
    }

    /// <summary>
    /// InputActionイベントを登録します。
    /// </summary>
    private void RegisterInputCallbacks()
    {
        if (IsInputActionValid(
                m_aAttackActionReference))
        {
            m_aAttackActionReference.action.performed +=
                OnAInputPerformed;

            m_aAttackActionReference.action.canceled +=
                OnAInputCanceled;
        }

        if (IsInputActionValid(
                m_bAttackActionReference))
        {
            m_bAttackActionReference.action.performed +=
                OnBInputPerformed;

            m_bAttackActionReference.action.canceled +=
                OnBInputCanceled;
        }
    }

    /// <summary>
    /// InputActionイベントを解除します。
    /// </summary>
    private void UnregisterInputCallbacks()
    {
        if (IsInputActionValid(
                m_aAttackActionReference))
        {
            m_aAttackActionReference.action.performed -=
                OnAInputPerformed;

            m_aAttackActionReference.action.canceled -=
                OnAInputCanceled;
        }

        if (IsInputActionValid(
                m_bAttackActionReference))
        {
            m_bAttackActionReference.action.performed -=
                OnBInputPerformed;

            m_bAttackActionReference.action.canceled -=
                OnBInputCanceled;
        }
    }

    /// <summary>
    /// QTE用InputActionを有効にします。
    /// </summary>
    private void EnableInputActions()
    {
        if (IsInputActionValid(
                m_aAttackActionReference) &&
            !m_aAttackActionReference.action.enabled)
        {
            m_aAttackActionReference.action.Enable();
        }

        if (IsInputActionValid(
                m_bAttackActionReference) &&
            !m_bAttackActionReference.action.enabled)
        {
            m_bAttackActionReference.action.Enable();
        }
    }

    /// <summary>
    /// QTE用InputActionを無効にします。
    /// </summary>
    private void DisableInputActions()
    {
        if (IsInputActionValid(
                m_aAttackActionReference) &&
            m_aAttackActionReference.action.enabled)
        {
            m_aAttackActionReference.action.Disable();
        }

        if (IsInputActionValid(
                m_bAttackActionReference) &&
            m_bAttackActionReference.action.enabled)
        {
            m_bAttackActionReference.action.Disable();
        }
    }

    /// <summary>
    /// A入力UIの表示状態を変更します。
    /// </summary>
    /// <param name="isActive">表示するか。</param>
    private void SetAInputUIActive(bool isActive)
    {
        if (m_aInputUI == null)
        {
            return;
        }

        m_aInputUI.SetActive(isActive);
    }

    /// <summary>
    /// B入力UIの表示状態を変更します。
    /// </summary>
    /// <param name="isActive">表示するか。</param>
    private void SetBInputUIActive(bool isActive)
    {
        if (m_bInputUI == null)
        {
            return;
        }

        m_bInputUI.SetActive(isActive);
    }

    /// <summary>
    /// 必須参照とTimeline設定を確認します。
    /// </summary>
    /// <returns>
    /// true：QTEを開始できます。
    /// false：設定が不足しています。
    /// </returns>
    private bool ValidateReferences()
    {
        if (m_playableDirector == null)
        {
            Debug.LogError(
                "PlayableDirectorが設定されていません。",
                this);

            return false;
        }

        if (!IsInputActionValid(
                m_aAttackActionReference))
        {
            Debug.LogError(
                "A攻撃用InputActionが設定されていません。",
                this);

            return false;
        }

        if (!IsInputActionValid(
                m_bAttackActionReference))
        {
            Debug.LogError(
                "B攻撃用InputActionが設定されていません。",
                this);

            return false;
        }

        if (m_aLoopEndTime <=
            m_aLoopStartTime)
        {
            Debug.LogError(
                "Aループ終了位置は開始位置より後に設定してください。",
                this);

            return false;
        }

        if (m_bAttackStartTime <
            m_aLoopEndTime)
        {
            Debug.LogError(
                "B攻撃開始位置はAループ終了位置以降に設定してください。",
                this);

            return false;
        }

        if (m_bLoopStartTime <
            m_bAttackStartTime)
        {
            Debug.LogError(
                "Bループ開始位置はB攻撃開始位置以降に設定してください。",
                this);

            return false;
        }

        return true;
    }

    /// <summary>
    /// InputActionReferenceが使用可能か確認します。
    /// </summary>
    /// <param name="actionReference">確認対象。</param>
    /// <returns>
    /// true：使用できます。
    /// false：使用できません。
    /// </returns>
    private bool IsInputActionValid(
        InputActionReference actionReference)
    {
        return actionReference != null &&
               actionReference.action != null;
    }
}