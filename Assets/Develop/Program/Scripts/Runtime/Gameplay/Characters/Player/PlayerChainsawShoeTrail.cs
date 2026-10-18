using UnityEngine;

/// <summary>アニメーション後の左右の靴を接地判定し、移動した地面に刃痕を刻みます。</summary>
[DisallowMultipleComponent]
public sealed class PlayerChainsawShoeTrail : MonoBehaviour
{
    [SerializeField] private Transform m_leftShoe;
    [SerializeField] private Transform m_rightShoe;
    [SerializeField] private PlayerMonitor m_monitor;
    [SerializeField] private PlayerStateMachineComponent m_stateMachine;
    [SerializeField] private Material m_material;
    [SerializeField] private LayerMask m_groundLayers = 8;
    [SerializeField, Min(0.01f)] private float m_contactDistance = 0.25f;
    [SerializeField, Min(0.01f)] private float m_sampleSpacing = 0.08f;
    [SerializeField, Min(0.01f)] private float m_width = 0.13f;
    [SerializeField, Min(0.001f)] private float m_surfaceOffset = 0.008f;
    [SerializeField, Min(0.1f)] private float m_maxConnectionDistance = 1.5f;
    [SerializeField, Min(0.1f), Tooltip("刃痕が完全に消えるまでの秒数。")]
    private float m_lifetime = 2f;
    [SerializeField, Range(0f, 0.99f), Tooltip("寿命に対するフェード開始の割合。0なら生成直後から、0.5なら寿命の半分から薄くなります。")]
    private float m_fadeStartRatio = 0.5f;
    [SerializeField, Range(128, 8192)] private int m_capacity = 4096;

    private const float RAY_LIFT = 0.15f;
    private const float MIN_GROUND_NORMAL_Y = 0.5f;
    private const float MIN_MOVE_SPEED = 0.05f;
    private readonly ShoeContact m_leftContact = new ShoeContact();
    private readonly ShoeContact m_rightContact = new ShoeContact();
    private ChainsawShoeTrailMesh m_trail;
    private Vector3 m_previousPosition;

    /// <summary>左右の靴で独立した接地履歴を保持します。</summary>
    private sealed class ShoeContact
    {
        internal bool m_connected;
        internal Vector3 m_point;
        internal Vector3 m_normal;
        internal Collider m_surface;
        internal float m_distance;
    }

    /// <summary>描画バッファを一度だけ確保します。</summary>
    private void Start()
    {
        if (m_leftShoe == null || m_rightShoe == null || m_monitor == null ||
            m_stateMachine == null || m_material == null)
        {
            Debug.LogError("Chainsaw shoe trail references are missing.", this);
            enabled = false;
            return;
        }
        m_trail = new ChainsawShoeTrailMesh(m_material, m_capacity, m_lifetime, gameObject.scene);
        m_previousPosition = transform.position;
    }

    /// <summary>足のアニメーションを反映した位置で接地と移動を確認します。</summary>
    private void LateUpdate()
    {
        if (m_trail == null) return;
        float displacement = Vector3.Distance(transform.position, m_previousPosition);
        m_previousPosition = transform.position;
        bool isMovingOnGround = m_monitor.IsGrounded && !m_monitor.IsRailed &&
            (m_stateMachine.IsCurrentState<PlayerWalkingState>() ||
             m_stateMachine.IsCurrentState<PlayerVRunningState>() ||
             m_stateMachine.IsCurrentState<PlayerBoostChargingState>() ||
             m_stateMachine.IsCurrentState<PlayerIdlingState>()) &&
            displacement > MIN_MOVE_SPEED * Time.deltaTime &&
            displacement <= m_maxConnectionDistance && Time.deltaTime > 0f;

        if (isMovingOnGround)
        {
            UpdateContact(m_leftShoe, m_leftContact);
            UpdateContact(m_rightShoe, m_rightContact);
        }
        else ResetContacts();
        // 再生中のInspector調整も、既に描画した刃痕を含めて反映します。
        m_trail.SetFadeParameters(m_lifetime, m_fadeStartRatio);
        m_trail.Upload(Time.time);
    }

    /// <summary>靴の真下の床だけを採用し、段差・空中・別の床との間を切断します。</summary>
    private void UpdateContact(Transform shoe, ShoeContact contact)
    {
        Vector3 origin = shoe.position + Vector3.up * RAY_LIFT;
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
                RAY_LIFT + m_contactDistance, m_groundLayers, QueryTriggerInteraction.Ignore) ||
            hit.normal.y < MIN_GROUND_NORMAL_Y || hit.rigidbody != null)
        {
            contact.m_connected = false;
            return;
        }

        Vector3 point = hit.point + hit.normal * m_surfaceOffset;
        float distance = Vector3.Distance(contact.m_point, point);
        bool canConnect = contact.m_connected && contact.m_surface == hit.collider &&
            distance <= m_maxConnectionDistance && Vector3.Dot(contact.m_normal, hit.normal) > 0.9f;
        if (canConnect && distance < m_sampleSpacing) return;
        if (canConnect)
        {
            m_trail.AddSegment(contact.m_point, point, contact.m_normal, hit.normal,
                m_width, contact.m_distance, Time.time);
            contact.m_distance += distance;
        }
        else contact.m_distance = 0f;

        contact.m_point = point;
        contact.m_normal = hit.normal;
        contact.m_surface = hit.collider;
        contact.m_connected = true;
    }

    /// <summary>次の接地まで刃痕の連結を止めます。</summary>
    private void ResetContacts()
    {
        m_leftContact.m_connected = false;
        m_rightContact.m_connected = false;
    }

    /// <summary>非表示・再有効化の前後で刃痕をつなぎません。</summary>
    private void OnDisable() => ResetContacts();

    /// <summary>シーン終了時に生成した描画リソースを解放します。</summary>
    private void OnDestroy() => m_trail?.Dispose();
}
