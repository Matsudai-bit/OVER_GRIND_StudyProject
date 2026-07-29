using System;
using UnityEngine;

[Serializable]
public enum P1BossLegPartState
{
    NORMAL,     // í èÌ
    BROKEN,     // ëπèù
    DESTROYED   // îjâÛ
}

[RequireComponent(typeof(Health))]
public class LegPartController 
    : MonoBehaviour 
    , IDamageable
{

 

    [DebugParameterField]
    P1BossLegPartState m_state;


    [SerializeField]
    private GameObject m_legObject;

    [SerializeField]
    private Material m_hitMaterial;

    [SerializeField]
    private Material m_breakMaterial;

    private SkinnedMeshRenderer m_renderer;
    private Material m_initialMaterial;

    private Health m_health;

    public Action<LegPartController> OnBroken; 

    public P1BossLegPartState CurrentState => m_state;

    public GameObject LegModelObject => m_legObject;

    void Awake()
    {
        m_renderer = m_legObject.GetComponent<SkinnedMeshRenderer>();
        m_initialMaterial = (m_renderer.sharedMaterial);

        m_health = GetComponent<Health>();


    }

    void Start()
    {
        m_state = P1BossLegPartState.NORMAL;
    }
    public void TakeDamage(int damage)
    {
        OnDamage(damage);
    }

    private void OnDamage(int damage)
    {
        m_renderer.material = m_hitMaterial;
        m_health.TakeDamage(damage);

        if (m_health.IsDead)
        {
            Invoke("Break", 0.5f);
        }
        else
        {
            Invoke("ResetMaterial", 0.5f);

        }

    }

    private void ResetMaterial()
    {
        m_renderer.material = m_initialMaterial;
        
    }
    private void Break()
    {

        m_renderer.material = m_breakMaterial;
        m_state = P1BossLegPartState.BROKEN;


        OnBroken?.Invoke(this);
    }
 
    

    // Update is called once per frame
    void Update()
    {
        
    }
}
