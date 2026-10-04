using UnityEngine;

/// <summary>空中攻撃の命中位置を対象に対して保持し、徐々に下へ滑らせます。</summary>
public sealed class PlayerAirAttackAttachment
{
    private readonly PlayerStateMachineComponent m_owner;
    private Transform m_target;
    private Vector3 m_localContactPosition;
    private float m_elapsedTime;
    private float m_fallSpeed;
    private float m_contactLostTime;

    /// <summary>吸着移動を行うプレイヤーを保持します。</summary>
    public PlayerAirAttackAttachment(PlayerStateMachineComponent owner)
    {
        m_owner = owner;
    }

    /// <summary>有効な対象への吸着が継続しているかを取得します。</summary>
    public bool IsAttached => m_target != null && m_target.gameObject.activeInHierarchy;

    /// <summary>最初に命中した対象上に、プレイヤーの相対位置を保存します。</summary>
    /// <param name="target">実際にダメージを与えた対象。</param>
    public void AttachToTarget(IDamageable target)
    {
        if (IsAttached || !(target is Component component) || component == null ||
            !component.gameObject.activeInHierarchy) return;
        m_target = component.transform;
        m_localContactPosition = m_target.InverseTransformPoint(m_owner.Motor.Position);
        m_elapsedTime = 0.0f;
        m_fallSpeed = 0.0f;
        m_contactLostTime = 0.0f;
    }

    /// <summary>対象の移動に追従し、停止猶予後に徐々に下降します。</summary>
    /// <param name="deltaTime">物理更新の間隔。</param>
    /// <returns>true：吸着移動中。false：対象消失・接触切れ・過大な移動で解除。</returns>
    public bool UpdateAttachment(float deltaTime)
    {
        if (!IsAttached || deltaTime <= 0.0f) { Detach(); return false; }
        PlayerAttackController attack = m_owner.AttackController;
        m_contactLostTime = attack.IsHittingAnyTarget() ? 0.0f : m_contactLostTime + deltaTime;
        if (m_contactLostTime > attack.AirAttachContactGrace) { Detach(); return false; }

        m_elapsedTime += deltaTime;
        if (m_elapsedTime > attack.AirAttachHoldDuration)
            m_fallSpeed = Mathf.MoveTowards(m_fallSpeed, attack.AirHitFallSpeed,
                attack.AirAttachFallAcceleration * deltaTime);

        Vector3 position = m_target.TransformPoint(m_localContactPosition);
        position += Vector3.down * (m_fallSpeed * deltaTime);
        // テレポートする対象に引きずられたり、離れた場所から吸い戻されたりしません。
        if (Vector3.Distance(position, m_owner.Motor.Position) > attack.AirAttachMaxDistance)
        {
            Detach();
            return false;
        }
        m_localContactPosition = m_target.InverseTransformPoint(position);
        m_owner.Motor.MoveToAirAttackContact(position, deltaTime);
        return true;
    }

    /// <summary>対象参照と吸着中の時間を解除します。</summary>
    public void Detach()
    {
        m_target = null;
        m_elapsedTime = 0.0f;
        m_fallSpeed = 0.0f;
        m_contactLostTime = 0.0f;
    }
}
