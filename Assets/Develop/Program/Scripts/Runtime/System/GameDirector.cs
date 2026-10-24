using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    Health m_playerHealth;

    [Header("遷移関連")]
    [SerializeField, Header("次のステージボタンを押した時の遷移先")]
    string nextStageSceneName;
    [SerializeField, Header("ステージ選択画面シーン名")]
    string stageSelectSceneName;

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
                m_gameplaySequenceChangedEvent.RegisterListener((data) => {
                    if (data.EventType == GameplaySequenceEventType.FINISHED && data.Sequence == m_sequenceAsset) 
                    { 
                        m_gameFlowController.EnterPlaying(); 
                        m_modeController.ActivateGameplay();
                        m_gameProgressElapsedTimer.StartTimer();
                    } 
                });
                m_sequenceController.StartSequence(m_sequenceAsset);
                
            }
        });

        m_playerHealth.HealthChanged += OnChangePlayerHealth;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        start = false;
        Cursor.visible = false;

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

    public void LoadNextStage()
    {
        SceneManager.LoadScene(nextStageSceneName);
    }
    public void LoadStageSelect()
    {
        SceneManager.LoadScene(stageSelectSceneName);
    }
    public void LoadRestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }


    void OnChangePlayerHealth(int hp, int hp1)
    {
        if (0 >= hp)
        {
            m_gameFlowController.ChangeState(GameFlowStateType.GAME_OVER);
        }
    }
}
