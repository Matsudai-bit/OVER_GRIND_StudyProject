using UnityEngine;

public class GameDirector : MonoBehaviour
{
    [SerializeField]
    private GameFlowStateChangedEvent m_gameFlowStateChangedEven;

    private void Awake()
    {
        if (!m_gameFlowStateChangedEven)
        {
            Debug.LogError(typeof(GameFlowStateChangedEvent).Name+"Ç™ê›íËÇ≥ÇÍÇƒÇ¢Ç‹ÇπÇÒ");
        }
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
