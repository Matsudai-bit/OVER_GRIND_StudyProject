using UnityEngine;

/// <summary>
/// ボスの突進攻撃パラメータを設定します。
/// </summary>
[CreateAssetMenu(
    fileName = "S1BossChargeAttackParameter",
    menuName = "Game/Parameters/Boss/Attack/Charge Attack Parameter")]
public sealed class S1BossChargeAttackParameterAsset :
    ScriptableObject
{
    // 突進前の準備時間
    [SerializeField, Header("予備動作")]
    [Min(0.0f)]
    private float m_preparationDuration =
        1.0f;

    // 準備中の回転速度
    [SerializeField]
    [Min(0.0f)]
    private float m_rotationSpeed =
        70.0f;

    // 突進最高速度
    [SerializeField, Header("突進")]
    [Min(0.0f)]
    private float m_chargeSpeed =
        30.0f;

    // 最大突進距離
    [SerializeField]
    [Min(0.0f)]
    private float m_maxChargeDistance =
        30.0f;

    // 対象を通り過ぎる距離
    [SerializeField]
    [Min(0.0f)]
    private float m_targetOverrunDistance =
        5.0f;

    // NavMesh境界との停止余白
    [SerializeField]
    [Min(0.0f)]
    private float m_stopMargin =
        0.5f;

    // 加速時間
    [SerializeField, Header("加速")]
    [Min(0.0f)]
    private float m_accelerationDuration =
        0.5f;

    // 加速イージング
    [SerializeField]
    private AnimationCurve m_accelerationCurve =
        AnimationCurve.EaseInOut(
            0.0f,
            0.0f,
            1.0f,
            1.0f);

    // 減速開始距離
    [SerializeField, Header("減速")]
    [Min(0.0f)]
    private float m_decelerationDistance =
        5.0f;

    // 減速イージング
    [SerializeField]
    private AnimationCurve m_decelerationCurve =
        AnimationCurve.EaseInOut(
            0.0f,
            0.0f,
            1.0f,
            1.0f);

    // 終了硬直時間
    [SerializeField, Header("終了動作")]
    [Min(0.0f)]
    private float m_endDuration =
        1.0f;

    // 突進攻撃中を示すAnimator Bool名
    [SerializeField, Header("Animation")]
    private string m_animationActiveBoolName =
        "Charging";

    // 突進攻撃の段階を示すAnimator Int名
    [SerializeField]
    private string m_animationPhaseIntName =
        "ChargingPhase";

    // 攻撃識別子
    [SerializeField, Header("攻撃判定")]
    private AttackIdentifier m_attackIdentifier;

    /// <summary>
    /// 実行時に使用する突進攻撃パラメータを生成します。
    /// </summary>
    /// <returns>
    /// 突進攻撃パラメータ。
    /// </returns>
    public S1BossChargeAttackParameters CreateParameters()
    {
        return new S1BossChargeAttackParameters(
            m_preparationDuration,
            m_rotationSpeed,
            m_chargeSpeed,
            m_maxChargeDistance,
            m_stopMargin,
            m_endDuration,
            m_accelerationDuration,
            m_accelerationCurve,
            m_decelerationDistance,
            m_decelerationCurve,
            m_targetOverrunDistance,
            m_animationActiveBoolName,
            m_animationPhaseIntName,
            m_attackIdentifier);
    }
}