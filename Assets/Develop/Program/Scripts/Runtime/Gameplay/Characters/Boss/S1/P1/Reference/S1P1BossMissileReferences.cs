using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// S1P1ボスのミサイル攻撃に必要な参照を管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class S1P1BossMissileReferences : MonoBehaviour
{
    // 生成するミサイルPrefab
    [SerializeField, Header("ミサイル")]
    private S1P1MissileController m_missilePrefab;

    // ミサイルを生成する親
    [SerializeField]
    private Transform m_missileParent;

    // ミサイルの発射地点
    [SerializeField, Header("発射地点")]
    private Transform[] m_launchSites;

    /// <summary>
    /// ミサイルPrefabを取得します。
    /// </summary>
    public S1P1MissileController MissilePrefab =>
        m_missilePrefab;

    /// <summary>
    /// ミサイルを生成する親を取得します。
    /// </summary>
    public Transform MissileParent =>
        m_missileParent;

    /// <summary>
    /// 発射地点一覧を取得します。
    /// </summary>
    public IReadOnlyList<Transform> LaunchSites =>
        m_launchSites;

    /// <summary>
    /// ミサイル攻撃に必要な参照が存在するか確認します。
    /// </summary>
    /// <returns>
    /// true：必要な参照が設定されています。
    /// false：参照が不足しています。
    /// </returns>
    public bool HasRequiredReferences()
    {
        if (m_missilePrefab == null ||
            m_missileParent == null ||
            m_launchSites == null ||
            m_launchSites.Length == 0)
        {
            return false;
        }

        foreach (Transform launchSite in m_launchSites)
        {
            if (launchSite != null)
            {
                return true;
            }
        }

        return false;
    }
}
