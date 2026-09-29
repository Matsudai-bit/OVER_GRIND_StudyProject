using UnityEngine;

/// <summary>
/// プレイヤーの物理状態と周辺環境を監視します。
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerMonitor : MonoBehaviour
{
    // 接地判定を行う位置
    [SerializeField]
    private Transform m_groundCheckOrigin;

    // 接地判定の半径
    [SerializeField, Min(0.01f)]
    private float m_groundCheckRadius = 0.25f;

    // 接地対象のレイヤー
    [SerializeField]
    private LayerMask m_groundLayerMask = ~0;

    // 接地対象のレイヤー
    [SerializeField]
    private LayerMask m_railLayerMask = ~0;

    [SerializeField, Header("レール搭乗判定")]
    [Tooltip("地上・空中の判定半径を設定するアセットです。未設定の場合は従来の接地半径を使います。")]
    private PlayerRailDetectionParameterAsset m_railDetectionParameter;

    // プレイヤーの物理ボディ
    private Rigidbody m_playerRigidbody;

    // 接地しているか
    private bool m_isGrounded;

    private bool m_isRailed;

    // 初期化されているか
    private bool m_isInitialized;

    private SplineRailInfo m_hitRailInfo;
    /// <summary>
    /// プレイヤーが接地しているかを取得します。
    /// </summary>
    /// <returns>
    /// true：接地しています。
    /// false：接地していません。
    /// </returns>
    public bool IsGrounded => m_isGrounded;
    public bool IsRailed => m_isRailed;

    public SplineRailInfo HitRailInfo => m_hitRailInfo;

    /// <summary>
    /// プレイヤーの現在速度を取得します。
    /// </summary>
    public Vector3 CurrentVelocity =>
        m_playerRigidbody != null
            ? m_playerRigidbody.linearVelocity
            : Vector3.zero;

    /// <summary>
    /// プレイヤーの水平速度を取得します。
    /// </summary>
    public Vector3 HorizontalVelocity
    {
        get
        {
            Vector3 velocity = CurrentVelocity;
            velocity.y = 0.0f;
            return velocity;
        }
    }

    /// <summary>
    /// プレイヤーの水平速度の大きさを取得します。
    /// </summary>
    public float HorizontalSpeed => HorizontalVelocity.magnitude;

    /// <summary>
    /// PlayerMonitorを初期化します。
    /// </summary>
    /// <param name="playerRigidbody">プレイヤーの物理ボディ。</param>
    public void Initialize(Rigidbody playerRigidbody)
    {
        if (playerRigidbody == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerMonitor)}] Rigidbodyが指定されていません。",
                this);

            m_isInitialized = false;
            return;
        }

        m_playerRigidbody = playerRigidbody;
        m_isInitialized = true;

        Refresh();
    }

    /// <summary>
    /// プレイヤーの監視情報を更新します。
    /// </summary>
    public void Refresh()
    {
        if (!m_isInitialized)
        {
            return;
        }

        Vector3 checkPosition = m_groundCheckOrigin != null
            ? m_groundCheckOrigin.position
            : transform.position;

        // 足元に地面が存在するか確認
        m_isGrounded = Physics.CheckSphere(
            checkPosition,
            m_groundCheckRadius,
            m_groundLayerMask,
            QueryTriggerInteraction.Ignore);

        RefreshRailDetection(checkPosition);
    }

    /// <summary>
    /// 地上・空中に対応する搭乗判定半径を取得します。
    /// </summary>
    private float GetRailDetectionRadius(bool isGrounded)
    {
        return m_railDetectionParameter != null
            ? m_railDetectionParameter.GetRadius(isGrounded)
            : m_groundCheckRadius;
    }

    /// <summary>
    /// 搭乗範囲にある有効なレールのうち、最も近いものを選択します。
    /// </summary>
    private void RefreshRailDetection(Vector3 checkPosition)
    {
        Collider[] hitColliders = Physics.OverlapSphere(
            checkPosition, GetRailDetectionRadius(m_isGrounded),
            m_railLayerMask, QueryTriggerInteraction.Ignore);

        // 範囲を広げた際にも、レール以外の接触や前回の参照で搭乗しないよう毎回更新します。
        m_hitRailInfo = null;
        float nearestDistance = float.PositiveInfinity;
        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider.attachedRigidbody == m_playerRigidbody) continue;

            SplineRailInfo rail = hitCollider.GetComponentInParent<SplineRailInfo>();
            if (rail == null) continue;

            float distance = (hitCollider.ClosestPoint(checkPosition) - checkPosition).sqrMagnitude;
            if (distance >= nearestDistance) continue;

            nearestDistance = distance;
            m_hitRailInfo = rail;
        }

        m_isRailed = m_hitRailInfo != null;
    }

    /// <summary>
    /// 接地判定と地上・空中のレール搭乗判定範囲をSceneビューに表示します。
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Vector3 checkPosition = m_groundCheckOrigin != null
            ? m_groundCheckOrigin.position
            : transform.position;

        Color previousColor = Gizmos.color;
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(checkPosition, m_groundCheckRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(checkPosition, GetRailDetectionRadius(true));
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(checkPosition, GetRailDetectionRadius(false));
        Gizmos.color = previousColor;
    }
}