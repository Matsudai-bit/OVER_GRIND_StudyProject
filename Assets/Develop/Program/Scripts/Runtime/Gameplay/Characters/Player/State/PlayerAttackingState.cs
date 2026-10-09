using UnityEngine;

/// <summary>接地中の連続攻撃と通常移動・ジャンプへの遷移を管理します。</summary>
public sealed class PlayerAttackingState : StateBase<PlayerStateMachineComponent>
{
    private PlayerContinuousAttack m_attack;
    private bool m_isTransitionPending;

    /// <summary>接地を確認して地上攻撃を開始します。</summary>
    protected override void OnStartState()
    {
        // 攻撃へ遷移した時点で、未消費のチャージ開始要求を破棄します。
        // ダッシュ中の入力イベントが攻撃終了後に再利用されることを防ぎます。
        Owner.InputReader.DiscardVBoostPendingInput();

        if (!Owner.Monitor.IsGrounded)
        {
            // 状態開始中は遷移を予約せず、次の更新で終了します。
            return;
        }
        m_attack = new PlayerContinuousAttack(Owner, false);
        m_attack.StartAttack();
    }

    /// <summary>入力解除時に攻撃を終了します。</summary>
    /// <param name="deltaTime">描画更新の間隔。</param>
    protected override void OnUpdate(float deltaTime)
    {
        if (!m_isTransitionPending && (m_attack == null || !Owner.InputReader.IsAttackHeld)) FinishAttack();
    }

    /// <summary>接地中のみ攻撃を続け、ジャンプ時は攻撃判定を即座に終了します。</summary>
    protected override void OnFixedUpdate()
    {
        if (m_isTransitionPending) return;
        Owner.InputReader.ConsumeAttackInput();
        if (m_attack == null || !Owner.Monitor.IsGrounded)
        {
            FinishAttack();
            return;
        }
        if (!m_attack.UpdateAttack(Time.fixedDeltaTime))
        {
            FinishAttack();
            return;
        }
        if (Owner.Monitor.CanStartJump &&
            Owner.InputReader.HasJumpInput)
        {
            m_isTransitionPending = true;
            StopAttack();
            Machine.ChangeState<PlayerJumpingState>();
        }
    }

    /// <summary>攻撃を終了し、通常移動へ戻します。</summary>
    private void FinishAttack()
    {
        m_isTransitionPending = true;
        StopAttack();
        Machine.ChangeState<PlayerIdlingState>();
    }

    /// <summary>遷移待ちの物理更新でも攻撃判定が残らないようにします。</summary>
    private void StopAttack()
    {
        m_attack?.StopAttack();
        m_attack = null;
        // 攻撃中に発生したチャージ入力は、攻撃終了後へ持ち越しません。
        Owner.InputReader.DiscardVBoostPendingInput();
    }

    /// <summary>被弾などによる中断時も攻撃を終了します。</summary>
    protected override void OnExitState()
    {
        StopAttack();
    }
}
