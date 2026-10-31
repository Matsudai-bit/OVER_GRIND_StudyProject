using System;
using UnityEngine;

/// <summary>
/// S1P1ボスのミサイル攻撃状態で使用するパラメータを保持します。
/// </summary>
[Serializable]
public sealed class S1P1BossMissileStateParameters
{
    // ミサイル本体の挙動パラメータ
    [SerializeField, Header("ミサイル本体")]
    private MissileParameterAsset m_missileParameterAsset;

    // 1回の攻撃で発射するミサイル数
    [SerializeField, Header("発射"), Min(1)]
    private int m_launchCount = 3;

    // 発射直後に上方向へ進む時間
    [SerializeField, Min(0.0f)]
    private float m_missileUpwardDuration = 1.5f;

    // ミサイル攻撃終了後の停止時間
    [SerializeField, Header("攻撃終了"), Min(0.0f)]
    private float m_idleDuration = 3.0f;

    /// <summary>
    /// ミサイル本体のパラメータを取得します。
    /// </summary>
    public MissileParameterAsset MissileParameterAsset =>
        m_missileParameterAsset;

    /// <summary>
    /// 1回の攻撃で発射するミサイル数を取得します。
    /// </summary>
    public int LaunchCount =>
        Mathf.Max(
            1,
            m_launchCount);

    /// <summary>
    /// ミサイルの上昇時間を取得します。
    /// </summary>
    public float MissileUpwardDuration =>
        m_missileUpwardDuration;

    /// <summary>
    /// 攻撃終了後の停止時間を取得します。
    /// </summary>
    public float IdleDuration =>
        m_idleDuration;

    /// <summary>
    /// 必要なパラメータが設定されているか確認します。
    /// </summary>
    /// <returns>
    /// true：必要なパラメータが設定されています。
    /// false：パラメータが不足しています。
    /// </returns>
    public bool HasRequiredParameters()
    {
        return m_missileParameterAsset != null &&
               m_launchCount > 0;
    }
}