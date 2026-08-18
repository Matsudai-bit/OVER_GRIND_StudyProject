using UnityEngine;

/// <summary>
/// プレイヤーのVブースト移動状態を管理します。
/// </summary>
public sealed class PlayerVRunningState
    : StateBase<PlayerStateMachineComponent>
{
    /// <summary>
    /// Vブースト内部フェーズ。
    /// </summary>
    private enum VBoostPhase
    {
        INITIAL_BOOST,
        STABLE_BOOST
    }

    // 初速ブーストの移動パラメータ
    private PlayerMoveParameters m_initialBoostParameters;

    // 安定ブーストの移動パラメータ
    private PlayerMoveParameters m_stableBoostParameters;

    // 現在のブーストフェーズ
    private VBoostPhase m_currentPhase;

    // 現在フェーズの経過時間
    private float m_elapsedTime;

    /// <summary>
    /// 状態開始時に呼ばれます。
    /// </summary>
    protected override void OnStartState()
    {
        PlayerVBoostMovementParameterAsset parameterAsset =
            Owner.VBoostMovementParameterAsset;

        m_initialBoostParameters =
            parameterAsset.CreateInitialBoostParameters();

        m_stableBoostParameters =
            parameterAsset.CreateStableBoostParameters();

        m_currentPhase =
            VBoostPhase.INITIAL_BOOST;

        m_elapsedTime = 0.0f;

        Owner.AnimationPresenter.PlayWalkAnimation();
    }

    /// <summary>
    /// 一定間隔の更新処理を行います。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        // 再入力は次回ブーストとして持ち越さない
        Owner.InputReader.ConsumeVBoostInput();

        // 攻撃入力を確認
        if (Owner.InputReader.ConsumeAttackInput())
        {
            Machine.ChangeState<PlayerAttackingState>();
            return;
        }

        // 移動入力がなくなったら待機状態へ遷移
        if (!Owner.InputReader.HasMoveInput)
        {
            Machine.ChangeState<PlayerIdlingState>();
            return;
        }

        // ジャンプ入力を確認
        if (Owner.Monitor.IsGrounded &&
            Owner.InputReader.HasJumpInput)
        {
            Machine.ChangeState<PlayerJumpingState>();
            return;
        }

        // 現在のフェーズに対応したパラメータで移動
        PlayerMoveParameters moveParameters =
            GetCurrentMoveParameters();

        Owner.Motor.Move(
            Owner.InputReader.MoveInput,
            moveParameters,
            Time.fixedDeltaTime);

        UpdateBoostPhase(Time.fixedDeltaTime);
    }

    /// <summary>
    /// 状態終了時に呼ばれます。
    /// </summary>
    protected override void OnExitState()
    {
        Owner.AnimationPresenter.StopWalkAnimation();
    }

    /// <summary>
    /// 現在のブーストフェーズを更新します。
    /// </summary>
    /// <param name="deltaTime">物理更新の経過時間。</param>
    private void UpdateBoostPhase(float deltaTime)
    {
        m_elapsedTime += deltaTime;

        switch (m_currentPhase)
        {
            case VBoostPhase.INITIAL_BOOST:
                UpdateInitialBoostPhase();
                break;

            case VBoostPhase.STABLE_BOOST:
                UpdateStableBoostPhase();
                break;
        }
    }

    /// <summary>
    /// 初速ブーストフェーズを更新します。
    /// </summary>
    private void UpdateInitialBoostPhase()
    {
        if (m_elapsedTime <
            Owner.VBoostMovementParameterAsset.InitialBoostDuration)
        {
            return;
        }

        m_currentPhase =
            VBoostPhase.STABLE_BOOST;

        m_elapsedTime = 0.0f;
    }

    /// <summary>
    /// 安定ブーストフェーズを更新します。
    /// </summary>
    private void UpdateStableBoostPhase()
    {
        if (m_elapsedTime <
            Owner.VBoostMovementParameterAsset.StableBoostDuration)
        {
            return;
        }

        // Vブースト終了後は通常歩行へ戻る
        Machine.ChangeState<PlayerWalkingState>();
    }

    /// <summary>
    /// 現在のフェーズに対応した移動パラメータを取得します。
    /// </summary>
    /// <returns>現在使用する移動パラメータ。</returns>
    private PlayerMoveParameters GetCurrentMoveParameters()
    {
        switch (m_currentPhase)
        {
            case VBoostPhase.INITIAL_BOOST:
                return m_initialBoostParameters;

            case VBoostPhase.STABLE_BOOST:
                return m_stableBoostParameters;

            default:
                return m_stableBoostParameters;
        }
    }
}