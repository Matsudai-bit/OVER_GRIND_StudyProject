using UnityEngine;

/// <summary>PlayerTargetingControllerのターゲット設定を保持します。</summary>
[CreateAssetMenu(fileName = "PlayerTargetingControllerParameter", menuName = "Game/Parameters/Player/Targeting Controller")]
public sealed class PlayerTargetingControllerParameterAsset : ScriptableObject
{
    [SerializeField, Min(0.1f)] private float m_targetSearchRadius = 20.0f;
    [SerializeField] private LayerMask m_targetLayer;
    [SerializeField] private TargetReticleBillboard m_targetReticlePrefab;
    [SerializeField, Min(0.0f)] private float m_reticleDistanceFromTarget = 0.15f;
    [SerializeField, Min(0.0f)] private float m_reticleMoveDuration = 0.2f;
    [SerializeField, Min(0.05f)] private float m_cameraFocusDuration = 0.25f;

    public float TargetSearchRadius => Mathf.Max(0.1f, m_targetSearchRadius);
    public LayerMask TargetLayer => m_targetLayer;
    public TargetReticleBillboard TargetReticlePrefab => m_targetReticlePrefab;
    public float ReticleDistanceFromTarget => Mathf.Max(0.0f, m_reticleDistanceFromTarget);
    public float ReticleMoveDuration => Mathf.Max(0.0f, m_reticleMoveDuration);
    public float CameraFocusDuration => Mathf.Max(0.05f, m_cameraFocusDuration);
}
