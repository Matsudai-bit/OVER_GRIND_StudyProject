using System;
using UnityEngine;

public class LegPartController 
    : MonoBehaviour 
    , IAttackDamageReceiver
{
    [SerializeField]
    private bool m_isBreaking;

    [SerializeField]
    private GameObject m_legObject;

    [SerializeField]
    private Material m_hitMaterial;

    [SerializeField]
    private Material m_breakMaterial;

    private SkinnedMeshRenderer m_renderer;
    private Material m_initialMaterial;

    public Action<LegPartController> OnBreak; 

    public  bool IsBreaking => m_isBreaking;

    public GameObject LegModelObject => m_legObject;

    void Awake()
    {
        m_renderer = m_legObject.GetComponent<SkinnedMeshRenderer>();
        m_initialMaterial = (m_renderer.sharedMaterial);

 
    }

    void Start()
    {
        m_isBreaking = false;
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
        m_isBreaking = true;

        OnBreak?.Invoke(this);
    }
 
    

    // Update is called once per frame
    void Update()
    {
        
    }
}
