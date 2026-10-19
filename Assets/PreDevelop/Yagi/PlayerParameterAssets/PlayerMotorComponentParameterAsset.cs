using UnityEngine;

/// <summary>PlayerMotorの障害物検知設定を保持します。</summary>
[CreateAssetMenu(fileName = "PlayerMotorComponentParameter", menuName = "Game/Parameters/Player/Motor Component")]
public sealed class PlayerMotorComponentParameterAsset : ScriptableObject
{
    [SerializeField] private LayerMask m_obstacleLayerMask;
    [SerializeField, Min(0.0f)] private float m_obstacleSkinWidth = 0.05f;
    [SerializeField, Min(0.0f)] private float m_obstacleDecelerationPerSecond = 20.0f;

    public LayerMask ObstacleLayerMask => m_obstacleLayerMask;
    public float ObstacleSkinWidth => Mathf.Max(0.0f, m_obstacleSkinWidth);
    public float ObstacleDecelerationPerSecond => Mathf.Max(0.0f, m_obstacleDecelerationPerSecond);
}
