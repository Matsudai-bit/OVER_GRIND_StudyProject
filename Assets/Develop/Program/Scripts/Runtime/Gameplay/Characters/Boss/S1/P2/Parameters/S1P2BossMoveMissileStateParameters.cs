using System;


using UnityEngine;


/// <summary>
/// S1P2ボスの移動ミサイル状態で使用するパラメータをまとめます。
/// </summary>
public readonly struct S1P2BossMoveMissileStateParameters
{
    /// <summary>
    /// 移動状態のパラメータを取得します。
    /// </summary>
    public S1P2BossMoveStateParameters Move { get; }

    /// <summary>
    /// ミサイル状態のパラメータを取得します。
    /// </summary>
    public S1P2BossMissileStateParameters Missile { get; }

    /// <summary>
    /// 移動ミサイル状態のパラメータを生成します。
    /// </summary>
    /// <param name="move">移動状態のパラメータ。</param>
    /// <param name="missile">ミサイル状態のパラメータ。</param>
    public S1P2BossMoveMissileStateParameters(
        S1P2BossMoveStateParameters move,
        S1P2BossMissileStateParameters missile)
    {
        Move = move;
        Missile = missile;
    }

    /// <summary>
    /// 必要なパラメータが設定されているか確認します。
    /// </summary>
    /// <returns>
    /// true：必要なパラメータが設定されています。
    /// false：パラメータが不足しています。
    /// </returns>
    public bool HasRequiredParameters()
    {
        return Move != null &&
               Missile != null &&
               Missile.HasRequiredParameters();
    }
}


/// <summary>
/// S1P2ボスの移動ミサイル状態で使用するパラメータの参照方法を設定します。
/// </summary>
[Serializable]
public sealed class S1P2BossMoveMissileStateParameterSettings
{
    // 通常の移動状態パラメータを共通使用するか
    [SerializeField, Header("共通設定")]
    private bool m_useSharedMoveParameters = true;

    // 通常のミサイル状態パラメータを共通使用するか
    [SerializeField]
    private bool m_useSharedMissileParameters = true;

    // 移動ミサイル状態専用の移動パラメータ
    [SerializeField, Header("専用移動パラメータ")]
    private S1P2BossMoveStateParameters m_moveParameters = new();

    // 移動ミサイル状態専用のミサイルパラメータ
    [SerializeField, Header("専用ミサイルパラメータ")]
    private S1P2BossMissileStateParameters m_missileParameters = new();

    /// <summary>
    /// 通常の移動状態パラメータを共通使用するか取得します。
    /// </summary>
    public bool UseSharedMoveParameters =>
        m_useSharedMoveParameters;

    /// <summary>
    /// 通常のミサイル状態パラメータを共通使用するか取得します。
    /// </summary>
    public bool UseSharedMissileParameters =>
        m_useSharedMissileParameters;

    /// <summary>
    /// 移動ミサイル状態で使用するパラメータを生成します。
    /// </summary>
    /// <param name="sharedMoveParameters">
    /// 通常の移動状態で使用する共通パラメータ。
    /// </param>
    /// <param name="sharedMissileParameters">
    /// 通常のミサイル状態で使用する共通パラメータ。
    /// </param>
    /// <returns>解決済みの移動ミサイル状態パラメータ。</returns>
    public S1P2BossMoveMissileStateParameters CreateParameters(
        S1P2BossMoveStateParameters sharedMoveParameters,
        S1P2BossMissileStateParameters sharedMissileParameters)
    {
        S1P2BossMoveStateParameters moveParameters =
            m_useSharedMoveParameters
                ? sharedMoveParameters
                : m_moveParameters;

        S1P2BossMissileStateParameters missileParameters =
            m_useSharedMissileParameters
                ? sharedMissileParameters
                : m_missileParameters;

        return new S1P2BossMoveMissileStateParameters(
            moveParameters,
            missileParameters);
    }

    /// <summary>
    /// 移動ミサイル状態で使用するパラメータを取得します。
    /// </summary>
    /// <param name="sharedMoveParameters">
    /// 通常の移動状態で使用する共通パラメータ。
    /// </param>
    /// <param name="sharedMissileParameters">
    /// 通常のミサイル状態で使用する共通パラメータ。
    /// </param>
    /// <param name="parameters">
    /// 解決済みの移動ミサイル状態パラメータ。
    /// </param>
    /// <returns>
    /// true：必要なパラメータを取得できました。
    /// false：パラメータが不足しています。
    /// </returns>
    public bool TryCreateParameters(
        S1P2BossMoveStateParameters sharedMoveParameters,
        S1P2BossMissileStateParameters sharedMissileParameters,
        out S1P2BossMoveMissileStateParameters parameters)
    {
        parameters =
            CreateParameters(
                sharedMoveParameters,
                sharedMissileParameters);

        return parameters.HasRequiredParameters();
    }
}

