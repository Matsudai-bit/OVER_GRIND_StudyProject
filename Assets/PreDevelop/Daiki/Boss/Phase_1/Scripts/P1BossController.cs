using System;
using System.Collections;

using TMPro;
using Unity.VisualScripting;
using UnityEngine;


public class P1BossController 
    : MonoBehaviour 
    , IStateStatusProvider
    , IAttackDamageReceiver
{


    

    [DebugParameterField, Header("状態標示用テキスト")]
    public TextMeshPro stateText;

    [SerializeField]
    private  AnimationEventReceiver m_animationEventReceiver;

    public SerializeDictionary<P1AttackType, AttackHitbox> attackHitBox;

    [SerializeField]
    private P1LegController m_legController;

    [DebugParameterField]
    private StateExecutionStatus m_currentStatus;
    private Animator m_animator;
    private StateMachine<P1BossController> m_stateMachine ;
    private Rigidbody m_rb;

    [SerializeField]
    public GameObject P2_boss;
    [SerializeField]
    public GameObject P1_boss;
    [SerializeField]
    public GameObject GroundCollider;

    private const string MOVE_SPEED_PARAMETER_NAME = "MoveSpeed";
    private const string ATTACK_PARAMETER_NAME = "Attack";

    private static readonly int MOVE_SPEED_PARAMETER_ID =
        Animator.StringToHash(MOVE_SPEED_PARAMETER_NAME);

    private static readonly int ATTACK_PARAMETER_ID =
        Animator.StringToHash(ATTACK_PARAMETER_NAME);

    public AnimationEventReceiver AnimationEventReceiver { get { return m_animationEventReceiver; } }

    public StateMachine<P1BossController> StateMachine { get { return m_stateMachine; } }
    public Animator Animator { get { return m_animator; } }

    public P1LegController LegController => m_legController;

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

    public void ReceiveAttackDamage(int damage)
    {
        Debug.Log("攻撃をくらった！");
    }

    /// <summary>
    /// 足が壊されたかどうか
    /// </summary>
    public bool AreBothLegsBroken=> m_legController.AreBothLegsBroken();
    
}
