using UnityEngine;

/// <summary>レール上のチャージ量と減速を管理します。</summary>
public sealed class PlayerRailGrindCharge
{
    private readonly PlayerRailGrindChargeParameterAsset m_parameters;
    private readonly float m_startSpeed;
    private float m_elapsedTime;

    /// <summary>チャージ開始時のレール速度を保存します。</summary>
    /// <param name="parameters">チャージ設定。</param>
    /// <param name="startSpeed">開始時のレール速度。</param>
    public PlayerRailGrindCharge(PlayerRailGrindChargeParameterAsset parameters, float startSpeed)
    {
        m_parameters = parameters;
        m_startSpeed = Mathf.Max(0.0f, startSpeed);
    }

    /// <summary>現在のチャージ割合です。</summary>
    public float ChargeRate => Mathf.Clamp01(m_elapsedTime / m_parameters.ChargeTime);

    /// <summary>チャージ開始前のレール速度です。</summary>
    public float StartSpeed => m_startSpeed;

    /// <summary>チャージを進めてレール走行を減速します。</summary>
    /// <param name="controller">レール走行制御。</param>
    /// <param name="deltaTime">物理更新時間。</param>
    public void UpdateCharge(SplineGrindController controller, float deltaTime)
    {
        m_elapsedTime += deltaTime;
        float targetSpeed = m_startSpeed * m_parameters.ChargeSpeedRate;
        controller.SetGrindSpeed(Mathf.MoveTowards(
            controller.CurrentSpeed, targetSpeed, m_parameters.Deceleration * deltaTime));
    }
}
