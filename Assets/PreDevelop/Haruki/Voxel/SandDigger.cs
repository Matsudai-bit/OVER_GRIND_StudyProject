using UnityEngine;

/// <summary>
/// 砂山に接触したとき、その接触点を削るコンポーネント。
/// 砂山を削らせたいオブジェクト（プレイヤー、ボール、車など）にアタッチする。
///
/// 対応するオブジェクト:
///   ・非Kinematicな Rigidbody + Collider   → OnCollisionEnter / Stay
///   ・CharacterController                   → OnControllerColliderHit
/// ※ Kinematic な Rigidbody は、静的コライダー(砂山)との衝突イベントが来ないので使えない。
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
    public float digInterval = 0.05f;
    [Tooltip("1回の衝突イベントで処理する接触点の最大数")]
    public int maxContactsPerEvent = 4;

    [Header("衝撃の強さで変化")]
    public bool scaleByImpactSpeed = true;
    [Tooltip("この速さ(m/s)のとき、半径・深さが等倍になる")]
    public float referenceSpeed = 5f;
    [Tooltip("等倍に対する最小/最大の倍率")]
    public Vector2 scaleRange = new Vector2(0.5f, 3f);

    private Rigidbody rb;
    private CharacterController controller;
    private float nextDigTime;

    // 同じ砂山に何度も GetComponent しないためのキャッシュ
    private Collider lastCollider;
    private MarchingCubesMountain lastMountain;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        controller = GetComponent<CharacterController>();
    }

    MarchingCubesMountain FindMountain(Collider col)
    {
        if (col == lastCollider) return lastMountain;
        lastCollider = col;
        lastMountain = col.GetComponent<MarchingCubesMountain>();
        return lastMountain;
    }

    float CurrentSpeed(float fallback)
    {
        if (controller != null) return controller.velocity.magnitude;
        if (rb != null) return rb.linearVelocity.magnitude;
        return fallback;
    }

    bool TryGetDigParams(float speed, out float radius, out float depth)
    {
        radius = digRadius;
        depth = digDepth;

        if (speed < minSpeed) return false;
        if (Time.time < nextDigTime) return false;

        if (scaleByImpactSpeed)
        {
            float s = Mathf.Clamp(speed / Mathf.Max(0.01f, referenceSpeed), scaleRange.x, scaleRange.y);
            radius *= s;
            depth *= s;
        }

        nextDigTime = Time.time + digInterval;
        return true;
    }

    // ---- Rigidbody 用 ----
    void OnCollisionEnter(Collision collision) { HandleCollision(collision); }
    void OnCollisionStay(Collision collision) { HandleCollision(collision); }

    void HandleCollision(Collision collision)
    {
        MarchingCubesMountain mountain = FindMountain(collision.collider);
        if (mountain == null) return;

        // 衝突直後は relativeVelocity の方が衝撃を正しく表す
        float speed = Mathf.Max(CurrentSpeed(0f), collision.relativeVelocity.magnitude);
        if (!TryGetDigParams(speed, out float radius, out float depth)) return;

        int count = Mathf.Min(collision.contactCount, maxContactsPerEvent);
        for (int i = 0; i < count; i++)
        {
            mountain.Carve(collision.GetContact(i).point, radius, depth);
        }
    }

    // ---- CharacterController 用 ----
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        MarchingCubesMountain mountain = FindMountain(hit.collider);
        if (mountain == null) return;

        if (!TryGetDigParams(CurrentSpeed(0f), out float radius, out float depth)) return;

        mountain.Carve(hit.point, radius, depth);
    }
}