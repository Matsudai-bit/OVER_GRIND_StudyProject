using UnityEngine;

public class GameDirector : MonoBehaviour
{
    [SerializeField]
    private GameFlowStateChangedEvent m_gameFlowStateChangedEvent;
    [SerializeField]
    GameFlowController m_gameFlowController;
    [SerializeField]
    GameplaySequenceController m_sequenceController;
    [SerializeField]
    GameplaySequenceAsset m_sequenceAsset;

    private void Awake()
    {
        if (!m_gameFlowStateChangedEvent)
        {
            Debug.LogError(typeof(GameFlowStateChangedEvent).Name+"‚ªÝ’è‚³‚ê‚Ä‚¢‚Ü‚¹‚ñ");
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_sequenceController.
        StartGameplay();

        m_gameFlowStateChangedEvent.RegisterListener((flowStateID) => 
        {
            if (flowStateID.CurrentState == GameFlowStateType.INTRO) { }
            {
                m_sequenceController.StartSequence(m_sequenceAsset);
            }
        });
    }

    void StartGameplay()
    {
        m_gameFlowController.ChangeState(GameFlowStateType.INTRO);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
