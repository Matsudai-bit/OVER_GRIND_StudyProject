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

    [SerializeField, Header("ミサイルを生成する親")]
    private Transform m_missileParent;


    // ミサイルの移動パラメータ
    [SerializeField]
    private MissileParameterAsset m_missileParameter;

    // ミサイルの発射地点
    [SerializeField, Header("発射地点")]
    private Transform[] m_launchSites;

    // ミサイルを順番に発射する間隔
    [SerializeField, Header("発射設定"), Min(0.0f)]
    private float m_launchInterval = 0.5f;
    [SerializeField, Header("発射直後のミサイルの上昇時間")]
    private float m_missileUpwardDuration = 1.5f;

    /// <summary>
    /// ミサイルPrefabを取得します。
    /// </summary>
    public S1P1MissileController MissilePrefab =>
        m_missilePrefab;

    /// <summary>
    /// ミサイルを生成する親を取得
    /// </summary>
    public Transform MissileParent =>
        m_missileParent;

    /// <summary>
    /// ミサイルパラメータを取得します。
    /// </summary>
    public MissileParameterAsset MissileParameter =>
        m_missileParameter;

    /// <summary>
    /// 発射地点一覧を取得します。
    /// </summary>
    public IReadOnlyList<Transform> LaunchSites =>
        m_launchSites;

    /// <summary>
    /// 発射間隔を取得します。
    /// </summary>
    public float LaunchInterval =>
        m_launchInterval;

    /// <summary>
    /// ミサイルの上昇時間
    /// </summary>
    public float MissileUpwardDuration =>
      m_missileUpwardDuration;

    /// <summary>
    /// ミサイル攻撃に必要な設定が存在するか確認します。
    /// </summary>
    /// <returns>
    /// true：ミサイル攻撃を実行できます。
    /// false：必要な設定が不足しています。
    /// </returns>
    public bool HasRequiredReferences()
    {
        if (m_missilePrefab == null ||
            m_missileParameter == null ||
            m_launchSites == null ||
            m_launchSites.Length == 0 ||
            m_missileParent　== null)
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