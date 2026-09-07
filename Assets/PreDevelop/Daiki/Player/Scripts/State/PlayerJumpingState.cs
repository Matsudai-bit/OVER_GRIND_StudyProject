using UnityEngine;

/// <summary>
/// �v���C���[�̃W�����v��Ԃ��Ǘ����܂��B
/// </summary>
public sealed class PlayerJumpingState
    : StateBase<PlayerStateMachineComponent>
{
    // �W�����v�J�n����̌o�ߎ���
    private float m_elapsedTime;

    /// <summary>
    /// ��ԊJ�n���ɌĂ΂�܂��B
    /// </summary>
    protected override void OnStartState()
    {
        m_elapsedTime = 0.0f;

        Owner.AnimationPresenter.PlayJumpAnimation();

        // �W�����v�����͊J�n���Ɉ�x�����^����
        Owner.Motor.Jump(
            Owner.MovementParameterAsset.JumpPower);
    }

    /// <summary>
    /// ���Ԋu�̍X�V�������s���܂��B
    /// </summary>
    protected override void OnFixedUpdate()
    {

        Debug.Log(
      $"[Jump] y-vel={Owner.Motor.VerticalVelocity:F3}, " +
      $"y-pos={Owner.transform.position.y:F3}, " +
      $"deltaTime={Time.fixedDeltaTime:F4}, " +
      $"gravity.y={Physics.gravity.y:F3}");

        m_elapsedTime += Time.fixedDeltaTime;

        PlayerMovementParameterAsset parameterAsset =
            Owner.MovementParameterAsset;

        bool isJumpHeld =
            m_elapsedTime <
                parameterAsset.JumpInputDuration &&
            Owner.InputReader.HasJumpInput;

        Owner.Motor.ApplyExtraGravity(
            parameterAsset,
            isJumpHeld,
            Time.fixedDeltaTime);

        // ���_���߂��ė����ɓ]�������ԑJ��
        if (Owner.Motor.VerticalVelocity <= 0.0f)
        {
            Machine.ChangeState<PlayerIdlingState>();
        }


        // �W�����v�O��V�u�[�X�g��Ԃ������ꍇ�́A
        // �ҋ@��Ԃ��o�R��������V�u�[�X�g�֕��A����
        if (Owner.IsBoostSuspended)
        {
            Machine.ChangeState<PlayerVRunningState>();
            return;
        }

        Machine.ChangeState<PlayerIdlingState>();

    }

    /// <summary>
    /// ��ԏI�����ɌĂ΂�܂��B
    /// </summary>
    protected override void OnExitState()
    {
        Owner.AnimationPresenter.StopJumpAnimation();
    }
}