using UnityEngine;

/// <summary>
/// プレイヤーの通常歩行状態を管理します。
/// </summary>
public sealed class PlayerWalkingState
    : StateBase<PlayerStateMachineComponent>
{
    // 通常移動パラメータ
    private PlayerMoveParameters m_moveParameters;
    private Vector3 m_movingChargeStartDirection;
    private bool m_isWaitingForMovingCharge;

    /// <summary>
    /// 状態開始時に呼ばれます。
    /// </summary>
    protected override void OnStartState()
    {
        m_isWaitingForMovingCharge = false;

        m_moveParameters =
            Owner.MovementParameterAsset.CreateMoveParameters();

        Owner.AnimationPresenter.PlayWalkAnimation();

        // 通常移動音はジャンプStateへ遷移しても継続するため、
        // StateローカルではなくActionID単位で再生します。
        PlayerAudioActionUtility.PlaySequential(
            Owner,
            PlayerAudioActionID.WALKING);
    }

    /// <summary>
    /// 一定間隔の更新処理を行います。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        // 攻撃入力を確認
        if (Owner.InputReader.ConsumeAttackInput() &&
            Owner.Monitor.IsGrounded)
        {
            // State遷移後に攻撃音を開始すると、
            // WALKINGの即時停止が先に聞こえて音が途切れやすいため、
            // 遷移要求前に攻撃音を先行再生します。
            StartAttackAudioBeforeTransition();

            Machine.ChangeState<PlayerAttackingState>();
            return;
        }

        // ジャンプ入力を移動入力なし判定より先に処理します。
        // ジャンプ開始と同時にスティックを離しても、
        // WALKING音を停止せずJumpingStateへ引き継ぎます。
        if (Owner.Monitor.CanStartJump &&
            Owner.InputReader.HasJumpInput)
        {
            Machine.ChangeState<PlayerJumpingState>();
            return;
        }

        // 移動入力がなければWalking音を停止して待機状態へ遷移
        if (!Owner.InputReader.HasMoveInput)
        {
            // 通常歩行中に移動入力がなくなった場合は、
            // Walking音を終了音へ遷移させて停止します。
            //
            // JumpingStateへの遷移ではこの分岐を通らないため、
            // ジャンプ中はWalking音を鳴らし続けます。
            PlayerAudioActionUtility.StopSequential(
                Owner,
                PlayerAudioActionID.WALKING);

            // Vブースト入力が残らないように消費
            Owner.InputReader.ConsumeVBoostInput();

            Machine.ChangeState<PlayerIdlingState>();
            return;
        }

        // Vブーストの長押し成立を確認。
        // ただし新規のブースト開始は接地中のみ許可する
        UpdateMovingChargeStartDirection();

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

        // 通常移動
        Owner.Motor.Move(
            Owner.InputReader.MoveInput,
            m_moveParameters,
            Time.fixedDeltaTime);
    }

    /// <summary>
    /// WalkingStateから攻撃Stateへ遷移する直前に、
    /// 攻撃音を先行再生して移動音との切り替えを滑らかにします。
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

        // ブースト攻撃専用音が未登録の場合は
        // 通常攻撃音へフォールバックします。
        if (!hasPlayedAttackAudio &&
            attackActionID ==
                PlayerAudioActionID.ATTACKING_BOOST)
        {
            PlayerAudioActionUtility.PlaySequential(
                Owner,
                PlayerAudioActionID.ATTACKING_NORMAL);
        }

        // 攻撃音の再生要求を出した後で移動音を即時停止します。
        // 終了音は鳴らしません。
        PlayerAudioActionUtility.StopSequential(
            Owner,
            PlayerAudioActionID.WALKING,
            true);
    }

    /// <summary>チャージボタン押下時の実移動方向を開始判定用に保持します。</summary>
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
    /// 状態終了時に呼ばれます。
    /// </summary>
    protected override void OnExitState()
    {
        // 移動音はここでは停止しません。
        // ・JumpingStateへ遷移した場合は鳴らし続ける
        // ・IdlingStateでは完全停止時に終了音を鳴らして停止する
        // ・Attack / Charge等は遷移先が即時停止する
        Owner.AnimationPresenter.StopWalkAnimation();
    }
}
