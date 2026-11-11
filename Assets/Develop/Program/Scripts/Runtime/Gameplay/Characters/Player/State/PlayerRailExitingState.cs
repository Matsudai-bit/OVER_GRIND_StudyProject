using UnityEngine;

/// <summary>
/// 衝突によるレール離脱中は、上昇・横移動の初速を保って着地を待ちます。
/// </summary>
public sealed class PlayerRailExitingState
    : StateBase<PlayerStateMachineComponent>
{
    private readonly bool m_startAirAttack;
    private float m_elapsedTime;
    private PlayerContinuousAttack m_airAttack;
    private bool m_hasStartedAttackWhileHeld;

    // 離脱中の空中攻撃で使用する音声ActionID
    private PlayerAudioActionID m_attackAudioActionID =
        PlayerAudioActionID.NONE;

    /// <summary>Creates a rail exit state for collision departure.</summary>
    public PlayerRailExitingState()
    {
        m_startAirAttack = false;
    }

    /// <summary>Creates a rail exit state that can start an air attack.</summary>
    /// <param name="startAirAttack">True to start the attack while its input is held.</param>
    public PlayerRailExitingState(bool startAirAttack)
    {
        m_startAirAttack = startAirAttack;
    }

    /// <summary>
    /// 衝突時の初速を維持して離脱用のジャンプアニメーションを開始します。
    /// </summary>
    protected override void OnStartState()
    {
        m_elapsedTime = 0f;
        Owner.AnimationPresenter.PlayJumpAnimation(PlayerJumpAnimationType.RAIL);
        if (m_startAirAttack && Owner.InputReader.IsAttackHeld)
        {
            StartAirAttack();
        }
    }

    /// <summary>
    /// 重力による下降後の着地を検出し、通常状態へ戻します。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        m_elapsedTime += Time.fixedDeltaTime;
        Owner.GrindController.UpdateCollisionExit(Time.fixedDeltaTime);
        Owner.InputReader.ConsumeAttackInput();
        if (!Owner.InputReader.IsAttackHeld)
        {
            m_hasStartedAttackWhileHeld = false;
        }
        else if (!m_hasStartedAttackWhileHeld &&
                 m_airAttack == null && !Owner.Monitor.IsGrounded)
        {
            StartAirAttack();
        }
        if (m_airAttack != null && !m_airAttack.UpdateAttack(Time.fixedDeltaTime))
        {
            StopAirAttack();
        }

        // 敵の面やレール付近を接地と判定しても、横への離脱補助中は減速状態に戻さない。
        // 上下方向はRigidbodyの重力と衝突に任せる。
        if (!Owner.GrindController.IsCollisionExitSeparating &&
            m_elapsedTime >= Owner.GrindController.CollisionExitLandingDelay &&
            Owner.Motor.VerticalVelocity <= 0f && Owner.Monitor.IsGrounded &&
            (m_airAttack == null || !m_airAttack.IsAirHitStopping))
        {
            StopAirAttack();
            Machine.ChangeState<PlayerIdlingState>();
        }
    }

    /// <summary>レール離脱中の空中攻撃を開始します。</summary>
    private void StartAirAttack()
    {
        m_hasStartedAttackWhileHeld = true;

        bool isBoostAttack =
            Owner.IsBoostSuspended &&
            Owner.SuspendedBoostGaugeRate > 0.0f;

        m_attackAudioActionID =
            PlayerAudioActionUtility.GetAttackActionID(
                isBoostAttack);

        PlayerAudioActionUtility.PlaySequential(
            Owner,
            m_attackAudioActionID);

        m_airAttack =
            new PlayerContinuousAttack(
                Owner,
                true);

        m_airAttack.StartAttack();
    }

    /// <summary>
    /// 離脱中の搭乗制限とジャンプアニメーションを解除します。
    /// </summary>
    protected override void OnExitState()
    {
        Owner.GrindController.FinishCollisionExit();
        StopAirAttack();
        Owner.AnimationPresenter.StopJumpAnimation();
        Owner.AnimationPresenter.StopAerialAttackingAnimation();
    }

    /// <summary>Stops the air attack and clears its hitboxes.</summary>
    private void StopAirAttack()
    {
        if (m_airAttack == null)
        {
            return;
        }

        m_airAttack.StopAttack();
        m_airAttack = null;

        if (m_attackAudioActionID !=
            PlayerAudioActionID.NONE)
        {
            PlayerAudioActionUtility.StopSequential(
                Owner,
                m_attackAudioActionID);

            m_attackAudioActionID =
                PlayerAudioActionID.NONE;
        }
    }
}
