using System;
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

    // ブーストダッシュの継続時間
    private const float BOOST_DASH_DURATION = 0.5f;

    // ブーストダッシュ中の最大移動速度倍率
    private const float DASH_SPEED_MULTIPLIER = 2.0f;

    // ブーストダッシュ中のプレイヤー回転速度
    // 値を大きくすると、より素早く進行方向を向きます。
    private const float BOOST_DASH_ROTATION_SPEED = 360.0f;

    // 現在のブーストフェーズ
    private VBoostPhase m_currentPhase;

    // 現在フェーズの経過時間（ダッシュ終了判定用）
    private float m_elapsedTime;

    // ダッシュ中に使用する移動パラメータ（基本速度の2倍・瞬時到達）
    private PlayerMoveParameters m_dashMoveParameters;

    // ダッシュ終了後に使用する通常移動パラメータ
    private PlayerMoveParameters m_normalMoveParameters;

    // チャージ終了時に確定したダッシュ方向
    // BOOST_DASH中はこの方向から変更しない
    private Vector3 m_boostDashDirection;
    private Vector3 m_movingChargeStartDirection;
    private bool m_isWaitingForMovingCharge;


    /// <summary>
    /// 状態開始時に呼ばれます。
    /// 中断（ジャンプ・停止など）から復帰した場合は、
    /// 中断時点のフェーズから再開します。
    /// </summary>
    protected override void OnStartState()
    {
        m_isWaitingForMovingCharge = false;

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

        float fullTankDuration =
            Mathf.Max(
                Owner.VBoostMovementParameterAsset
                    .StableBoostDuration,
                0.01f);

        Owner.SetBoostGaugeDepletionRate(
            1.0f / fullTankDuration);

        if (Owner.IsBoostSuspended)
        {
            // 中断された状態からの復帰
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
            // 新規Vブースト開始
            Owner.SuspendedBoostGaugeRate =
                Owner.CarriedBoostGaugeRate;

            m_currentPhase =
                VBoostPhase.BOOST_DASH;

            m_elapsedTime = 0.0f;

            m_boostDashDirection =
                Owner.BoostDashDirection;

            m_boostDashDirection.y = 0.0f;

            if (m_boostDashDirection.sqrMagnitude <= 0.0001f)
            {
                m_boostDashDirection =
                    Owner.transform.forward;

                m_boostDashDirection.y = 0.0f;
            }

            m_boostDashDirection.Normalize();

            Debug.Log(
                $"[PlayerVRunningState] ブーストダッシュ開始 " +
                $"引き継ぎゲージ量={Owner.SuspendedBoostGaugeRate:P1} " +
                $"ダッシュ方向={m_boostDashDirection} " +
                $"ダッシュ最高速度={m_dashMoveParameters.MaxMoveSpeed:F2} " +
                $"（通常速度={normalParameters.MaxMoveSpeed:F2}） " +
                $"ダッシュ時間={BOOST_DASH_DURATION:F2}秒 " +
                $"満タン消費時間={fullTankDuration:F2}秒",
                Owner);
        }

        // Vブースト走行アニメーションを開始
        Owner.AnimationPresenter.PlayVBoostRunningAnimation();

        Owner.VGaugePlaceModel.SetGaugeRate(
            Owner.SuspendedBoostGaugeRate);

        if (Owner.VGaugeUI != null)
        {
            Owner.VGaugeUI.SetCharging(true);
        }
    }


    /// <summary>
    /// 一定間隔の更新処理を行います。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        // 攻撃入力を確認
        if (Owner.InputReader.ConsumeAttackInput() && Owner.Monitor.IsGrounded)
        {
            // 攻撃中も残量を引き継ぎ、チャージの継続消費を維持する。
            SuspendBoost();
            Machine.ChangeState<PlayerAttackingState>();
            return;
        }


        // ゲージが本体側の消費によって尽きていないか確認
        // （中断復帰直後や、消費が進んで0になった場合はここで検知する）
        if (Owner.SuspendedBoostGaugeRate <= 0.0f)
        {
            // 現在のVブースト入力状態を消費して、
            // WalkingStateへ遷移した直後に
            // 同じ入力で再チャージされることを防ぐ。
            Owner.InputReader.ConsumeVBoostInput();

            Debug.Log(
                "[PlayerVRunningState] " +
                "ゲージを消費しきったため通常歩行へ遷移します。",
                Owner);

            Machine.ChangeState<PlayerWalkingState>();
            return;
        }


        // 移動入力がなくなった場合は、打ち切らずに中断する。
        // 待機中に再び移動入力が入れば復帰できるようにする。
        // ただし、ブーストダッシュ中は入力を必要としないため、
        // 通常移動フェーズでのみ中断判定を行う。
        if (m_currentPhase == VBoostPhase.NORMAL_MOVE &&
            !Owner.InputReader.HasMoveInput)
        {
            SuspendBoost();

            Machine.ChangeState<PlayerIdlingState>();
            return;
        }



        // ジャンプ入力を確認
        // 同様に打ち切らず中断し、着地後に復帰させる
        if (Owner.Monitor.CanStartJump &&
            Owner.InputReader.HasJumpInput)
        {
            SuspendBoost();

            Machine.ChangeState<PlayerJumpingState>();
            return;
        }

        UpdatePhaseMovement();
    }


    /// <summary>チャージボタン押下時の実移動方向を開始判定用に保持します。</summary>
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
    /// 状態終了時に呼ばれます。
    /// </summary>
    protected override void OnExitState()
    {
        // Vブースト走行アニメーションを停止
        Owner.AnimationPresenter.StopVBoostRunningAnimation();

        // 中断による終了の場合は、後で再開するため
        // ゲージ表示・演出をリセットしない
        if (Owner.IsBoostSuspended)
        {
            return;
        }

        // ゲージを消費しきった場合などの完全終了
        Owner.VGaugePlaceModel.SetGaugeRate(0.0f);

        if (Owner.VGaugeUI != null)
        {
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

                // ダッシュ中に押されたチャージ入力を通常移動へ持ち越しません。
                // 押し続けた場合も、いったん離して再度押すまで再チャージを禁止します。
                Owner.InputReader.DiscardVBoostPendingInput();

                // ------------------------------------------------
                // ブーストダッシュ
                // ------------------------------------------------
                //
                // スティック入力は使用しない。
                // チャージ終了時に確定した方向へ固定する。
                //

                Owner.Motor.MoveAtFixedWorldDirection(
                    m_boostDashDirection,
                    m_dashMoveParameters.MaxMoveSpeed,
                    BOOST_DASH_ROTATION_SPEED,
                    Time.fixedDeltaTime);

                m_elapsedTime += Time.fixedDeltaTime;

                if (m_elapsedTime >= BOOST_DASH_DURATION)
                {
                    // ダッシュ終了時に正面を進行方向へ合わせる
                    Vector3 finalForward =
                        m_boostDashDirection;

                    finalForward.y = 0.0f;

                    if (finalForward.sqrMagnitude > 0.0001f)
                    {
                        finalForward.Normalize();

                        Owner.transform.forward =
                            finalForward;
                    }

                    Debug.Log(
                        "[PlayerVRunningState] " +
                        "ブーストダッシュ終了 → 通常移動フェーズへ",
                        Owner);

                    m_currentPhase =
                        VBoostPhase.NORMAL_MOVE;
                }

                break;


            case VBoostPhase.NORMAL_MOVE:

                // ------------------------------------------------
                // 新しいVブーストチャージ
                // ------------------------------------------------
                //
                // 現在のVブーストゲージが残っている場合は、
                // 新しい長押し成立時に残量を引き継いでチャージを再開する。
                //

                UpdateMovingChargeStartDirection();

                if (Owner.Monitor.IsGrounded &&
                    m_isWaitingForMovingCharge &&
                    Owner.InputReader.HasVBoostHoldStarted &&
                    Owner.BoostChargingParameterAsset.HasLateralChargeInput(
                        Owner.InputReader.MoveInput) &&
                    Owner.BoostChargingParameterAsset.HasExceededFreeSteeringAngle(
                        m_movingChargeStartDirection,
                        Owner.Motor.HorizontalDirection) &&
                    Owner.InputReader.ConsumeVBoostHoldStarted())
                {
                    Debug.Log(
                        $"[PlayerVRunningState] " +
                        $"残りゲージを引き継いでVブーストチャージを開始します。" +
                        $"引き継ぎ率={Owner.SuspendedBoostGaugeRate:P1}",
                        Owner);

                    Machine.ChangeState<PlayerBoostChargingState>();
                    return;
                }

                // ------------------------------------------------
                // 通常移動
                // ------------------------------------------------

                Owner.Motor.Move(
                    Owner.InputReader.MoveInput,
                    m_normalMoveParameters,
                    Time.fixedDeltaTime);

                break;
        }
    }
}
