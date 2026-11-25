using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trigger コライダーの有効化・速度・接触タイミング追跡および砂表面との重なり点収集を行うクラス。
/// 行列の事前計算と高さ先行判定により高速に走査します。
/// </summary>
public class SandTriggerScanner
{
    // ---- 定数 ----
    private const float MIN_SAMPLE_STEP = 0.01f;           // 表面サンプリングの最小間隔(m)
    private const int MAX_SAMPLES_PER_AXIS = 20;           // 1軸あたりの最大サンプリング分割数
    private const float CLOSEST_POINT_EPSILON_SQ = 1e-6f;  // 内外判定用の距離二乗閾値

    // ---- メンバー変数 ----
    private Vector3[] m_lastCenters;                       // 前フレームの各コライダー中心座標
    private float[] m_speeds;                              // 各コライダーの移動速度(m/s)
    private bool[] m_wasActive;                            // 前フレームでコライダーが有効だったか
    private float[] m_activatedTimes;                      // コライダーが有効になった時刻
    private bool[] m_wasOverlapping;                       // 前フレームで砂山と重なっていたか
    private float[] m_contactStartTimes;                   // 砂山と重なり始めた時刻
    private bool m_warnedMeshCollider;                     // 凹MeshCollider警告の重複防止フラグ
    private readonly List<Vector3> m_overlapPoints = new List<Vector3>();  // 重なった砂表面の座標リスト
    private readonly List<Vector3> m_overlapNormals = new List<Vector3>(); // 重なった砂表面の法線リスト

    /// <summary>直近のスキャンで検出された重なり座標のリスト。</summary>
    public IReadOnlyList<Vector3> OverlapPoints => m_overlapPoints;

    /// <summary>直近のスキャンで検出された重なり法線のリスト。</summary>
    public IReadOnlyList<Vector3> OverlapNormals => m_overlapNormals;

    /// <summary>
    /// トリガー追跡用の位置・速度・タイマーキャッシュを初期化します。
    /// </summary>
    public void ResetTracking(Collider[] triggerColliders, float currentTime)
    {
        int count = triggerColliders != null ? triggerColliders.Length : 0;
        m_lastCenters = new Vector3[count];
        m_speeds = new float[count];
        m_wasActive = new bool[count];
        m_activatedTimes = new float[count];
        m_wasOverlapping = new bool[count];
        m_contactStartTimes = new float[count];

        for (int i = 0; i < count; i++)
        {
            Collider col = triggerColliders[i];
            if (IsColliderActive(col))
            {
                m_wasActive[i] = true;
                m_activatedTimes[i] = currentTime;
                m_lastCenters[i] = col.bounds.center;
            }
        }
    }

    /// <summary>
    /// 各トリガーコライダーの有効状態と移動速度を更新します。
    /// </summary>
    public void UpdateTriggerStates(Collider[] triggerColliders, float deltaTime, float currentTime)
    {
        if (triggerColliders == null)
        {
            return;
        }

        if (m_lastCenters == null || m_lastCenters.Length != triggerColliders.Length)
        {
            ResetTracking(triggerColliders, currentTime);
            return;
        }

        for (int i = 0; i < triggerColliders.Length; i++)
        {
            Collider col = triggerColliders[i];
            if (!IsColliderActive(col))
            {
                m_wasActive[i] = false;
                m_wasOverlapping[i] = false;
                m_speeds[i] = 0f;
                continue;
            }

            Vector3 center = col.bounds.center;
            if (!m_wasActive[i])
            {
                m_wasActive[i] = true;
                m_wasOverlapping[i] = false;
                m_activatedTimes[i] = currentTime;
                m_lastCenters[i] = center;
                m_speeds[i] = 0f;
            }
            else
            {
                m_speeds[i] = (center - m_lastCenters[i]).magnitude / deltaTime;
                m_lastCenters[i] = center;
            }
        }
    }

    /// <summary>
    /// 指定インデックスのトリガーコライダーの速度を取得します。
    /// </summary>
    public float GetTriggerSpeed(int index)
    {
        if (m_speeds == null || index < 0 || index >= m_speeds.Length)
        {
            return 0f;
        }

        return m_speeds[index];
    }

    /// <summary>
    /// コライダーが有効化されてから指定の遅延時間が経過したかを確認します。
    /// </summary>
    /// <returns>
    /// true：遅延時間を経過しており反応可能です。
    /// false：まだ待機中です。
    /// </returns>
    public bool IsActivationDelayElapsed(int index, float currentTime, float activationDelay)
    {
        if (m_wasActive == null || index < 0 || index >= m_wasActive.Length || !m_wasActive[index])
        {
            return false;
        }

        return (currentTime - m_activatedTimes[index]) >= activationDelay;
    }

    /// <summary>
    /// 砂山との重なり状態と重なり遅延時間の経過を更新・判定します。
    /// </summary>
    /// <returns>
    /// true：重なりがあり、かつ遅延時間を経過しています。
    /// false：重なりがないか、遅延時間の待機中です。
    /// </returns>
    public bool UpdateContactAndCheckDelay(int index, bool hasOverlap, float currentTime, float contactDelay)
    {
        if (m_wasOverlapping == null || index < 0 || index >= m_wasOverlapping.Length)
        {
            return false;
        }

        if (!hasOverlap)
        {
            m_wasOverlapping[index] = false;
            return false;
        }

        if (!m_wasOverlapping[index])
        {
            m_wasOverlapping[index] = true;
            m_contactStartTimes[index] = currentTime;
        }

        return (currentTime - m_contactStartTimes[index]) >= contactDelay;
    }

    /// <summary>
    /// コライダーが Trigger 判定に使用可能かを検証します。
    /// </summary>
    /// <returns>
    /// true：使用可能です。
    /// false：無効または非ConvexなMeshColliderのため使用できません。
    /// </returns>
    public bool IsSupportedCollider(Collider col, Object logContext)
    {
        if (!IsColliderActive(col))
        {
            return false;
        }

        MeshCollider meshCollider = col as MeshCollider;
        if (meshCollider != null && !meshCollider.convex)
        {
            if (!m_warnedMeshCollider)
            {
                m_warnedMeshCollider = true;
                Debug.LogWarning("[SandDigger] " + col.name + " は Convex でない MeshCollider なので Trigger 判定に使えません。", logContext);
            }

            return false;
        }

        return true;
    }

    /// <summary>
    /// コライダーの AABB 内を格子状に走査し、砂表面がコライダー内側にある点を収集します。
    /// </summary>
    public void CollectOverlapPoints(MarchingCubesMountain mountain, Collider col, Bounds bounds, float sampleStepMultiplier)
    {
        m_overlapPoints.Clear();
        m_overlapNormals.Clear();

        if (mountain == null || col == null)
        {
            return;
        }

        float step = Mathf.Max(MIN_SAMPLE_STEP, mountain.WorldVoxelSize * sampleStepMultiplier);
        int nx = Mathf.Min(Mathf.CeilToInt(bounds.size.x / step) + 1, MAX_SAMPLES_PER_AXIS);
        int nz = Mathf.Min(Mathf.CeilToInt(bounds.size.z / step) + 1, MAX_SAMPLES_PER_AXIS);

        float startWorldX = nx == 1 ? bounds.center.x : bounds.min.x;
        float startWorldZ = nz == 1 ? bounds.center.z : bounds.min.z;
        float stepWorldX = nx == 1 ? 0f : bounds.size.x / (nx - 1);
        float stepWorldZ = nz == 1 ? 0f : bounds.size.z / (nz - 1);

        // ループ外で行列を1回だけ計算し、ループ内のTransformアクセスを排除する
        Transform mountainTransform = mountain.transform;
        Matrix4x4 worldToLocal = mountainTransform.worldToLocalMatrix;
        Matrix4x4 localToWorld = mountainTransform.localToWorldMatrix;

        Vector3 localOrigin = worldToLocal.MultiplyPoint3x4(new Vector3(startWorldX, bounds.center.y, startWorldZ));
        Vector3 localStepX = worldToLocal.MultiplyVector(new Vector3(stepWorldX, 0f, 0f));
        Vector3 localStepZ = worldToLocal.MultiplyVector(new Vector3(0f, 0f, stepWorldZ));

        float minWorldY = bounds.min.y;
        float maxWorldY = bounds.max.y;

        for (int ix = 0; ix < nx; ix++)
        {
            Vector3 rowLocal = localOrigin + localStepX * ix;
            for (int iz = 0; iz < nz; iz++)
            {
                Vector3 sampleLocal = rowLocal + localStepZ * iz;

                // 先に高さだけを取得し、高さ範囲外なら ClosestPoint と法線計算をスキップする
                if (!mountain.TrySampleLocalHeight(sampleLocal.x, sampleLocal.z, out float localSurfaceY))
                {
                    continue;
                }

                Vector3 localPoint = new Vector3(sampleLocal.x, localSurfaceY, sampleLocal.z);
                Vector3 worldSurfacePoint = localToWorld.MultiplyPoint3x4(localPoint);
                if (worldSurfacePoint.y < minWorldY || worldSurfacePoint.y > maxWorldY)
                {
                    continue;
                }

                Vector3 closest = col.ClosestPoint(worldSurfacePoint);
                if ((closest - worldSurfacePoint).sqrMagnitude > CLOSEST_POINT_EPSILON_SQ)
                {
                    continue;
                }

                // 実際に重なった点だけ法線を計算する
                Vector3 localNormal = mountain.CalculateLocalNormal(sampleLocal.x, sampleLocal.z);
                Vector3 worldNormal = localToWorld.MultiplyVector(localNormal).normalized;

                m_overlapPoints.Add(worldSurfacePoint);
                m_overlapNormals.Add(worldNormal);
            }
        }
    }

    /// <summary>
    /// コライダーがシーン上でアクティブかつ有効かを確認します。
    /// </summary>
    private static bool IsColliderActive(Collider col)
    {
        return col != null && col.enabled && col.gameObject.activeInHierarchy;
    }
}