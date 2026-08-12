using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// ボスのNavMeshを使用した経路判定を管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class BossNavigation : MonoBehaviour
{
    // NavMesh上の位置を検索する最小距離
    private const float MIN_SAMPLE_DISTANCE = 0.1f;

    // 使用するNavMeshSurface
    [SerializeField, Header("NavMesh設定")]
    private NavMeshSurface m_navMeshSurface;

    // NavMesh上の位置を取得する基準位置
    [SerializeField]
    private Transform m_navigationOrigin;

    // NavMesh上の位置を検索する最大距離
    [SerializeField, Min(MIN_SAMPLE_DISTANCE)]
    private float m_sampleDistance = 2.0f;

    /// <summary>
    /// 初期化します。
    /// </summary>
    private void Awake()
    {
        if (m_navigationOrigin == null)
        {
            m_navigationOrigin = transform;
        }

        if (m_navMeshSurface == null)
        {
            Debug.LogError(
                $"{nameof(NavMeshSurface)}が設定されていません。",
                this);
        }
    }

    /// <summary>
    /// 指定方向へ直進できるか確認します。
    /// </summary>
    /// <param name="direction">確認する方向。</param>
    /// <param name="distance">確認する距離。</param>
    /// <returns>
    /// true：指定方向へ直進できます。
    /// false：指定方向へ直進できません。
    /// </returns>
    public bool CanMoveStraight(
        Vector3 direction,
        float distance)
    {
        if (m_navMeshSurface == null ||
            m_navigationOrigin == null ||
            distance <= 0.0f)
        {
            return false;
        }

        Vector3 horizontalDirection = new Vector3(
            direction.x,
            0.0f,
            direction.z);

        if (horizontalDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            return false;
        }

        horizontalDirection.Normalize();

        NavMeshQueryFilter queryFilter =
            CreateQueryFilter();

        // 現在位置に最も近いNavMesh上の位置を取得します。
        if (!NavMesh.SamplePosition(
                m_navigationOrigin.position,
                out NavMeshHit startHit,
                m_sampleDistance,
                queryFilter))
        {
            Debug.LogWarning(
                $"現在位置付近にNavMeshが見つかりません。" +
                $" AgentTypeID: {m_navMeshSurface.agentTypeID}",
                this);

            return false;
        }

        Vector3 targetPosition =
            startHit.position +
            horizontalDirection * distance;

        // 現在位置から前方へ直進可能か確認します。
        bool isBlocked = NavMesh.Raycast(
            startHit.position,
            targetPosition,
            out _,
            queryFilter);

        return !isBlocked;
    }

    /// <summary>
    /// NavMesh検索条件を生成します。
    /// </summary>
    /// <returns>NavMesh検索条件。</returns>
    private NavMeshQueryFilter CreateQueryFilter()
    {
        return new NavMeshQueryFilter
        {
            // Surfaceと同じAgent Typeを自動的に使用します。
            agentTypeID = m_navMeshSurface.agentTypeID,

            // 現時点ではすべてのAreaを通行可能とします。
            areaMask = NavMesh.AllAreas
        };
    }
}