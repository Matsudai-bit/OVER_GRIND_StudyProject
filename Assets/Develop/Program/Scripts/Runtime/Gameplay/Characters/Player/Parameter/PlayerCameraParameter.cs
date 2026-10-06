using UnityEngine;

public readonly struct PlayerCameraParameter
{
    public PlayerCameraParameter(
        float cameraSensitivityX,
        float cameraSensitivityY,
        bool cameraReverseY,
        float chargeCameraDirectionInfluence,
        float chargeCameraVerticalAngle,
        float chargeCameraVerticalTurnSpeed)
    {
        m_cameraSensitivityX = cameraSensitivityX;
        m_cameraSensitivityY = cameraSensitivityY;
        m_isCameraReverseY = cameraReverseY;

        m_chargeCameraDirectionInfluence = chargeCameraDirectionInfluence;
        m_chargeCameraVerticalAngle = chargeCameraVerticalAngle;
        m_chargeCameraVerticalTurnSpeed = chargeCameraVerticalTurnSpeed;
    }

    /// <summary>
    /// X軸カメラ感度を取得します。
    /// </summary>
    public float m_cameraSensitivityX { get; }

    /// <summary>
    /// Y軸カメラ感度を取得します。
    /// </summary>
    public float m_cameraSensitivityY { get; }

    /// <summary>
    /// Y軸カメラの操作を反転させるかどうかを取得します。
    /// </summary>
    public bool m_isCameraReverseY { get; }

    /// <summary>
    /// チャージ中に進行方向へ向く割合を取得します。
    /// </summary>
    public float m_chargeCameraDirectionInfluence { get; }

    /// <summary>
    /// チャージ中に維持するカメラの上下角度を取得します。
    /// </summary>
    public float m_chargeCameraVerticalAngle { get; }

    /// <summary>
    /// チャージ中にカメラの高さを目的角度へ戻す速度[度/秒]を取得します。
    /// </summary>
    public float m_chargeCameraVerticalTurnSpeed { get; }
}
