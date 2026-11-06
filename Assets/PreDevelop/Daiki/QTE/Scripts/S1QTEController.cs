using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

/// <summary>
/// ステージ1のQTE進行を制御します。
/// </summary>
[DisallowMultipleComponent]
public sealed class S1QTEController : MonoBehaviour
{
    /// <summary>
    /// QTEの進行状態です。
    /// </summary>
    private enum QTEState
    {
        INACTIVE,
        WAITING_INPUT,
        HOLDING,
        SUCCEEDED,
        FAILED
    }

    // Timelineを制御するPlayableDirector
    [SerializeField, Header("Timeline")]
    private PlayableDirector m_playableDirector;

    // 入力待機アニメーションのループ開始位置
    [SerializeField, Header("Timeline位置"), Min(0.0f)]
    private double m_waitLoopStartTime;

    // 2ボタン入力成立時に移動する長押し演出開始位置
    [SerializeField, Min(0.0f)]
    private double m_holdStartTime;

    // 長押しアニメーションのループ開始位置
    [SerializeField, Min(0.0f)]
    private double m_holdLoopStartTime;

    // QTE失敗時のTimeline移動位置
    [SerializeField, Min(0.0f)]
    private double m_failureTime;

    // 1つ目の長押し入力
    [SerializeField, Header("QTE入力")]
    private InputActionReference m_firstHoldActionReference;

    // 2つ目の長押し入力
    [SerializeField]
    private InputActionReference m_secondHoldActionReference;

    // QTE開始から失敗までの制限時間
    [SerializeField, Header("QTE設定"), Min(0.1f)]
    private float m_qteTimeLimit = 10.0f;

    // QTE成功に必要な連続長押し時間
    [SerializeField, Min(0.1f)]
    private float m_requiredHoldDuration = 3.0f;

    // 現在のQTE状態
    private QTEState m_currentState = QTEState.INACTIVE;

    // QTEが進行中か
    private bool m_isQteActive;

    // 1つ目の入力を保持しているか
    private bool m_isFirstInputHeld;

    // 2つ目の入力を保持しているか
    private bool m_isSecondInputHeld;

    // QTE開始からの経過時間
    private float m_elapsedQteTime;

    // 2ボタンを連続で保持している時間
    private float m_holdDuration;

    // このクラスが1つ目のInputActionを有効化したか
    private bool m_didEnableFirstAction;

    // このクラスが2つ目のInputActionを有効化したか
    private bool m_didEnableSecondAction;

    private void OnEnable()
    {
        RegisterInputCallbacks();
    }

    private void OnDisable()
    {
        UnregisterInputCallbacks();
        DisableInputActions();
    }

    private void Update()
    {
        if (!m_isQteActive)
        {
            return;
        }

        float deltaTime = Time.unscaledDeltaTime;

        /*
         * 同一フレームで成功時間と制限時間の両方に
         * 到達した場合は成功を優先します。
         */
        UpdateHoldDuration(deltaTime);

        if (!m_isQteActive)
        {
            return;
        }

        UpdateQteTime(deltaTime);
    }
    /// <summary>
    /// QTEが進行中か取得します。
    /// </summary>
    public bool IsQteActive => m_isQteActive;

    /// <summary>
    /// 2ボタンを長押し中か取得します。
    /// </summary>
    public bool IsHolding =>
        m_currentState == QTEState.HOLDING;

    /// <summary>
    /// 現在の長押し進行率を取得します。
    /// </summary>
    public float HoldProgress
    {
        get
        {
            if (m_requiredHoldDuration <= 0.0f)
            {
                return 0.0f;
            }

            return Mathf.Clamp01(
                m_holdDuration /
                m_requiredHoldDuration);
        }
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

        m_currentState =
            QTEState.WAITING_INPUT;

        m_isFirstInputHeld = false;
        m_isSecondInputHeld = false;

        m_elapsedQteTime = 0.0f;
        m_holdDuration = 0.0f;

        EnableInputActions();

        /*
         * QTE開始後もTimelineは停止させません。
         * WAIT_LOOP_ENDのSignalによって
         * 入力待機Animationをループさせます。
         */
        SetTimelineSpeed(1.0);
    }

    /// <summary>
    /// 入力待機アニメーションのループ終端を処理します。
    /// TimelineのSignalから呼び出します。
    /// </summary>
    public void HandleWaitLoopEnd()
    {
        if (!m_isQteActive ||
            m_currentState != QTEState.WAITING_INPUT)
        {
            return;
        }

        // 入力待機Animationを継続
        JumpTimeline(m_waitLoopStartTime);
    }

    /// <summary>
    /// 長押しアニメーションのループ終端を処理します。
    /// TimelineのSignalから呼び出します。
    /// </summary>
    public void HandleHoldLoopEnd()
    {
        if (!m_isQteActive ||
            m_currentState != QTEState.HOLDING)
        {
            /*
             * 成功後はm_isQteActiveがfalseになるため、
             * ここでループせずTimelineがそのまま先へ進みます。
             */
            return;
        }

        if (!AreBothInputsHeld())
        {
            ReturnToWait();
            return;
        }

        // 2ボタン保持中なので長押しAnimationを継続
        JumpTimeline(m_holdLoopStartTime);
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

        FinishQTE(false);
    }

    /// <summary>
    /// 1つ目の入力開始を処理します。
    /// </summary>
    /// <param name="context">入力情報。</param>
    private void OnFirstInputPerformed(
        InputAction.CallbackContext context)
    {
        if (!m_isQteActive)
        {
            return;
        }

        m_isFirstInputHeld = true;

        TryStartHolding();
    }

    /// <summary>
    /// 1つ目の入力解除を処理します。
    /// </summary>
    /// <param name="context">入力情報。</param>
    private void OnFirstInputCanceled(
        InputAction.CallbackContext context)
    {
        if (!m_isQteActive)
        {
            return;
        }

        m_isFirstInputHeld = false;

        HandleInputReleased();
    }

    /// <summary>
    /// 2つ目の入力開始を処理します。
    /// </summary>
    /// <param name="context">入力情報。</param>
    private void OnSecondInputPerformed(
        InputAction.CallbackContext context)
    {
        if (!m_isQteActive)
        {
            return;
        }

        m_isSecondInputHeld = true;

        TryStartHolding();
    }

    /// <summary>
    /// 2つ目の入力解除を処理します。
    /// </summary>
    /// <param name="context">入力情報。</param>
    private void OnSecondInputCanceled(
        InputAction.CallbackContext context)
    {
        if (!m_isQteActive)
        {
            return;
        }

        m_isSecondInputHeld = false;

        HandleInputReleased();
    }

    /// <summary>
    /// 2ボタン同時長押しの開始を試みます。
    /// </summary>
    private void TryStartHolding()
    {
        if (m_currentState != QTEState.WAITING_INPUT ||
            !AreBothInputsHeld())
        {
            return;
        }

        m_currentState =
            QTEState.HOLDING;

        // 連続長押し時間を最初から計測
        m_holdDuration = 0.0f;

        /*
         * Timeline位置は変更しません。
         *
         * 現在再生している待機Animationの途中から
         * そのままTimelineを進めます。
         *
         * 状態がHOLDINGへ変わるため、
         * WAIT_LOOP_ENDへ到達しても待機ループには戻らず、
         * そのまま長押し演出側へ進みます。
         */
        SetTimelineSpeed(1.0);
    }
    /// <summary>
    /// 長押し中の入力解除を処理します。
    /// </summary>
    private void HandleInputReleased()
    {
        if (m_currentState != QTEState.HOLDING)
        {
            return;
        }

        /*
         * 両方とも保持している場合は
         * 長押し状態を継続します。
         */
        if (AreBothInputsHeld())
        {
            return;
        }

        ReturnToWait();
    }

    /// <summary>
    /// 入力待機状態へ戻します。
    /// </summary>
    private void ReturnToWait()
    {
        m_currentState =
            QTEState.WAITING_INPUT;

        // 連続長押し時間をリセット
        m_holdDuration = 0.0f;

        /*
         * 入力状態自体はリセットしません。
         *
         * 片方だけ保持している状態で待機へ戻り、
         * もう片方を再入力した場合でも
         * 再度2ボタン成立として扱えます。
         */

        // 待機Animationのループ開始地点へ戻る
        JumpTimeline(m_waitLoopStartTime);

        // 待機Animationを再生
        SetTimelineSpeed(1.0);
    }

    /// <summary>
    /// QTE成功に必要な長押し時間を更新します。
    /// </summary>
    /// <param name="deltaTime">経過時間。</param>
    private void UpdateHoldDuration(float deltaTime)
    {
        if (m_currentState != QTEState.HOLDING ||
            !AreBothInputsHeld())
        {
            return;
        }

        m_holdDuration += deltaTime;

        if (m_holdDuration <
            m_requiredHoldDuration)
        {
            return;
        }

        FinishQTE(true);
    }

    /// <summary>
    /// QTE全体の経過時間を更新します。
    /// </summary>
    /// <param name="deltaTime">経過時間。</param>
    private void UpdateQteTime(float deltaTime)
    {
        m_elapsedQteTime += deltaTime;

        if (m_elapsedQteTime <
            m_qteTimeLimit)
        {
            return;
        }

        FinishQTE(false);
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

        m_isQteActive = false;

        m_currentState = isSuccess
            ? QTEState.SUCCEEDED
            : QTEState.FAILED;

        m_isFirstInputHeld = false;
        m_isSecondInputHeld = false;

        DisableInputActions();

        if (isSuccess)
        {
            /*
             * 成功時はTimelineをジャンプさせません。
             *
             * QTEが終了しているため、
             * HandleHoldLoopEnd()は以降ループ処理を行わず、
             * Timelineはそのまま成功後演出へ進みます。
             */
            SetTimelineSpeed(1.0);
            return;
        }

        /*
         * 失敗時はTimeline上の失敗演出開始地点へ
         * 即座に移動します。
         */
        JumpTimeline(m_failureTime);

        SetTimelineSpeed(1.0);
    }

    /// <summary>
    /// 2つの入力を同時に保持しているか確認します。
    /// </summary>
    /// <returns>
    /// true：両方の入力を保持しています。
    /// false：どちらか一方以上が入力されていません。
    /// </returns>
    private bool AreBothInputsHeld()
    {
        return m_isFirstInputHeld &&
               m_isSecondInputHeld;
    }

    /// <summary>
    /// Timelineを指定位置へ移動します。
    /// </summary>
    /// <param name="targetTime">移動先のTimeline時間。</param>
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
            m_playableDirector.playableGraph
                .GetRootPlayableCount() <= 0)
        {
            return;
        }

        /*
         * PlayableDirector自体がPause状態の場合、
         * RootPlayableのSpeedを戻すだけでは再開しないため
         * Resumeも行います。
         */
        if (speed > 0.0 &&
            m_playableDirector.state == PlayState.Paused)
        {
            m_playableDirector.Resume();
        }

        m_playableDirector.playableGraph
            .GetRootPlayable(0)
            .SetSpeed(speed);
    }

    /// <summary>
    /// InputActionのイベントを登録します。
    /// </summary>
    private void RegisterInputCallbacks()
    {
        if (IsInputActionValid(
                m_firstHoldActionReference))
        {
            m_firstHoldActionReference.action.performed +=
                OnFirstInputPerformed;

            m_firstHoldActionReference.action.canceled +=
                OnFirstInputCanceled;
        }

        if (IsInputActionValid(
                m_secondHoldActionReference))
        {
            m_secondHoldActionReference.action.performed +=
                OnSecondInputPerformed;

            m_secondHoldActionReference.action.canceled +=
                OnSecondInputCanceled;
        }
    }

    /// <summary>
    /// InputActionのイベント登録を解除します。
    /// </summary>
    private void UnregisterInputCallbacks()
    {
        if (IsInputActionValid(
                m_firstHoldActionReference))
        {
            m_firstHoldActionReference.action.performed -=
                OnFirstInputPerformed;

            m_firstHoldActionReference.action.canceled -=
                OnFirstInputCanceled;
        }

        if (IsInputActionValid(
                m_secondHoldActionReference))
        {
            m_secondHoldActionReference.action.performed -=
                OnSecondInputPerformed;

            m_secondHoldActionReference.action.canceled -=
                OnSecondInputCanceled;
        }
    }

    /// <summary>
    /// QTE用InputActionを有効にします。
    /// </summary>
    private void EnableInputActions()
    {
        m_didEnableFirstAction = false;
        m_didEnableSecondAction = false;

        if (IsInputActionValid(
                m_firstHoldActionReference) &&
            !m_firstHoldActionReference.action.enabled)
        {
            m_firstHoldActionReference.action.Enable();
            m_didEnableFirstAction = true;
        }

        if (IsInputActionValid(
                m_secondHoldActionReference) &&
            !m_secondHoldActionReference.action.enabled)
        {
            m_secondHoldActionReference.action.Enable();
            m_didEnableSecondAction = true;
        }
    }

    /// <summary>
    /// このクラスが有効化したQTE用InputActionを無効にします。
    /// </summary>
    private void DisableInputActions()
    {
        /*
         * もともと他システムによって有効だったActionは
         * このクラスから無効化しません。
         */
        if (m_didEnableFirstAction &&
            IsInputActionValid(
                m_firstHoldActionReference))
        {
            m_firstHoldActionReference.action.Disable();
        }

        if (m_didEnableSecondAction &&
            IsInputActionValid(
                m_secondHoldActionReference))
        {
            m_secondHoldActionReference.action.Disable();
        }

        m_didEnableFirstAction = false;
        m_didEnableSecondAction = false;
    }

    /// <summary>
    /// 必須参照とTimeline設定を確認します。
    /// </summary>
    /// <returns>
    /// true：QTEを開始できます。
    /// false：設定に問題があります。
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
                m_firstHoldActionReference))
        {
            Debug.LogError(
                "1つ目のQTE入力が設定されていません。",
                this);

            return false;
        }

        if (!IsInputActionValid(
                m_secondHoldActionReference))
        {
            Debug.LogError(
                "2つ目のQTE入力が設定されていません。",
                this);

            return false;
        }

        if (m_holdStartTime <
            m_waitLoopStartTime)
        {
            Debug.LogError(
                "長押し開始位置は待機ループ開始位置以降に設定してください。",
                this);

            return false;
        }

        if (m_holdLoopStartTime <
            m_holdStartTime)
        {
            Debug.LogError(
                "長押しループ開始位置は長押し開始位置以降に設定してください。",
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