using UnityEngine;

/// <summary>
/// 接地中の連続攻撃を管理します。
/// 攻撃の継続判定、エフェクト再生、通常状態への遷移を担当します。
/// </summary>
public sealed class PlayerAttackingState
    : StateBase<PlayerStateMachineComponent>
{
    // 連続攻撃の制御
    private PlayerContinuousAttack m_attack;

    // 状態遷移要求済みか
    private bool m_isTransitionPending;

    // 今回の攻撃がチャージ攻撃として開始されたか
    private bool m_isChargeAttack;

    // 攻撃ヒット時のエフェクト
    private EffectHandle m_hitEffectHandle;

    /// <summary>
    /// 現在チャージ攻撃を実行中かを取得します。
    /// </summary>
    private bool IsChargeAttacking =>
        m_isChargeAttack &&
        m_attack != null &&
        !m_isTransitionPending;

    /// <summary>
    /// 現在チャージリソースが残っているかを取得します。
    /// </summary>
    private bool HasChargeEnergy =>
        Owner.SuspendedBoostGaugeRate > 0.0f;

    /// <summary>
    /// 状態開始時に呼ばれます。
    /// </summary>
    protected override void OnStartState()
    {
        // --------------------------------------------------------
        // 状態初期化
        // --------------------------------------------------------

        m_isTransitionPending = false;
        m_attack = null;
        m_hitEffectHandle = default;

        // 攻撃開始時点で攻撃種別を確定する。
        // 以降、チャージ残量が0になっても
        // State終了まではチャージ攻撃として扱う。
        m_isChargeAttack =
            Owner.IsBoostSuspended &&
            Owner.SuspendedBoostGaugeRate > 0.0f;

        // 前の状態から持ち越された入力を破棄する。
        Owner.InputReader.DiscardVBoostPendingInput();

        // 前回のアニメーション状態が残らないよう初期化する。
        Owner.AnimationPresenter
            .StopChargeAttackingAnimation();

        Owner.AnimationPresenter
            .StopAttackAnimation();

        // --------------------------------------------------------
        // 攻撃開始可否
        // --------------------------------------------------------

        // 空中では地上攻撃を開始しない。
        if (!Owner.Monitor.IsGrounded)
        {
            return;
        }

        // --------------------------------------------------------
        // 攻撃開始
        // --------------------------------------------------------

        // 連続攻撃本体を生成する。
        m_attack =
            new PlayerContinuousAttack(
                Owner,
                false);

        // 攻撃アニメーションを開始する。
        PlayAttackAnimation();

        // 攻撃判定を開始する。
        m_attack.StartAttack();
    }

    /// <summary>
    /// 毎フレームの更新処理です。
    /// 攻撃入力が解除された場合は攻撃を終了します。
    /// </summary>
    /// <param name="deltaTime">経過時間。</param>
    protected override void OnUpdate(
        float deltaTime)
    {
        if (m_isTransitionPending)
        {
            return;
        }

        // 攻撃が存在しない場合、
        // または攻撃入力が解除された場合は終了する。
        if (m_attack == null ||
            !Owner.InputReader.IsAttackHeld)
        {
            FinishAttack();
        }
    }

    /// <summary>
    /// 一定間隔の更新処理です。
    /// 攻撃判定、状態遷移、エフェクトの再生を行います。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        if (m_isTransitionPending)
        {
            return;
        }

        // 攻撃入力を消費する。
        Owner.InputReader.ConsumeAttackInput();

        // 攻撃継続の可否を確認する。
        if (!CanContinueAttack())
        {
            FinishAttack();
            return;
        }

        // ジャンプへの遷移を確認する。
        if (TryTransitionToJump())
        {
            return;
        }

        // 攻撃エフェクトを更新する。
        UpdateHitEffect();
    }

    /// <summary>
    /// 状態終了時に呼ばれます。
    /// 被弾などによる中断にも対応します。
    /// </summary>
    protected override void OnExitState()
    {
        StopAttack();
    }

    // ============================================================
    // 攻撃制御
    // ============================================================

    /// <summary>
    /// 攻撃を継続できるか確認します。
    /// </summary>
    /// <returns>
    /// 攻撃を継続できる場合はtrue。
    /// </returns>
    private bool CanContinueAttack()
    {
        // 攻撃が存在しない場合は終了する。
        if (m_attack == null)
        {
            return false;
        }

        // 接地していない場合は終了する。
        if (!Owner.Monitor.IsGrounded)
        {
            return false;
        }

        // チャージ残量とは独立して、
        // PlayerContinuousAttack自身の終了条件で判定する。
        return m_attack.UpdateAttack(
            Time.fixedDeltaTime);
    }

    /// <summary>
    /// 現在の攻撃種別に応じたアニメーションを開始します。
    /// </summary>
    private void PlayAttackAnimation()
    {
        // 攻撃開始時に確定した種類を使用する。
        // チャージ残量が途中で0になっても切り替えない。
        if (m_isChargeAttack)
        {
            Owner.AnimationPresenter
                .PlayChargeAttackingAnimation();

            return;
        }

        Owner.AnimationPresenter
            .PlayAttackAnimation();
    }

    /// <summary>
    /// 攻撃を停止します。
    /// </summary>
    private void StopAttack()
    {
        // --------------------------------------------------------
        // 演出停止
        // --------------------------------------------------------

        StopHitEffect();

        // 攻撃開始時の種類に関係なく、
        // 両方を停止してAnimator状態を確実に解除する。
        Owner.AnimationPresenter
            .StopChargeAttackingAnimation();

        Owner.AnimationPresenter
            .StopAttackAnimation();

        // --------------------------------------------------------
        // 攻撃判定停止
        // --------------------------------------------------------

        m_attack?.StopAttack();
        m_attack = null;

        // --------------------------------------------------------
        // 入力整理
        // --------------------------------------------------------

        Owner.InputReader
            .DiscardVBoostPendingInput();

        // --------------------------------------------------------
        // 攻撃種別リセット
        // --------------------------------------------------------

        // 攻撃に関する終了処理がすべて終わった後で解除する。
        m_isChargeAttack = false;
    }

    // ============================================================
    // エフェクト制御
    // ============================================================

    /// <summary>
    /// 攻撃の命中状況に応じてエフェクトを再生します。
    /// </summary>
    private void UpdateHitEffect()
    {
        // 攻撃が存在しない場合は更新しない。
        if (m_attack == null)
        {
            return;
        }

        // 攻撃が命中していなければ再生しない。
        if (!Owner.AttackController
                .IsHittingAnyTarget())
        {
            return;
        }

        // すでにエフェクトが有効なら追加再生しない。
        if (m_hitEffectHandle.IsValid)
        {
            return;
        }

        // 攻撃開始時に確定した種類から
        // 対応するエフェクトを取得する。
        EffectId effectId =
            GetHitEffectId();

        if (effectId == EffectId.NONE)
        {
            return;
        }

        m_hitEffectHandle =
            Owner.EffectEmitter.PlayAttached(
                effectId);
    }

    /// <summary>
    /// 現在の攻撃に対応するエフェクトIDを取得します。
    /// </summary>
    /// <returns>再生するエフェクトID。</returns>
    private EffectId GetHitEffectId()
    {
        // 現在のチャージ残量ではなく、
        // 攻撃開始時に確定した攻撃種別で決定する。
        return m_isChargeAttack
            ? EffectId.PLAYER_ATTACK_VBOOST_NORMAL_L
            : EffectId.PLAYER_ATTACK_NORMAL_L;
    }

    /// <summary>
    /// 攻撃エフェクトを停止します。
    /// </summary>
    private void StopHitEffect()
    {
        m_hitEffectHandle.Stop(
            EffectStopMode.GRACEFUL);

        m_hitEffectHandle = default;
    }

    // ============================================================
    // 状態遷移
    // ============================================================

    /// <summary>
    /// ジャンプへの遷移を試みます。
    /// </summary>
    /// <returns>
    /// 遷移した場合はtrue。
    /// </returns>
    private bool TryTransitionToJump()
    {
        if (!Owner.Monitor.CanStartJump ||
            !Owner.InputReader.HasJumpInput)
        {
            return false;
        }

        m_isTransitionPending = true;

        StopAttack();

        Machine.ChangeState<
            PlayerJumpingState>();

        return true;
    }

    /// <summary>
    /// 攻撃を終了し、待機状態へ遷移します。
    /// </summary>
    private void FinishAttack()
    {
        if (m_isTransitionPending)
        {
            return;
        }

        m_isTransitionPending = true;

        StopAttack();

        Machine.ChangeState<
            PlayerIdlingState>();
    }
}