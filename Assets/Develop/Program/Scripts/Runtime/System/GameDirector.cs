using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class GameDirector : MonoBehaviour
{
    [SerializeField]
    private ElapsedTimer m_gameProgressElapsedTimer;

    [SerializeField]
    private GameFlowStateChangedEvent m_gameFlowStateChangedEvent;
    [SerializeField]
    GameFlowController m_gameFlowController;
    [SerializeField]
    GameplaySequenceController m_sequenceController;
    [SerializeField]
    GameplaySequenceAsset m_sequenceAsset;
    [SerializeField]
    GameplayModeController m_modeController;
    [SerializeField]
    GameplaySequenceChangedEvent m_gameplaySequenceChangedEvent;

    [SerializeField]
    CollisionSensor m_startSensor;
    [SerializeField]
    Transform m_startTransform;
    [SerializeField]
    Rigidbody m_playerTransform;

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
                
                
            }
        });
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_startSensor.CollisionDetected += StartGame;
        start = false;
        m_modeController.ActivateGameplay();
   
    }
    void Update()
    {
        //if (!start)
        //{
        //StartGameplay();
        //    start = true;
        //}

    }

    void StartGame(Vector3 position)
    {

        m_startSensor.CollisionDetected -= StartGame;
        m_startSensor.enabled = false;

        Debug.Log("シーケンス依頼");
        m_gameplaySequenceChangedEvent.RegisterListener((data) =>
        {
            if (data.EventType == GameplaySequenceEventType.STARTED)
            {
                StartCoroutine(DelayAction(1.0f, () => {

                    m_playerTransform.position = m_startTransform.position;
                    m_playerTransform.rotation = m_startTransform.rotation;
                }));
            }
            if (data.EventType == GameplaySequenceEventType.FINISHED && data.Sequence == m_sequenceAsset)
            {
                m_gameFlowController.EnterPlaying();
                m_gameProgressElapsedTimer.StartTimer();
            }
        });
        m_sequenceController.StartSequence(m_sequenceAsset);
         
    }
 

    IEnumerator DelayAction(float delaySeconds, Action callback)
    {
        yield return new WaitForSeconds(delaySeconds);
        callback?.Invoke();
    }

   


}
