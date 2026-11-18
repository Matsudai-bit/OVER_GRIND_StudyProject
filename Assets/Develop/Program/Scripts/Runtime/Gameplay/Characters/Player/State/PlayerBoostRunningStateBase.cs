using UnityEngine;

/// <summary>ブーストダッシュと通常ブースト移動に共通する入力と演出を管理します。</summary>
public abstract class PlayerBoostRunningStateBase : StateBase<PlayerStateMachineComponent>
{
    private const float MIN_BOOST_DURATION = 0.01f;

    private EffectHandle m_loopEffect;
    private bool m_isHandingOff;

    /// <summary>通常ブースト移動中に入力がなくなった場合だけ停止します。</summary>
    protected abstract bool StopsWithoutMoveInput { get; }

    /// <summary>状態ごとのブースト移動を更新します。</summary>
    protected abstract void UpdateBoostMovement();

    /// <summary>ブーストゲージの消費と共通演出を開始します。</summary>
    /// <param name="isHandoff">ダッシュからの引き継ぎならtrue。</param>
    /// <param name="loopEffect">引き継ぐ継続エフェクト。</param>
    protected void StartBoost(bool isHandoff = false, EffectHandle loopEffect = default)
    {
        m_isHandingOff = false;
        float duration = Mathf.Max(
            Owner.VBoostMovementParameterAsset.StableBoostDuration,
            MIN_BOOST_DURATION);
        Owner.SetBoostGaugeDepletionRate(1.0f / duration);

        if (isHandoff)
        {
            m_loopEffect = loopEffect;
            return;
        }

        PlayerAudioActionUtility.StopSequential(
            Owner, PlayerAudioActionID.WALKING, true);
        PlayerAudioActionUtility.PlaySequential(
            Owner, PlayerAudioActionID.V_RUNNING);
        Owner.AnimationPresenter.PlayVBoostRunningAnimation();
        Owner.VGaugePlaceModel.SetGaugeRate(Owner.SuspendedBoostGaugeRate);
        Owner.EffectEmitter.Play(EffectId.PLAYER_MOVE_VBOOST_I);
        m_loopEffect = Owner.EffectEmitter.Play(EffectId.PLAYER_MOVE_VBOOST_L);
    }

    /// <summary>ダッシュ演出を維持したまま通常ブースト移動へ渡します。</summary>
    protected void HandOffToStableBoost()
    {
        m_isHandingOff = true;
        Machine.ChangeState<PlayerVRunningState>(m_loopEffect);
    }

    /// <summary>攻撃、ゲージ切れ、ジャンプ、レール移動を判定してから移動します。</summary>
    protected override void OnFixedUpdate()
    {
        if (m_isHandingOff) return;

        // 接地前の押下を消費せず、ダッシュ終端の一時的な非接地を跨いで保持する。
        if (Owner.Monitor.IsGrounded &&
            Owner.InputReader.IsAttackHeld &&
            Owner.InputReader.ConsumeAttackInput())
        {
            SuspendBoost();
            PlayerAudioActionUtility.StopSequential(
                Owner, PlayerAudioActionID.V_RUNNING, true);
            if (Owner.IsBoostSuspended && Owner.SuspendedBoostGaugeRate > 0.0f)
                Machine.ChangeState<PlayerChargeAttackingState>();
            else
                Machine.ChangeState<PlayerAttackingState>();
            return;
        }

        if (Owner.SuspendedBoostGaugeRate <= 0.0f)
        {
            Owner.InputReader.ConsumeVBoostInput();
            Machine.ChangeState<PlayerWalkingState>();
            return;
        }

        if (StopsWithoutMoveInput && !Owner.InputReader.HasMoveInput)
        {
            SuspendBoost();
            Machine.ChangeState<PlayerIdlingState>();
            return;
        }

        if (Owner.Monitor.CanStartJump && Owner.InputReader.HasJumpInput)
        {
            SuspendBoost();
            Machine.ChangeState<PlayerJumpingState>();
            return;
        }

        if (Owner.GrindController.CanStartGrind(
            Owner.Monitor.IsRailed ? Owner.Monitor.HitRailInfo : null))
        {
            SuspendBoost();
            Machine.ChangeState<PlayerGrindingState>(Owner.Monitor.HitRailInfo);
            return;
        }

        UpdateBoostMovement();
    }

    /// <summary>ブースト中断時に残量を保持します。</summary>
    private void SuspendBoost()
    {
        Owner.IsBoostSuspended = true;
    }

    /// <summary>ブースト演出を終了します。ダッシュからの引き継ぎ時は維持します。</summary>
    protected override void OnExitState()
    {
        if (m_isHandingOff) return;

        Owner.AnimationPresenter.StopVBoostRunningAnimation();
        m_loopEffect.Stop();
        Owner.EffectEmitter.Play(EffectId.PLAYER_MOVE_VBOOST_O);

        if (Owner.IsBoostSuspended) return;

        PlayerAudioActionUtility.StopSequential(
            Owner, PlayerAudioActionID.V_RUNNING);
        Owner.VGaugePlaceModel.SetGaugeRate(0.0f);
    }
}
