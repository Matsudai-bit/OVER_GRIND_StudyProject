using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

/// <summary>
/// レールに沿ったグラインド移動と、終端・ジャンプ・衝突による離脱を制御します。
/// </summary>
[RequireComponent(typeof(Rigidbody))]

public class SplineGrindController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float baseGrindSpeed = 15f; // 基本速度
    [SerializeField] private float maxGrindSpeed = 40f;  // 最高速度
    [SerializeField] private float exitSpeedScale = 1.0f;

    [Header("Collision Exit Settings")]
    [SerializeField, Min(0f)] private float collisionExitUpSpeed = 3f;
    [SerializeField, Min(0f)] private float collisionExitSideSpeed = 4f;
    [SerializeField, Min(0f)] private float collisionExitAwaySpeed = 0.75f;

    [SerializeField, Min(0f)] private float collisionExitLandingDelay = 0.15f;

    [SerializeField, Min(0f), Tooltip("衝突地点から横へ離れるまで移動を補助する距離（m）です。")]
    private float collisionExitSideDistance = 2f;
    [SerializeField, Min(0f), Tooltip("壁などで離脱できない場合に、移動補助を終了する上限時間（秒）です。")]
    private float collisionExitAssistDuration = 1f;

    private Vector3 collisionExitStartPosition;
    private Vector3 collisionExitSideDirection;
    private Vector3 collisionExitHorizontalVelocity;
    private float collisionExitAssistElapsed;

    public bool IsCollisionExiting { get; private set; }
    public bool IsCollisionExitSeparating => IsCollisionExiting &&
        collisionExitAssistElapsed < collisionExitAssistDuration &&
        Vector3.Dot(transform.position - collisionExitStartPosition, collisionExitSideDirection) <
            collisionExitSideDistance;
    public float CollisionExitLandingDelay => collisionExitLandingDelay;

    /// <summary>
    /// 衝突による離脱状態を終了し、レールへの搭乗を再び許可します。
    /// </summary>
    public void FinishCollisionExit()
    {
        IsCollisionExiting = false;
    }

    // 現在の状態管理
    public bool IsGrinding { get; private set; } = false;

    /// <summary>離脱元のレールから離れるまで、同じレールへの再搭乗を抑制します。</summary>
    /// <param name="rail">離脱元のレール。</param>
    public void BlockRailUntilSeparated(SplineRailInfo rail)
    {
        collisionExitRail = rail;
    }

    private Rigidbody m_rb;
    private PlayerMotor m_motor;

    [SerializeField, Min(0f), Tooltip("レールからプレイヤー原点までの高さ（m）。")]
    private float rideHeight = 2.4f;

    /// <summary>ジャンプへ引き継ぐレールを取得します。</summary>
    public SplineRailInfo CurrentRail => currentRail;
    /// <summary>ジャンプへ引き継ぐスプライン位置を取得します。</summary>
    public float CurrentPositionT => currentT;
    /// <summary>ジャンプへ引き継ぐ進行方向を取得します。</summary>
    public int Direction => directionFactor;
    /// <summary>ジャンプへ引き継ぐ滑走速度を取得します。</summary>
    public float CurrentSpeed => currentSpeed;
    /// <summary>レールからプレイヤー原点までの高さを取得します。</summary>
    public float RideHeight => Mathf.Max(0f, rideHeight);
    private PlayerMonitor m_monitor;
    private SplineRailInfo currentRail;
    private SplineRailInfo collisionExitRail;
    private int railLayer;

    /// <summary>
    /// 離脱中および衝突直後の同じレールへの再搭乗を防ぎます。
    /// </summary>
    public bool CanStartGrind(SplineRailInfo rail)
    {
        return isActiveAndEnabled && !IsCollisionExiting && rail != null &&
            rail.isActiveAndEnabled && rail != collisionExitRail && rail.Container != null &&
            rail.Container.Splines.Count > 0 && rail.Container.Splines[0].Count >= 2;
    }

    [DebugParameterField]
    private float currentT = 0f;       // 0.0 ~ 1.0 の進捗率
    [DebugParameterField]
    private float splineLength = 0f;   // レールの総延長（メートル）
    [DebugParameterField]
    private float currentSpeed = 0f;   // 現在のプレイヤーのリアルタイム速度
    [DebugParameterField]
    private int directionFactor = 1;   // 1 = 順方向, -1 = 逆方向

    /// <summary>
    /// 移動とレール接触確認に使用するコンポーネントを取得します。
    /// </summary>
    private void Awake()
    {
        m_rb = GetComponent<Rigidbody>();
        m_motor = GetComponent<PlayerMotor>();
        m_monitor = GetComponent<PlayerMonitor>();
        railLayer = LayerMask.NameToLayer("Rail");
    }

    /// <summary>
    /// 衝突後に元のレールから離れたことを確認します。移動は物理更新側で行います。
    /// </summary>
    void Update()
    {
        // Re-arm boarding only after leaving the rail that caused the collision exit.
        if (collisionExitRail != null && m_monitor != null &&
            (!m_monitor.IsRailed || m_monitor.HitRailInfo != collisionExitRail))
        {
            collisionExitRail = null;
        }

    }

    /// <summary>
    /// レールへの搭乗処理（トリガー衝突時などに外部から呼ぶ）
    /// </summary>
    public void StartGrind(SplineRailInfo rail)
    {
        if (!CanStartGrind(rail) || m_motor == null || !m_motor.IsInitialized) return;
        using (var spline = new NativeSpline(rail.Container.Splines[0], rail.Container.transform.localToWorldMatrix))
        {
            if (spline.GetLength() <= Mathf.Epsilon) return;
            // プレイヤー原点の高さを引き、坂でも搭乗位置を前方へずらしません。
            Vector3 railPosition = m_rb.position - Vector3.up * RideHeight;
            SplineUtility.GetNearestPoint(spline, (float3)railPosition, out _, out float nearestT);
            Vector3 tangent = spline.EvaluateTangent(nearestT);
            int direction = Vector3.Dot(tangent, m_rb.rotation * Vector3.forward) >= 0f ? 1 : -1;
            StartGrindAt(rail, nearestT, direction, baseGrindSpeed * rail.SpeedMultiplier);
        }
    }

    /// <summary>着地済みのレール上の位置・方向・速度から滑走を再開します。</summary>
    public void StartGrindAt(SplineRailInfo rail, float positionT, int direction, float speed)
    {
        if (!CanStartGrind(rail) || m_motor == null || !m_motor.IsInitialized) return;
        currentRail = rail;
        currentT = Mathf.Clamp01(positionT);
        directionFactor = direction >= 0 ? 1 : -1;
        currentSpeed = Mathf.Max(0f, speed);
        IsGrinding = true;
        m_motor.BeginRailMotion();
    }

    /// <summary>
    /// ジャンプなど外部からの要求によってグラインドを終了します。
    /// </summary>
    public void StopGrind()
    {
        // ジャンプからの着地は同じレールに戻れるよう、終端の再搭乗制限を付けません。
        ExitGrind(false);
    }

    /// <summary>コンポーネント無効化時に物理設定を復元します。</summary>
    private void OnDisable()
    {
        StopGrind();
    }

    /// <summary>PlayerのFixedUpdateから一度だけ呼び、Rigidbodyで滑走します。</summary>
    /// <param name="deltaTime">物理更新時間。</param>
    public void UpdateGrind(float deltaTime)
    {
        if (!IsGrinding) return;
        if (currentRail == null || !currentRail.isActiveAndEnabled || currentRail.Container == null ||
            currentRail.Container.Splines.Count == 0 || currentRail.Container.Splines[0].Count < 2)
        {
            ExitGrind(false);
            return;
        }

        using (var spline = new NativeSpline(currentRail.Container.Splines[0],
            currentRail.Container.transform.localToWorldMatrix))
        {
            splineLength = spline.GetLength();
            if (splineLength <= Mathf.Epsilon)
            {
                ExitGrind(false);
                return;
            }
            Vector3 nextPosition = spline.GetPointAtLinearDistance(
                currentT, currentSpeed * directionFactor * deltaTime, out float nextT);
            if ((directionFactor > 0 && nextT >= 1f) || (directionFactor < 0 && nextT <= 0f))
            {
                ExitGrind(true);
                return;
            }

            currentT = nextT;
            Vector3 forward = ((Vector3)spline.EvaluateTangent(currentT)).normalized * directionFactor;
            if (!m_motor.TryMoveAlongRail(nextPosition + Vector3.up * RideHeight, forward,
                deltaTime, out RaycastHit obstacle))
                ExitGrindOnCollision(obstacle.normal);
        }
    }

    /// <summary>
    /// 自身・トリガー・レール以外の衝突対象かを判定します。
    /// </summary>
    private bool IsObstacle(Collider other)
    {
        return other != null && !other.isTrigger &&
            other.attachedRigidbody != m_rb &&
            !IsRailCollider(other);
    }

    /// <summary>
    /// レールのコンポーネント・レイヤー・タグを親階層まで確認します。
    /// </summary>
    private bool IsRailCollider(Collider other)
    {
        // 見た目や当たり判定が別の子オブジェクトでもレール自身を障害物にしません。
        for (Transform target = other.transform; target != null; target = target.parent)
        {
            if (target.gameObject.layer == railLayer || target.CompareTag("Rail") ||
                target.TryGetComponent<SplineRailInfo>(out _)) return true;
        }
        return false;
    }

    /// <summary>
    /// 新しく接触した障害物に対してレール離脱を判定します。
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        HandleGrindCollision(collision);
    }

    /// <summary>
    /// 既に接触している障害物に対してもレール離脱を判定します。
    /// </summary>
    private void OnCollisionStay(Collision collision)
    {
        HandleGrindCollision(collision);
    }

    /// <summary>
    /// グラインド中の障害物接触から法線を取得して離脱します。
    /// </summary>
    private void HandleGrindCollision(Collision collision)
    {
        if (!IsGrinding) return;

        // 複合コライダーでは接触点ごとに相手を確認し、レールの接触点を除外します。
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            if (!IsObstacle(contact.otherCollider)) continue;
            ExitGrindOnCollision(contact.normal);
            return;
        }
    }

    /// <summary>
    /// 障害物への衝突時にレールの横へ浮き上がる初速を与えます。
    /// </summary>
    private void ExitGrindOnCollision(Vector3 normal)
    {
        // レール進行方向に対して横へ離脱し、敵側に押し込まない向きを選びます。
        Vector3 travelDirection = currentRail.Container.EvaluateTangent(Mathf.Clamp01(currentT));
        travelDirection = Vector3.ProjectOnPlane(travelDirection * directionFactor, Vector3.up);
        Vector3 sideDirection = travelDirection.sqrMagnitude > 0.0001f
            ? Vector3.Cross(Vector3.up, travelDirection).normalized
            : Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        if (sideDirection.sqrMagnitude < 0.0001f) sideDirection = Vector3.right;
        Vector3 awayDirection = Vector3.ProjectOnPlane(normal, Vector3.up).normalized;
        if (awayDirection.sqrMagnitude > 0.0001f)
        {
            // 大きな敵の正面でも、敵の面へ向かず面に沿って横へ抜ける方向を選びます。
            Vector3 surfaceSide = Vector3.Cross(Vector3.up, awayDirection).normalized;
            if (Vector3.Dot(surfaceSide, sideDirection) < 0f) surfaceSide = -surfaceSide;
            // 端への接触で既に敵から離れる向きなら、従来の横方向を維持します。
            if (Vector3.Dot(sideDirection, awayDirection) < 0f) sideDirection = -sideDirection;
            if (Vector3.Dot(sideDirection, awayDirection) < 0.5f) sideDirection = surfaceSide;
        }

        collisionExitRail = currentRail;
        ExitGrind(false);
        IsCollisionExiting = true;

        collisionExitStartPosition = transform.position;
        collisionExitSideDirection = sideDirection;
        collisionExitAssistElapsed = 0f;
        collisionExitHorizontalVelocity = sideDirection * collisionExitSideSpeed +
            awayDirection * collisionExitAwaySpeed;
        Vector3 velocity = Vector3.up * collisionExitUpSpeed + collisionExitHorizontalVelocity;
        float inwardSpeed = Vector3.Dot(velocity, normal);
        if (inwardSpeed < 0f)
        {
            velocity -= normal * inwardSpeed;
        }
        m_rb.linearVelocity = velocity;
    }

    /// <summary>
    /// 離脱距離を確保するまで横速度を補助し、上下の速度は物理挙動に任せます。
    /// </summary>
    public void UpdateCollisionExit(float deltaTime)
    {
        if (!IsCollisionExiting) return;
        if (IsCollisionExitSeparating)
        {
            // 摩擦や大きな敵への接触で横の初速を失っても、重力を保ったまま離脱を続けます。
            Vector3 velocity = collisionExitHorizontalVelocity;
            velocity.y = m_rb.linearVelocity.y;
            m_rb.linearVelocity = velocity;
        }
        collisionExitAssistElapsed += deltaTime;
    }

    /// <summary>
    /// レールからの離脱処理
    /// </summary>
    private void ExitGrind(bool isEndOfRail)
    {
        if (!IsGrinding) return;
        Vector3 exitVelocity = m_motor != null ? m_motor.RailVelocity : Vector3.zero;
        if (currentRail != null && currentRail.Container != null &&
            currentRail.Container.Splines.Count > 0 && currentRail.Container.Splines[0].Count >= 2)
        {
            Vector3 tangent = currentRail.Container.EvaluateTangent(Mathf.Clamp01(currentT));
            exitVelocity = tangent.normalized * currentSpeed * directionFactor;
            if (currentRail.IsBoostRail) exitVelocity *= exitSpeedScale;
        }

        // 終端付近で毎フレーム再搭乗して位置が戻ることを防ぎます。
        if (isEndOfRail) collisionExitRail = currentRail;
        IsGrinding = false;
        currentRail = null;
        if (m_motor != null) m_motor.EndRailMotion(exitVelocity);
    }
}
