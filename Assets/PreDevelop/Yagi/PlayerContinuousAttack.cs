using UnityEngine;

/// <summary>地上・空中で共通の連続攻撃、速度消費、命中時の滞空を管理します。</summary>
public sealed class PlayerContinuousAttack
{
    private const float SPEED_DECAY_PER_SECOND = 8.0f;
    private const float SPEED_EPSILON = 0.01f;
    private readonly PlayerStateMachineComponent m_owner;
    private readonly bool m_isAirAttack;
    private float m_initialSpeed;
    private float m_remainingSpeed;
    private float m_hitTimer;
    private float m_airHitStopTimer;
    private Vector3 m_direction;
    private PlayerAirAttackAttachment m_airAttachment;

    /// <summary>攻撃の所有者と空中攻撃かどうかを保持します。</summary>
    public PlayerContinuousAttack(PlayerStateMachineComponent owner, bool isAirAttack)
    {
        m_owner = owner;
        m_isAirAttack = isAirAttack;
    }

    /// <summary>速度と向きを保存し、攻撃判定とアニメーションを開始します。</summary>
    public void StartAttack()
    {
        m_remainingSpeed = m_owner.Motor.HorizontalSpeed;
        m_initialSpeed = Mathf.Max(m_remainingSpeed, SPEED_EPSILON);
        // 空中ではジャンプの慣性方向を引き継ぎ、元の体の向きへ速度を曲げません。
        m_direction = m_isAirAttack
            ? m_owner.Motor.HorizontalDirection
            : m_owner.Motor.FacingDirection;
        if (m_isAirAttack) m_owner.Motor.AlignFacingToDirection(m_direction);
        m_hitTimer = 0.0f;
        m_airHitStopTimer = 0.0f;
        if (m_isAirAttack)
        {
            m_airAttachment = new PlayerAirAttackAttachment(m_owner);
            m_owner.AttackController.ContinuousAttackHit += m_airAttachment.AttachToTarget;
        }
        m_owner.AttackController.EnableContinuousAttackHitboxes();
        m_owner.AnimationPresenter.PlayAttackAnimation();
        m_owner.SetSpeedDisplayOverride(m_remainingSpeed);
    }

    /// <summary>速度を消費しながら移動と連続ヒットを更新します。</summary>
    /// <param name="deltaTime">物理更新の間隔。</param>
    /// <returns>true：攻撃継続。false：入力解除または速度切れで終了。</returns>
    public bool UpdateAttack(float deltaTime)
    {
        if (!m_owner.InputReader.IsAttackHeld) return false;
        m_remainingSpeed = Mathf.Max(0.0f, m_remainingSpeed - SPEED_DECAY_PER_SECOND * deltaTime);
        if (m_remainingSpeed <= SPEED_EPSILON) return false;

        if (!m_isAirAttack) UpdateMovement(deltaTime);
        m_owner.SetSpeedDisplayOverride(m_remainingSpeed);
        // 地上攻撃はブースト残量に応じた頻度を引き継ぎ、空中攻撃は従来の頻度を使用します。
        float baseHitsPerSecond = m_isAirAttack
            ? m_owner.AttackController.BaseHitsPerSecond
            : m_owner.AttackController.GetHitsPerSecond(m_owner.SuspendedBoostGaugeRate);
        float hitsPerSecond = baseHitsPerSecond *
            Mathf.Clamp01(m_remainingSpeed / m_initialSpeed);
        float interval = 1.0f / Mathf.Max(hitsPerSecond, 0.0001f);
        m_hitTimer += deltaTime;
        while (m_hitTimer >= interval)
        {
            m_hitTimer -= interval;
            if (m_owner.AttackController.ApplyContinuousHitTick() && m_isAirAttack)
                m_airHitStopTimer = m_owner.AttackController.AirHitStopDuration;
        }

        // 衝突で実速度がゼロになっても、残り速度を使って接触位置を維持します。
        // 吸着中は通常の移動・減速・押し出しを行いません。
        if (m_airAttachment != null && m_airAttachment.UpdateAttachment(deltaTime)) return true;
        if (m_isAirAttack) UpdateMovement(deltaTime);

        // 空振り中も緩やかに落下し、命中時はさらに滞空時間を延ばします。
        if (m_isAirAttack)
        {
            float fallSpeed = m_owner.AttackController.AirAttackFallSpeed;
            if (m_airHitStopTimer > 0.0f)
                fallSpeed = Mathf.Min(fallSpeed, m_owner.AttackController.AirHitFallSpeed);
            m_owner.Motor.LimitAttackFallSpeed(fallSpeed);
            m_airHitStopTimer = Mathf.Max(0.0f, m_airHitStopTimer - deltaTime);
        }

        if (!m_isAirAttack &&
            m_owner.AttackController.TryGetMaxPenetration(out Vector3 direction, out float distance))
            m_owner.Motor.ResolvePenetration(direction, distance);
        return true;
    }

    /// <summary>地上攻撃と同じ入力減速・命中減速を適用します。</summary>
    private void UpdateMovement(float deltaTime)
    {
        if (!m_owner.InputReader.HasMoveInput)
        {
            PlayerMoveParameters normal = m_owner.MovementParameterAsset.CreateMoveParameters();
            float multiplier = Mathf.Max(m_owner.MovementParameterAsset.AttackDecelerationMultiplier, 1.0f);
            var attack = new PlayerMoveParameters(normal.MaxMoveSpeed, normal.TimeToMaxSpeed,
                normal.TimeToStop * multiplier, normal.RotationSpeed);
            m_owner.Motor.Decelerate(attack, deltaTime);
            return;
        }

        float speed = m_remainingSpeed;
        if (m_owner.AttackController.IsHittingAnyTarget())
            speed *= m_owner.MovementParameterAsset.AttackHitMovementSpeedMultiplier;
        m_owner.Motor.MoveAtFixedWorldDirection(m_direction, speed, 0.0f, deltaTime,
            applyObstacleAvoidance: false);
    }

    /// <summary>攻撃判定とアニメーション、速度表示の上書きを終了します。</summary>
    public void StopAttack()
    {
        if (m_airAttachment != null)
        {
            m_owner.AttackController.ContinuousAttackHit -= m_airAttachment.AttachToTarget;
            m_airAttachment.Detach();
            m_airAttachment = null;
        }
        m_owner.AnimationPresenter.StopAttackAnimation();
        m_owner.AttackController.DisableAttackHitboxes();
        m_owner.ClearSpeedDisplayOverride();
        m_airHitStopTimer = 0.0f;
    }
}
