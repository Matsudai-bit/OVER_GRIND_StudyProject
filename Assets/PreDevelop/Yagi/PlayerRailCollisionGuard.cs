using UnityEngine;

/// <summary>
/// 搭乗中のレールとPlayerの物理衝突を無視し、
/// 離脱後に安全な分離を確認してから衝突を復元します。
/// </summary>
public sealed class PlayerRailCollisionGuard
{
    private const float DEFAULT_MAX_RELEASE_DURATION = 60.0f;
    private const int OVERLAP_BUFFER_SIZE = 32;

    private readonly Collider[] m_overlapBuffer =
        new Collider[OVERLAP_BUFFER_SIZE];

    private CapsuleCollider m_playerCollider;
    private SplineRailInfo m_sourceRail;
    private Collider[] m_railColliders;

    private bool m_isIgnoring;
    private bool m_isReleaseRequested;

    private float m_releaseElapsedTime;
    private float m_minimumReleaseDuration;
    private float m_maximumReleaseDuration;
    private float m_releaseRequestedFixedTime;

    /// <summary>
    /// 指定したレールとの衝突を現在無視しているか確認します。
    /// </summary>
    public bool IsIgnoring(
        SplineRailInfo rail)
    {
        return m_isIgnoring &&
               rail != null &&
               m_sourceRail == rail;
    }

    /// <summary>
    /// 指定レールとの衝突Ignoreを開始し、搭乗中は保持します。
    /// </summary>
    /// <param name="playerCollider">Playerの物理Collider。</param>
    /// <param name="rail">搭乗するレール。</param>
    /// <returns>true：開始成功。false：設定不足。</returns>
    public bool Begin(
        CapsuleCollider playerCollider,
        SplineRailInfo rail)
    {
        if (playerCollider == null ||
            rail == null)
        {
            return false;
        }

        // 同じレールへ再搭乗した場合は、衝突を一瞬復元せず
        // Release待ちを解除してHOLDINGへ戻します。
        if (m_isIgnoring &&
            m_sourceRail == rail &&
            m_playerCollider == playerCollider)
        {
            m_isReleaseRequested = false;
            m_releaseElapsedTime = 0.0f;
            m_minimumReleaseDuration = 0.0f;
            m_maximumReleaseDuration =
                DEFAULT_MAX_RELEASE_DURATION;

            return true;
        }

        Restore();

        SplineRailCollisionSettings collisionSettings =
            rail.GetComponent<SplineRailCollisionSettings>();

        if (collisionSettings == null ||
            !collisionSettings.HasCollisionCollider())
        {
            Debug.LogWarning(
                $"[{nameof(PlayerRailCollisionGuard)}] " +
                $"{rail.name} に {nameof(SplineRailCollisionSettings)} " +
                "または衝突対象Colliderが設定されていません。",
                rail);

            return false;
        }

        m_playerCollider = playerCollider;
        m_sourceRail = rail;
        m_railColliders =
            collisionSettings.CollisionColliders;

        SetIgnoreCollision(true);

        m_isIgnoring = true;
        m_isReleaseRequested = false;
        m_releaseElapsedTime = 0.0f;
        m_minimumReleaseDuration = 0.0f;
        m_maximumReleaseDuration =
            DEFAULT_MAX_RELEASE_DURATION;

        return true;
    }

    /// <summary>
    /// 搭乗保持を終了し、レールから分離した後の衝突復帰を要求します。
    /// </summary>
    /// <param name="minimumDuration">
    /// Release要求後にIgnoreを最低維持する時間。
    /// </param>
    public void RequestRelease(
        float minimumDuration = 0.0f)
    {
        if (!m_isIgnoring)
        {
            return;
        }

        m_isReleaseRequested = true;
        m_releaseElapsedTime = 0.0f;
        m_minimumReleaseDuration =
            Mathf.Max(
                0.0f,
                minimumDuration);

        m_maximumReleaseDuration =
            Mathf.Max(
                DEFAULT_MAX_RELEASE_DURATION,
                m_minimumReleaseDuration);

        // 同一FixedUpdate内で即時解除されないよう、
        // Release要求を行った物理時刻を保持します。
        m_releaseRequestedFixedTime =
            Time.fixedTime;
    }

    /// <summary>
    /// Release要求後の衝突復帰条件を物理更新ごとに確認します。
    /// </summary>
    public void Update()
    {
        if (!m_isIgnoring ||
            !m_isReleaseRequested)
        {
            return;
        }

        if (m_playerCollider == null ||
            m_sourceRail == null)
        {
            Restore();
            return;
        }

        // Release要求と同じFixedUpdateでは判定しない。
        // 少なくとも1回Physics Simulationを通過させます。
        if (Time.fixedTime <=
            m_releaseRequestedFixedTime)
        {
            return;
        }

        m_releaseElapsedTime +=
            Time.fixedDeltaTime;

        if (m_releaseElapsedTime <
            m_minimumReleaseDuration)
        {
            return;
        }

        if (!IsOverlappingSourceRail())
        {
            Restore();
            return;
        }

        if (m_releaseElapsedTime >=
            m_maximumReleaseDuration)
        {
            Debug.LogWarning(
                $"[{nameof(PlayerRailCollisionGuard)}] " +
                $"{m_sourceRail.name} から分離できなかったため、" +
                "Rail Collision Ignoreを強制解除します。",
                m_sourceRail);

            Restore();
        }
    }

    /// <summary>
    /// IgnoreしていたRail Colliderとの衝突を即座に復元します。
    /// </summary>
    public void Restore()
    {
        if (m_isIgnoring)
        {
            SetIgnoreCollision(false);
        }

        ClearState();
    }

    /// <summary>
    /// Player Capsuleと明示指定Rail Colliderが重なっているか確認します。
    /// </summary>
    private bool IsOverlappingSourceRail()
    {
        if (!IsColliderAvailable(
                m_playerCollider) ||
            m_railColliders == null)
        {
            return false;
        }

        if (!TryGetCapsuleWorldGeometry(
                m_playerCollider,
                out Vector3 point0,
                out Vector3 point1,
                out float radius))
        {
            return true;
        }

        int hitCount =
            Physics.OverlapCapsuleNonAlloc(
                point0,
                point1,
                radius,
                m_overlapBuffer,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide);

        for (int hitIndex = 0;
             hitIndex < hitCount;
             hitIndex++)
        {
            Collider hitCollider =
                m_overlapBuffer[hitIndex];

            if (hitCollider == null)
            {
                continue;
            }

            foreach (Collider railCollider in
                     m_railColliders)
            {
                if (railCollider != null &&
                    hitCollider == railCollider)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// CapsuleColliderのワールド空間形状を取得します。
    /// </summary>
    private static bool TryGetCapsuleWorldGeometry(
        CapsuleCollider capsule,
        out Vector3 point0,
        out Vector3 point1,
        out float radius)
    {
        point0 = Vector3.zero;
        point1 = Vector3.zero;
        radius = 0.0f;

        if (capsule == null)
        {
            return false;
        }

        Transform capsuleTransform =
            capsule.transform;

        Vector3 lossyScale =
            capsuleTransform.lossyScale;

        Vector3 axis;
        float axisScale;
        float radiusScale;

        switch (capsule.direction)
        {
            case 0:
                axis = capsuleTransform.right;
                axisScale = Mathf.Abs(lossyScale.x);
                radiusScale =
                    Mathf.Max(
                        Mathf.Abs(lossyScale.y),
                        Mathf.Abs(lossyScale.z));
                break;

            case 2:
                axis = capsuleTransform.forward;
                axisScale = Mathf.Abs(lossyScale.z);
                radiusScale =
                    Mathf.Max(
                        Mathf.Abs(lossyScale.x),
                        Mathf.Abs(lossyScale.y));
                break;

            default:
                axis = capsuleTransform.up;
                axisScale = Mathf.Abs(lossyScale.y);
                radiusScale =
                    Mathf.Max(
                        Mathf.Abs(lossyScale.x),
                        Mathf.Abs(lossyScale.z));
                break;
        }

        radius =
            capsule.radius *
            radiusScale;

        float height =
            Mathf.Max(
                capsule.height * axisScale,
                radius * 2.0f);

        float halfSegmentLength =
            Mathf.Max(
                height * 0.5f - radius,
                0.0f);

        Vector3 worldCenter =
            capsuleTransform.TransformPoint(
                capsule.center);

        axis.Normalize();

        point0 =
            worldCenter +
            axis * halfSegmentLength;

        point1 =
            worldCenter -
            axis * halfSegmentLength;

        return radius > 0.0f;
    }

    /// <summary>
    /// Colliderが判定可能な状態か確認します。
    /// </summary>
    private static bool IsColliderAvailable(
        Collider collider)
    {
        return collider != null &&
               collider.enabled &&
               collider.gameObject.activeInHierarchy;
    }

    /// <summary>
    /// Player Capsuleと指定Rail ColliderのペアだけIgnore状態を切り替えます。
    /// </summary>
    private void SetIgnoreCollision(
        bool isIgnoring)
    {
        if (m_playerCollider == null ||
            m_railColliders == null)
        {
            return;
        }

        foreach (Collider railCollider in
                 m_railColliders)
        {
            if (railCollider == null ||
                railCollider == m_playerCollider)
            {
                continue;
            }

            Physics.IgnoreCollision(
                m_playerCollider,
                railCollider,
                isIgnoring);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log(
                $"[RailCollisionGuard] " +
                $"Player={m_playerCollider.name}, " +
                $"Rail={railCollider.name}, " +
                $"Ignore={Physics.GetIgnoreCollision(m_playerCollider, railCollider)}");
#endif
        }
    }

    /// <summary>
    /// Guard内部状態を初期化します。
    /// </summary>
    private void ClearState()
    {
        m_playerCollider = null;
        m_sourceRail = null;
        m_railColliders = null;

        m_isIgnoring = false;
        m_isReleaseRequested = false;

        m_releaseElapsedTime = 0.0f;
        m_minimumReleaseDuration = 0.0f;
        m_maximumReleaseDuration =
            DEFAULT_MAX_RELEASE_DURATION;
        m_releaseRequestedFixedTime =
            float.NegativeInfinity;

        System.Array.Clear(
            m_overlapBuffer,
            0,
            m_overlapBuffer.Length);
    }
}
