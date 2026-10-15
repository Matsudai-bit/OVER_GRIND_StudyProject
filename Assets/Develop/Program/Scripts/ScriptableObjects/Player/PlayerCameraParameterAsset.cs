using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerCameraParamater",
    menuName = "Game/Parameters/Player/Camera Parameter")]
public class PlayerCameraParameterAsset : ScriptableObject
{
    [SerializeField, Range(0.1f, 5.0f)]
    [Tooltip("X軸カメラ感度。")]
    private float m_cameraSensitivityX = 1.0f;

    [SerializeField, Range(0.1f, 5.0f)]
    [Tooltip("Y軸カメラ感度。")]
    private float m_cameraSensitivityY = 1.0f;

    [SerializeField]
    [Tooltip("Y軸カメラの操作を反転させるかどうか。")]
    private bool m_isCameraReverseY = false;


    [SerializeField, Range(0.0f, 1.0f)]
    [Tooltip("チャージ中に進行方向へ向く割合。0でチャージ開始時の向き、1で進行方向を向きます。")]
    private float m_chargeCameraDirectionInfluence = 1.0f;

    [SerializeField, Range(-10.0f, 45.0f)]
    [Tooltip("チャージ中に維持するカメラの上下角度。Playerを少し上から見る角度です。")]
    private float m_chargeCameraVerticalAngle = 8.0f;

    [SerializeField, Min(0.0f)]
    [Tooltip("チャージ中にカメラの高さを目的角度へ戻す速度[度/秒]。")]
    private float m_chargeCameraVerticalTurnSpeed = 120.0f;

    public PlayerCameraParameter CreateCameraParameter()
    {
        return new PlayerCameraParameter(
            m_cameraSensitivityX,
            m_cameraSensitivityY,
            m_isCameraReverseY,
            m_chargeCameraDirectionInfluence,
            m_chargeCameraVerticalAngle,
            m_chargeCameraVerticalTurnSpeed
        );
    }
}
