using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerGroundParamater",
    menuName = "Game/Parameters/Player/Ground Parameter")]
public class PlayerGroundParameterAsset : ScriptableObject
{ 
    [SerializeField, Header("接地判定関連")]
    [Tooltip("接地判定を行う位置")]
    private Transform m_groundCheckOrigin;

    [SerializeField, Min(0.01f)]
    [Tooltip("接地判定の半径")]
    private float m_groundCheckRadius = 0.25f;

    [SerializeField]
    [Tooltip("接地対象のレイヤー")]
    private LayerMask m_groundLayerMask = ~0;

    [SerializeField]
    [Tooltip("接地対象のレイヤー（レール）")]
    private LayerMask m_railLayerMask = ~0;

    [SerializeField, Header("レール搭乗判定")]
    [Tooltip("地上・空中の判定半径を設定するアセットです。未設定の場合は従来の接地半径を使います。")]
    private PlayerRailDetectionParameterAsset m_railDetectionParameter;

    /// <summary>
    /// 接地パラメータを生成します。
    /// </summary>
    /// <returns>接地パラメータ。</returns>
    public PlayerGroundParameter CreateGroundParameter()
    {
        return new PlayerGroundParameter(
            m_groundCheckOrigin,
            m_groundCheckRadius,
            m_groundLayerMask,
            m_railLayerMask,
            m_railDetectionParameter
        );
    }
}
