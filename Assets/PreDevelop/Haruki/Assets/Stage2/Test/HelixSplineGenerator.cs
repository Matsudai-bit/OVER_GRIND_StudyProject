using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// 指定したパラメータに基づいて螺旋状のスプラインを生成するコンポーネントです。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SplineContainer))]
public class HelixSplineGenerator : MonoBehaviour
{
    /// <summary>
    /// 螺旋が伸びる中心軸の種類を定義します。
    /// </summary>
    public enum HelixAxis
    {
        /// <summary>Y 軸方向。</summary>
        AXIS_Y,
        /// <summary>X 軸方向。</summary>
        AXIS_X,
        /// <summary>Z 軸方向。</summary>
        AXIS_Z
    }

    /// <summary>
    /// ノットの回転における上方向（Up ベクトル）の基準を定義します。
    /// </summary>
    public enum KnotUpMode
    {
        /// <summary>螺旋の中心軸方向を上とします。</summary>
        UP_AXIS,
        /// <summary>中心軸から外側へ向かう方向を上とします。</summary>
        UP_RADIAL_OUTWARD,
        /// <summary>外側から中心軸へ向かう方向を上とします。</summary>
        UP_RADIAL_INWARD
    }

    // デフォルト値と制限値の定数定義
    private const float DEFAULT_START_RADIUS = 2.0f;
    private const float DEFAULT_END_RADIUS = 2.0f;
    private const float DEFAULT_TURNS = 3.0f;
    private const float DEFAULT_HEIGHT = 6.0f;
    private const int DEFAULT_KNOTS_PER_TURN = 8;
    private const float DEFAULT_START_ANGLE_DEGREES = 0.0f;

    private const float MIN_TURNS = 0.01f;
    private const int MIN_KNOTS_PER_TURN = 3;
    private const int MAX_KNOTS_PER_TURN = 64;
    private const int MAX_SPLINE_INDEX = 16;

    // 数学計算用の定数定義
    private const float FULL_CIRCLE_RADIANS = math.PI * 2.0f;
    private const float BEZIER_CIRCLE_FACTOR = 4.0f / 3.0f;
    private const float LINEAR_TANGENT_RATIO = 1.0f / 3.0f;
    private const float QUARTER_FACTOR = 0.25f;
    private const float ANGLE_CEIL_TOLERANCE = 0.0001f;
    private const float MIN_ANGLE_EPSILON = 1e-5f;
    private const float MIN_VECTOR_LENGTH_SQUARED = 1e-8f;
    private const float UP_PARALLEL_THRESHOLD = 0.999f;
    private const float RIGHT_ANGLE_DEGREES = 90.0f;

    [Header("螺旋の基本設定")]
    [Tooltip("開始位置の半径です。")]
    [SerializeField] private float m_startRadius = DEFAULT_START_RADIUS;

    [Tooltip("終了位置の半径です。開始半径と変えることで円錐状の螺旋になります。")]
    [SerializeField] private float m_endRadius = DEFAULT_END_RADIUS;

    [Tooltip("螺旋の巻き数です。小数での指定も可能です。")]
    [SerializeField] private float m_turns = DEFAULT_TURNS;

    [Tooltip("螺旋全体の高さです。負の値で逆方向に伸びます。")]
    [SerializeField] private float m_height = DEFAULT_HEIGHT;

    [Header("分割・向き設定")]
    [Tooltip("1周あたりの分割ノット数です。値が大きいほど真円に近づきます。")]
    [SerializeField] private int m_knotsPerTurn = DEFAULT_KNOTS_PER_TURN;

    [Tooltip("螺旋の開始角度（度数法）です。")]
    [SerializeField] private float m_startAngleDegrees = DEFAULT_START_ANGLE_DEGREES;

    [Tooltip("true の場合は時計回り、false の場合は反時計回りに巻きます。")]
    [SerializeField] private bool m_isClockwise = false;

    [Tooltip("螺旋が伸びるローカル座標の基準軸です。")]
    [SerializeField] private HelixAxis m_helixAxis = HelixAxis.AXIS_Y;

    [Tooltip("各ノットの回転（Up ベクトル）の基準方向です。")]
    [SerializeField] private KnotUpMode m_knotUpMode = KnotUpMode.UP_AXIS;

    [Tooltip("螺旋の中心位置のローカルオフセットです。")]
    [SerializeField] private Vector3 m_centerOffset = Vector3.zero;

    [Header("生成対象・エディタ設定")]
    [Tooltip("生成先となる SplineContainer 内のスプラインインデックスです。")]
    [SerializeField] private int m_splineIndex = 0;

    [Tooltip("Inspector で値を変更したときに自動で再生成するかどうかです。")]
    [SerializeField] private bool m_autoUpdate = true;

    // 対象となる SplineContainer のキャッシュ
    private SplineContainer m_splineContainer;

    /// <summary>
    /// 開始位置の半径を取得または設定します。
    /// </summary>
    public float StartRadius
    {
        get => m_startRadius;
        set => m_startRadius = Mathf.Max(0.0f, value);
    }

    /// <summary>
    /// 終了位置の半径を取得または設定します。
    /// </summary>
    public float EndRadius
    {
        get => m_endRadius;
        set => m_endRadius = Mathf.Max(0.0f, value);
    }

    /// <summary>
    /// 螺旋の巻き数を取得または設定します。
    /// </summary>
    public float Turns
    {
        get => m_turns;
        set => m_turns = Mathf.Max(MIN_TURNS, value);
    }

    /// <summary>
    /// 螺旋全体の高さを取得または設定します。
    /// </summary>
    public float Height
    {
        get => m_height;
        set => m_height = value;
    }

    /// <summary>
    /// 1周あたりの分割ノット数を取得または設定します。
    /// </summary>
    public int KnotsPerTurn
    {
        get => m_knotsPerTurn;
        set => m_knotsPerTurn = Mathf.Clamp(value, MIN_KNOTS_PER_TURN, MAX_KNOTS_PER_TURN);
    }

    /// <summary>
    /// 螺旋の開始角度（度数法）を取得または設定します。
    /// </summary>
    public float StartAngleDegrees
    {
        get => m_startAngleDegrees;
        set => m_startAngleDegrees = value;
    }

    /// <summary>
    /// 時計回りに巻くかどうかを取得または設定します。
    /// </summary>
    public bool IsClockwise
    {
        get => m_isClockwise;
        set => m_isClockwise = value;
    }

    /// <summary>
    /// 螺旋の中心軸を取得または設定します。
    /// </summary>
    public HelixAxis Axis
    {
        get => m_helixAxis;
        set => m_helixAxis = value;
    }

    /// <summary>
    /// ノットの Up ベクトル基準モードを取得または設定します。
    /// </summary>
    public KnotUpMode UpMode
    {
        get => m_knotUpMode;
        set => m_knotUpMode = value;
    }

    /// <summary>
    /// 中心位置のローカルオフセットを取得または設定します。
    /// </summary>
    public Vector3 CenterOffset
    {
        get => m_centerOffset;
        set => m_centerOffset = value;
    }

    /// <summary>
    /// コンポーネントの初期化時またはリセット時に呼び出されます。
    /// </summary>
    private void Reset()
    {
        // アタッチ直後にデフォルト設定で螺旋を生成する
        GenerateHelixSpline();
    }

    /// <summary>
    /// Inspector の値が変更されたときに呼び出されます。
    /// </summary>
    private void OnValidate()
    {
        // 不正な値が入らないようにクランプする
        ValidateParameters();
    }

    /// <summary>
    /// 自動更新が有効かどうかを取得します。
    /// </summary>
    /// <returns>
    /// true：Inspector 変更時に自動再生成を行います。
    /// false：自動再生成を行いません。
    /// </returns>
    public bool GetAutoUpdate()
    {
        return m_autoUpdate;
    }

    /// <summary>
    /// 対象の SplineContainer コンポーネントを取得します。
    /// </summary>
    /// <returns>取得した SplineContainer。存在しない場合は null。</returns>
    public SplineContainer GetTargetSplineContainer()
    {
        // 未キャッシュの場合は自身から取得を試みる
        if (m_splineContainer == null)
        {
            m_splineContainer = GetComponent<SplineContainer>();
        }

        return m_splineContainer;
    }

    /// <summary>
    /// 現在のパラメータをもとに螺旋状のスプラインを生成します。
    /// </summary>
    [ContextMenu("Generate Helix Spline")]
    public void GenerateHelixSpline()
    {
        // パラメータを安全な範囲に補正する
        ValidateParameters();

        // コンテナと対象スプラインの取得・確保を行う
        SplineContainer container = GetTargetSplineContainer();
        if (container == null)
        {
            return;
        }

        Spline targetSpline = GetOrCreateTargetSpline(container);
        if (targetSpline == null)
        {
            return;
        }

        // 既存ノットを消去し、開いたスプラインとして初期化する
        targetSpline.Clear();
        targetSpline.Closed = false;

        // 螺旋生成に必要な角度・分割数・軸回転を事前計算する
        quaternion axisRotation = CalculateAxisRotation(m_helixAxis);
        float startAngleRad = math.radians(m_startAngleDegrees);
        float directionSign = m_isClockwise ? -1.0f : 1.0f;
        float totalAngleRad = m_turns * FULL_CIRCLE_RADIANS * directionSign;

        int segmentCount = math.max(1, (int)math.ceil(m_turns * m_knotsPerTurn - ANGLE_CEIL_TOLERANCE));
        float segmentAngleAbs = math.abs(totalAngleRad) / segmentCount;
        float tangentScale = CalculateBezierTangentScale(segmentAngleAbs);
        float deltaTime = 1.0f / segmentCount;
        float deltaRadius = m_endRadius - m_startRadius;

        // 各分割点にベジェノットを計算して順番に追加する
        for (int i = 0; i <= segmentCount; i++)
        {
            float progress = i * deltaTime;
            BezierKnot knot = CreateHelixKnot(
                progress,
                deltaTime,
                startAngleRad,
                totalAngleRad,
                deltaRadius,
                tangentScale,
                axisRotation
            );

            targetSpline.Add(knot, TangentMode.Continuous);
        }
    }

    /// <summary>
    /// 設定値を安全な範囲に補正します。
    /// </summary>
    private void ValidateParameters()
    {
        m_startRadius = Mathf.Max(0.0f, m_startRadius);
        m_endRadius = Mathf.Max(0.0f, m_endRadius);
        m_turns = Mathf.Max(MIN_TURNS, m_turns);
        m_knotsPerTurn = Mathf.Clamp(m_knotsPerTurn, MIN_KNOTS_PER_TURN, MAX_KNOTS_PER_TURN);
        m_splineIndex = Mathf.Clamp(m_splineIndex, 0, MAX_SPLINE_INDEX);
    }

    /// <summary>
    /// 指定インデックスに対応する Spline を取得し、不足している場合は新規追加します。
    /// </summary>
    /// <param name="container">対象の SplineContainer。</param>
    /// <returns>操作対象の Spline。</returns>
    private Spline GetOrCreateTargetSpline(SplineContainer container)
    {
        if (container == null)
        {
            return null;
        }

        // 指定インデックスに到達するまでスプラインを追加する
        while (container.Splines.Count <= m_splineIndex)
        {
            container.AddSpline();
        }

        return container.Splines[m_splineIndex];
    }

    /// <summary>
    /// 螺旋の基準軸に対応する回転クォータニオンを算出します。
    /// </summary>
    /// <param name="axis">基準となる軸。</param>
    /// <returns>Y 軸基準から指定軸へ変換する回転。</returns>
    private quaternion CalculateAxisRotation(HelixAxis axis)
    {
        switch (axis)
        {
            case HelixAxis.AXIS_X:
                return quaternion.Euler(0.0f, 0.0f, math.radians(-RIGHT_ANGLE_DEGREES));
            case HelixAxis.AXIS_Z:
                return quaternion.Euler(math.radians(RIGHT_ANGLE_DEGREES), 0.0f, 0.0f);
            case HelixAxis.AXIS_Y:
            default:
                return quaternion.identity;
        }
    }

    /// <summary>
    /// 3次ベジェ曲線で円弧を高精度に近似するための接線スケール係数を計算します。
    /// </summary>
    /// <param name="segmentAngleAbs">1セグメントあたりの角度（ラジアン）。</param>
    /// <returns>微分ベクトルに対する接線長係数。</returns>
    private float CalculateBezierTangentScale(float segmentAngleAbs)
    {
        // 極小角度時のゼロ除算を回避する
        if (segmentAngleAbs < MIN_ANGLE_EPSILON)
        {
            return LINEAR_TANGENT_RATIO;
        }

        return (BEZIER_CIRCLE_FACTOR * math.tan(segmentAngleAbs * QUARTER_FACTOR)) / segmentAngleAbs;
    }

    /// <summary>
    /// 指定した進行度における螺旋上の BezierKnot を生成します。
    /// </summary>
    /// <param name="progress">全体の進行度（0.0 ～ 1.0）。</param>
    /// <param name="deltaTime">1セグメントあたりの進行度幅。</param>
    /// <param name="startAngleRad">開始角度（ラジアン）。</param>
    /// <param name="totalAngleRad">総回転角度（ラジアン）。</param>
    /// <param name="deltaRadius">開始半径から終了半径への変化量。</param>
    /// <param name="tangentScale">ベジェ接線のスケーリング係数。</param>
    /// <param name="axisRotation">基準軸へ変換するための回転。</param>
    /// <returns>構築された BezierKnot。</returns>
    private BezierKnot CreateHelixKnot(
        float progress,
        float deltaTime,
        float startAngleRad,
        float totalAngleRad,
        float deltaRadius,
        float tangentScale,
        quaternion axisRotation
    )
    {
        // Y 軸基準空間での現在角度・半径・高さを計算する
        float angle = startAngleRad + totalAngleRad * progress;
        float radius = m_startRadius + deltaRadius * progress;
        float heightPos = m_height * progress;

        float cosAngle = math.cos(angle);
        float sinAngle = math.sin(angle);

        // 位置を算出して指定軸とオフセットを適用する
        float3 basePosition = new float3(radius * cosAngle, heightPos, radius * sinAngle);
        float3 knotPosition = math.mul(axisRotation, basePosition) + (float3)m_centerOffset;

        // 進行度による位置の微分ベクトル（接線方向）を算出する
        float derivativeX = deltaRadius * cosAngle - radius * totalAngleRad * sinAngle;
        float derivativeY = m_height;
        float derivativeZ = deltaRadius * sinAngle + radius * totalAngleRad * cosAngle;

        float3 baseTangent = new float3(derivativeX, derivativeY, derivativeZ) * (deltaTime * tangentScale);
        float3 rotatedTangent = math.mul(axisRotation, baseTangent);

        // 接線の長さと進行方向ベクトルを決定する
        float tangentLength = math.length(rotatedTangent);
        float3 defaultForward = math.mul(axisRotation, new float3(0.0f, 0.0f, 1.0f));
        float3 forward = math.normalizesafe(rotatedTangent, defaultForward);

        // ノットの回転と前後の接線を組み立てる
        float3 radialBaseDir = new float3(cosAngle, 0.0f, sinAngle);
        float3 upVector = ResolveUpVector(forward, radialBaseDir, axisRotation);
        quaternion knotRotation = quaternion.LookRotationSafe(forward, upVector);

        float3 tangentIn = new float3(0.0f, 0.0f, -tangentLength);
        float3 tangentOut = new float3(0.0f, 0.0f, tangentLength);

        return new BezierKnot(knotPosition, tangentIn, tangentOut, knotRotation);
    }

    /// <summary>
    /// ノットの回転に使用する Up ベクトルを安全に決定します。
    /// </summary>
    /// <param name="forward">ノットの進行方向（正規化済み）。</param>
    /// <param name="radialBaseDir">Y 軸基準空間における外向き放射ベクトル。</param>
    /// <param name="axisRotation">基準軸へ変換するための回転。</param>
    /// <returns>LookRotation に安全に使用できる Up ベクトル。</returns>
    private float3 ResolveUpVector(float3 forward, float3 radialBaseDir, quaternion axisRotation)
    {
        float3 axisUp = math.mul(axisRotation, new float3(0.0f, 1.0f, 0.0f));
        float3 radialOut = math.mul(axisRotation, radialBaseDir);

        // 設定されたモードに応じて優先する Up ベクトルを選択する
        float3 desiredUp = axisUp;
        if (m_knotUpMode == KnotUpMode.UP_RADIAL_OUTWARD)
        {
            desiredUp = radialOut;
        }
        else if (m_knotUpMode == KnotUpMode.UP_RADIAL_INWARD)
        {
            desiredUp = -radialOut;
        }

        // 進行方向と平行に近い場合や長さゼロの場合はフォールバック軸に切り替える
        if (math.lengthsq(desiredUp) < MIN_VECTOR_LENGTH_SQUARED ||
            math.abs(math.dot(forward, math.normalize(desiredUp))) > UP_PARALLEL_THRESHOLD)
        {
            float3 fallbackUp = radialOut;
            if (math.lengthsq(fallbackUp) < MIN_VECTOR_LENGTH_SQUARED ||
                math.abs(math.dot(forward, math.normalize(fallbackUp))) > UP_PARALLEL_THRESHOLD)
            {
                fallbackUp = math.mul(axisRotation, new float3(1.0f, 0.0f, 0.0f));
            }

            return math.normalizesafe(fallbackUp, new float3(0.0f, 1.0f, 0.0f));
        }

        return math.normalize(desiredUp);
    }
}