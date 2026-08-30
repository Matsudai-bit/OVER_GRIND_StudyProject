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

    // チャージ不足時でも保証する最低のブースト効果割合
    // （チャージ率が低いほどこの値に近づき、
    // 　1.0チャージでアセット本来の値になる）
    private const float MIN_BOOST_EFFECT_RATE = 0.4f;

    // 初速ブーストの移動パラメータ
    private PlayerMoveParameters m_initialBoostParameters;

    // 安定ブーストの移動パラメータ
    private PlayerMoveParameters m_stableBoostParameters;

    // チャージ率に応じて調整された初速ブースト時間
    private float m_initialBoostDuration;

    // チャージ率に応じて調整された安定ブースト時間
    private float m_stableBoostDuration;

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

        PlayerMoveParameters baseInitialParameters =
            parameterAsset.CreateInitialBoostParameters();

        PlayerMoveParameters baseStableParameters =
            parameterAsset.CreateStableBoostParameters();

        // チャージ率が低いほど効果を弱める倍率を計算する
        // （MIN_BOOST_EFFECT_RATE ～ 1.0 の範囲で変動）
        float boostEffectRate =
            Mathf.Lerp(
                MIN_BOOST_EFFECT_RATE,
                1.0f,
                Owner.LastBoostChargeRate);

        // 速度をチャージ率に応じてスケーリングする
        m_initialBoostParameters =
            new PlayerMoveParameters(
                baseInitialParameters.MaxMoveSpeed *
                    boostEffectRate,
                baseInitialParameters.TimeToMaxSpeed,
                baseInitialParameters.TimeToStop,
                baseInitialParameters.RotationSpeed);

        m_stableBoostParameters =
            new PlayerMoveParameters(
                baseStableParameters.MaxMoveSpeed *
                    boostEffectRate,
                baseStableParameters.TimeToMaxSpeed,
                baseStableParameters.TimeToStop,
                baseStableParameters.RotationSpeed);

        // 持続時間もチャージ率に応じてスケーリングする
        m_initialBoostDuration =
            parameterAsset.InitialBoostDuration *
            boostEffectRate;

        m_stableBoostDuration =
            parameterAsset.StableBoostDuration *
            boostEffectRate;

        Debug.Log(
            $"[PlayerVRunningState] ブースト開始 " +
            $"チャージ率={Owner.LastBoostChargeRate:P1} " +
            $"効果倍率={boostEffectRate:F2} " +
            $"初速最高速度={m_initialBoostParameters.MaxMoveSpeed:F2} " +
            $"安定最高速度={m_stableBoostParameters.MaxMoveSpeed:F2} " +
            $"初速持続={m_initialBoostDuration:F2}秒 " +
            $"安定持続={m_stableBoostDuration:F2}秒",
            Owner);

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
        if (m_elapsedTime < m_initialBoostDuration)
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
        if (m_elapsedTime < m_stableBoostDuration)
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