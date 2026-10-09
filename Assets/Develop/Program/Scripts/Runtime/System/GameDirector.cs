using Unity.VisualScripting;
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

    bool start = false;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        if (!m_gameFlowStateChangedEvent)
        {
            Debug.LogError(typeof(GameFlowStateChangedEvent).Name+"が設定されていません");
        }

        m_gameFlowStateChangedEvent.RegisterListener((flowStateID) =>
        {
            if (flowStateID.CurrentState == GameFlowStateType.INTRO) 
            {
                Debug.Log("シーケンス依頼");
                m_sequenceController.StartSequence(m_sequenceAsset);
            }
        });
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        start = false;
   
    }
    void Update()
    {
        //if (!start)
        //{
        //StartGameplay();
        //    start = true;
        //}

    }

    void StartGameplay()
    {

        m_gameFlowController.ChangeState(GameFlowStateType.INTRO);
    }


}
