using UnityEngine;

/// <summary>
/// 地上・空中で使用するレール搭乗判定の半径を設定します。
/// </summary>
[CreateAssetMenu(fileName = "PlayerRailDetectionParameter",
    menuName = "Game/Parameters/Player/Rail Detection Parameter")]
public sealed class PlayerRailDetectionParameterAsset : ScriptableObject
{
    private const float MinimumRadius = 0.01f;

    [SerializeField, Min(MinimumRadius), InspectorName("地上のレール判定半径（m）")]
    [Tooltip("接地中の搭乗判定の半径です。大きくするとレールを拾いやすくなります。")]
    private float m_groundedRadius = 0.62f;

    [SerializeField, Min(MinimumRadius), InspectorName("空中のレール判定半径（m）")]
    [Tooltip("空中の搭乗判定の半径です。プレイヤー本体の衝突形状や接地判定には影響しません。")]
    private float m_airborneRadius = 0.9f;

    /// <summary>
    /// 接地状態に対応するレール搭乗判定の半径をメートル単位で取得します。
    /// </summary>
    public float GetRadius(bool isGrounded)
    {
        return Mathf.Max(MinimumRadius, isGrounded ? m_groundedRadius : m_airborneRadius);
    }

    /// <summary>
    /// Inspectorで入力された判定半径を正の値に制限します。
    /// </summary>
    private void OnValidate()
    {
        m_groundedRadius = Mathf.Max(MinimumRadius, m_groundedRadius);
        m_airborneRadius = Mathf.Max(MinimumRadius, m_airborneRadius);
    }
}
