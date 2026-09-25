using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Dynamic;

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

    private Rigidbody m_rb;
    private PlayerMonitor m_monitor;
    private SplineRailInfo currentRail;
    private SplineRailInfo collisionExitRail;
    private const float CollisionSkin = 0.02f;
    private int railLayer;

    /// <summary>
    /// 離脱中および衝突直後の同じレールへの再搭乗を防ぎます。
    /// </summary>
    public bool CanStartGrind(SplineRailInfo rail)
    {
        return !IsCollisionExiting && rail != null && rail != collisionExitRail;
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
        m_monitor = GetComponent<PlayerMonitor>();
        railLayer = LayerMask.NameToLayer("Rail");
    }

    /// <summary>
    /// レールから離れたことを確認し、グラインド中の位置を更新します。
    /// </summary>
    void Update()
    {
        // Re-arm boarding only after leaving the rail that caused the collision exit.
        if (collisionExitRail != null && m_monitor != null &&
            (!m_monitor.IsRailed || m_monitor.HitRailInfo != collisionExitRail))
        {
            collisionExitRail = null;
        }

        if (IsGrinding)
        {
            ExecuteGrind();
        }
    }

    /// <summary>
    /// レールへの搭乗処理（トリガー衝突時などに外部から呼ぶ）
    /// </summary>
    public void StartGrind(SplineRailInfo rail)
    {
        if (!CanStartGrind(rail)) return;

        Debug.Log("グラインドの開始");

        currentRail = rail;
        splineLength = rail.Container.CalculateLength();

        // 1. プレイヤーの現在地から、レール上の最も近いノード（T値）を割り出す
        Vector3 localPlayerPos = rail.Container.transform.InverseTransformPoint(transform.position);
        SplineUtility.GetNearestPoint(rail.Container.Splines[0], localPlayerPos, out float3 _, out float nearestT);
        currentT = nearestT;

        // 2. 進入方向の判定（順方向か、逆方向か）
        Vector3 railDirection = Vector3.Normalize(rail.Container.EvaluateTangent(currentT));
        Vector3 playerDirection = transform.forward; // プレイヤーの進行方向

        // 内積を計算し、プレイヤーとレールの向きが逆なら逆走モード(-1)にする
        directionFactor = Vector3.Dot(railDirection, playerDirection) >= 0 ? 1 : -1;

        // 3. 初期速度の設定（現在の速度を引き継ぐか、基本速度にするか）
        currentSpeed = baseGrindSpeed * rail.SpeedMultiplier;
        IsGrinding = true;

        // ※ここで元の物理挙動（CharacterControllerやRigidbody）を無効化する
    }

    /// <summary>
    /// ジャンプなど外部からの要求によってグラインドを終了します。
    /// </summary>
    public void StopGrind()
    {
        ExitGrind(true);
    }

    /// <summary>
    /// 毎フレームの移動計算
    /// </summary>
    private void ExecuteGrind()
    {
        if (currentRail == null) return;

        if (splineLength < 1.0f)
        {
            Debug.LogError("距離が短すぎます");
        }

        // 【ここがポイント！】速度ベースでT値を進める（逆走時はマイナスされる）
        float deltaT = (currentSpeed / splineLength) * Time.deltaTime;
        currentT += deltaT * directionFactor;

        // 【ソニックフィール】傾き（G値）による加減速をここに書く
        // 例: Vector3 tangent = currentRail.Container.EvaluateTangent(currentT);
        // tangent.y がマイナス（下り坂）なら currentSpeed を上げる、など

        // 終点または始点に達したら離脱
        if (currentT > 1.0f || currentT < 0.0f)
        {
            ExitGrind(isEndOfRail: true);
            return;
        }

        // 座標と回転の更新
        Vector3 nextPosition = currentRail.Container.EvaluatePosition(currentT);
        Vector3 nextTangent = currentRail.Container.EvaluateTangent(currentT);


        Vector3 targetPosition = nextPosition + Vector3.up * 2.4f;
        Vector3 movement = targetPosition - transform.position;
        float distance = movement.magnitude;
        if (distance > Mathf.Epsilon)
        {
            // Transform movement can skip thin obstacles between frames.
            Physics.SyncTransforms();
            RaycastHit[] hits = m_rb.SweepTestAll(
                movement / distance, distance, QueryTriggerInteraction.Ignore);
            RaycastHit nearestHit = default;
            float nearestDistance = float.PositiveInfinity;
            foreach (RaycastHit hit in hits)
            {
                if (!IsObstacle(hit.collider) || hit.distance >= nearestDistance) continue;
                nearestHit = hit;
                nearestDistance = hit.distance;
            }

            if (nearestDistance < float.PositiveInfinity)
            {
                transform.position += movement / distance *
                    Mathf.Max(0f, nearestDistance - CollisionSkin);
                ExitGrindOnCollision(nearestHit.normal);
                return;
            }
        }

        transform.position = targetPosition;
        if (nextTangent != Vector3.zero)
        {
            // 逆走時は回転も180度反転させる
            transform.rotation = Quaternion.LookRotation(nextTangent * directionFactor);
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
        if (!IsGrinding || currentRail == null) return;

        IsGrinding = false;
        var direction = (Vector3)currentRail.Container.EvaluateTangent(Mathf.Clamp(currentT, 0.1f, 0.99f));
      
       
        // 離脱時のベクトルの計算（レールの向き × 最終速度）
        Vector3 exitVelocity = direction.normalized * currentSpeed * new float3(directionFactor, 1.0f, directionFactor) ;

        if (currentRail.IsBoostRail)
        {
            // ※ここでプレイヤーの元の物理挙動を有効化し、exitVelocity を Rigidbody.velocity 等にブチ込む！
            m_rb.linearVelocity = exitVelocity * exitSpeedScale;
        }
        m_rb.linearVelocity = exitVelocity;

        currentRail = null;
        Debug.Log("グラインド終了！飛び出し速度" + exitVelocity * exitSpeedScale);
    }
}