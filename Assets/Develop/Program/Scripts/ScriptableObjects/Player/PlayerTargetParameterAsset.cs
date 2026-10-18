using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerTargetParamater",
    menuName = "Game/Parameters/Player/Target Parameter")]
public class PlayerTargetParameterAsset : ScriptableObject
{
    [SerializeField, Header("ロックオン対象")]
    [Tooltip("ロックオン対象を探す範囲"), Min(0.1f)]
    private float m_targetSearchRadius = 20.0f;

    [SerializeField]
    [Tooltip("ロックオン対象として検索するLayer")]
    private LayerMask m_targetLayer;


    [SerializeField, Header("Target Reticle")]
    [Tooltip("対象からのレティクル距離"), Min(0.0f)]
    private float m_reticleDistanceFromTarget = 0.15f;

    [SerializeField]
    [Tooltip("レティクル移動時間"), Min(0.0f)]
    private float m_reticleMoveDuration = 0.2f;


    [SerializeField, Header("Camera")]
    [Tooltip("カメラフォーカス時間"), Min(0.0f)]
    private float m_cameraFocusDuration = 0.25f;

    /// <summary>
    /// ロックオンパラメータを生成します。
    /// </summary>
    /// <returns>ロックオンパラメータ。</returns>
    public PlayerTargetParameter CreateTargetParameter()
    {
        return new PlayerTargetParameter(
            m_targetSearchRadius,
            m_targetLayer,
            m_reticleDistanceFromTarget,
            m_reticleMoveDuration,
            m_cameraFocusDuration
        );
    }
}
