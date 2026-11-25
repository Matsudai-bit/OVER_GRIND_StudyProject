using UnityEngine;

/// <summary>
/// 砂山に接触したとき、その接触点を削るコンポーネント。
/// Rigidbody + Collider、CharacterController、Is Trigger コライダーの3種類に対応します。
/// </summary>
public class SandDigger : MonoBehaviour
{
    // ---- 定数 ----
    private const float MIN_DELTA_TIME = 1e-4f;        // ゼロ除算防止用の最小経過時間(秒)
    private const float MIN_REFERENCE_SPEED = 0.01f;   // 基準速度の最小値(m/s)
    private const float DEBUG_LOG_INTERVAL = 0.5f;     // デバッグログの出力間隔(秒)

    // ---- Inspector 設定項目 ----
    [Header("削り方")]
    [Tooltip("接触点を中心に削る範囲の半径(m)")]
    [SerializeField] private float m_digRadius = 0.5f;
    [Tooltip("1回の接触で掘り下げる深さ(m)")]
    [SerializeField] private float m_digDepth = 0.08f;

    [Header("発動条件")]
    [Tooltip("この速さ(m/s)未満なら削らない。立っているだけで沈まないようにする")]
    [SerializeField] private float m_minSpeed = 0.5f;
    [Tooltip("通常の衝突で削る間隔(秒)")]
    [SerializeField] private float m_digInterval = 0.1f;
    [Tooltip("1回の衝突イベントで処理する接触点の最大数")]
    [SerializeField] private int m_maxContactsPerEvent = 4;

    [Header("Trigger コライダーでの判定")]
    [Tooltip("削りに使いたい Is Trigger のコライダーを登録する（子オブジェクトのものも可）。空なら Trigger 判定は行わない")]
    [SerializeField] private Collider[] m_triggerColliders;
    [Tooltip("砂の表面を調べる間隔の倍率。1 = 砂のサンプル点と同じ間隔。小さいほど細かく調べるが重くなる")]
    [Range(0.5f, 2f)]
    [SerializeField] private float m_triggerSampleStep = 1f;
    [Tooltip("Trigger コライダーで連続して削る間隔(秒)。大きくすると反応間隔を遅くできる")]
    [Min(0f)]
    [SerializeField] private float m_triggerDigInterval = 0.25f;
    [Tooltip("攻撃などで Trigger コライダーが有効になってから、反応し始めるまでの遅延時間(秒)")]
    [Min(0f)]
    [SerializeField] private float m_triggerActivationDelay = 0.1f;
    [Tooltip("Trigger コライダーが砂山と重なり始めてから、実際に削るまでの遅延時間(秒)")]
    [Min(0f)]
    [SerializeField] private float m_triggerContactDelay = 0f;

    [Header("デバッグ")]
    [Tooltip("ONにすると、衝突・速度・削りの判定結果をConsoleに出す（0.5秒に1回）")]
    [SerializeField] private bool m_debugLog = false;

    [Header("足元の扱い（引っかかり対策）")]
    [Tooltip("法線の上向き成分(0〜1)がこれ以上の接触は「足元（地面）」とみなす。安息角33°の斜面は約0.84")]
    [Range(0f, 1f)]
    [SerializeField] private float m_groundNormalY = 0.6f;
    [Tooltip("足元の接触での深さの倍率。0 にすると歩いても足元は削らない（壁にぶつかった時と着地の衝撃だけ削る）")]
    [Range(0f, 1f)]
    [SerializeField] private float m_groundDepthScale = 0.2f;
    [Tooltip("この速さ(m/s)以上で当たった着地は、足元でも全力で削る")]
    [SerializeField] private float m_impactSpeed = 4f;

    [Header("衝撃の強さで変化")]
    [Tooltip("速度に応じて削り半径・深さをスケールさせるかどうか")]
    [SerializeField] private bool m_scaleByImpactSpeed = true;
    [Tooltip("この速さ(m/s)のとき、半径・深さが等倍になる")]
    [SerializeField] private float m_referenceSpeed = 5f;
    [Tooltip("等倍に対する最小/最大の倍率")]
    [SerializeField] private Vector2 m_scaleRange = new Vector2(0.5f, 3f);

    // ---- メンバー変数 ----
    private Rigidbody m_rigidbody;                     // 自身の Rigidbody キャッシュ
    private CharacterController m_characterController; // 自身の CharacterController キャッシュ
    private float m_nextDigTime;                       // 通常の衝突で次に削れる時刻
    private float m_nextTriggerDigTime;                // Trigger判定で次に削れる時刻
    private float m_nextLogTime;                       // 次にログ出力可能な時刻
    private Vector3 m_lastPosition;                    // 前フレームのワールド座標
    private float m_measuredSpeed;                     // 座標差分から算出した移動速度(m/s)
    private Collider m_lastCollider;                   // 最後に衝突したコライダーのキャッシュ
    private MarchingCubesMountain m_lastMountain;      // 最後に衝突した砂山のキャッシュ
    private readonly SandTriggerScanner m_triggerScanner = new SandTriggerScanner(); // Trigger走査担当

    // ---- Unity ライフサイクル ----
    private void Awake()
    {
        m_rigidbody = GetComponent<Rigidbody>();
        m_characterController = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        m_lastPosition = transform.position;
        m_measuredSpeed = 0f;
        m_triggerScanner.ResetTracking(m_triggerColliders, Time.time);
    }

    private void FixedUpdate()
    {
        float dt = Mathf.Max(Time.fixedDeltaTime, MIN_DELTA_TIME);
        Vector3 currentPos = transform.position;
        m_measuredSpeed = (currentPos - m_lastPosition).magnitude / dt;
        m_lastPosition = currentPos;

        if (m_triggerColliders == null || m_triggerColliders.Length == 0)
        {
            return;
        }

        m_triggerScanner.UpdateTriggerStates(m_triggerColliders, dt, Time.time);
        DigWithTriggers();
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleCollision(collision, true);
    }

    private void OnCollisionStay(Collision collision)
    {
        HandleCollision(collision, false);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        MarchingCubesMountain mountain = GetOrCacheMountain(hit.collider);
        if (mountain == null)
        {
            return;
        }

        float factor = CalculateDepthFactor(hit.normal, false);
        if (factor <= 0f)
        {
            return;
        }

        if (!TryGetDigParams(GetCurrentSpeed(), m_digInterval, ref m_nextDigTime, out float radius, out float depth))
        {
            return;
        }

        LogDebug("削った: " + hit.point);
        mountain.Carve(hit.point, radius, depth * factor);
    }

    // ---- 内部処理 ----
    /// <summary>
    /// デバッグログが有効な場合、一定間隔でメッセージを出力します。
    /// </summary>
    private void LogDebug(string message)
    {
        if (!m_debugLog || Time.time < m_nextLogTime)
        {
            return;
        }

        m_nextLogTime = Time.time + DEBUG_LOG_INTERVAL;
        Debug.Log("[SandDigger] " + message, this);
    }

    /// <summary>
    /// コライダー（またはその親）から砂山コンポーネントを取得します（前回分をキャッシュ）。
    /// </summary>
    private MarchingCubesMountain GetOrCacheMountain(Collider col)
    {
        if (col == null)
        {
            return null;
        }

        if (col == m_lastCollider)
        {
            return m_lastMountain;
        }

        m_lastCollider = col;
        m_lastMountain = col.GetComponentInParent<MarchingCubesMountain>();
        return m_lastMountain;
    }

    /// <summary>
    /// 現在の移動速度(m/s)を取得します。
    /// </summary>
    private float GetCurrentSpeed()
    {
        float speed = m_measuredSpeed;
        if (m_characterController != null)
        {
            speed = Mathf.Max(speed, m_characterController.velocity.magnitude);
        }

        if (m_rigidbody != null)
        {
            speed = Mathf.Max(speed, m_rigidbody.linearVelocity.magnitude);
        }

        return speed;
    }

    /// <summary>
    /// 速度とクールタイムを検証し、掘削に使う半径と深さを算出します。
    /// </summary>
    /// <returns>
    /// true：掘削可能です。
    /// false：速度不足またはクールタイム中のため掘削しません。
    /// </returns>
    private bool TryGetDigParams(float speed, float interval, ref float nextAllowedTime, out float radius, out float depth)
    {
        radius = m_digRadius;
        depth = m_digDepth;

        if (speed < m_minSpeed)
        {
            LogDebug("速さが足りない: " + speed.ToString("F2") + " < minSpeed " + m_minSpeed);
            return false;
        }

        if (Time.time < nextAllowedTime)
        {
            return false;
        }

        if (m_scaleByImpactSpeed)
        {
            float scale = Mathf.Clamp(
                speed / Mathf.Max(MIN_REFERENCE_SPEED, m_referenceSpeed),
                m_scaleRange.x,
                m_scaleRange.y
            );
            radius *= scale;
            depth *= scale;
        }

        nextAllowedTime = Time.time + interval;
        return true;
    }

    /// <summary>
    /// 接触法線が足元か壁かを判定し、掘削深さの倍率を返します。
    /// </summary>
    private float CalculateDepthFactor(Vector3 contactNormal, bool isImpact)
    {
        bool isGround = Vector3.Dot(contactNormal, Vector3.up) >= m_groundNormalY;
        if (!isGround || isImpact)
        {
            return 1f;
        }

        return m_groundDepthScale;
    }

    /// <summary>
    /// Rigidbody の衝突イベントを処理して砂山を削ります。
    /// </summary>
    private void HandleCollision(Collision collision, bool isEnter)
    {
        MarchingCubesMountain mountain = GetOrCacheMountain(collision.collider);
        if (mountain == null)
        {
            LogDebug("砂山ではない物に衝突: " + collision.collider.name);
            return;
        }

        float relativeSpeed = collision.relativeVelocity.magnitude;
        float speed = Mathf.Max(GetCurrentSpeed(), relativeSpeed);
        if (!TryGetDigParams(speed, m_digInterval, ref m_nextDigTime, out float radius, out float depth))
        {
            return;
        }

        bool isImpact = isEnter && relativeSpeed >= m_impactSpeed;
        int contactCount = Mathf.Min(collision.contactCount, m_maxContactsPerEvent);
        int dugCount = 0;

        for (int i = 0; i < contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            float factor = CalculateDepthFactor(contact.normal, isImpact);
            if (factor <= 0f)
            {
                continue;
            }

            mountain.Carve(contact.point, radius, depth * factor);
            dugCount++;
        }

        LogDebug("接触点 " + contactCount + " 個中 " + dugCount + " 個を削った（半径 " + radius.ToString("F2") + "）");
    }

    /// <summary>
    /// 登録された Trigger コライダーと砂山の重なりを調べて削ります。
    /// 速度不足やクールタイム中はスキャン自体をスキップして負荷を抑えます。
    /// </summary>
    private void DigWithTriggers()
    {
        float currentTime = Time.time;

        // 接触遅延が不要かつクールタイム中であれば、スキャン処理を丸ごとスキップする
        if (m_triggerContactDelay <= 0f && currentTime < m_nextTriggerDigTime)
        {
            return;
        }

        var mountains = MarchingCubesMountain.Instances;
        float baseSpeed = GetCurrentSpeed();

        for (int i = 0; i < m_triggerColliders.Length; i++)
        {
            Collider col = m_triggerColliders[i];
            if (!m_triggerScanner.IsSupportedCollider(col, this))
            {
                continue;
            }

            if (!m_triggerScanner.IsActivationDelayElapsed(i, currentTime, m_triggerActivationDelay))
            {
                continue;
            }

            // 速度条件を満たしていないコライダーは高コストなサンプリングを行わない
            float speed = Mathf.Max(baseSpeed, m_triggerScanner.GetTriggerSpeed(i));
            if (speed < m_minSpeed)
            {
                m_triggerScanner.UpdateContactAndCheckDelay(i, false, currentTime, m_triggerContactDelay);
                continue;
            }

            Bounds bounds = col.bounds;
            bool hasAnyOverlap = false;

            for (int m = 0; m < mountains.Count; m++)
            {
                MarchingCubesMountain mountain = mountains[m];
                if (mountain == null || !mountain.WorldBounds.Intersects(bounds))
                {
                    continue;
                }

                m_triggerScanner.CollectOverlapPoints(mountain, col, bounds, m_triggerSampleStep);
                var points = m_triggerScanner.OverlapPoints;
                var normals = m_triggerScanner.OverlapNormals;
                if (points.Count == 0)
                {
                    continue;
                }

                hasAnyOverlap = true;

                if (!m_triggerScanner.UpdateContactAndCheckDelay(i, true, currentTime, m_triggerContactDelay))
                {
                    continue;
                }

                if (!TryGetDigParams(speed, m_triggerDigInterval, ref m_nextTriggerDigTime, out float radius, out float depth))
                {
                    continue;
                }

                int total = points.Count;
                int useCount = Mathf.Min(total, Mathf.Max(1, m_maxContactsPerEvent));
                int dugCount = 0;

                for (int k = 0; k < useCount; k++)
                {
                    int idx = (useCount == total) ? k : Mathf.FloorToInt((k + 0.5f) * total / useCount);
                    float factor = CalculateDepthFactor(normals[idx], false);
                    if (factor <= 0f)
                    {
                        continue;
                    }

                    mountain.Carve(points[idx], radius, depth * factor);
                    dugCount++;
                }

                LogDebug("Trigger[" + col.name + "] 重なり " + total + " 点中 " + dugCount + " 点を削った");
            }

            if (!hasAnyOverlap)
            {
                m_triggerScanner.UpdateContactAndCheckDelay(i, false, currentTime, m_triggerContactDelay);
            }
        }
    }
}