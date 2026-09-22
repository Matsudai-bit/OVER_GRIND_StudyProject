using Unity.AI.Navigation;
using UnityEngine;

/// <summary>
/// ボスのフェーズごとの共通参照を保持します。
/// </summary>
[DisallowMultipleComponent]
public sealed class BossPhaseReferences : MonoBehaviour
{
    // フェーズで使用する地面Collider
    [SerializeField, Header("地面")]
    private Collider m_groundCollider;

    // フェーズで使用するNavMeshSurface
    [SerializeField, Header("ナビゲーション")]
    private NavMeshSurface m_navMeshSurface;

    // ボスのNavMesh占有範囲
    [SerializeField, Header("NavMesh占有範囲")]
    private BossNavMeshFootprint m_bossNavMeshFootprint;

    [SerializeField, Header("原点")]
    private Transform m_bossOrigin;

    // プレイヤーの座標
    [SerializeField, Header("プレイヤーの座標")]
    private Transform m_playerTransform;


    /// <summary>
    /// フェーズで使用する地面Colliderを取得します。
    /// </summary>
    public Collider GroundCollider => m_groundCollider;

    /// <summary>
    /// フェーズで使用するNavMeshSurfaceを取得します。
    /// </summary>
    public NavMeshSurface NavMeshSurface => m_navMeshSurface;

    public Transform Origin => m_bossOrigin;
    public Transform PlayerTransform => m_playerTransform;

    /// <summary>
    ///  占有範囲の取得
    /// </summary>
    public BossNavMeshFootprint NavMeshFootprint => m_bossNavMeshFootprint;


    /// <summary>
    /// 必須参照が設定されているか確認します。
    /// </summary>
    /// <returns>
    /// true：必要な参照が設定されています。
    /// false：必要な参照が不足しています。
    /// </returns>
    public bool IsValid()
    {
        return m_groundCollider != null &&
               m_navMeshSurface != null;
    }
}