using UnityEngine;

/// <summary>
/// プレイヤーのロックオンに関するパラメータを保持します。
/// </summary>
public readonly struct PlayerTargetParameter
{
    /// <summary>
    /// ロックオンパラメータを生成します。
    /// </summary>
    public PlayerTargetParameter(
        float targetSearchRadius,
        LayerMask targetLayer,
        float reticleDistanceFromTarget,
        float reticleMoveDuration,
        float cameraFocusDuration)
    {
        m_targetSearchRadius = targetSearchRadius;
        m_targetLayer = targetLayer;
        m_reticleDistanceFromTarget = reticleDistanceFromTarget;
        m_reticleMoveDuration = reticleMoveDuration;
        m_cameraFocusDuration = cameraFocusDuration;
    }

    /// <summary>
    /// ロックオン対象を探す範囲の半径を取得します。
    /// </summary>
    private float m_targetSearchRadius { get; }

    /// <summary>
    /// ロックオン対象として検索するLayerを取得します。
    /// </summary>
    private LayerMask m_targetLayer { get; }

    /// <summary>
    /// 対象からのレティクル距離を取得します。
    /// </summary>
    private float m_reticleDistanceFromTarget { get; }

    /// <summary>
    /// レティクル移動時間を取得します。
    /// </summary>
    private float m_reticleMoveDuration { get; }

    /// <summary>
    /// カメラフォーカス時間を取得します。
    /// </summary>
    private float m_cameraFocusDuration { get; }
}
