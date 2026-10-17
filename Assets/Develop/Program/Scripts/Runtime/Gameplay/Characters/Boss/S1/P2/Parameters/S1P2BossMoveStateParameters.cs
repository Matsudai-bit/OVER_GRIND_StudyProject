using System;
using UnityEngine;

/// <summary>
/// S1P2ボスの移動状態で使用するパラメータを保持します。
/// </summary>
[Serializable]
public sealed class S1P2BossMoveStateParameters
{
    // 時間パラメータの最小値
    private const float MIN_TIME = 0.01f;

    // 経路地点数の最小値
    private const int MIN_ROUTE_POINT_COUNT = 2;

    // 選択する経路地点数の最小値
    [SerializeField, Header("経路生成")]
    [Min(MIN_ROUTE_POINT_COUNT)]
    private int m_minRoutePointCount = 3;

    // 選択する経路地点数の最大値
    [SerializeField]
    [Min(MIN_ROUTE_POINT_COUNT)]
    private int m_maxRoutePointCount = 5;

    // 最低1地点に要求するBossからの距離
    [SerializeField, Min(0.0f)]
    private float m_requiredFarPointDistance = 5.0f;

    // 経路生成の最大試行回数
    [SerializeField, Min(1)]
    private int m_routeSearchAttemptCount = 30;

    // 経路を生成・検証するサンプル間隔
    [SerializeField, Min(0.1f)]
    private float m_routeSampleInterval = 3.0f;

    // 開始時の進行方向を曲線へ反映する距離
    [SerializeField, Min(0.0f)]
    private float m_initialDirectionGuideDistance = 5.0f;

    [SerializeField, Header("経路形状")]
    [Min(0.0f)]
    private float m_minRouteSegmentDistance = 5.0f;

    [SerializeField, Range(0.0f, 180.0f)]
    private float m_maxRouteTurnAngle = 100.0f;

    // 最高移動速度
    [SerializeField, Header("移動"), Min(0.0f)]
    private float m_maxMoveSpeed = 34.8f;

    // 最高速度までの到達時間
    [SerializeField, Min(MIN_TIME)]
    private float m_timeToMaxSpeed = 2.0f;

    // 停止までの時間
    [SerializeField, Min(MIN_TIME)]
    private float m_timeToStop = 1.0f;

    // 1秒間の最大回転角度
    [SerializeField, Min(0.0f)]
    private float m_rotationSpeed = 70.0f;

    // 旋回減速を開始する角度
    [SerializeField, Header("旋回"), Range(0.0f, 180.0f)]
    private float m_turnSlowdownStartAngle = 60.0f;

    // その場旋回へ切り替える角度
    [SerializeField, Range(0.0f, 180.0f)]
    private float m_turnInPlaceAngle = 100.0f;

    // 経路上で先読みする距離
    [SerializeField, Header("経路追従"), Min(0.0f)]
    private float m_lookAheadDistance = 50.0f;

    // 最終地点への到着判定距離
    [SerializeField, Min(0.0f)]
    private float m_arrivalDistance = 3.0f;

    /// <summary>
    /// 最小経路地点数を取得します。
    /// </summary>
    public int MinRoutePointCount =>
        m_minRoutePointCount;

    /// <summary>
    /// 最大経路地点数を取得します。
    /// </summary>
    public int MaxRoutePointCount =>
        m_maxRoutePointCount;

    /// <summary>
    /// 最低1地点に要求する距離を取得します。
    /// </summary>
    public float RequiredFarPointDistance =>
        m_requiredFarPointDistance;

    /// <summary>
    /// 経路生成の最大試行回数を取得します。
    /// </summary>
    public int RouteSearchAttemptCount =>
        m_routeSearchAttemptCount;

    /// <summary>
    /// 経路サンプル間隔を取得します。
    /// </summary>
    public float RouteSampleInterval =>
        m_routeSampleInterval;

    /// <summary>
    /// 開始方向を曲線へ反映する距離を取得します。
    /// </summary>
    public float InitialDirectionGuideDistance =>
        m_initialDirectionGuideDistance;

    /// <summary>
    /// 最高移動速度を取得します。
    /// </summary>
    public float MaxMoveSpeed =>
        m_maxMoveSpeed;

    /// <summary>
    /// 最高速度までの到達時間を取得します。
    /// </summary>
    public float TimeToMaxSpeed =>
        m_timeToMaxSpeed;

    /// <summary>
    /// 停止までの時間を取得します。
    /// </summary>
    public float TimeToStop =>
        m_timeToStop;

    /// <summary>
    /// 回転速度を取得します。
    /// </summary>
    public float RotationSpeed =>
        m_rotationSpeed;

    /// <summary>
    /// 経路の先読み距離を取得します。
    /// </summary>
    public float LookAheadDistance =>
        m_lookAheadDistance;

    /// <summary>
    /// 最終地点の到着判定距離を取得します。
    /// </summary>
    public float ArrivalDistance =>
        m_arrivalDistance;

    /// <summary>
    /// 加速度を取得します。
    /// </summary>
    public float Acceleration =>
        m_maxMoveSpeed /
        Mathf.Max(
            m_timeToMaxSpeed,
            MIN_TIME);

    /// <summary>
    /// 減速度を取得します。
    /// </summary>
    public float Deceleration =>
        m_maxMoveSpeed /
        Mathf.Max(
            m_timeToStop,
            MIN_TIME);

    /// <summary>
    /// 旋回減速を開始する角度を取得します。
    /// </summary>
    public float TurnSlowdownStartAngle =>
        m_turnSlowdownStartAngle;

    /// <summary>
    /// その場旋回へ切り替える角度を取得します。
    /// </summary>
    public float TurnInPlaceAngle =>
        m_turnInPlaceAngle;

    /// <summary>
    /// 連続する経路地点間の最低距離を取得します。
    /// </summary>
    public float MinRouteSegmentDistance =>
        m_minRouteSegmentDistance;

    /// <summary>
    /// 経路地点で許容する最大旋回角度を取得します。
    /// </summary>
    public float MaxRouteTurnAngle =>
        m_maxRouteTurnAngle;
}
