using UnityEngine;

/// <summary>Phase 2ボス本体の上に立ったPlayerを外側へ滑らせます。</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody), typeof(PlayerMonitor))]
[DefaultExecutionOrder(100)]
public sealed class PlayerBossTopSlideController : MonoBehaviour
{
    [SerializeField, Min(0.0f)]
    [Tooltip("ボスの上で外側へ滑る速度です。")]
    private float m_slideSpeed = 7.0f;

    [SerializeField, Min(0.0f)]
    [Tooltip("外側への滑り速度に到達するまでの加速度です。")]
    private float m_slideAcceleration = 18.0f;

    private Rigidbody m_body;
    private PlayerMonitor m_monitor;
    private PlayerStateMachineComponent m_stateMachine;
    private SplineGrindController m_grindController;

    /// <summary>Playerの接地状態と物理移動に必要な参照を取得します。</summary>
    private void Awake()
    {
        m_body = GetComponent<Rigidbody>();
        m_monitor = GetComponent<PlayerMonitor>();
        m_stateMachine = GetComponent<PlayerStateMachineComponent>();
        m_grindController = GetComponent<SplineGrindController>();
    }

    /// <summary>レール以外でPhase 2ボス上に接地したときだけ外側へ滑らせます。</summary>
    private void FixedUpdate()
    {
        if (m_body == null || m_body.isKinematic || m_monitor == null ||
            !m_monitor.IsGrounded || m_monitor.GroundCollider == null ||
            (m_grindController != null && m_grindController.IsGrinding) ||
            (m_stateMachine != null &&
             (m_stateMachine.IsCurrentState<PlayerGrindingState>() ||
              m_stateMachine.IsCurrentState<PlayerRailJumpingState>())))
        {
            return;
        }

        S1P2BossReferences boss =
            m_monitor.GroundCollider.GetComponentInParent<S1P2BossReferences>();
        if (boss == null) return;

        Vector3 outward = Vector3.ProjectOnPlane(
            m_body.position - boss.transform.position, Vector3.up);
        if (outward.sqrMagnitude < 0.0001f)
        {
            outward = Vector3.ProjectOnPlane(boss.transform.forward, Vector3.up);
        }
        if (outward.sqrMagnitude < 0.0001f) return;

        Vector3 velocity = m_body.linearVelocity;
        Vector3 horizontal = Vector3.ProjectOnPlane(velocity, Vector3.up);
        Vector3 target = outward.normalized * m_slideSpeed;
        horizontal = Vector3.MoveTowards(
            horizontal, target, m_slideAcceleration * Time.fixedDeltaTime);
        m_body.linearVelocity = horizontal + Vector3.up * velocity.y;
    }
}
