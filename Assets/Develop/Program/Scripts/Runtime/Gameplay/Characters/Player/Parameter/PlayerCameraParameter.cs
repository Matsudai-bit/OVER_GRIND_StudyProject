using UnityEngine;

public readonly struct PlayerCameraParameter
{
    public PlayerCameraParameter(
        float chargeCameraDirectionInfluence,
        float chargeCameraVerticalAngle,
        float chargeCameraVerticalTurnSpeed)
    {
        m_chargeCameraDirectionInfluence = chargeCameraDirectionInfluence;
        m_chargeCameraVerticalAngle = chargeCameraVerticalAngle;
        m_chargeCameraVerticalTurnSpeed = chargeCameraVerticalTurnSpeed;
    }

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
