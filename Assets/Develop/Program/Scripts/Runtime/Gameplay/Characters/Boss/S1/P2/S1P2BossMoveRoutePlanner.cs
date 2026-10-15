using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// S1P2ボスの移動経路を生成します。
/// </summary>
[DisallowMultipleComponent]
public sealed class S1P2BossMoveRoutePlanner :
    MonoBehaviour
{
    // 同一地点とみなす距離
    private const float MIN_POINT_DISTANCE = 0.1f;

    // 移動させるBossのTransform
    [SerializeField, Header("移動基準")]
    private Transform m_movementRoot;

    // NavMesh判定
    [SerializeField]
    private BossNavigation m_navigation;

    // BossのNavMesh占有範囲
    [SerializeField]
    private BossNavMeshFootprint m_footprint;

    // デバッグ表示用の最後に生成した経路
    private BossMoveRoute m_lastGeneratedRoute;

    /// <summary>
    /// 最後に生成した移動経路を取得します。
    /// </summary>
    public BossMoveRoute LastGeneratedRoute =>
        m_lastGeneratedRoute;

    // 移動候補地点
    [SerializeField, Header("移動候補地点")]
    private List<S1P2BossMovePoint> m_movePoints =
        new();

    /// <summary>
    /// 現在位置から有効な移動経路を生成します。
    /// </summary>
    /// <param name="parameters">移動状態パラメータ。</param>
    /// <param name="route">生成した移動経路。</param>
    /// <returns>
    /// true：有効な経路を生成しました。
    /// false：経路を生成できませんでした。
    /// </returns>
    public bool TryCreateRoute(
    S1P2BossMoveStateParameters parameters,
    out BossMoveRoute route)
    {
        route =
            null;

        m_lastGeneratedRoute =
            null;

        if (!ValidateReferences(
                parameters))
        {
            return false;
        }

        Vector3 startPosition =
            m_movementRoot.position;
        bool isCurrentPoseInside =
            m_navigation.IsFootprintInsideNavMesh(
                m_footprint);

        Debug.Log(
            $"S1P2 Route Start - " +
            $"Position: {startPosition}, " +
            $"CurrentPoseInside: {isCurrentPoseInside}",
            this);
        List<S1P2BossMovePoint> candidates =
            CreateCandidatePointList(
                startPosition,
                parameters.ArrivalDistance);

        if (candidates.Count <
            parameters.MinRoutePointCount)
        {
            Debug.LogWarning(
                "移動候補地点が不足しています。",
                this);

            return false;
        }

        List<S1P2BossMovePoint> farCandidates =
            CreateFarPointList(
                candidates,
                startPosition,
                parameters.RequiredFarPointDistance);

        if (farCandidates.Count == 0)
        {
            Debug.LogWarning(
                "必要距離以上離れた移動候補地点がありません。",
                this);

            return false;
        }

        int minPointCount =
            Mathf.Max(
                2,
                parameters.MinRoutePointCount);

        int maxPointCount =
            Mathf.Clamp(
                parameters.MaxRoutePointCount,
                minPointCount,
                candidates.Count);

        List<S1P2BossMovePoint> selectedPoints =
            new();

        for (int attempt = 0;
             attempt <
             parameters.RouteSearchAttemptCount;
             attempt++)
        {
            int pointCount =
                Random.Range(
                    minPointCount,
                    maxPointCount + 1);

            // 使用地点自体はランダムに選択
            SelectRoutePoints(
                candidates,
                farCandidates,
                pointCount,
                selectedPoints);

            // 通過順は自然につながるよう整理
            OrderRoutePoints(
                startPosition,
                selectedPoints);

            List<Vector3> controlPoints =
     CreateControlPoints(
         startPosition,
         selectedPoints);

            // 経路そのものに無理な折り返しがないか確認する
            if (!ValidateRouteShape(
                    controlPoints,
                    parameters))
            {
                continue;
            }

            List<Vector3> samplePoints =
                CreateSamplePoints(
                    controlPoints,
                    parameters);


            if (!ValidateRoute(
           samplePoints,
           out int failedSampleIndex))
            {
#if UNITY_EDITOR

                string failedLocation =
                    failedSampleIndex == 0
                        ? "START"
                        : failedSampleIndex ==
                          samplePoints.Count - 1
                            ? "END"
                            : "MIDDLE";

                Debug.LogWarning(
                    $"S1P2経路候補を破棄。" +
                    $" Attempt: {attempt + 1}/" +
                    $"{parameters.RouteSearchAttemptCount}," +
                    $" Failed: {failedLocation}," +
                    $" Index: {failedSampleIndex}/" +
                    $"{samplePoints.Count - 1}," +
                    $" Position: " +
                    $"{samplePoints[failedSampleIndex]}",
                    this);

#endif

                continue;
            }

            route =
                new BossMoveRoute(
                    controlPoints,
                    samplePoints);

            if (!route.IsValid)
            {
                continue;
            }

            m_lastGeneratedRoute =
                route;

            return true;
        }

        Debug.LogWarning(
            $"有効なボス移動経路を生成できませんでした。" +
            $" 試行回数: " +
            $"{parameters.RouteSearchAttemptCount}",
            this);

        return false;
    }

    /// <summary>
    /// 使用可能な移動候補地点を生成します。
    /// </summary>
    /// <param name="startPosition">Bossの開始位置。</param>
    /// <param name="exclusionDistance">
    /// 現在位置付近から除外する距離。
    /// </param>
    /// <returns>使用可能な移動候補地点。</returns>
    private List<S1P2BossMovePoint> CreateCandidatePointList(
        Vector3 startPosition,
        float exclusionDistance)
    {
        List<S1P2BossMovePoint> candidates =
            new();

        float validExclusionDistance =
            Mathf.Max(
                MIN_POINT_DISTANCE,
                exclusionDistance);

        float exclusionDistanceSqr =
            validExclusionDistance *
            validExclusionDistance;

        foreach (S1P2BossMovePoint movePoint
                 in m_movePoints)
        {
            if (movePoint == null)
            {
                continue;
            }

            Vector3 difference =
                movePoint.Position -
                startPosition;

            difference.y =
                0.0f;

            if (difference.sqrMagnitude <=
                exclusionDistanceSqr)
            {
                continue;
            }

            candidates.Add(
                movePoint);
        }

        return candidates;
    }

    /// <summary>
    /// Bossから一定距離以上離れた候補地点を取得します。
    /// </summary>
    /// <param name="candidates">候補地点。</param>
    /// <param name="startPosition">Bossの開始位置。</param>
    /// <param name="requiredDistance">必要距離。</param>
    /// <returns>一定距離以上離れた候補地点。</returns>
    private List<S1P2BossMovePoint> CreateFarPointList(
        IReadOnlyList<S1P2BossMovePoint> candidates,
        Vector3 startPosition,
        float requiredDistance)
    {
        List<S1P2BossMovePoint> farPoints =
            new();

        float requiredDistanceSqr =
            requiredDistance *
            requiredDistance;

        foreach (S1P2BossMovePoint movePoint
                 in candidates)
        {
            Vector3 difference =
                movePoint.Position -
                startPosition;

            difference.y =
                0.0f;

            if (difference.sqrMagnitude <
                requiredDistanceSqr)
            {
                continue;
            }

            farPoints.Add(
                movePoint);
        }

        return farPoints;
    }

    /// <summary>
    /// 今回使用する移動地点をランダムに選択します。
    /// </summary>
    /// <param name="candidates">全候補地点。</param>
    /// <param name="farCandidates">遠距離候補地点。</param>
    /// <param name="pointCount">選択数。</param>
    /// <param name="selectedPoints">選択結果。</param>
    private void SelectRoutePoints(
     IReadOnlyList<S1P2BossMovePoint> candidates,
     IReadOnlyList<S1P2BossMovePoint> farCandidates,
     int pointCount,
     List<S1P2BossMovePoint> selectedPoints)
    {
        selectedPoints.Clear();

        /*
         * 最低1地点は現在位置から
         * RequiredFarPointDistance以上離れた地点にする。
         */
        S1P2BossMovePoint farPoint =
            farCandidates[
                Random.Range(
                    0,
                    farCandidates.Count)];

        selectedPoints.Add(
            farPoint);

        List<S1P2BossMovePoint> remainingPoints =
            new();

        foreach (S1P2BossMovePoint candidate
                 in candidates)
        {
            if (candidate == farPoint)
            {
                continue;
            }

            remainingPoints.Add(
                candidate);
        }

        while (selectedPoints.Count <
               pointCount &&
               remainingPoints.Count > 0)
        {
            int randomIndex =
                Random.Range(
                    0,
                    remainingPoints.Count);

            selectedPoints.Add(
                remainingPoints[randomIndex]);

            remainingPoints.RemoveAt(
                randomIndex);
        }
    }

    /// <summary>
    /// 選択済みの移動地点を現在位置から自然につながる順番へ並べます。
    /// </summary>
    /// <param name="startPosition">経路開始位置。</param>
    /// <param name="points">並べ替える移動地点。</param>
    private void OrderRoutePoints(
        Vector3 startPosition,
        List<S1P2BossMovePoint> points)
    {
        if (points == null ||
            points.Count <= 1)
        {
            return;
        }

        List<S1P2BossMovePoint> remainingPoints =
            new(points);

        points.Clear();

        Vector3 currentPosition =
            startPosition;

        while (remainingPoints.Count > 0)
        {
            int nearestIndex =
                0;

            float nearestDistanceSqr =
                GetHorizontalDistanceSqr(
                    currentPosition,
                    remainingPoints[0].Position);

            for (int i = 1;
                 i < remainingPoints.Count;
                 i++)
            {
                float distanceSqr =
                    GetHorizontalDistanceSqr(
                        currentPosition,
                        remainingPoints[i].Position);

                if (distanceSqr >=
                    nearestDistanceSqr)
                {
                    continue;
                }

                nearestDistanceSqr =
                    distanceSqr;

                nearestIndex =
                    i;
            }

            S1P2BossMovePoint nextPoint =
                remainingPoints[nearestIndex];

            points.Add(
                nextPoint);

            currentPosition =
                nextPoint.Position;

            remainingPoints.RemoveAt(
                nearestIndex);
        }
    }

    /// <summary>
    /// 2地点間の水平距離の2乗を取得します。
    /// </summary>
    /// <param name="from">開始位置。</param>
    /// <param name="to">終了位置。</param>
    /// <returns>水平距離の2乗。</returns>
    private static float GetHorizontalDistanceSqr(
        Vector3 from,
        Vector3 to)
    {
        float x =
            to.x - from.x;

        float z =
            to.z - from.z;

        return x * x +
               z * z;
    }

    /// <summary>
    /// Catmull-Romに使用する制御点を生成します。
    /// </summary>
    /// <param name="startPosition">開始位置。</param>
    /// <param name="selectedPoints">選択された移動地点。</param>
    /// <returns>経路制御点。</returns>
    private List<Vector3> CreateControlPoints(
        Vector3 startPosition,
        IReadOnlyList<S1P2BossMovePoint> selectedPoints)
    {
        List<Vector3> controlPoints =
            new()
            {
                startPosition
            };

        foreach (S1P2BossMovePoint movePoint
                 in selectedPoints)
        {
            Vector3 pointPosition =
                movePoint.Position;

            pointPosition.y =
                startPosition.y;

            controlPoints.Add(
                pointPosition);
        }

        return controlPoints;
    }

    /// <summary>
    /// Catmull-Rom曲線から経路サンプルを生成します。
    /// </summary>
    /// <param name="controlPoints">経路制御点。</param>
    /// <param name="parameters">移動状態パラメータ。</param>
    /// <returns>経路サンプル点。</returns>
    private List<Vector3> CreateSamplePoints(
        IReadOnlyList<Vector3> controlPoints,
        S1P2BossMoveStateParameters parameters)
    {
        List<Vector3> samplePoints =
            new();

        if (controlPoints == null ||
            controlPoints.Count < 2)
        {
            return samplePoints;
        }

        float sampleInterval =
            Mathf.Max(
                0.1f,
                parameters.RouteSampleInterval);

        for (int segmentIndex = 0;
             segmentIndex < controlPoints.Count - 1;
             segmentIndex++)
        {
            Vector3 p1 =
                controlPoints[segmentIndex];

            Vector3 p2 =
                controlPoints[segmentIndex + 1];

            /*
             * 開始地点では現在のBossの向きを使用しない。
             * Bossは移動開始前に最初の経路方向へその場旋回するため、
             * 最初の曲線はp1→p2方向から開始させる。
             */
            Vector3 p0 =
                segmentIndex == 0
                    ? p1
                    : controlPoints[segmentIndex - 1];

            Vector3 p3 =
                segmentIndex + 2 <
                controlPoints.Count
                    ? controlPoints[segmentIndex + 2]
                    : p2;

            float segmentDistance =
                Vector3.Distance(
                    p1,
                    p2);

            int sampleCount =
                Mathf.Max(
                    1,
                    Mathf.CeilToInt(
                        segmentDistance /
                        sampleInterval));

            for (int sampleIndex = 0;
                 sampleIndex <= sampleCount;
                 sampleIndex++)
            {
                if (segmentIndex > 0 &&
                    sampleIndex == 0)
                {
                    continue;
                }

                float t =
                    sampleIndex /
                    (float)sampleCount;

                Vector3 samplePoint =
                    EvaluateCatmullRom(
                        p0,
                        p1,
                        p2,
                        p3,
                        t);

                samplePoints.Add(
                    samplePoint);
            }
        }

        return samplePoints;
    }

    /// <summary>
    /// Catmull-Rom曲線上の位置を計算します。
    /// </summary>
    /// <param name="p0">前制御点。</param>
    /// <param name="p1">開始制御点。</param>
    /// <param name="p2">終了制御点。</param>
    /// <param name="p3">次制御点。</param>
    /// <param name="t">補間率。</param>
    /// <returns>曲線上の位置。</returns>
    private Vector3 EvaluateCatmullRom(
        Vector3 p0,
        Vector3 p1,
        Vector3 p2,
        Vector3 p3,
        float t)
    {
        float t2 =
            t * t;

        float t3 =
            t2 * t;

        return 0.5f *
               (
                   2.0f * p1 +
                   (-p0 + p2) * t +
                   (2.0f * p0 -
                    5.0f * p1 +
                    4.0f * p2 -
                    p3) * t2 +
                   (-p0 +
                    3.0f * p1 -
                    3.0f * p2 +
                    p3) * t3
               );
    }

    /// <summary>
    /// 経路全体がNavMesh内に収まるか確認します。
    /// </summary>
    /// <param name="samplePoints">経路サンプル点。</param>
    /// <returns>
    /// true：経路全体がNavMesh内です。
    /// false：NavMesh外へ出る地点があります。
    /// </returns>
    private bool ValidateRoute(
      IReadOnlyList<Vector3> samplePoints,
      out int failedSampleIndex)
    {
        failedSampleIndex =
            -1;

        if (samplePoints == null ||
            samplePoints.Count < 2)
        {
            return false;
        }

        Quaternion predictedRotation =
            m_movementRoot.rotation;

        for (int i = 0;
             i < samplePoints.Count;
             i++)
        {
            Vector3 direction;

            if (i <
                samplePoints.Count - 1)
            {
                direction =
                    samplePoints[i + 1] -
                    samplePoints[i];
            }
            else
            {
                direction =
                    samplePoints[i] -
                    samplePoints[i - 1];
            }

            direction.y =
                0.0f;

            if (direction.sqrMagnitude >
                Mathf.Epsilon)
            {
                predictedRotation =
                    Quaternion.LookRotation(
                        direction.normalized,
                        Vector3.up);
            }

            if (m_navigation.IsFootprintInsideNavMesh(
                    m_footprint,
                    m_movementRoot,
                    samplePoints[i],
                    predictedRotation))
            {
                continue;
            }

            failedSampleIndex =
                i;

            return false;
        }

        return true;
    }
    /// <summary>
    /// 制御点から生成される経路形状が移動可能か確認します。
    /// </summary>
    /// <param name="controlPoints">経路制御点。</param>
    /// <param name="parameters">移動パラメータ。</param>
    /// <returns>
    /// true：使用可能な経路です。
    /// false：急旋回または短すぎる区間があります。
    /// </returns>
    private bool ValidateRouteShape(
        IReadOnlyList<Vector3> controlPoints,
        S1P2BossMoveStateParameters parameters)
    {
        if (controlPoints == null ||
            controlPoints.Count < 2 ||
            parameters == null)
        {
            return false;
        }

        float minSegmentDistance =
            Mathf.Max(
                0.0f,
                parameters.MinRouteSegmentDistance);

        float minSegmentDistanceSqr =
            minSegmentDistance *
            minSegmentDistance;

        // 各区間が短すぎないか確認する
        for (int i = 0;
             i < controlPoints.Count - 1;
             i++)
        {
            Vector3 difference =
                controlPoints[i + 1] -
                controlPoints[i];

            difference.y =
                0.0f;

            if (difference.sqrMagnitude <
                minSegmentDistanceSqr)
            {
#if UNITY_EDITOR
                Debug.Log(
                    $"経路区間が短すぎるため破棄します。" +
                    $" Segment: {i}," +
                    $" Distance: {difference.magnitude:F2}",
                    this);
#endif

                return false;
            }
        }

        float maxTurnAngle =
            Mathf.Clamp(
                parameters.MaxRouteTurnAngle,
                0.0f,
                180.0f);

        // 各経由地点での旋回角度を確認する
        for (int i = 1;
             i < controlPoints.Count - 1;
             i++)
        {
            Vector3 previousDirection =
                controlPoints[i] -
                controlPoints[i - 1];

            Vector3 nextDirection =
                controlPoints[i + 1] -
                controlPoints[i];

            previousDirection.y =
                0.0f;

            nextDirection.y =
                0.0f;

            if (previousDirection.sqrMagnitude <=
                    Mathf.Epsilon ||
                nextDirection.sqrMagnitude <=
                    Mathf.Epsilon)
            {
                return false;
            }

            float turnAngle =
                Vector3.Angle(
                    previousDirection,
                    nextDirection);

            if (turnAngle <=
                maxTurnAngle)
            {
                continue;
            }

#if UNITY_EDITOR
            Debug.Log(
                $"急旋回経路のため破棄します。" +
                $" Point: {i}," +
                $" Angle: {turnAngle:F1}," +
                $" Max: {maxTurnAngle:F1}",
                this);
#endif

            return false;
        }

        return true;
    }
    /// <summary>
    /// 必要な参照が設定されているか確認します。
    /// </summary>
    /// <param name="parameters">移動状態パラメータ。</param>
    /// <returns>
    /// true：使用できます。
    /// false：設定が不足しています。
    /// </returns>
    private bool ValidateReferences(
        S1P2BossMoveStateParameters parameters)
    {
        if (parameters == null)
        {
            Debug.LogError(
                "移動状態パラメータが設定されていません。",
                this);

            return false;
        }

        if (m_movementRoot == null)
        {
            Debug.LogError(
                "移動基準Transformが設定されていません。",
                this);

            return false;
        }

        if (m_navigation == null)
        {
            Debug.LogError(
                $"{nameof(BossNavigation)}が設定されていません。",
                this);

            return false;
        }

        if (m_footprint == null)
        {
            Debug.LogError(
                $"{nameof(BossNavMeshFootprint)}が設定されていません。",
                this);

            return false;
        }

        if (m_movePoints == null ||
            m_movePoints.Count == 0)
        {
            Debug.LogError(
                "移動候補地点が設定されていません。",
                this);

            return false;
        }

        return true;
    }

    /// <summary>
    /// Inspector設定時に参照を自動取得します。
    /// </summary>
    private void Reset()
    {
        BossController bossController =
            GetComponentInParent<BossController>();

        if (bossController == null)
        {
            return;
        }

        m_movementRoot =
            bossController.transform;

        m_navigation =
            bossController.GetComponentInChildren<BossNavigation>(
                true);

        m_footprint =
            bossController.GetComponentInChildren<BossNavMeshFootprint>(
                true);
    }
}
