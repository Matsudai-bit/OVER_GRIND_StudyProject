using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤーの物理状態と周辺環境を監視します。
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerMonitor : MonoBehaviour
{
    [SerializeField, Header("Parameter Asset")]
    private PlayerMonitorParameterAsset m_parameterAsset;

    // 接地判定を行う位置
    [SerializeField]
    private Transform m_groundCheckOrigin;

    // 接地判定の半径
    private float m_groundCheckRadius = 0.25f;

    // 接地対象のレイヤー
    private LayerMask m_groundLayerMask = ~0;

    // レール判定対象のレイヤー
    private LayerMask m_railLayerMask = ~0;

    // 地上・空中のレール搭乗判定設定
    private PlayerRailDetectionParameterAsset m_railDetectionParameter;

    // Ground Probeで一度に取得する最大ヒット数
    private const int GROUND_PROBE_HIT_CAPACITY = 16;

    // 複数の接地面候補を同距離とみなす許容差
    private const float GROUND_HIT_DISTANCE_TOLERANCE = 0.01f;

    // Ground Probeの半径
    private float m_groundProbeRadius = 0.18f;

    // Ground Probe開始位置の上方向オフセット
    private float m_groundProbeStartOffset = 0.25f;

    // 接地判定位置から下方向へ探索する追加距離
    private float m_groundProbeDistance = 0.4f;

    // 登ることのできる最大斜面角度
    private float m_maxClimbSlopeAngle = 50.0f;

    // 接地面法線が追従する最大角速度
    private float m_groundNormalSmoothSpeed = 720.0f;

    // Ground Probe用ヒットバッファ
    private readonly RaycastHit[] m_groundProbeHits =
        new RaycastHit[GROUND_PROBE_HIT_CAPACITY];

    // 有効な支持面が存在するか
    private bool m_hasGroundSurface;

    // 実際に選択された支持面の法線
    private Vector3 m_rawGroundNormal = Vector3.up;

    // 移動計算で使用する平滑化済み法線
    private Vector3 m_groundNormal = Vector3.up;

    // 現在プレイヤーを支えているCollider
    private Collider m_groundCollider;

    // プレイヤーの物理ボディ
    private Rigidbody m_playerRigidbody;

    // 接地しているか
    private bool m_isGrounded;

    private bool m_isRailed;

    // 初期化されているか
    private bool m_isInitialized;

    // 実際に足元を支えている接触先。検出球が着地前に地面へ届いた場合と区別します。
    private readonly HashSet<int> m_groundContactColliderIds = new();

    private SplineRailInfo m_hitRailInfo;
    /// <summary>
    /// プレイヤーが接地しているかを取得します。
    /// </summary>
    /// <returns>
    /// true：接地しています。
    /// false：接地していません。
    /// </returns>
    public bool IsGrounded => m_isGrounded;

    /// <summary>
    /// 有効な接地面が存在するかを取得します。
    /// </summary>
    public bool HasGroundSurface => m_hasGroundSurface;

    /// <summary>
    /// 実際に検出された接地面法線を取得します。
    /// </summary>
    public Vector3 RawGroundNormal => m_rawGroundNormal;

    /// <summary>
    /// 移動計算用に平滑化された接地面法線を取得します。
    /// </summary>
    public Vector3 GroundNormal => m_groundNormal;

    /// <summary>
    /// 現在の接地面角度を取得します。
    /// </summary>
    public float GroundSlopeAngle =>
        m_hasGroundSurface
            ? Vector3.Angle(
                m_rawGroundNormal,
                Vector3.up)
            : 0.0f;

    /// <summary>
    /// 現在プレイヤーを支えているColliderを取得します。
    /// </summary>
    public Collider GroundCollider => m_groundCollider;

    /// <summary>
    /// 接地対象のレイヤーを取得します。
    /// </summary>
    public LayerMask GroundLayerMask => m_groundLayerMask;

    /// <summary>
    /// 指定した法線が登坂可能な接地面か確認します。
    /// </summary>
    /// <param name="normal">確認する接地面法線。</param>
    /// <returns>true：登坂可能。false：急斜面または壁。</returns>
    public bool IsClimbableGroundSurface(Vector3 normal)
    {
        return IsClimbableGroundNormal(normal);
    }

    /// <summary>
    /// 接地検出範囲内に加えて、上向きの物理接触で地面に支えられているかを取得します。
    /// </summary>
    /// <returns>true：実際に地面へ接触中。false：空中または接地検出範囲内のみ。</returns>
    public bool CanStartJump =>
        m_isGrounded &&
        m_groundContactColliderIds.Count > 0;
    /// <summary>足元の下向き判定で床面を確認し、側面への接触を除外します。</summary>
    /// <returns>true：足元に登坂可能な支持面あり。false：側面接触または空中。</returns>
    public bool HasGroundSupport()
    {
        return m_isInitialized &&
               m_isGrounded &&
               m_hasGroundSurface;
    }
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
        ApplyParameterAsset();

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

        Vector3 checkPosition =
            m_groundCheckOrigin != null
                ? m_groundCheckOrigin.position
                : transform.position;

        // 足元に地面が存在するか確認
        m_isGrounded = Physics.CheckSphere(
            checkPosition,
            m_groundCheckRadius,
            m_groundLayerMask,
            QueryTriggerInteraction.Ignore);

        // 現在プレイヤーを支えている接地面を更新
        RefreshGroundSurface(
            checkPosition,
            Time.fixedDeltaTime);

        RefreshRailDetection(checkPosition);
    }

    /// <summary>
    /// 足元周辺から現在プレイヤーを支えている接地面を選択します。
    /// </summary>
    /// <param name="checkPosition">接地判定基準位置。</param>
    /// <param name="deltaTime">物理更新時間。</param>
    private void RefreshGroundSurface(
        Vector3 checkPosition,
        float deltaTime)
    {
        bool hadGroundSurface =
            m_hasGroundSurface;

        m_hasGroundSurface = false;
        m_groundCollider = null;

        float startOffset =
            Mathf.Max(
                m_groundProbeStartOffset,
                m_groundProbeRadius + 0.01f);

        Vector3 castOrigin =
            checkPosition +
            Vector3.up * startOffset;

        float castDistance =
            startOffset +
            m_groundProbeDistance;

        int hitCount =
            Physics.SphereCastNonAlloc(
                castOrigin,
                m_groundProbeRadius,
                Vector3.down,
                m_groundProbeHits,
                castDistance,
                m_groundLayerMask,
                QueryTriggerInteraction.Ignore);

        float nearestDistance =
            float.PositiveInfinity;

        float bestNormalContinuity =
            float.NegativeInfinity;

        RaycastHit selectedHit =
            default;

        bool hasSelectedHit =
            false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit =
                m_groundProbeHits[i];

            Collider hitCollider =
                hit.collider;

            if (hitCollider == null)
            {
                continue;
            }

            if (hitCollider.attachedRigidbody ==
                m_playerRigidbody)
            {
                continue;
            }

            if (!IsClimbableGroundNormal(
                hit.normal))
            {
                continue;
            }

            float normalContinuity =
                Vector3.Dot(
                    m_rawGroundNormal,
                    hit.normal);

            bool isClearlyCloser =
                hit.distance <
                nearestDistance -
                GROUND_HIT_DISTANCE_TOLERANCE;

            bool isNearlySameDistance =
                Mathf.Abs(
                    hit.distance -
                    nearestDistance) <=
                GROUND_HIT_DISTANCE_TOLERANCE;

            // 基本は最も近い面を採用し、
            // ほぼ同距離の場合だけ前回法線に近い面を優先する。
            if (!isClearlyCloser &&
                !(isNearlySameDistance &&
                  normalContinuity >
                  bestNormalContinuity))
            {
                continue;
            }

            selectedHit = hit;
            nearestDistance = hit.distance;
            bestNormalContinuity =
                normalContinuity;

            hasSelectedHit = true;
        }

        if (!m_isGrounded ||
            !hasSelectedHit)
        {
            m_rawGroundNormal =
                Vector3.up;

            m_groundNormal =
                Vector3.up;

            return;
        }

        m_hasGroundSurface = true;
        m_groundCollider =
            selectedHit.collider;

        m_rawGroundNormal =
            selectedHit.normal.normalized;

        // 着地直後は旧法線を補間せず、実際の支持面を即座に使用する。
        if (!hadGroundSurface)
        {
            m_groundNormal =
                m_rawGroundNormal;

            return;
        }

        float maxRadiansDelta =
            m_groundNormalSmoothSpeed *
            Mathf.Deg2Rad *
            Mathf.Max(
                deltaTime,
                0.0f);

        m_groundNormal =
            Vector3.RotateTowards(
                m_groundNormal,
                m_rawGroundNormal,
                maxRadiansDelta,
                0.0f);

        m_groundNormal.Normalize();
    }

    /// <summary>
    /// 指定した法線が登坂可能な接地面か確認します。
    /// </summary>
    /// <param name="normal">確認する接地面法線。</param>
    /// <returns>true：登坂可能。false：急斜面または壁。</returns>
    private bool IsClimbableGroundNormal(
        Vector3 normal)
    {
        if (normal.sqrMagnitude <=
            Mathf.Epsilon)
        {
            return false;
        }

        float slopeAngle =
            Vector3.Angle(
                normal,
                Vector3.up);

        return slopeAngle <=
               m_maxClimbSlopeAngle;
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

            bool supportsClosestPoint =
                hitCollider is BoxCollider ||
                hitCollider is SphereCollider ||
                hitCollider is CapsuleCollider ||
                (hitCollider is MeshCollider meshCollider && meshCollider.convex);

            Vector3 closestPoint = supportsClosestPoint
                ? hitCollider.ClosestPoint(checkPosition)
                : hitCollider.bounds.ClosestPoint(checkPosition);

            float distance = (closestPoint - checkPosition).sqrMagnitude; if (distance >= nearestDistance) continue;

            nearestDistance = distance;
            m_hitRailInfo = rail;
        }

        m_isRailed = m_hitRailInfo != null;
    }

    /// <summary>衝突開始時に、ジャンプ可能な地面接触を記録します。</summary>
    /// <param name="collision">衝突情報。</param>
    private void OnCollisionEnter(Collision collision)
    {
        RefreshGroundContact(collision);
    }

    /// <summary>衝突継続中に、ジャンプ可能な地面接触を更新します。</summary>
    /// <param name="collision">衝突情報。</param>
    private void OnCollisionStay(Collision collision)
    {
        RefreshGroundContact(collision);
    }

    /// <summary>衝突終了時に、記録済みの地面接触を解除します。</summary>
    /// <param name="collision">衝突情報。</param>
    private void OnCollisionExit(Collision collision)
    {
        m_groundContactColliderIds.Remove(
            collision.collider.GetInstanceID());
    }

    /// <summary>接触面の向きとレイヤーから、足元を支える接触かを更新します。</summary>
    /// <param name="collision">衝突情報。</param>
    private void RefreshGroundContact(Collision collision)
    {
        int colliderId = collision.collider.GetInstanceID();
        int colliderLayerMask = 1 << collision.gameObject.layer;

        if ((m_groundLayerMask.value & colliderLayerMask) == 0)
        {
            m_groundContactColliderIds.Remove(colliderId);
            return;
        }

        for (int i = 0; i < collision.contactCount; i++)
        {
            if (IsClimbableGroundNormal(
                collision.GetContact(i).normal))
            {
                m_groundContactColliderIds.Add(colliderId);
                return;
            }
        }

        m_groundContactColliderIds.Remove(colliderId);
    }

    /// <summary>無効化時に物理接触の記録を破棄します。</summary>
    private void OnDisable()
    {
        m_groundContactColliderIds.Clear();

        m_hasGroundSurface = false;
        m_groundCollider = null;
        m_rawGroundNormal = Vector3.up;
        m_groundNormal = Vector3.up;
    }

    /// <summary>
    /// 接地判定・Ground Probe・レール搭乗判定範囲をSceneビューに表示します。
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Vector3 checkPosition =
            m_groundCheckOrigin != null
                ? m_groundCheckOrigin.position
                : transform.position;

        Color previousColor =
            Gizmos.color;

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(
            checkPosition,
            m_groundCheckRadius);

        float startOffset =
            Mathf.Max(
                m_groundProbeStartOffset,
                m_groundProbeRadius + 0.01f);

        Vector3 castOrigin =
            checkPosition +
            Vector3.up * startOffset;

        Vector3 castEnd =
            castOrigin +
            Vector3.down *
            (startOffset +
             m_groundProbeDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            castOrigin,
            m_groundProbeRadius);

        Gizmos.DrawWireSphere(
            castEnd,
            m_groundProbeRadius);

        Gizmos.DrawLine(
            castOrigin,
            castEnd);

        if (Application.isPlaying &&
            m_hasGroundSurface)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(
                checkPosition,
                checkPosition +
                m_groundNormal);
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(
            checkPosition,
            GetRailDetectionRadius(true));

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(
            checkPosition,
            GetRailDetectionRadius(false));

        Gizmos.color =
            previousColor;
    }

    /// <summary>Applies the assigned parameter asset.</summary>
    private void ApplyParameterAsset()
    {
        if (m_parameterAsset == null) return;
        m_groundCheckRadius = m_parameterAsset.GroundCheckRadius;
        m_groundLayerMask = m_parameterAsset.GroundLayerMask;
        m_railLayerMask = m_parameterAsset.RailLayerMask;
        m_railDetectionParameter = m_parameterAsset.RailDetectionParameter;
        m_groundProbeRadius = m_parameterAsset.GroundProbeRadius;
        m_groundProbeStartOffset = m_parameterAsset.GroundProbeStartOffset;
        m_groundProbeDistance = m_parameterAsset.GroundProbeDistance;
        m_maxClimbSlopeAngle = m_parameterAsset.MaxClimbSlopeAngle;
        m_groundNormalSmoothSpeed = m_parameterAsset.GroundNormalSmoothSpeed;
    }

}
