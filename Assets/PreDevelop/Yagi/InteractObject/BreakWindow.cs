using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>攻撃命中で指定ガラスを非表示にし、ガラス片を飛散させます。</summary>
[DisallowMultipleComponent]
public sealed class BreakWindow : MonoBehaviour,
    IDirectionalDamageable,
    IAttackContactReceiver,
    IAttackContactDamageReceiver
{
    [SerializeField, Header("イベント")]
    private UnityEvent m_destroyedEvent;
    [SerializeField, Header("Damage")]
    [Min(1)]
    private int m_requiredDamage = 100;

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

    [SerializeField, Header("Hit Shake")]
    [Tooltip("命中中に揺らす見た目のTransform。未設定時はこのTransformを使用します。")]
    private Transform m_visualRoot;

    [SerializeField, Min(0f)]
    [Tooltip("命中中の位置揺れ幅です。")]
    private float m_shakePositionAmplitude = 0.04f;

    [SerializeField, Min(0f)]
    [Tooltip("命中中の回転揺れ幅（度）です。")]
    private float m_shakeRotationAngle = 3f;

    [SerializeField, Min(0f)]
    [Tooltip("命中中の揺れ速度です。")]
    private float m_shakeFrequency = 28f;

    [SerializeField, Header("デバッグ")]
    [Tooltip("有効な場合、Pキーを押すとBreakを呼び出します。")]
    private bool m_enableDebugBreak;

    private readonly List<Rigidbody> m_fragments = new();
    private readonly HashSet<int> m_attackContactSourceIds = new();
    private bool m_isBroken;
    private bool m_hasAcceptedDamageDuringContact;
    private int m_accumulatedDamage;
    private Vector3 m_launchDirection;
    private Vector3 m_visualBaseLocalPosition;
    private Quaternion m_visualBaseLocalRotation;

    /// <summary>指定した破片だけに物理判定を準備します。</summary>
    private void Awake()
    {
        // Pキー・直接呼び出しでは窓の前方へ飛ばします。
        m_launchDirection = transform.forward;
        if (m_visualRoot == null) m_visualRoot = transform;
        m_visualBaseLocalPosition = m_visualRoot.localPosition;
        m_visualBaseLocalRotation = m_visualRoot.localRotation;
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

    /// <summary>攻撃判定との接触中、ガラスの見た目だけを振動させます。</summary>
    private void LateUpdate()
    {
        if (m_isBroken ||
            !m_hasAcceptedDamageDuringContact ||
            m_attackContactSourceIds.Count == 0 ||
            m_visualRoot == null)
        {
            return;
        }

        float phase = Time.time * m_shakeFrequency;
        Vector3 positionOffset = new Vector3(
            Mathf.Sin(phase),
            Mathf.Sin(phase * 1.37f) * 0.5f,
            Mathf.Cos(phase * 1.73f)) * m_shakePositionAmplitude;
        Vector3 rotationOffset = new Vector3(
            Mathf.Sin(phase * 1.19f),
            Mathf.Cos(phase * 1.43f),
            Mathf.Sin(phase * 1.67f)) * m_shakeRotationAngle;

        m_visualRoot.localPosition = m_visualBaseLocalPosition + positionOffset;
        m_visualRoot.localRotation =
            m_visualBaseLocalRotation * Quaternion.Euler(rotationOffset);
    }

    /// <summary>攻撃判定との接触開始を記録します。</summary>
    /// <param name="attackSourceId">接触した攻撃判定のInstance ID。</param>
    public void BeginAttackContact(int attackSourceId)
    {
        m_attackContactSourceIds.Add(attackSourceId);
    }

    /// <summary>攻撃判定との接触終了を記録します。</summary>
    /// <param name="attackSourceId">離れた攻撃判定のInstance ID。</param>
    public void EndAttackContact(int attackSourceId)
    {
        m_attackContactSourceIds.Remove(attackSourceId);
        if (m_attackContactSourceIds.Count == 0)
        {
            m_hasAcceptedDamageDuringContact = false;
            RestoreVisualTransform();
        }
    }

    /// <summary>方向情報のない攻撃を受け付けます。</summary>
    public void TakeDamage(AttackDamageData damageData)
    {
        TryTakeDamage(damageData, transform.position - transform.forward);
    }

    /// <summary>攻撃方向を保存してダメージを蓄積します。</summary>
    /// <returns>true：ダメージを受け付けた。false：破壊済み、無効な攻撃、または破片未設定。</returns>
    public bool TryTakeDamage(AttackDamageData damageData, Vector3 attackCenter)
    {
        if (m_isBroken || damageData.Damage <= 0 || m_fragments.Count == 0) return false;
        if (!damageData.TryGetSourceFacingDirection(out m_launchDirection))
        {
            m_launchDirection = (transform.position - attackCenter).normalized;
            if (m_launchDirection == Vector3.zero) m_launchDirection = transform.forward;
        }

        m_hasAcceptedDamageDuringContact = true;
        m_accumulatedDamage += damageData.Damage;
        if (m_accumulatedDamage >= m_requiredDamage)
        {
            Break();
        }

        return true;
    }

    /// <summary>揺らした見た目のローカルTransformを元に戻します。</summary>
    private void RestoreVisualTransform()
    {
        if (m_visualRoot == null) return;
        m_visualRoot.localPosition = m_visualBaseLocalPosition;
        m_visualRoot.localRotation = m_visualBaseLocalRotation;
    }

    /// <summary>各ガラス片を一度だけ解放し、飛散させます。</summary>
    [ContextMenu("Break")]
    public void Break()
    {
        if (!Application.isPlaying || m_isBroken || m_fragments.Count == 0) return;
        m_isBroken = true;
        RestoreVisualTransform();
        if (m_hideOnBreak != null)
        {
            foreach (GameObject target in m_hideOnBreak)
            {
                if (target != null) target.SetActive(false);
            }
        }

        // 全破片の姿勢とColliderを先に確定し、未解放の破片に飛散を阻まれないようにします。
        List<Collider> fragmentColliders = new();
        foreach (Rigidbody fragment in m_fragments)
        {
            if (fragment == null) continue;
            fragment.transform.SetParent(null, true);
            fragment.gameObject.SetActive(true);
            fragment.gameObject.layer = 0;
            fragment.constraints = RigidbodyConstraints.None;
            foreach (MeshCollider collider in fragment.GetComponents<MeshCollider>())
                collider.convex = true;
            fragmentColliders.AddRange(fragment.GetComponents<Collider>());
        }

        // 隣接する薄いガラスのCollider同士が重なっていても、全破片を飛散させます。
        for (int i = 0; i < fragmentColliders.Count; i++)
        {
            for (int j = i + 1; j < fragmentColliders.Count; j++)
            {
                Physics.IgnoreCollision(fragmentColliders[i], fragmentColliders[j]);
            }
        }

        Physics.SyncTransforms();
        foreach (Rigidbody fragment in m_fragments)
        {
            if (fragment == null) continue;
            fragment.isKinematic = false;
            fragment.useGravity = true;
            fragment.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            fragment.linearVelocity = m_launchDirection * m_launchSpeed
                + Vector3.up * m_upwardSpeed + Random.insideUnitSphere * m_spreadSpeed;
            fragment.angularVelocity = Random.onUnitSphere * m_rotationSpeed;
            fragment.WakeUp();
            Destroy(fragment.gameObject, m_fragmentLifetime);
        }
        m_destroyedEvent?.Invoke();
    }

    /// <summary>無効化時に揺れと接触状態を解除します。</summary>
    private void OnDisable()
    {
        RestoreVisualTransform();
        m_hasAcceptedDamageDuringContact = false;
        m_attackContactSourceIds.Clear();
    }

    /// <summary>Inspector設定値を有効範囲へ制限します。</summary>
    private void OnValidate()
    {
        m_requiredDamage = Mathf.Max(1, m_requiredDamage);
        m_shakePositionAmplitude = Mathf.Max(0f, m_shakePositionAmplitude);
        m_shakeRotationAngle = Mathf.Max(0f, m_shakeRotationAngle);
        m_shakeFrequency = Mathf.Max(0f, m_shakeFrequency);
        m_fragmentLifetime = Mathf.Max(0.1f, m_fragmentLifetime);
    }
}
