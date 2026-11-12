
using UnityEngine;

/// <summary>
/// プレイヤーの通常歩行状態を管理します。
/// </summary>
public sealed class PlayerWalkingState
    : StateBase<PlayerStateMachineComponent>
{
    // 通常移動用パラメータ
    private PlayerMoveParameters m_moveParameters;
    private Vector3 m_movingChargeStartDirection;
    private bool m_isWaitingForMovingCharge;

    /// <summary>
    /// 状態開始時に移動パラメータを取得し、
    /// 歩行アニメーションと歩行SEを開始します。
    /// </summary>
    protected override void OnStartState()
    {
        m_isWaitingForMovingCharge = false;

        m_moveParameters =
            Owner.MovementParameterAsset.CreateMoveParameters();

        Owner.AnimationPresenter.PlayWalkAnimation();

        // 歩行SEはジャンプ中も継続するため、
        // State単位ではなくActionID単位で管理します。
        PlayerAudioActionUtility.PlaySequential(
            Owner,
            PlayerAudioActionID.WALKING);
    }

    /// <summary>
    /// 固定時間間隔で状態遷移の判定と移動処理を実行します。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        // 攻撃入力を確認します。
        if (Owner.InputReader.ConsumeAttackInput() &&
            Owner.Monitor.IsGrounded)
        {
            // 攻撃状態へ遷移する前に攻撃SEを再生し、
            // 歩行SEとの切り替えを行います。
            StartAttackAudioBeforeTransition();

            Machine.ChangeState<PlayerAttackingState>();
            return;
        }

        // ジャンプ入力を確認します。
        // 移動入力がなくてもジャンプできるようにします。
        if (Owner.Monitor.CanStartJump &&
            Owner.InputReader.HasJumpInput)
        {
            Machine.ChangeState<PlayerJumpingState>();
            return;
        }

        // グラインド可能な場合はグラインド状態へ遷移します。
        if (Owner.GrindController.CanStartGrind(
            Owner.Monitor.IsRailed
                ? Owner.Monitor.HitRailInfo
                : null))
        {
            // グラインド中に歩行SEが残らないよう停止します。
            PlayerAudioActionUtility.StopSequential(
                Owner,
                PlayerAudioActionID.WALKING);

            Machine.ChangeState<PlayerGrindingState>(
                Owner.Monitor.HitRailInfo);

            // グラインドへの遷移後は通常移動処理を実行しません。
            return;
        }

        // 移動入力がなくなった場合は待機状態へ遷移します。
        if (!Owner.InputReader.HasMoveInput)
        {
            // 待機状態への遷移時に歩行SEを停止します。
            // ジャンプ時はこの処理を通らないため、
            // ジャンプ中の歩行SEは継続します。
            PlayerAudioActionUtility.StopSequential(
                Owner,
                PlayerAudioActionID.WALKING);

            // Vブースト入力が残らないように消費します。
            Owner.InputReader.ConsumeVBoostInput();

            Machine.ChangeState<PlayerIdlingState>();
            return;
        }

        // 移動チャージの開始方向を更新します。
        UpdateMovingChargeStartDirection();

        // 接地中のみ新規の移動チャージを許可します。
        if (Owner.Monitor.IsGrounded &&
            m_isWaitingForMovingCharge &&
            Owner.InputReader.HasVBoostHoldStarted &&
            Owner.BoostChargingParameterAsset.CanStartMovingCharge(
                Owner.Motor.HorizontalSpeed) &&
            Owner.BoostChargingParameterAsset.HasLateralChargeInput(
                Owner.InputReader.MoveInput) &&
            Owner.BoostChargingParameterAsset.HasExceededFreeSteeringAngle(
                m_movingChargeStartDirection,
                Owner.Motor.HorizontalDirection) &&
            Owner.InputReader.ConsumeVBoostHoldStarted())
        {
            Machine.ChangeState<PlayerBoostChargingState>();
            return;
        }

        // 通常移動を実行します。
        Owner.Motor.Move(
            Owner.InputReader.MoveInput,
            m_moveParameters,
            Time.fixedDeltaTime);
    }

    /// <summary>
    /// 攻撃状態への遷移前に攻撃SEを開始し、
    /// 歩行SEを停止します。
    /// </summary>
    private void StartAttackAudioBeforeTransition()
    {
        bool isBoostAttack =
            Owner.IsBoostSuspended &&
            Owner.SuspendedBoostGaugeRate > 0.0f;

        PlayerAudioActionID attackActionID =
            PlayerAudioActionUtility.GetAttackActionID(
                isBoostAttack);

        bool hasPlayedAttackAudio =
            PlayerAudioActionUtility.PlaySequential(
                Owner,
                attackActionID);

        // ブースト攻撃SEが未登録の場合は、
        // 通常攻撃SEへフォールバックします。
        if (!hasPlayedAttackAudio &&
            attackActionID ==
                PlayerAudioActionID.ATTACKING_BOOST)
        {
            PlayerAudioActionUtility.PlaySequential(
                Owner,
                PlayerAudioActionID.ATTACKING_NORMAL);
        }

        // 攻撃SEの開始時に歩行SEを即座に停止します。
        // 終了音は鳴らしません。
        PlayerAudioActionUtility.StopSequential(
            Owner,
            PlayerAudioActionID.WALKING,
            true);
    }

    /// <summary>
    /// チャージボタン押下時の移動方向を保持し、
    /// 移動チャージの開始判定に使用します。
    /// </summary>
    private void UpdateMovingChargeStartDirection()
    {
        if (!Owner.InputReader.IsVBoostHeld)
        {
            m_isWaitingForMovingCharge = false;
            return;
        }

        if (m_isWaitingForMovingCharge ||
            !Owner.InputReader.ConsumeVBoostStarted())
        {
            return;
        }

        m_movingChargeStartDirection =
            Owner.Motor.HorizontalDirection;

        m_isWaitingForMovingCharge = true;
    }

    /// <summary>
    /// 状態終了時に歩行アニメーションを停止します。
    /// </summary>
    protected override void OnExitState()
    {
        // 歩行SEはここでは停止しません。
        // ・JumpingStateでは歩行SEを継続します。
        // ・IdlingStateへの遷移前に停止します。
        // ・AttackingStateへの遷移前に停止します。
        // ・GrindingStateへの遷移前に停止します。
        // ・BoostChargingStateへの遷移時は遷移先で管理します。
        Owner.AnimationPresenter.StopWalkAnimation();
    }
}
