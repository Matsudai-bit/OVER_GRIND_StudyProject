using UnityEngine;

/// <summary>チャージ終了時に確定した方向へブーストダッシュを実行します。</summary>
public sealed class PlayerBoostDash
{
    private const float DIRECTION_EPSILON = 0.0001f;
    private const float MIN_ACCELERATION_TIME = 0.01f;

    private readonly PlayerStateMachineComponent m_owner;
    private readonly PlayerMoveParameters m_parameters;
    private readonly Vector3 m_direction;
    private float m_currentSpeed;
    private float m_elapsedTime;

    /// <summary>ダッシュの移動設定と開始方向を保存します。</summary>
    /// <param name="owner">移動するプレイヤー。</param>
    public PlayerBoostDash(PlayerStateMachineComponent owner)
    {
        m_owner = owner;
        m_parameters = owner.VBoostMovementParameterAsset.CreateInitialBoostParameters();
        m_currentSpeed = Mathf.Min(owner.Motor.HorizontalSpeed, m_parameters.MaxMoveSpeed);

        Vector3 direction = owner.BoostDashDirection;
        direction.y = 0.0f;
        if (direction.sqrMagnitude <= DIRECTION_EPSILON)
        {
            direction = owner.transform.forward;
            direction.y = 0.0f;
        }

        m_direction = direction.normalized;
    }

    /// <summary>固定方向へ移動し、終了時にプレイヤーの正面を揃えます。</summary>
    /// <param name="deltaTime">物理更新時間。</param>
    /// <returns>trueはダッシュ終了、falseは継続を示します。</returns>
    public bool UpdateDash(float deltaTime)
    {
        m_owner.InputReader.DiscardVBoostPendingInput();

        m_currentSpeed = Mathf.MoveTowards(
            m_currentSpeed,
            m_parameters.MaxMoveSpeed,
            m_parameters.MaxMoveSpeed /
            Mathf.Max(MIN_ACCELERATION_TIME, m_parameters.TimeToMaxSpeed) * deltaTime);

        m_owner.Motor.MoveAtFixedWorldDirection(
            m_direction,
            m_currentSpeed,
            m_parameters.RotationSpeed,
            deltaTime);

        m_elapsedTime += deltaTime;
        if (m_elapsedTime < m_owner.VBoostMovementParameterAsset.InitialBoostDuration)
            return false;

        if (m_direction.sqrMagnitude > DIRECTION_EPSILON)
            m_owner.transform.forward = m_direction;

        return true;
    }
}
