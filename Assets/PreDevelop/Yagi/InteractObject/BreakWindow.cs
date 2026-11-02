using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>攻撃命中で指定ガラスを非表示にし、ガラス片を飛散させます。</summary>
[DisallowMultipleComponent]
public sealed class BreakWindow : MonoBehaviour, IDirectionalDamageable, IAttackContactDamageReceiver
{
    [SerializeField, Header("壊れるガラスのルート")]
    private Transform[] m_breakableRoots;

    [SerializeField, Header("破壊時に非表示にするオブジェクト")]
    private GameObject[] m_hideOnBreak;
    [SerializeField, Min(0f), Header("破片の飛散速度")]
    private float m_launchSpeed = 5f;
    [SerializeField, Min(0f)]
    private float m_upwardSpeed = 2f;
    [SerializeField, Min(0f)]
    private float m_spreadSpeed = 2f;
    [SerializeField, Min(0f)]
    private float m_rotationSpeed = 4f;
    [SerializeField, Min(0.1f), Header("破片が消えるまでの秒数")]
    private float m_fragmentLifetime = 8f;

    [SerializeField, Header("デバッグ")]
    [Tooltip("有効な場合、Pキーを押すとBreakを呼び出します。")]
    private bool m_enableDebugBreak;

    private readonly List<Rigidbody> m_fragments = new();
    private bool m_isBroken;
    private Vector3 m_launchDirection;

    /// <summary>指定した破片だけに物理判定を準備します。</summary>
    private void Awake()
    {
        m_launchDirection = transform.forward;
        int attackLayer = LayerMask.NameToLayer("InteractObject");
        if (m_breakableRoots == null) return;
        HashSet<MeshFilter> registered = new();
        foreach (Transform root in m_breakableRoots)
        {
            if (root == null) continue;
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null || !registered.Add(filter)) continue;
                GameObject fragment = filter.gameObject;
                fragment.isStatic = false;
                if (attackLayer >= 0) fragment.layer = attackLayer;
                // 薄い破片にも厚みを持つ判定を用意します。
                if (fragment.GetComponent<Collider>() == null)
                {
                    BoxCollider collider = fragment.AddComponent<BoxCollider>();
                    Bounds bounds = filter.sharedMesh.bounds;
                    collider.center = bounds.center;
                    collider.size = Vector3.Max(bounds.size, Vector3.one * 0.02f);
                }
                Rigidbody body = fragment.GetComponent<Rigidbody>();
                if (body == null) body = fragment.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
                m_fragments.Add(body);
            }
        }
    }

    /// <summary>デバッグが有効な場合だけPキーで破壊します。</summary>
    private void Update()
    {
        if (!m_enableDebugBreak || m_isBroken) return;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.pKey.wasPressedThisFrame)
        {
            Break();
        }
    }

    /// <summary>方向情報のない攻撃を受け付けます。</summary>
    public void TakeDamage(AttackDamageData damageData)
    {
        TryTakeDamage(damageData, transform.position - transform.forward);
    }

    /// <summary>攻撃方向を保存してBreakを呼びます。</summary>
    /// <returns>true：破壊を受け付けた。false：破壊済み、無効な攻撃、または破片未設定。</returns>
    public bool TryTakeDamage(AttackDamageData damageData, Vector3 attackCenter)
    {
        if (m_isBroken || damageData.Damage <= 0 || m_fragments.Count == 0) return false;
        if (!damageData.TryGetSourceFacingDirection(out m_launchDirection))
        {
            m_launchDirection = (transform.position - attackCenter).normalized;
            if (m_launchDirection == Vector3.zero) m_launchDirection = transform.forward;
        }
        Break();
        return true;
    }

    /// <summary>各ガラス片を一度だけ解放し、飛散させます。</summary>
    [ContextMenu("Break")]
    public void Break()
    {
        if (!Application.isPlaying || m_isBroken || m_fragments.Count == 0) return;
        m_isBroken = true;
        if (m_hideOnBreak != null)
        {
            foreach (GameObject target in m_hideOnBreak)
            {
                if (target != null) target.SetActive(false);
            }
        }

        foreach (Rigidbody fragment in m_fragments)
        {
            if (fragment == null) continue;
            fragment.transform.SetParent(null, true);
            fragment.gameObject.SetActive(true);
            fragment.gameObject.layer = 0;
            fragment.constraints = RigidbodyConstraints.None;
            fragment.isKinematic = false;
            fragment.useGravity = true;
            foreach (MeshCollider collider in fragment.GetComponents<MeshCollider>())
                collider.convex = true;
            fragment.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            fragment.linearVelocity = -m_launchDirection * m_launchSpeed
                + Vector3.up * m_upwardSpeed + Random.insideUnitSphere * m_spreadSpeed;
            fragment.angularVelocity = Random.onUnitSphere * m_rotationSpeed;
            Destroy(fragment.gameObject, m_fragmentLifetime);
        }
    }
}
