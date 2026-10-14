using UnityEngine;

/// <summary>PlayerMonitorの接地・レール検知設定を保持します。</summary>
[CreateAssetMenu(fileName = "PlayerMonitorParameter", menuName = "Game/Parameters/Player/Monitor")]
public sealed class PlayerMonitorParameterAsset : ScriptableObject
{
    [SerializeField, Min(0.01f)] private float m_groundCheckRadius = 0.25f;
    [SerializeField] private LayerMask m_groundLayerMask = ~0;
    [SerializeField] private LayerMask m_railLayerMask = ~0;
    [SerializeField] private PlayerRailDetectionParameterAsset m_railDetectionParameter;

    public float GroundCheckRadius => Mathf.Max(0.01f, m_groundCheckRadius);
    public LayerMask GroundLayerMask => m_groundLayerMask;
    public LayerMask RailLayerMask => m_railLayerMask;
    public PlayerRailDetectionParameterAsset RailDetectionParameter => m_railDetectionParameter;
}
