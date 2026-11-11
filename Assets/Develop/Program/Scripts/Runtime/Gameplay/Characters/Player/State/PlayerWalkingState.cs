using UnityEngine;

/// <summary>
/// �v���C���[�̒ʏ���s��Ԃ��Ǘ����܂��B
/// </summary>
public sealed class PlayerWalkingState
    : StateBase<PlayerStateMachineComponent>
{
    // �ʏ�ړ��p�����[�^
    private PlayerMoveParameters m_moveParameters;
    private Vector3 m_movingChargeStartDirection;
    private bool m_isWaitingForMovingCharge;

    /// <summary>
    /// ��ԊJ�n���ɌĂ΂�܂��B
    /// </summary>
    protected override void OnStartState()
    {
        m_isWaitingForMovingCharge = false;

        m_moveParameters =
            Owner.MovementParameterAsset.CreateMoveParameters();

        Owner.AnimationPresenter.PlayWalkAnimation();

        // �ʏ�ړ����̓W�����vState�֑J�ڂ��Ă��p�����邽�߁A
        // State���[�J���ł͂Ȃ�ActionID�P�ʂōĐ����܂��B
        PlayerAudioActionUtility.PlaySequential(
            Owner,
            PlayerAudioActionID.WALKING);
    }

    /// <summary>
    /// ���Ԋu�̍X�V�������s���܂��B
    /// </summary>
    protected override void OnFixedUpdate()
    {
        // �U�����͂��m�F
        if (Owner.InputReader.ConsumeAttackInput() &&
            Owner.Monitor.IsGrounded)
        {
            // State�J�ڌ�ɍU�������J�n����ƁA
            // WALKING�̑�����~����ɕ������ĉ����r�؂�₷�����߁A
            // �J�ڗv���O�ɍU�������s�Đ����܂��B
            StartAttackAudioBeforeTransition();

            Machine.ChangeState<PlayerAttackingState>();
            return;
        }

        // �W�����v���͂��ړ����͂Ȃ��������ɏ������܂��B
        // �W�����v�J�n�Ɠ����ɃX�e�B�b�N�𗣂��Ă��A
        // WALKING�����~����JumpingState�ֈ����p���܂��B
        if (Owner.Monitor.CanStartJump &&
            Owner.InputReader.HasJumpInput)
        {
            Machine.ChangeState<PlayerJumpingState>();
            return;
        }

        if (Owner.GrindController.CanStartGrind(
            Owner.Monitor.IsRailed ? Owner.Monitor.HitRailInfo : null))
        {
            Machine.ChangeState<PlayerGrindingState>(Owner.Monitor.HitRailInfo);
        // �ړ����͂��Ȃ����Walking�����~���đҋ@��Ԃ֑J��
        if (!Owner.InputReader.HasMoveInput)
        {
            // �ʏ���s���Ɉړ����͂��Ȃ��Ȃ����ꍇ�́A
            // Walking�����I�����֑J�ڂ����Ē�~���܂��B
            //
            // JumpingState�ւ̑J�ڂł͂��̕����ʂ�Ȃ����߁A
            // �W�����v����Walking����炵�����܂��B
            PlayerAudioActionUtility.StopSequential(
                Owner,
                PlayerAudioActionID.WALKING);

            // V�u�[�X�g���͂��c��Ȃ��悤�ɏ���
            Owner.InputReader.ConsumeVBoostInput();

            Machine.ChangeState<PlayerIdlingState>();
            return;
        }

        // V�u�[�X�g�̒������������m�F�B
        // �������V�K�̃u�[�X�g�J�n�͐ڒn���̂݋�����
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

        // �ʏ�ړ�
        Owner.Motor.Move(
            Owner.InputReader.MoveInput,
            m_moveParameters,
            Time.fixedDeltaTime);
    }

    /// <summary>
    /// WalkingState����U��State�֑J�ڂ��钼�O�ɁA
    /// �U�������s�Đ����Ĉړ����Ƃ̐؂�ւ������炩�ɂ��܂��B
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

        // �u�[�X�g�U����p�������o�^�̏ꍇ��
        // �ʏ�U�����փt�H�[���o�b�N���܂��B
        if (!hasPlayedAttackAudio &&
            attackActionID ==
                PlayerAudioActionID.ATTACKING_BOOST)
        {
            PlayerAudioActionUtility.PlaySequential(
                Owner,
                PlayerAudioActionID.ATTACKING_NORMAL);
        }

        // �U�����̍Đ��v�����o������ňړ����𑦎���~���܂��B
        // �I�����͖炵�܂���B
        PlayerAudioActionUtility.StopSequential(
            Owner,
            PlayerAudioActionID.WALKING,
            true);
    }

    /// <summary>�`���[�W�{�^���������̎��ړ��������J�n����p�ɕێ����܂��B</summary>
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
    /// ��ԏI�����ɌĂ΂�܂��B
    /// </summary>
    protected override void OnExitState()
    {
        // �ړ����͂����ł͒�~���܂���B
        // �EJumpingState�֑J�ڂ����ꍇ�͖炵������
        // �EIdlingState�ł͊��S��~���ɏI������炵�Ē�~����
        // �EAttack / Charge���͑J�ڐ悪������~����
        Owner.AnimationPresenter.StopWalkAnimation();
    }
}
