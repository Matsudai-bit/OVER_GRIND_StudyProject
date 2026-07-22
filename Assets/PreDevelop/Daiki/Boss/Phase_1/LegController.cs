using UnityEngine;

public class LegController 
    : MonoBehaviour 
    , IAttackDamageReceiver
{
    [SerializeField]
    private GameObject m_legObject;

    [SerializeField]
    private Material m_hitMaterial;

    [SerializeField]
    private Material m_breakMaterial;

    private SkinnedMeshRenderer m_renderer;
    private Material m_initialMaterial;

    void Awake()
    {
        m_renderer = m_legObject.GetComponent<SkinnedMeshRenderer>();
        m_initialMaterial = (m_renderer.sharedMaterial);
    }
    public void ReceiveAttackDamage(int damage)
    {
        OnDamage();
    
    }

    private void OnDamage()
    {
        m_renderer.material = m_hitMaterial;
        Invoke("ResetMaterial", 0.9f);

        Invoke("Break", 1.0f);

    }

    private void ResetMaterial()
    {
        m_renderer.material = m_initialMaterial;
        
    }
    private void Break()
    {

        m_renderer.material = m_breakMaterial;
        m_legObject.SetActive(false);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
