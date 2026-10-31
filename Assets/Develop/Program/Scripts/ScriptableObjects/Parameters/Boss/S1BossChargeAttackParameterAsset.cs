using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// ボスの突進攻撃パラメータを設定します。
/// </summary>
[CreateAssetMenu(
    fileName = "S1BossChargeAttackParameter",
    menuName =
        "Game/Parameters/Boss/Attack/Charge Attack Parameter")]
public sealed class S1BossChargeAttackParameterAsset :
    ScriptableObject
{
    // 狙い確定時間の調整範囲
    private const float MIN_TARGET_LOCK_DURATION =
        0.0f;

    private const float MAX_TARGET_LOCK_DURATION =
        10.0f;

    // 回転速度の調整範囲
    private const float MIN_ROTATION_SPEED =
        0.0f;

    private const float MAX_ROTATION_SPEED =
        720.0f;

    // 突進速度の調整範囲
    private const float MIN_CHARGE_SPEED =
        0.1f;

    private const float MAX_CHARGE_SPEED =
        100.0f;

    // 突進距離の調整範囲
    private const float MIN_CHARGE_DISTANCE =
        0.1f;

    private const float MAX_CHARGE_DISTANCE =
        100.0f;

    // 停止余白の調整範囲
    private const float MIN_STOP_MARGIN =
        0.0f;

    private const float MAX_STOP_MARGIN =
        5.0f;

    // 終了硬直時間の調整範囲
    private const float MIN_END_DURATION =
        0.0f;

    private const float MAX_END_DURATION =
        10.0f;

    // 狙い確定状態の継続時間
    [FormerlySerializedAs(
        "m_preparationDuration")]
    [SerializeField, Header("ゲーム調整")]
    [Tooltip(
        "目標方向へ向いた後、その方向を固定して待機する時間です。")]
    [Range(
        MIN_TARGET_LOCK_DURATION,
        MAX_TARGET_LOCK_DURATION)]
    private float m_targetLockDuration =
        1.0f;

    // 回転速度
    [SerializeField]
    [Tooltip(
        "目標方向へ回転する速度です。")]
    [Range(
        MIN_ROTATION_SPEED,
        MAX_ROTATION_SPEED)]
    private float m_rotationSpeed =
        70.0f;

    // 突進速度
    [SerializeField]
    [Tooltip(
        "突進中の移動速度です。")]
    [Range(
        MIN_CHARGE_SPEED,
        MAX_CHARGE_SPEED)]
    private float m_chargeSpeed =
        30.0f;

    // 最大突進距離
    [SerializeField]
    [Tooltip(
        "1回の突進で移動できる最大距離です。")]
    [Range(
        MIN_CHARGE_DISTANCE,
        MAX_CHARGE_DISTANCE)]
    private float m_maxChargeDistance =
        30.0f;

    // 停止余白
    [SerializeField]
    [Tooltip(
        "障害物や移動範囲の手前で停止する距離です。")]
    [Range(
        MIN_STOP_MARGIN,
        MAX_STOP_MARGIN)]
    private float m_stopMargin =
        0.5f;

    // 終了硬直時間
    [SerializeField]
    [Tooltip(
        "突進終了後に次の行動へ移るまでの時間です。")]
    [Range(
        MIN_END_DURATION,
        MAX_END_DURATION)]
    private float m_endDuration =
        1.0f;

    // 突進攻撃中Bool名
    [SerializeField, Header("Animation")]
    private string m_animationActiveBoolName =
        "Charging";

    // 突進攻撃フェーズInt名
    [SerializeField]
    private string m_animationPhaseIntName =
        "ChargingPhase";

    // 左右旋回方向Int名
    [SerializeField]
    private string m_animationTurnDirectionIntName =
        "ChargingTurnDirection";

    // 攻撃識別子
    [SerializeField, Header("攻撃判定")]
    private AttackIdentifier m_attackIdentifier;

    // 左旋回Trigger名
    [SerializeField]
    private string m_animationTurnLeftTriggerName =
        "Charging_Left";

    // 右旋回Trigger名
    [SerializeField]
    private string m_animationTurnRightTriggerName =
        "Charging_Right";

    /// <summary>
    /// 実行時に使用する突進攻撃パラメータを生成します。
    /// </summary>
    public S1BossChargeAttackParameters
        CreateParameters()
    {
        return new S1BossChargeAttackParameters(
            m_targetLockDuration,
            m_rotationSpeed,
            m_chargeSpeed,
            m_maxChargeDistance,
            m_stopMargin,
            m_endDuration,
            m_animationActiveBoolName,
            m_animationPhaseIntName,
            m_animationTurnDirectionIntName,
            m_attackIdentifier);
    }
}