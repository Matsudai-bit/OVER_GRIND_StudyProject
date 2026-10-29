using UnityEngine;

/// <summary>
/// PlayerMotorコンポーネント固有の物理移動設定を保持します。
/// </summary>
[CreateAssetMenu(
    fileName = "PlayerMotorComponentParameter",
    menuName = "Game/Parameters/Player/Motor Component")]
public sealed class PlayerMotorComponentParameterAsset : ScriptableObject
{
    // 障害物として扱うレイヤー
    [SerializeField, Header("障害物検知")]
    private LayerMask m_obstacleLayerMask;

    // 障害物への食い込みを防ぐための手前バッファ距離
    [SerializeField]
    [Min(0.0f)]
    private float m_obstacleSkinWidth = 0.05f;

    // 障害物に接触している間、1秒あたり減速する速度
    [SerializeField]
    [Min(0.0f)]
    private float m_obstacleDecelerationPerSecond = 20.0f;

    // 高速移動中の衝突で発生する意図しない上向き速度を抑制する最低速度
    [SerializeField, Header("高速衝突")]
    [Tooltip("衝突による意図しない上向き速度を抑制する最低水平速度です。")]
    [Min(0.0f)]
    private float m_collisionLiftPreventionMinSpeed = 16.0f;

    // 自動で乗り越えられる最大段差高さ
    [SerializeField, Header("段差乗り越え")]
    [Min(0.0f)]
    private float m_maxStepHeight = 0.3f;

    // Capsule前方から段差正面を探索する距離
    [SerializeField]
    [Min(0.01f)]
    private float m_stepCheckDistance = 0.25f;

    // 地面付近から段差正面判定を行う最大高さ
    [SerializeField]
    [Tooltip("地面すれすれからこの高さまで複数のRayで段差正面を確認します。")]
    [Min(0.0f)]
    private float m_stepLowerCheckHeight = 0.02f;

    // 段差正面より奥で上面を探索する際のサンプル間隔
    [SerializeField]
    [Min(0.0f)]
    private float m_stepForwardOffset = 0.05f;

    // 乗り越え後のCollider接触を避けるための余白
    [SerializeField]
    [Min(0.0f)]
    private float m_stepClearanceSkin = 0.02f;

    /// <summary>
    /// 障害物として検知するレイヤーを取得します。
    /// </summary>
    public LayerMask ObstacleLayerMask =>
        m_obstacleLayerMask;

    /// <summary>
    /// 障害物の手前で確保する間隔を取得します。
    /// </summary>
    public float ObstacleSkinWidth =>
        Mathf.Max(
            m_obstacleSkinWidth,
            0.0f);

    /// <summary>
    /// 障害物接触中の1秒あたりの減速量を取得します。
    /// </summary>
    public float ObstacleDecelerationPerSecond =>
        Mathf.Max(
            m_obstacleDecelerationPerSecond,
            0.0f);

    /// <summary>
    /// 衝突による意図しない上向き速度を抑制する最低水平速度を取得します。
    /// </summary>
    public float CollisionLiftPreventionMinSpeed =>
        Mathf.Max(
            m_collisionLiftPreventionMinSpeed,
            0.0f);

    /// <summary>
    /// 自動で乗り越えられる最大段差高さを取得します。
    /// </summary>
    public float MaxStepHeight =>
        Mathf.Max(
            m_maxStepHeight,
            0.0f);

    /// <summary>
    /// 段差正面を探索する距離を取得します。
    /// </summary>
    public float StepCheckDistance =>
        Mathf.Max(
            m_stepCheckDistance,
            0.01f);

    /// <summary>
    /// 地面付近から段差正面判定を行う最大高さを取得します。
    /// </summary>
    public float StepLowerCheckHeight =>
        Mathf.Max(
            m_stepLowerCheckHeight,
            0.0f);

    /// <summary>
    /// 段差上面を探索する前方オフセットを取得します。
    /// </summary>
    public float StepForwardOffset =>
        Mathf.Max(
            m_stepForwardOffset,
            0.0f);

    /// <summary>
    /// 段差乗り越え後の空間確認に使用する余白を取得します。
    /// </summary>
    public float StepClearanceSkin =>
        Mathf.Max(
            m_stepClearanceSkin,
            0.0f);
}