using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 砂山に接触したとき、その接触点を削るコンポーネント。
/// 砂山を削らせたいオブジェクト（プレイヤー、ボール、車など）にアタッチする。
///
/// 対応するオブジェクト:
///   ・非Kinematicな Rigidbody + Collider   → OnCollisionEnter / Stay
///   ・CharacterController                   → OnControllerColliderHit
///   ・Is Trigger のコライダー               → triggerColliders に登録（物理イベントを使わず、砂の表面との重なりを直接調べる）
/// ※ Kinematic な Rigidbody は、静的コライダー(砂山)との衝突イベントが来ないので使えない。
///   Trigger 判定は Rigidbody の種類に関係なく動く。
/// </summary>
public class SandDigger : MonoBehaviour
{
    [Header("削り方")]
    [Tooltip("接触点を中心に削る範囲の半径(m)")]
    public float digRadius = 0.5f;
    [Tooltip("1回の接触で掘り下げる深さ(m)")]
    public float digDepth = 0.08f;

    [Header("発動条件")]
    [Tooltip("この速さ(m/s)未満なら削らない。立っているだけで沈まないようにする")]
    public float minSpeed = 0.5f;
    [Tooltip("削る間隔(秒)")]
    public float digInterval = 0.1f;
    [Tooltip("1回の衝突イベントで処理する接触点の最大数")]
    public int maxContactsPerEvent = 4;

    [Header("Trigger コライダーでの判定")]
    [Tooltip("削りに使いたい Is Trigger のコライダーを登録する（子オブジェクトのものも可）。空なら Trigger 判定は行わない")]
    public Collider[] triggerColliders;
    [Tooltip("砂の表面を調べる間隔の倍率。1 = 砂のサンプル点と同じ間隔。小さいほど細かく調べるが重くなる")]
    [Range(0.5f, 2f)] public float triggerSampleStep = 1f;

    [Header("デバッグ")]
    [Tooltip("ONにすると、衝突・速度・削りの判定結果をConsoleに出す（0.5秒に1回）")]
    public bool debugLog = false;

    [Header("足元の扱い（引っかかり対策）")]
    [Tooltip("法線の上向き成分(0〜1)がこれ以上の接触は「足元（地面）」とみなす。安息角33°の斜面は約0.84")]
    [Range(0f, 1f)] public float groundNormalY = 0.6f;
    [Tooltip("足元の接触での深さの倍率。0 にすると歩いても足元は削らない（壁にぶつかった時と着地の衝撃だけ削る）")]
    [Range(0f, 1f)] public float groundDepthScale = 0.2f;
    [Tooltip("この速さ(m/s)以上で当たった着地は、足元でも全力で削る")]
    public float impactSpeed = 4f;

    [Header("衝撃の強さで変化")]
    public bool scaleByImpactSpeed = true;
    [Tooltip("この速さ(m/s)のとき、半径・深さが等倍になる")]
    public float referenceSpeed = 5f;
    [Tooltip("等倍に対する最小/最大の倍率")]
    public Vector2 scaleRange = new Vector2(0.5f, 3f);

    private Rigidbody rb;
    private CharacterController controller;
    private float nextDigTime;
    private float nextTriggerDigTime;
    private float nextLogTime;

    // Trigger 判定用
    private Vector3[] triggerLastCenter;
    private float[] triggerSpeed;
    private bool warnedMeshCollider;
    private readonly List<Vector3> overlapPoints = new List<Vector3>();
    private readonly List<Vector3> overlapNormals = new List<Vector3>();

    // 位置の変化から求めた速さ。MovePosition や transform で動かすスクリプトだと
    // Rigidbody.linearVelocity が 0 のままなので、その代わりに使う
    private Vector3 lastPosition;
    private float measuredSpeed;

    // 同じ砂山に何度も GetComponent しないためのキャッシュ
    private Collider lastCollider;
    private MarchingCubesMountain lastMountain;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        controller = GetComponent<CharacterController>();
    }

    void OnEnable()
    {
        lastPosition = transform.position;
        measuredSpeed = 0f;
        ResetTriggerTracking();
    }

    void ResetTriggerTracking()
    {
        int count = triggerColliders != null ? triggerColliders.Length : 0;
        triggerLastCenter = new Vector3[count];
        triggerSpeed = new float[count];
        for (int i = 0; i < count; i++)
        {
            if (triggerColliders[i] != null) triggerLastCenter[i] = triggerColliders[i].bounds.center;
        }
    }

    void FixedUpdate()
    {
        float dt = Mathf.Max(Time.fixedDeltaTime, 1e-4f);

        Vector3 p = transform.position;
        measuredSpeed = (p - lastPosition).magnitude / dt;
        lastPosition = p;

        if (triggerColliders == null || triggerColliders.Length == 0) return;

        // Inspector で配列の長さを変えた場合に備える
        if (triggerLastCenter == null || triggerLastCenter.Length != triggerColliders.Length) ResetTriggerTracking();

        // 各コライダー自身の速さ（剣を振る動きなどは親の速さでは分からない）
        for (int i = 0; i < triggerColliders.Length; i++)
        {
            Collider col = triggerColliders[i];
            if (col == null) continue;
            Vector3 c = col.bounds.center;
            triggerSpeed[i] = (c - triggerLastCenter[i]).magnitude / dt;
            triggerLastCenter[i] = c;
        }

        DigWithTriggers();
    }

    void Log(string message)
    {
        if (!debugLog || Time.time < nextLogTime) return;
        nextLogTime = Time.time + 0.5f;
        Debug.Log("[SandDigger] " + message, this);
    }

    MarchingCubesMountain FindMountain(Collider col)
    {
        if (col == lastCollider) return lastMountain;
        lastCollider = col;
        lastMountain = col.GetComponent<MarchingCubesMountain>();
        return lastMountain;
    }

    float CurrentSpeed()
    {
        float speed = measuredSpeed;
        if (controller != null) speed = Mathf.Max(speed, controller.velocity.magnitude);
        if (rb != null) speed = Mathf.Max(speed, rb.linearVelocity.magnitude);
        return speed;
    }

    bool TryGetDigParams(float speed, out float radius, out float depth)
    {
        return TryGetDigParams(speed, ref nextDigTime, out radius, out depth);
    }

    bool TryGetDigParams(float speed, ref float nextTime, out float radius, out float depth)
    {
        radius = digRadius;
        depth = digDepth;

        if (speed < minSpeed)
        {
            Log("速さが足りない: " + speed.ToString("F2") + " < minSpeed " + minSpeed);
            return false;
        }
        if (Time.time < nextTime) return false;

        if (scaleByImpactSpeed)
        {
            float s = Mathf.Clamp(speed / Mathf.Max(0.01f, referenceSpeed), scaleRange.x, scaleRange.y);
            radius *= s;
            depth *= s;
        }

        nextTime = Time.time + digInterval;
        return true;
    }

    // ---- Rigidbody 用 ----
    void OnCollisionEnter(Collision collision) { HandleCollision(collision, true); }
    void OnCollisionStay(Collision collision) { HandleCollision(collision, false); }

    // 接触が足元（地面）か壁かで、削る深さの倍率を変える。0 なら削らない
    float DepthFactor(Vector3 contactNormal, bool isImpact)
    {
        bool isGround = Vector3.Dot(contactNormal, Vector3.up) >= groundNormalY;
        if (!isGround || isImpact) return 1f;
        return groundDepthScale;
    }

    void HandleCollision(Collision collision, bool isEnter)
    {
        MarchingCubesMountain mountain = FindMountain(collision.collider);
        if (mountain == null)
        {
            Log("砂山ではない物に衝突: " + collision.collider.name);
            return;
        }

        // 衝突直後は relativeVelocity の方が衝撃を正しく表す
        float speed = Mathf.Max(CurrentSpeed(), collision.relativeVelocity.magnitude);
        if (!TryGetDigParams(speed, out float radius, out float depth)) return;

        bool isImpact = isEnter && collision.relativeVelocity.magnitude >= impactSpeed;

        int count = Mathf.Min(collision.contactCount, maxContactsPerEvent);
        int dug = 0;
        for (int i = 0; i < count; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            float factor = DepthFactor(contact.normal, isImpact);
            if (factor <= 0f) continue;

            mountain.Carve(contact.point, radius, depth * factor);
            dug++;
        }
        Log("接触点 " + count + " 個中 " + dug + " 個を削った（半径 " + radius.ToString("F2") + "）");
    }

    // ---- CharacterController 用 ----
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        MarchingCubesMountain mountain = FindMountain(hit.collider);
        if (mountain == null) return;

        if (!TryGetDigParams(CurrentSpeed(), out float radius, out float depth)) return;

        float factor = DepthFactor(hit.normal, false);
        if (factor <= 0f) return;

        Log("削った: " + hit.point);
        mountain.Carve(hit.point, radius, depth * factor);
    }

    // ---- Is Trigger コライダー用 ----
    // トリガーは接触点が取れないので、「砂の表面の点がコライダーの内側に入っているか」を調べて
    // 重なっている点を擬似的な接触点として扱う。

    void DigWithTriggers()
    {
        for (int i = 0; i < triggerColliders.Length; i++)
        {
            Collider col = triggerColliders[i];
            if (col == null || !col.enabled || !col.gameObject.activeInHierarchy) continue;

            // ClosestPoint は凹メッシュコライダーに使えない
            MeshCollider mc = col as MeshCollider;
            if (mc != null && !mc.convex)
            {
                if (!warnedMeshCollider)
                {
                    warnedMeshCollider = true;
                    Debug.LogWarning("[SandDigger] " + col.name + " は Convex でない MeshCollider なので Trigger 判定に使えません。", this);
                }
                continue;
            }

            Bounds b = col.bounds;
            for (int m = 0; m < MarchingCubesMountain.Instances.Count; m++)
            {
                MarchingCubesMountain mountain = MarchingCubesMountain.Instances[m];
                if (!mountain.WorldBounds.Intersects(b)) continue;

                CollectOverlap(mountain, col, b);
                if (overlapPoints.Count == 0) continue;

                float speed = Mathf.Max(CurrentSpeed(), triggerSpeed[i]);
                if (!TryGetDigParams(speed, ref nextTriggerDigTime, out float radius, out float depth)) continue;

                // 重なった点が多いときは均等に間引く
                int total = overlapPoints.Count;
                int use = Mathf.Min(total, Mathf.Max(1, maxContactsPerEvent));
                int dug = 0;
                for (int k = 0; k < use; k++)
                {
                    int idx = (use == total) ? k : Mathf.FloorToInt((k + 0.5f) * total / use);
                    float factor = DepthFactor(overlapNormals[idx], false);
                    if (factor <= 0f) continue;

                    mountain.Carve(overlapPoints[idx], radius, depth * factor);
                    dug++;
                }
                Log("Trigger[" + col.name + "] 重なり " + total + " 点中 " + dug + " 点を削った");
            }
        }
    }

    // コライダーの AABB 内を格子状に調べ、砂の表面がコライダーの内側にある点を集める
    void CollectOverlap(MarchingCubesMountain mountain, Collider col, Bounds b)
    {
        overlapPoints.Clear();
        overlapNormals.Clear();

        float step = Mathf.Max(0.01f, mountain.WorldVoxelSize * triggerSampleStep);
        int nx = Mathf.Min(Mathf.CeilToInt(b.size.x / step) + 1, 40);
        int nz = Mathf.Min(Mathf.CeilToInt(b.size.z / step) + 1, 40);

        for (int ix = 0; ix < nx; ix++)
        {
            float x = nx == 1 ? b.center.x : Mathf.Lerp(b.min.x, b.max.x, ix / (float)(nx - 1));
            for (int iz = 0; iz < nz; iz++)
            {
                float z = nz == 1 ? b.center.z : Mathf.Lerp(b.min.z, b.max.z, iz / (float)(nz - 1));

                if (!mountain.TrySampleSurface(new Vector3(x, b.center.y, z), out Vector3 sp, out Vector3 sn)) continue;
                if (sp.y < b.min.y || sp.y > b.max.y) continue;

                // 内側の点なら ClosestPoint は同じ点を返す
                Vector3 cp = col.ClosestPoint(sp);
                if ((cp - sp).sqrMagnitude > 1e-6f) continue;

                overlapPoints.Add(sp);
                overlapNormals.Add(sn);
            }
        }
    }
}