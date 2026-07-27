using UnityEngine;

/// <summary>
/// プレイヤーの攻撃コントローラ
/// </summary>
public class PlayerAttackController : MonoBehaviour
{
    [SerializeField]
    private PlayerAnimationEventReceiver m_animationEventReceiver;

    private void Awake()
    {
        SubscribeAnimationEvents();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void SubscribeAnimationEvents()
    {
        m_animationEventReceiver.AttackHitboxStarted +=
            HandleAttackHitboxStarted;

        m_animationEventReceiver.AttackHitboxEnded +=
            HandleAttackHitboxEnded;
        m_animationEventReceiver.AttackAnimationStarted +=
          HandleAttackAnimationStarted;
        m_animationEventReceiver.AttackAnimationFinished +=
            HandleAttackAnimationFinished;
    }

    private void HandleAttackHitboxStarted(int comboStage)
    {
        Debug.Log("通知：HandleAttackHitboxStarted," + comboStage);
    }
    private void HandleAttackHitboxEnded(int comboStage)
    {
        Debug.Log("通知：HandleAttackHitboxEnded," + comboStage);

    }

    private void HandleAttackAnimationStarted(int comboStage)
    {
        Debug.Log("通知：HandleAttackAnimationStarted," + comboStage);

    }

    private void HandleAttackAnimationFinished(int comboStage)
    {
        Debug.Log("通知：HandleAttackAnimationFinished," + comboStage);

    }
}
