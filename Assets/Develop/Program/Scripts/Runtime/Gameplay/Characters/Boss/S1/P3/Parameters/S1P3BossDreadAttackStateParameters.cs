using System;
using UnityEngine;

/// <summary>
/// S1P3ボスのドレット攻撃状態で使用するパラメータを保持します。
/// </summary>
[Serializable]
public sealed class S1P3BossDreadAttackStateParameters
{
    // オイルの上昇時間
    [SerializeField, Header("オイル飛行"), Min(0.0f)]
    private float m_ascentDuration = 0.5f;

    // 着地点上空まで移動する時間
    [SerializeField, Min(0.0f)]
    private float m_horizontalMoveDuration = 0.5f;

    // 着地点まで落下する時間
    [SerializeField, Min(0.0f)]
    private float m_fallDuration = 1.0f;

    // オイルが飛翔する高さ
    [SerializeField, Min(0.0f)]
    private float m_flightHeight = 10.0f;

    // Playerを中心とした着地点のランダム半径
    [SerializeField, Header("着地点"), Min(0.0f)]
    private float m_landingRadius = 5.0f;

    // 地面探索Rayの開始高さ
    [SerializeField, Min(0.0f)]
    private float m_groundRaycastStartHeight = 30.0f;

    // 地面探索Rayの最大距離
    [SerializeField, Min(0.0f)]
    private float m_groundRaycastDistance = 100.0f;

    // 全オイル着弾後から状態終了までの時間
    [SerializeField, Header("状態終了"), Min(0.0f)]
    private float m_finishDelay = 2.0f;
    // 着弾予告の最小サイズ
    [SerializeField, Header("着弾予告"), Min(0.0f)]
    private float m_targetDecalMinScale = 0.3f;

    // 着弾予告の最大サイズ
    [SerializeField, Min(0.0f)]
    private float m_targetDecalMaxScale = 1.0f;

    // 着弾予告の最小不透明度
    [SerializeField, Range(0.0f, 1.0f)]
    private float m_targetDecalMinAlpha = 0.15f;

    // 着弾予告の最大不透明度
    [SerializeField, Range(0.0f, 1.0f)]
    private float m_targetDecalMaxAlpha = 0.8f;

    // 地面との表示ずれ防止用オフセット
    [SerializeField, Min(0.0f)]
    private float m_decalGroundOffset = 0.02f;

    // 着弾痕を表示する時間
    [SerializeField, Header("着弾痕"), Min(0.0f)]
    private float m_oilDecalDuration = 5.0f;
    /// <summary>
    /// 上昇時間を取得します。
    /// </summary>
    public float AscentDuration =>
        m_ascentDuration;

    /// <summary>
    /// 着地点上空への移動時間を取得します。
    /// </summary>
    public float HorizontalMoveDuration =>
        m_horizontalMoveDuration;

    /// <summary>
    /// 落下時間を取得します。
    /// </summary>
    public float FallDuration =>
        m_fallDuration;

    /// <summary>
    /// 飛翔高度を取得します。
    /// </summary>
    public float FlightHeight =>
        m_flightHeight;

    /// <summary>
    /// 着地点のランダム半径を取得します。
    /// </summary>
    public float LandingRadius =>
        m_landingRadius;

    /// <summary>
    /// 地面探索Rayの開始高さを取得します。
    /// </summary>
    public float GroundRaycastStartHeight =>
        m_groundRaycastStartHeight;

    /// <summary>
    /// 地面探索Rayの最大距離を取得します。
    /// </summary>
    public float GroundRaycastDistance =>
        m_groundRaycastDistance;

    /// <summary>
    /// 全弾着弾後の待機時間を取得します。
    /// </summary>
    public float FinishDelay =>
        m_finishDelay;

    /// <summary>
    /// 着弾予告の最小サイズを取得します。
    /// </summary>
    public float TargetDecalMinScale =>
        m_targetDecalMinScale;

    /// <summary>
    /// 着弾予告の最大サイズを取得します。
    /// </summary>
    public float TargetDecalMaxScale =>
        m_targetDecalMaxScale;

    /// <summary>
    /// 着弾予告の最小不透明度を取得します。
    /// </summary>
    public float TargetDecalMinAlpha =>
        m_targetDecalMinAlpha;

    /// <summary>
    /// 着弾予告の最大不透明度を取得します。
    /// </summary>
    public float TargetDecalMaxAlpha =>
        m_targetDecalMaxAlpha;

    /// <summary>
    /// Decalの地面からのオフセットを取得します。
    /// </summary>
    public float DecalGroundOffset =>
        m_decalGroundOffset;

    /// <summary>
    /// 着弾痕の表示時間を取得します。
    /// </summary>
    public float OilDecalDuration =>
        m_oilDecalDuration;
}