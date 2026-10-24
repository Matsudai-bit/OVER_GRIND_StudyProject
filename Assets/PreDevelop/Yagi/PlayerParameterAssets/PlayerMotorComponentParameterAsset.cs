using UnityEngine;

/// <summary>PlayerMotorの障害物検知設定を保持します。</summary>
[CreateAssetMenu(fileName = "PlayerMotorComponentParameter", menuName = "Game/Parameters/Player/Motor Component")]
public sealed class PlayerMotorComponentParameterAsset : ScriptableObject
{
    [SerializeField] private LayerMask m_obstacleLayerMask;
    [SerializeField, Min(0.0f)] private float m_obstacleSkinWidth = 0.05f;
    [SerializeField, Min(0.0f)] private float m_obstacleDecelerationPerSecond = 20.0f;
    [SerializeField, Header("高速衝突")]
    [Tooltip("衝突による意図しない上向き速度を抑制する最低水平速度です。")]
    [Min(0.0f)]
    private float m_collisionLiftPreventionMinSpeed = 16.0f;

    /// <summary>障害物として検知するレイヤーを取得します。</summary>
    public LayerMask ObstacleLayerMask => m_obstacleLayerMask;

    /// <summary>障害物の手前で確保する間隔を取得します。</summary>
    public float ObstacleSkinWidth => Mathf.Max(0.0f, m_obstacleSkinWidth);

    /// <summary>障害物接触中の1秒あたりの減速量を取得します。</summary>
    public float ObstacleDecelerationPerSecond => Mathf.Max(0.0f, m_obstacleDecelerationPerSecond);

    /// <summary>衝突による意図しない上向き速度を抑制する最低水平速度を取得します。</summary>
    public float CollisionLiftPreventionMinSpeed =>
        Mathf.Max(0.0f, m_collisionLiftPreventionMinSpeed);
}
