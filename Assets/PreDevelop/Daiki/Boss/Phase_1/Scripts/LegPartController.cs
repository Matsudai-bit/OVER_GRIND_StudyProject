using System;
using UnityEngine;

[Serializable]
public enum P1BossLegPartState
{
    NORMAL,     // í èÌ
    BROKEN,     // ëπèù
    DESTROYED   // îjâÛ
}

public class LegPartController 
    : MonoBehaviour 
    , IAttackDamageReceiver
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

    public Action<LegPartController> OnBroken; 

    public P1BossLegPartState CurrentState => m_state;

    public GameObject LegModelObject => m_legObject;

    void Awake()
    {
        m_renderer = m_legObject.GetComponent<SkinnedMeshRenderer>();
        m_initialMaterial = (m_renderer.sharedMaterial);

 
    }

    void Start()
    {
        m_state = P1BossLegPartState.NORMAL;
    }
    public void ReceiveAttackDamage(int damage)
    {
        OnDamage();
    }

    private void OnDamage()
    {
        m_renderer.material = m_hitMaterial;
        Invoke("ResetMaterial", 0.9f);
        Invoke("Break", 1.2f);

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
