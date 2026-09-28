using UnityEngine;

/// <summary>被弾による吹き飛びと復帰待機を管理し、その間の操作を無効にします。</summary>
public sealed class PlayerHitState : StateBase<PlayerStateMachineComponent>
{
    private readonly Vector3 m_attackCenter;
    private readonly float m_horizontalSpeed;
    private readonly float m_liftSpeed;
    private readonly float m_duration;
    private readonly float m_recoveryDuration;
    private float m_elapsedTime;
    private float m_recoveryElapsedTime;
    private bool m_isRecovering;

    /// <summary>被弾時点の中心と確定済みの速度・時間を保持します。</summary>
    public PlayerHitState(Vector3 attackCenter, float horizontalSpeed, float liftSpeed,
        float duration, float recoveryDuration)
    {
        m_attackCenter = attackCenter;
        m_horizontalSpeed = horizontalSpeed;
        m_liftSpeed = liftSpeed;
        m_duration = duration;
        m_recoveryDuration = recoveryDuration;
    }

    /// <summary>攻撃やグラインドを中断し、攻撃中心から離れる速度を適用します。</summary>
    protected override void OnStartState()
    {
        if (Owner.GrindController.IsGrinding)
        {
            Owner.GrindController.StopGrind();
        }
        Owner.AttackController.DisableAttackHitboxes();
        Owner.IsBoostSuspended = false;
        Owner.CarriedBoostGaugeRate = 0.0f;
        Owner.SuspendedBoostGaugeRate = 0.0f;
        Owner.ClearSpeedDisplayOverride();
        if (Owner.VGaugeUI != null)
        {
            Owner.VGaugeUI.SetGaugeRate(0.0f);
            Owner.VGaugeUI.SetCharging(false);
        }
        Owner.InputReader.DiscardInput();
        Owner.AnimationPresenter.PlayHitAnimation();
        Owner.Motor.ApplyKnockback(m_attackCenter, m_horizontalSpeed, m_liftSpeed);
        // 倍率0でも被弾・無敵・復帰待機は発生させ、吹き飛びだけを省略します。
        m_isRecovering = m_horizontalSpeed <= 0.0f && m_liftSpeed <= 0.0f;
    }

    /// <summary>被弾中の入力を破棄し、復帰後への先行入力を防ぎます。</summary>
    protected override void OnUpdate(float deltaTime)
    {
        Owner.InputReader.DiscardInput();
    }

    /// <summary>着地・壁衝突・時間経過で吹き飛びを終了し、停止状態へ戻します。</summary>
    protected override void OnFixedUpdate()
    {
        if (!m_isRecovering)
        {
            m_elapsedTime += Time.fixedDeltaTime;
            bool hasLanded = Owner.Monitor.IsGrounded && Owner.Motor.VerticalVelocity <= 0.0f;
            if (!hasLanded && !Owner.HasHitEnvironment && m_elapsedTime < m_duration)
            {
                return;
            }

            Owner.Motor.StopKnockback();
            m_isRecovering = true;
        }

        // 空中で時間切れになった場合も、重力による落下は継続します。
        Owner.Motor.StopImmediately();
        m_recoveryElapsedTime += Time.fixedDeltaTime;
        if (m_recoveryElapsedTime >= m_recoveryDuration)
        {
            Machine.ChangeState<PlayerIdlingState>();
        }
    }

    /// <summary>被弾中の先行入力と無敵状態を解除します。</summary>
    protected override void OnExitState()
    {
        Owner.InputReader.DiscardInput();
        Owner.EndHitReaction();
    }
}
