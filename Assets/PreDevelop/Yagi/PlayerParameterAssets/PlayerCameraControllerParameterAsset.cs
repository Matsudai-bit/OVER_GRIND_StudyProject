using UnityEngine;

/// <summary>PlayerCameraの感度・チャージ中のカメラ設定を保持します。</summary>
[CreateAssetMenu(fileName = "PlayerCameraControllerParameter", menuName = "Game/Parameters/Player/Camera Controller")]
public sealed class PlayerCameraControllerParameterAsset : ScriptableObject
{
    [SerializeField, Range(0.1f, 5.0f)] private float m_cameraSensitivityX = 1.0f;
    [SerializeField, Range(0.1f, 5.0f)] private float m_cameraSensitivityY = 1.0f;
    [SerializeField] private bool m_isCameraReverseY;
    [SerializeField, Range(0.0f, 1.0f)] private float m_chargeCameraDirectionInfluence = 1.0f;
    [SerializeField, Range(-10.0f, 45.0f)] private float m_chargeCameraVerticalAngle = 8.0f;
    [SerializeField, Min(0.0f)] private float m_chargeCameraVerticalTurnSpeed = 120.0f;

    public float CameraSensitivityX => Mathf.Clamp(m_cameraSensitivityX, 0.1f, 5.0f);
    public float CameraSensitivityY => Mathf.Clamp(m_cameraSensitivityY, 0.1f, 5.0f);
    public bool IsCameraReverseY => m_isCameraReverseY;
    public float ChargeCameraDirectionInfluence => Mathf.Clamp01(m_chargeCameraDirectionInfluence);
    public float ChargeCameraVerticalAngle => Mathf.Clamp(m_chargeCameraVerticalAngle, -10.0f, 45.0f);
    public float ChargeCameraVerticalTurnSpeed => Mathf.Max(0.0f, m_chargeCameraVerticalTurnSpeed);
}
