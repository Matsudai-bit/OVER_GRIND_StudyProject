using UnityEngine;

/// <summary>
/// プレイヤーのVブースト状態を管理します。
/// 解放直後の「ブーストダッシュ」と、その後の「通常移動」の
/// 2フェーズで構成され、チャージしたゲージを消費しきるまで継続します。
/// ゲージの消費自体はPlayerStateMachineComponent側で
/// Stateに関係なく（中断中も）行われます。
/// </summary>
public sealed class PlayerVRunningState
    : StateBase<PlayerStateMachineComponent>
{
    /// <summary>
    /// Vブースト内部フェーズ。
    /// </summary>
    private enum VBoostPhase
    {
        // ブーストダッシュ（固定0.5秒、圧倒的な高速移動）
        BOOST_DASH,

        // 通常移動（ダッシュ終了後、ゲージを消費しきるまで継続）
        NORMAL_MOVE
    }

    // ブーストダッシュの継続時間（仮の固定値）
    private const float BOOST_DASH_DURATION = 0.5f;

    // ブーストダッシュ中の最大移動速度倍率（仮で基本の2倍）
    private const float DASH_SPEED_MULTIPLIER = 2.0f;

    // 現在のブーストフェーズ
    private VBoostPhase m_currentPhase;

    // 現在フェーズの経過時間（ダッシュ終了判定用）
    private float m_elapsedTime;

    // ダッシュ中に使用する移動パラメータ（基本速度の2倍・瞬時到達）
    private PlayerMoveParameters m_dashMoveParameters;

    // ダッシュ終了後に使用する通常移動パラメータ
    private PlayerMoveParameters m_normalMoveParameters;

    /// <summary>
    /// 状態開始時に呼ばれます。
    /// 中断（ジャンプ・停止など）から復帰した場合は、
    /// 中断時点のフェーズから再開します。
    /// </summary>
    protected override void OnStartState()
    {
        PlayerMoveParameters normalParameters =
            Owner.MovementParameterAsset
                .CreateMoveParameters();

        m_dashMoveParameters =
            new PlayerMoveParameters(
                normalParameters.MaxMoveSpeed *
                    DASH_SPEED_MULTIPLIER,
                normalParameters.TimeToMaxSpeed,
                normalParameters.TimeToStop,
                normalParameters.RotationSpeed);

        m_normalMoveParameters = normalParameters;

        // 満タンのゲージが、アセット設定の秒数で
        // 使い切られるよう消費レートを算出し、本体側へ設定する
        float fullTankDuration =
            Mathf.Max(
                Owner.VBoostMovementParameterAsset
                    .StableBoostDuration,
                0.01f);

        Owner.SetBoostGaugeDepletionRate(
            1.0f / fullTankDuration);

        if (Owner.IsBoostSuspended)
        {
            // 中断された状態からの復帰。
            // ダッシュフェーズは終えている前提で
            // 通常移動フェーズから再開する。
            // ゲージ量は中断中も本体側で消費され続けているため、
            // ここで読み直すだけでよい。
            m_currentPhase =
                VBoostPhase.NORMAL_MOVE;

            m_elapsedTime = 0.0f;

            Owner.IsBoostSuspended = false;

            Debug.Log(
                $"[PlayerVRunningState] " +
                $"中断から復帰 " +
                $"残りゲージ量={Owner.SuspendedBoostGaugeRate:P1}",
                Owner);
        }
        else
        {
            // 通常のブーストチャージからの新規開始
            Owner.SuspendedBoostGaugeRate =
                Owner.CarriedBoostGaugeRate;

            m_currentPhase =
                VBoostPhase.BOOST_DASH;

            m_elapsedTime = 0.0f;

            Debug.Log(
                $"[PlayerVRunningState] ブーストダッシュ開始 " +
                $"引き継ぎゲージ量={Owner.SuspendedBoostGaugeRate:P1} " +
                $"ダッシュ最高速度={m_dashMoveParameters.MaxMoveSpeed:F2} " +
                $"（通常速度={normalParameters.MaxMoveSpeed:F2}） " +
                $"ダッシュ時間={BOOST_DASH_DURATION:F2}秒 " +
                $"満タン消費時間={fullTankDuration:F2}秒",
                Owner);
        }

        Owner.AnimationPresenter.PlayWalkAnimation();

        if (Owner.VGaugeUI != null)
        {
            Owner.VGaugeUI.SetGaugeRate(
                Owner.SuspendedBoostGaugeRate);

            Owner.VGaugeUI.SetCharging(true);
        }
    }

    /// <summary>
    /// 一定間隔の更新処理を行います。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        // 攻撃入力を確認
        if (Owner.InputReader.ConsumeAttackInput())
        {
            Machine.ChangeState<PlayerAttackingState>();
            return;
        }

        // ゲージが本体側の消費によって尽きていないか確認
        // （中断復帰直後や、消費が進んで0になった場合はここで検知する）
        if (Owner.SuspendedBoostGaugeRate <= 0.0f)
        {
            Debug.Log(
                "[PlayerVRunningState] " +
                "ゲージを消費しきったため通常歩行へ遷移します。",
                Owner);

            Machine.ChangeState<PlayerWalkingState>();
            return;
        }

        // 移動入力がなくなった場合は、打ち切らずに中断する。
        // 待機中に再び移動入力が入れば復帰できるようにする。
        if (!Owner.InputReader.HasMoveInput)
        {
            SuspendBoost();

            Machine.ChangeState<PlayerIdlingState>();
            return;
        }

        // ジャンプ入力を確認
        // 同様に打ち切らず中断し、着地後に復帰させる
        if (Owner.Monitor.IsGrounded &&
            Owner.InputReader.HasJumpInput)
        {
            SuspendBoost();

            Machine.ChangeState<PlayerJumpingState>();
            return;
        }

        UpdatePhaseMovement();
    }

    /// <summary>
    /// 状態終了時に呼ばれます。
    /// </summary>
    protected override void OnExitState()
    {
        Owner.AnimationPresenter.StopWalkAnimation();

        // 中断による終了の場合は、後で再開するため
        // ゲージ表示・演出をリセットしない
        if (Owner.IsBoostSuspended)
        {
            return;
        }

        // それ以外（ゲージを消費しきった、攻撃で打ち切られた等）の
        // 正真正銘の終了時は、表示・演出をリセットする
        if (Owner.VGaugeUI != null)
        {
            Owner.VGaugeUI.SetGaugeRate(0.0f);
            Owner.VGaugeUI.SetCharging(false);
        }
    }

    /// <summary>
    /// 現在の状態を中断情報として保存します。
    /// ゲージ量自体は本体側で保持され続けているため、
    /// ここではフラグを立てるだけでよいです。
    /// </summary>
    private void SuspendBoost()
    {
        Owner.IsBoostSuspended = true;

        Debug.Log(
            $"[PlayerVRunningState] " +
            $"Vブーストを中断 " +
            $"残りゲージ量={Owner.SuspendedBoostGaugeRate:P1}",
            Owner);
    }

    /// <summary>
    /// 現在のフェーズに応じた移動処理を行い、
    /// ダッシュ時間経過時はフェーズを切り替えます。
    /// </summary>
    private void UpdatePhaseMovement()
    {
        switch (m_currentPhase)
        {
            case VBoostPhase.BOOST_DASH:
                Owner.Motor.MoveAtFixedSpeed(
                    Owner.InputReader.MoveInput,
                    m_dashMoveParameters.MaxMoveSpeed,
                    m_dashMoveParameters.RotationSpeed,
                    Time.fixedDeltaTime);

                m_elapsedTime += Time.fixedDeltaTime;

                if (m_elapsedTime >= BOOST_DASH_DURATION)
                {
                    Debug.Log(
                        "[PlayerVRunningState] " +
                        "ブーストダッシュ終了 → 通常移動フェーズへ",
                        Owner);

                    m_currentPhase =
                        VBoostPhase.NORMAL_MOVE;
                }

                break;

            case VBoostPhase.NORMAL_MOVE:
                Owner.Motor.Move(
                    Owner.InputReader.MoveInput,
                    m_normalMoveParameters,
                    Time.fixedDeltaTime);

                break;
        }
    }
}