using UnityEngine;

/// <summary>
/// ステージ1フェーズ3の突進攻撃を実行します。
/// </summary>
public sealed class S1P3BossChargingAttackState :
    StateBase<BossController>
{
    /// <summary>
    /// 突進攻撃の実行フェーズ。
    /// </summary>
    private enum ChargePhase
    {
        ROTATING,
        CHARGING,
        FINISHED
    }

    // 方向ベクトルの有効判定に使用する閾値
    private const float DIRECTION_SQR_THRESHOLD = 0.0001f;

    // プレイヤーを追尾して回転する時間
    private const float ROTATION_DURATION = 5.0f;

    // 回転速度
    private const float ROTATION_SPEED = 70.0f;

    // 最大突進距離
    private const float MAX_CHARGE_DISTANCE = 30.0f;

    // 突進時間
    private const float CHARGE_DURATION = 1.0f;

    // 障害物や行動範囲境界との停止余白
    private const float CHARGE_STOP_MARGIN = 0.5f;

    // Animator Trigger名
    private readonly string m_animationTriggerName;

    // 攻撃ID
    private readonly AttackIdentifier m_attackIdentifier;

    // 突進移動カーブ
    private readonly AnimationCurve m_chargeMoveCurve;

    // Animator Trigger ID
    private int m_animationTriggerID;

    // 使用中のAnimationEventReceiver
    private AnimationEventReceiver m_animationEventReceiver;

    // プレイヤー
    private Transform m_playerTransform;

    // 現在の突進フェーズ
    private ChargePhase m_chargePhase;

    // 回転開始からの経過時間
    private float m_rotationElapsedTime;

    // 突進開始からの経過時間
    private float m_chargeElapsedTime;

    // 突進方向
    private Vector3 m_chargeDirection;

    // 突進開始位置
    private Vector3 m_chargeStartPosition;

    // 突進終了位置
    private Vector3 m_chargeEndPosition;

    /// <summary>
    /// 攻撃ステートを生成します。
    /// </summary>
    /// <param name="animationTriggerName">
    /// Animator Trigger名。
    /// </param>
    /// <param name="attackIdentifier">
    /// 攻撃ID。
    /// </param>
    /// <param name="chargeMoveCurve">
    /// 突進の移動割合を制御するカーブ。
    /// </param>
    public S1P3BossChargingAttackState(
        string animationTriggerName,
        AttackIdentifier attackIdentifier,
        AnimationCurve chargeMoveCurve)
    {
        m_animationTriggerName = animationTriggerName;
        m_attackIdentifier = attackIdentifier;

        m_chargeMoveCurve =
            chargeMoveCurve ??
            AnimationCurve.Linear(
                0.0f,
                0.0f,
                1.0f,
                1.0f);
    }

    /// <summary>
    /// 攻撃を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        if (string.IsNullOrEmpty(m_animationTriggerName) ||
            m_attackIdentifier == null ||
            Owner.AnimationController == null ||
            Owner.Motor == null ||
            Owner.Navigation == null)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
            return;
        }

        var player =
            GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
            return;
        }

        m_playerTransform = player.transform;

        m_animationEventReceiver =
            Owner.AnimationController.CurrentAnimationEventReceiver;

        if (m_animationEventReceiver == null)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
            return;
        }

        // 前の行動の速度を残さないようにします。
        Owner.Motor.StopHorizontalMovement();

        // 実行情報を初期化します。
        m_chargePhase = ChargePhase.ROTATING;

        m_rotationElapsedTime = 0.0f;
        m_chargeElapsedTime = 0.0f;

        m_chargeDirection = Vector3.zero;
        m_chargeStartPosition = Vector3.zero;
        m_chargeEndPosition = Vector3.zero;

        m_animationTriggerID =
            Animator.StringToHash(
                m_animationTriggerName);

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);

        m_animationEventReceiver.AttackEventReceived +=
            HandleAttackEvent;

        Owner.AnimationController.SetTrigger(
            m_animationTriggerID);
    }

    /// <summary>
    /// 突進攻撃を更新します。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        switch (m_chargePhase)
        {
            case ChargePhase.ROTATING:
                UpdateRotation();
                break;

            case ChargePhase.CHARGING:
                UpdateCharge();
                break;

            case ChargePhase.FINISHED:
                break;
        }
    }

    /// <summary>
    /// プレイヤー方向への回転を更新します。
    /// </summary>
    private void UpdateRotation()
    {
        if (m_playerTransform == null)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
            return;
        }

        Vector3 direction =
            m_playerTransform.position -
            Owner.transform.position;

        // 水平方向のみ追尾します。
        direction.y = 0.0f;

        if (direction.sqrMagnitude >
            DIRECTION_SQR_THRESHOLD)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(
                    direction.normalized,
                    Vector3.up);

            Owner.Motor.RotateTowards(
                targetRotation,
                ROTATION_SPEED,
                Time.fixedDeltaTime);
        }

        m_rotationElapsedTime +=
            Time.fixedDeltaTime;

        if (m_rotationElapsedTime <
            ROTATION_DURATION)
        {
            return;
        }

        StartCharge();
    }

    /// <summary>
    /// 突進を開始します。
    /// </summary>
    private void StartCharge()
    {
        // 回転終了時点の前方を突進方向として固定します。
        m_chargeDirection =
            Owner.transform.forward;

        m_chargeDirection.y = 0.0f;

        if (m_chargeDirection.sqrMagnitude <=
            DIRECTION_SQR_THRESHOLD)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
            return;
        }

        m_chargeDirection.Normalize();

        // 回転中などに残っている速度を除去します。
        Owner.Motor.StopHorizontalMovement();

        float chargeDistance =
            Owner.Navigation.GetMaxStraightMoveDistance(
                m_chargeDirection,
                MAX_CHARGE_DISTANCE,
                CHARGE_STOP_MARGIN);

        if (chargeDistance <= 0.0f)
        {
            FinishCharge();
            return;
        }

        m_chargeStartPosition =
            Owner.transform.position;

        m_chargeEndPosition =
            m_chargeStartPosition +
            m_chargeDirection * chargeDistance;

        m_chargeElapsedTime = 0.0f;

        m_chargePhase =
            ChargePhase.CHARGING;

        Debug.DrawLine(
            m_chargeStartPosition,
            m_chargeEndPosition,
            Color.red,
            CHARGE_DURATION);
    }

    /// <summary>
    /// 直線突進を更新します。
    /// </summary>
    private void UpdateCharge()
    {
        m_chargeElapsedTime +=
            Time.fixedDeltaTime;

        float normalizedTime =
            Mathf.Clamp01(
                m_chargeElapsedTime /
                CHARGE_DURATION);

        // カーブの傾きによって突進の速度変化を作ります。
        float moveRate =
            Mathf.Clamp01(
                m_chargeMoveCurve.Evaluate(
                    normalizedTime));

        Vector3 targetPosition =
            Vector3.Lerp(
                m_chargeStartPosition,
                m_chargeEndPosition,
                moveRate);

        Owner.Motor.MovePosition(
            targetPosition);

        if (normalizedTime < 1.0f)
        {
            return;
        }

        // 最終位置の誤差を防ぎます。
        Owner.Motor.MovePosition(
            m_chargeEndPosition);

        FinishCharge();
    }

    /// <summary>
    /// 突進移動を終了します。
    /// </summary>
    private void FinishCharge()
    {
        m_chargePhase =
            ChargePhase.FINISHED;

        Owner.Motor.StopHorizontalMovement();
        Owner.SetStateExecutionStatus(
                    StateExecutionStatus.SUCCEEDED);

        if (Owner.AttackHitboxRegistry .TryGetHitbox(m_attackIdentifier, out AttackHitbox outHitBox) && outHitBox.enabled)
        {
            Owner.AttackHitboxRegistry?.DisableHitbox(
            m_attackIdentifier);
        }
 
    }

    /// <summary>
    /// 攻撃AnimationEventを処理します。
    /// </summary>
    /// <param name="attackEventData">
    /// 攻撃イベント情報。
    /// </param>
    private void HandleAttackEvent(
        AttackEventData attackEventData)
    {
        if (attackEventData == null ||
            attackEventData.AttackIdentifier !=
            m_attackIdentifier)
        {
            return;
        }

        switch (attackEventData.AttackEventType)
        {
            case AttackEventType.HITBOX_ENABLE:
                Owner.AttackHitboxRegistry?.EnableHitbox(
                    m_attackIdentifier);
                break;

            case AttackEventType.HITBOX_DISABLE:
                Owner.AttackHitboxRegistry?.DisableHitbox(
                    m_attackIdentifier);
                break;

            case AttackEventType.ANIMATION_END:
                Owner.SetStateExecutionStatus(
                    StateExecutionStatus.SUCCEEDED);
                break;
        }
    }

    /// <summary>
    /// 攻撃を終了します。
    /// </summary>
    protected override void OnExitState()
    {
        // State変更後に速度が残らないようにします。
        Owner.Motor?.StopHorizontalMovement();

        m_chargePhase =
            ChargePhase.FINISHED;

        m_rotationElapsedTime = 0.0f;
        m_chargeElapsedTime = 0.0f;

        m_chargeDirection = Vector3.zero;
        m_chargeStartPosition = Vector3.zero;
        m_chargeEndPosition = Vector3.zero;

        m_playerTransform = null;

        if (m_animationEventReceiver != null)
        {
            m_animationEventReceiver.AttackEventReceived -=
                HandleAttackEvent;

            m_animationEventReceiver = null;
        }

        Owner.AttackHitboxRegistry?.DisableHitbox(
            m_attackIdentifier);

        if (Owner.GetStateExecutionStatus() ==
            StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }
    }
}