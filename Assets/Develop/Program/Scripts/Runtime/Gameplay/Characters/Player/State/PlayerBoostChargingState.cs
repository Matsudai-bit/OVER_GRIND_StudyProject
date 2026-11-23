
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 停止チャージと移動チャージに共通するゲージ、入力、演出の状態を管理します。
/// </summary>
public abstract class PlayerBoostChargingState
    : StateBase<PlayerStateMachineComponent>
{
    // 使用するパラメータアセット
    protected PlayerBoostChargingParameterAsset m_parameterAsset;

    // チャージ経過時間
    private float m_chargeTime;

    // 速度ログの経過時間
    private float m_speedLogElapsedTime;

    // チャージ開始時の実速度へ設定倍率を掛けた固定速度
    protected float m_chargeSpeed;

    // 現在のチャージ中の移動方向
    protected Vector3 m_currentVelocityDirection;

    // 現在のプレイヤーの向き
    // 実際のPlayer本体は回転させず、移動方向・カメラ等の基準として使用します。
    protected Vector3 m_currentFacingDirection;

    // チャージ中のモデルの見た目の向き
    // 実際の移動方向とは独立して、ドリフト中の見た目を表現するために使用します。
    protected Vector3 m_currentModelFacingDirection;

    // チャージ開始前のモデルのローカル回転
    // チャージ終了時に、Prefabで設定された本来の姿勢へ正確に戻すために保持する。
    private Quaternion m_defaultModelLocalRotation;

    // 停止中にチャージを開始したか
    private bool m_startedFromStationary;

    // 現在使用している最大チャージ時間
    private float m_currentMaxChargeTime;

    protected bool m_isDashRequested;

    // 現在のロックオン対象を参照するターゲット制御
    private PlayerTargetingController m_targetingController;

    // Vチャージエフェクトの段階ごとの継続時間（今後アセットへ移行する想定）
    private const float CHARGE_V0_DURATION_SECONDS = 1.0f;
    private const float CHARGE_V1_DURATION_SECONDS = 2.0f;
    private const float CHARGE_V2_DURATION_SECONDS = 3.0f;

    // Vチャージ中に発生させたエフェクトを、状態終了時にまとめて停止します。
    private readonly Dictionary<EffectId, EffectHandle> m_effectMap = new();

    // 段階ごとの継続時間
    private readonly Dictionary<EffectId, float> m_vUpGradeIntervalTimeMap = new();

    // 現在のエフェクト段階と、その段階の経過時間
    private EffectId m_currentEffect = EffectId.NONE;
    private float m_intervalTime;

    // 状態遷移後の遅延コールバックによる誤更新を防止します。
    private bool m_isChargeEffectActive;

    /// <summary>
    /// 現在のチャージ割合を取得します。
    /// </summary>
    protected float ChargeRate =>
        Mathf.Clamp01(
            m_chargeTime /
            m_currentMaxChargeTime);

    /// <summary>
    /// 状態開始時に呼ばれます。
    /// </summary>
    /// <summary>停止開始チャージの場合はtrueを返します。</summary>
    protected abstract bool IsStaticCharge { get; }

    /// <summary>開始形態に対応するアニメーションを再生します。</summary>
    protected abstract void PlayChargeAnimation();

    /// <summary>開始形態に対応するアニメーションを停止します。</summary>
    protected abstract void StopChargeAnimation();

    /// <summary>開始形態に対応する向きと移動を更新します。</summary>
    protected abstract void UpdateChargeMode(Vector2 input, float turnSpeed, Transform target);

    /// <summary>移動方向をAnimatorへ反映します。</summary>
    protected abstract void UpdateChargeDirectionAnimation(Vector2 input);

    /// <summary>開始形態ごとの方向計算を初期化します。</summary>
    protected abstract void InitializeChargeMode();

    /// <summary>終了時に必要な向きの調整を行います。</summary>
    protected abstract void AlignFacingOnExit();

    protected override void OnStartState()
    {
        // 通常移動音からチャージ音へ切り替えるため、
        // 移動側の終了音は鳴らさず即時停止します。
        PlayerAudioActionUtility.StopSequential(
            Owner,
            PlayerAudioActionID.WALKING,
            true);



        // Stateの再入時もID登録が重複しないように初期化します。
        m_effectMap.Clear();
        m_vUpGradeIntervalTimeMap.Clear();

        m_vUpGradeIntervalTimeMap.Add(
            EffectId.PLAYER_SP_VCharge_L_V0, CHARGE_V0_DURATION_SECONDS);
        m_vUpGradeIntervalTimeMap.Add(
            EffectId.PLAYER_SP_VCharge_L_V1, CHARGE_V1_DURATION_SECONDS);
        m_vUpGradeIntervalTimeMap.Add(
            EffectId.PLAYER_SP_VCharge_L_V2, CHARGE_V2_DURATION_SECONDS);
        m_currentEffect = EffectId.NONE;
        m_intervalTime = 0.0f;
        m_isChargeEffectActive = true;
        // 前回の移動チャージ方向がAnimatorへ残らないよう初期化する。
        Owner.AnimationPresenter.ResetMoveChargeDirection();

        Owner.InputReader.ConsumeVBoostStarted();

        m_parameterAsset =
            Owner.BoostChargingParameterAsset;

        float inheritedChargeRate =
            Mathf.Clamp01(
                Owner.SuspendedBoostGaugeRate);

        // 中断扱いの残量から再チャージする場合は、
        // チャージ中にバックグラウンド消費されないよう解除します。
        Owner.IsBoostSuspended = false;
        Owner.CarriedBoostGaugeRate =
            inheritedChargeRate;
        m_speedLogElapsedTime = 0.0f;
        m_isDashRequested = false;

        m_targetingController =
            Owner.GetComponent<PlayerTargetingController>();



        float currentSpeedAtChargeStart =
            Owner.Motor.HorizontalSpeed;

        // チャージ開始時に停止していたか判定
        m_startedFromStationary = IsStaticCharge;

        // 開始状態に応じてチャージ時間を選択
        m_currentMaxChargeTime =
            m_startedFromStationary
                ? m_parameterAsset.StationaryStartChargeTime
                : m_parameterAsset.MovingStartChargeTime;

        m_chargeTime =
            m_currentMaxChargeTime *
            inheritedChargeRate;

        m_chargeSpeed =
            currentSpeedAtChargeStart *
            m_parameterAsset.ChargeMoveSpeedRate;

        PlayerAudioActionUtility.PlaySequential(
    Owner,
     PlayerAudioActionUtility.GetChargeActionID(m_startedFromStationary));
        // --------------------------------------------------------
        // チャージ開始時の移動方向を取得
        // --------------------------------------------------------

        m_currentVelocityDirection =
            Owner.Motor.HorizontalDirection;

        m_currentVelocityDirection.y = 0.0f;

        if (m_currentVelocityDirection.sqrMagnitude <= 0.0001f)
        {
            m_currentVelocityDirection =
                Owner.transform.forward;

            m_currentVelocityDirection.y = 0.0f;
        }

        m_currentVelocityDirection.Normalize();

        InitializeChargeMode();

        // --------------------------------------------------------
        // プレイヤーの向きを初期化
        // --------------------------------------------------------

        m_currentFacingDirection =
            m_currentVelocityDirection;

        // モデルの見た目の向きも移動方向で初期化する
        m_currentModelFacingDirection =
            m_currentVelocityDirection;

        // モデルは物理ボディとは独立して回転させる。基準姿勢を保持しておくことで、
        // Prefab側で設定された回転を終了時に壊さない。
        if (Owner.ModelTransform != null)
        {
            m_defaultModelLocalRotation =
                Owner.ModelTransform.localRotation;

            // チャージ開始前に表示されていた角度を補間開始位置として使用し、
            // 開始フレームで進行方向へスナップすることを防ぐ。
            Vector3 currentModelDirection =
                Owner.ModelTransform.forward;
            currentModelDirection.y = 0.0f;

            if (currentModelDirection.sqrMagnitude > 0.0001f)
            {
                m_currentModelFacingDirection =
                    currentModelDirection.normalized;
            }

        }

        Debug.Log(
            $"[PlayerBoostChargingState] チャージ開始 " +
            $"開始時実速度={currentSpeedAtChargeStart:F2} " +
            $"停止開始={m_startedFromStationary} " +
            $"引き継ぎ率={inheritedChargeRate:P1} " +
            $"最大チャージ時間={m_currentMaxChargeTime:F2}秒 " +
            $"チャージ速度={m_chargeSpeed:F2}",
            Owner);

        Owner.VGaugePlaceModel.SetGaugeRate(ChargeRate);

        // チャージ開始時の状態に応じてアニメーションを切り替える
        PlayChargeAnimation();
        // 最初のワンショットエフェクトの自然終了を待ってV1へ移行します。
        m_currentEffect = EffectId.PLAYER_SP_VCharge_L_V0;
        ApplyNextEffect(m_currentEffect);
    }

    /// <summary>
    /// V0の自然終了を受け取り、V1へ進めます。
    /// </summary>
    /// <param name="reason">終了理由。</param>
    private void OnInitialChargeEffectCompleted(EffectCompletionReason reason)
    {
        if (!m_isChargeEffectActive ||
            m_currentEffect != EffectId.PLAYER_SP_VCharge_L_V0 ||
            reason != EffectCompletionReason.COMPLETED)
        {
            return;
        }

        ApplyNextEffect(EffectId.PLAYER_SP_VCharge_L_V0);
    }

    /// <summary>
    /// V1・V2の継続時間を計測し、次のエフェクトへ進めます。
    /// </summary>
    /// <param name="deltaTime">経過秒数。</param>
    private void UpdateChargeEffects(float deltaTime)
    {
        if (!m_isChargeEffectActive ||
            !m_vUpGradeIntervalTimeMap.TryGetValue(
                m_currentEffect, out float requiredTime))
        {
            return;
        }

        m_intervalTime += deltaTime;
        if (m_intervalTime >= requiredTime)
        {
            ApplyNextEffect(m_currentEffect);
        }
    }


    EffectId GetEffectForV(int vValue)
    {
        switch (vValue)
        {
            case 0:
                return EffectId.PLAYER_SP_VCharge_L_V0;

            case 1:
                return EffectId.PLAYER_SP_VCharge_L_V1;

            case 2:
                return EffectId.PLAYER_SP_VCharge_L_V2;

            case 3:
                return EffectId.PLAYER_SP_VCharge_L_V3;

        }
        return EffectId.NONE;
    }

    /// <summary>
    /// 現在の段階と一致する場合だけ、次のエフェクトを開始します。
    /// </summary>
    /// <param name="effectId">遷移前のエフェクトID。</param>
    private void ApplyNextEffect(EffectId effectId)
    {
        if (!m_isChargeEffectActive || m_currentEffect != effectId)
        {
            return;
        }

        EffectId nextEffect;
        switch (effectId)
        {
            case EffectId.PLAYER_SP_VCharge_L_V0:
                nextEffect = EffectId.PLAYER_SP_VCharge_L_V1;
                break;
            case EffectId.PLAYER_SP_VCharge_L_V1:
                nextEffect = EffectId.PLAYER_SP_VCharge_L_V2;
                break;
            case EffectId.PLAYER_SP_VCharge_L_V2:
                nextEffect = EffectId.PLAYER_SP_VCharge_L_V3;
                break;
            default:
                return;
        }

        EffectHandle nextHandle = Owner.EffectEmitter.Play(nextEffect);
        if (!nextHandle.IsValid)
        {
            Debug.LogWarning(
                $"[PlayerBoostChargingState] エフェクト再生失敗: {nextEffect}",
                Owner);
            return;
        }

        // 既存の段階のエフェクトは重ねて再生し、State終了時にまとめて停止します。
        m_effectMap[nextEffect] = nextHandle;
        m_currentEffect = nextEffect;
        m_intervalTime = 0.0f;
    }

    /// <summary>
    /// チャージ状態から離れるとき、終了通知を解除してエフェクトを停止します。
    /// </summary>
    private void StopChargeEffects()
    {
        // 停止中のイベント経由で次の段階へ遷移しないよう先に無効化します。
        m_isChargeEffectActive = false;

        if (m_effectMap.TryGetValue(
            EffectId.PLAYER_SP_VCharge_L_V0, out EffectHandle initialHandle))
        {
            initialHandle.UnregisterCompletedCallback(OnInitialChargeEffectCompleted);
        }

        foreach (EffectHandle handle in m_effectMap.Values)
        {
            handle.Stop(EffectStopMode.GRACEFUL);
        }

        m_effectMap.Clear();
        m_currentEffect = EffectId.NONE;
        m_intervalTime = 0.0f;
    }

    /// <summary>
    /// AnimatorとConstraintが評価される前に、
    /// チャージ中のモデル向きを反映します。
    /// </summary>
    /// <param name="deltaTime">
    /// 前フレームからの経過時間。
    /// </param>
    protected override void OnUpdate(
        float deltaTime)
    {
        ApplyModelFacing();
    }

    /// <summary>
    /// 一定間隔の更新処理を行います。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        // 遷移要求が成立した場合は、移動入力をそれ以上更新しません。
        if (m_isDashRequested)
        {
            return;
        }

        // StateBaseの更新経路を使用してエフェクト段階を進めます。
        UpdateChargeEffects(Time.fixedDeltaTime);

        // チャージ中の攻撃入力は破棄し、チャージ状態を継続します。
        Owner.InputReader.ConsumeAttackInput();

        if (Owner.Monitor.CanStartJump &&
            Owner.InputReader.HasJumpInput)
        {
            // チャージを中断するため、保持しているゲージを破棄する
            Owner.CarriedBoostGaugeRate = 0.0f;
            Owner.SuspendedBoostGaugeRate = 0.0f;

            // UIのチャージゲージも0にする
            Owner.VGaugePlaceModel.SetGaugeRate(0.0f);

            Debug.Log(
                "[PlayerBoostChargingState] " +
                "ジャンプによりチャージを中断しました。チャージゲージを0%にします。",
                Owner);

            Machine.ChangeState<PlayerJumpingState>();
            return;
        }

        if (Owner.InputReader.ConsumeVBoostReleased())
        {
            Debug.Log(
                $"[PlayerBoostChargingState] " +
                $"チャージ解除 " +
                $"経過時間={m_chargeTime:F2}秒 " +
                $"チャージ率={ChargeRate:P1} " +
                $"実速度={Owner.Motor.HorizontalSpeed:F2}",
                Owner);

            ReleaseCharge();
            return;
        }

        // --------------------------------------------------------
        // チャージ時間更新
        // --------------------------------------------------------

        m_chargeTime += Time.fixedDeltaTime;

        if (m_chargeTime >= m_currentMaxChargeTime)
        {
            m_chargeTime =
                m_currentMaxChargeTime;
        }

        // --------------------------------------------------------
        // 入力方向取得
        // --------------------------------------------------------

        Vector2 normalizedInput =
            Vector2.ClampMagnitude(
                Owner.InputReader.MoveInput,
                1.0f);

        // 移動チャージ中だけ横入力方向をAnimatorへ渡す。
        // -1：左、0：正面・入力なし、1：右。
        UpdateChargeDirectionAnimation(normalizedInput);

        // --------------------------------------------------------
        // チャージ率に応じた、現在の曲がりやすさを計算
        // --------------------------------------------------------

        float currentDriftTurnSpeed =
            Mathf.Lerp(
                m_parameterAsset.DriftTurnSpeedAtChargeStart,
                m_parameterAsset.DriftTurnSpeedAtFullCharge,
                ChargeRate);

        Transform targetTransform =
            m_targetingController != null
                ? m_targetingController.GetCurrentTargetTransform()
                : null;

        // --------------------------------------------------------
        // チャージ中の移動方向を更新
        // --------------------------------------------------------

        UpdateChargeMode(normalizedInput, currentDriftTurnSpeed, targetTransform);

        // --------------------------------------------------------
        // ゲージ更新
        // --------------------------------------------------------

        Owner.VGaugePlaceModel.SetGaugeRate(ChargeRate);

        // --------------------------------------------------------
        // デバッグログ
        // --------------------------------------------------------

        m_speedLogElapsedTime +=
            Time.fixedDeltaTime;

        if (m_speedLogElapsedTime >=
            m_parameterAsset.SpeedLogInterval)
        {
            m_speedLogElapsedTime = 0.0f;

            Debug.Log(
                $"[PlayerBoostChargingState] " +
                $"チャージ={ChargeRate:P1} " +
                $"実速度={Owner.Motor.HorizontalSpeed:F2} " +
                $"固定速度={m_chargeSpeed:F2} " +
                $"開始時速度倍率={m_parameterAsset.ChargeMoveSpeedRate:F2} " +
                $"設定={m_parameterAsset.name} " +
                $"曲がりやすさ={currentDriftTurnSpeed:F1}deg/s",
                Owner);
        }
    }

    /// <summary>
    /// ターゲット中のモデルとプレイヤーの向きを対象へ合わせます。
    /// 高低差による傾きを防ぐため、水平方向だけを使用します。
    /// </summary>
    /// <param name="targetTransform">現在のターゲット。</param>
    protected void UpdateTargetFacingDirection(Transform targetTransform)
    {
        if (targetTransform == null)
        {
            return;
        }

        Vector3 targetDirection =
            targetTransform.position - Owner.transform.position;
        targetDirection.y = 0.0f;

        if (targetDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        targetDirection.Normalize();

        float maxRadiansDelta =
            m_parameterAsset.FacingRotationSpeed *
            Mathf.Deg2Rad *
            Time.fixedDeltaTime;

        // 現在の角度からターゲット方向へ徐々に回転させる。
        m_currentFacingDirection =
            Vector3.RotateTowards(
                m_currentFacingDirection,
                targetDirection,
                maxRadiansDelta,
                0.0f).normalized;

        m_currentModelFacingDirection =
            Vector3.RotateTowards(
                m_currentModelFacingDirection,
                targetDirection,
                maxRadiansDelta,
                0.0f).normalized;
    }

    /// <summary>
    /// モデルの現在姿勢を維持したまま、
    /// 水平方向の向きだけを目標方向へ合わせます。
    /// </summary>
    private void ApplyModelFacing()
    {
        Transform modelTransform =
            Owner.ModelTransform;

        if (modelTransform == null)
        {
            return;
        }

        Vector3 targetDirection =
            m_currentModelFacingDirection;

        targetDirection.y = 0.0f;

        if (targetDirection.sqrMagnitude <=
            0.0001f)
        {
            return;
        }

        targetDirection.Normalize();

        Vector3 currentDirection =
            modelTransform.forward;

        currentDirection.y = 0.0f;

        if (currentDirection.sqrMagnitude <=
            0.0001f)
        {
            return;
        }

        currentDirection.Normalize();

        float yawDelta =
            Vector3.SignedAngle(
                currentDirection,
                targetDirection,
                Vector3.up);

        // Quaternion.LookRotationでPrefab固有の軸補正を上書きせず、
        // 現在姿勢へYaw差分だけを加えます。
        modelTransform.rotation =
            Quaternion.AngleAxis(
                yawDelta,
                Vector3.up) *
            modelTransform.rotation;
    }

    /// <summary>
    /// 状態終了時に呼ばれます。
    /// </summary>
    protected override void OnExitState()
    {
        // チャージ継続音を終了音へ遷移させます。
        PlayerAudioActionUtility.StopSequential(
            Owner,
            PlayerAudioActionUtility.GetChargeActionID(m_startedFromStationary));

        // State終了後にエフェクトの終了通知で次段階へ進まないよう解除します。
        StopChargeEffects();

        // State終了時は移動チャージ状態と方向を必ず初期化する。
        Owner.AnimationPresenter.StopChargeMovingAnimation();
        Owner.AnimationPresenter.ResetMoveChargeDirection();

        Debug.Log(
            "[PlayerBoostChargingState] チャージ状態終了",
            Owner);

        // 移動開始チャージの場合のみ、
        // 終了時の進行方向へ本体を合わせる
        AlignFacingOnExit();

        // チャージ中だけ適用していたモデル向きを解除し、
        // Prefabで設定された基準姿勢へ戻します。
        if (Owner.ModelTransform != null)
        {
            Owner.ModelTransform.localRotation =
                m_defaultModelLocalRotation;
        }

        // 開始状態に対応したアニメーションを停止
        StopChargeAnimation();
    }

    /// <summary>
    /// チャージを解除したときの遷移を行います。
    ///
    /// ダッシュ可能なチャージ量の場合は、
    /// チャージ終了時点のプレイヤーの正面方向を
    /// ダッシュ方向として使用します。
    /// </summary>
    private void ReleaseCharge()
    {
        if (ChargeRate >=
            m_parameterAsset.MinBoostChargeRate)
        {
            // PresenterがLateUpdateで表示する向きと同じ論理値を使用する。
            // ModelTransform.forwardはAnimatorの更新順やPrefabの初期回転に左右されるため、
            // 統合先のモデル階層でも見た目とダッシュ方向がずれないようにする。
            Vector3 dashDirection =
                m_currentModelFacingDirection;

            dashDirection.y = 0.0f;

            if (dashDirection.sqrMagnitude >
                0.0001f)
            {
                dashDirection.Normalize();
            }

            Owner.BoostDashDirection =
                dashDirection;
            m_isDashRequested = true;

            Debug.Log(
                $"[PlayerBoostChargingState] " +
                $"チャージ率{ChargeRate:P1} → Vブーストへ遷移 " +
                $"ダッシュ方向={dashDirection}",
                Owner);

            Owner.CarriedBoostGaugeRate =
                ChargeRate;

            Machine.ChangeState<PlayerBoostDashingState>();
            return;
        }

        Debug.Log(
            $"[PlayerBoostChargingState] " +
            $"チャージ率{ChargeRate:P1} → 通常歩行へ遷移",
            Owner);

        Owner.VGaugePlaceModel.SetGaugeRate(0.0f);

        Machine.ChangeState<PlayerWalkingState>();
    }
}
