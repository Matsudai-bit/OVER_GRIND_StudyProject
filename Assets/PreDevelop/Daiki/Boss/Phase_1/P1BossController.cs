using Unity.Behavior;
using UnityEngine;


public class P1BossController : MonoBehaviour
{
    private Animator m_animator;
    private StateMachine<P1BossController> m_stateMachine ;
    private  BehaviorGraphAgent m_agent;
    private Rigidbody m_rb;

    private const string MOVE_SPEED_PARAMETER_NAME = "MoveSpeed";
    private const string ATTACK_PARAMETER_NAME = "Attack";

    private static readonly int MOVE_SPEED_PARAMETER_ID =
        Animator.StringToHash(MOVE_SPEED_PARAMETER_NAME);

    private static readonly int ATTACK_PARAMETER_ID =
        Animator.StringToHash(ATTACK_PARAMETER_NAME);

    public BehaviorGraphAgent Agent { get { return m_agent; } }
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
        m_agent = GetComponent<BehaviorGraphAgent>();
        m_animator = GetComponent<Animator>();
        m_rb = GetComponent<Rigidbody>();
    }

   

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
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
}
