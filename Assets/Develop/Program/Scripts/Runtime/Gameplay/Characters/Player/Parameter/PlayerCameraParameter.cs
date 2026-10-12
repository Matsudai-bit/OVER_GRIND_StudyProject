using UnityEngine;

public readonly struct PlayerCameraParameter
{
    public PlayerCameraParameter(
        float cameraSensitivityX,
        float cameraSensitivityY,
        bool cameraReverseY,
        float cameraBaseDistance,
        float cameraMinDistance,
        float cameraMaxDistance,
        Vector3 cameraDefaultOffset,
        float cameraFollowSpeed,
        float chargeCameraDirectionInfluence,
        float chargeCameraVerticalAngle,
        float chargeCameraVerticalTurnSpeed)
    {
        m_cameraSensitivityX = cameraSensitivityX;
        m_cameraSensitivityY = cameraSensitivityY;
        m_isCameraReverseY = cameraReverseY;

        m_cameraBaseDistance = cameraBaseDistance;
        m_cameraMinDistance = cameraMinDistance;
        m_cameraMaxDistance = cameraMaxDistance;
        m_cameraDefaultOffset = cameraDefaultOffset;
        m_cameraFollowSpeed = cameraFollowSpeed;

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
    /// プレイヤーとの基本距離を取得します。
    /// </summary> 
    public float m_cameraBaseDistance { get; }

    /// <summary>
    /// プレイヤーとの最小距離を取得します。
    /// </summary>
    public float m_cameraMinDistance { get; }

    /// <summary>
    /// プレイヤーとの最大距離を取得します。
    /// </summary>
    public float m_cameraMaxDistance { get; }

    /// <summary>
    /// カメラの初期位置 / 基準位置を取得します。
    /// </summary>
    public Vector3 m_cameraDefaultOffset { get; }

    /// <summary>
    /// プレイヤーへの追従速度を取得します。
    /// </summary>
    public float m_cameraFollowSpeed { get; }


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
