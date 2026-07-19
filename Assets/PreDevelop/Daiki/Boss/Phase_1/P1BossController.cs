using System;
using System.Collections;
using UnityEngine;


public class P1BossController : MonoBehaviour ,IStateStatusProvider
{
    private StateExecutionStatus m_currentStatus;
    private Animator m_animator;
    private StateMachine<P1BossController> m_stateMachine ;
    private Rigidbody m_rb;

    private const string MOVE_SPEED_PARAMETER_NAME = "MoveSpeed";
    private const string ATTACK_PARAMETER_NAME = "Attack";

    private static readonly int MOVE_SPEED_PARAMETER_ID =
        Animator.StringToHash(MOVE_SPEED_PARAMETER_NAME);

    private static readonly int ATTACK_PARAMETER_ID =
        Animator.StringToHash(ATTACK_PARAMETER_NAME);

    public StateMachine<P1BossController> StateMachine { get { return m_stateMachine; } }
    public Animator Animator { get { return m_animator; } }

    public Rigidbody Rigidbody { get { return m_rb; } }
    enum P1BossStateID
    {
        IDLE,
        WALK,
        ATTACK
    }

    private void Awake()
    {
        // ステートマシーンの初期化
        m_stateMachine = new(this);
        m_animator = GetComponent<Animator>();
        m_rb = GetComponent<Rigidbody>();
    }

   

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_currentStatus = StateExecutionStatus.SUCCEEDED;
    }

    private void FixedUpdate()
    {
        m_stateMachine.FixedUpdate();
    }

    // Update is called once per frame
    void Update()
    {
        m_stateMachine.Update(Time.deltaTime);
    }

    public StateExecutionStatus GetStateExecutionStatus()
    {
        return m_currentStatus;
    }
    public void SetStateExecutionStatus(StateExecutionStatus status)
    {
        m_currentStatus = status;
    }

    public void StartDelayCoroutine(float seconds, Action action)
    {
        StartCoroutine(DelayCoroutine(seconds, action));
    }

    // 一定時間後に処理を呼び出すコルーチン
    private IEnumerator DelayCoroutine(float seconds, Action action)
    {
        yield return new WaitForSeconds(seconds);
        action?.Invoke();
    }
}
