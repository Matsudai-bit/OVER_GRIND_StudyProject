using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ドレット攻撃状態で使用する参照を保持します。
/// </summary>
[Serializable]
public sealed class DreadAttackStateReferences
{


    // オイルPrefab
    [SerializeField, Header("オイル")]
    private S1P3OilController m_oilPrefab;

    // オイル発射地点
    [SerializeField]
    private List<Transform> m_launchPoints = new();

    // 着地点として使用する地面Layer
    [SerializeField, Header("地面")]
    private LayerMask m_groundLayerMask;

    [SerializeField, Header("Decal")]
    private TargetDecalController m_targetDecalPrefab;

    [SerializeField]
    private S1P3OilDecalController m_oilDecalPrefab;

    /// <summary>
    /// オイルPrefabを取得します。
    /// </summary>
    public S1P3OilController OilPrefab =>
        m_oilPrefab;

    /// <summary>
    /// オイル発射地点を取得します。
    /// </summary>
    public IReadOnlyList<Transform> LaunchPoints =>
        m_launchPoints;

    /// <summary>
    /// 地面Layerを取得します。
    /// </summary>
    public LayerMask GroundLayerMask =>
        m_groundLayerMask;

    /// <summary>
    /// 着地点予告Prefabを取得します。
    /// </summary>
    public TargetDecalController TargetDecalPrefab =>
        m_targetDecalPrefab;

    /// <summary>
    /// オイル着弾痕Prefabを取得します。
    /// </summary>
    public S1P3OilDecalController OilDecalPrefab =>
        m_oilDecalPrefab;
}