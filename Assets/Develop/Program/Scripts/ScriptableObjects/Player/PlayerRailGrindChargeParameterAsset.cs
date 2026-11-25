using UnityEngine;

/// <summary>レール上のチャージと解放時の速度を設定します。</summary>
[CreateAssetMenu(fileName = "PlayerRailGrindChargeParameter", menuName = "Game/Parameters/Player/Rail Grind Charge")]
public sealed class PlayerRailGrindChargeParameterAsset : ScriptableObject
{
    [SerializeField, Range(0.0f, 1.0f)] private float m_chargeSpeedRate = 0.5f;
    [SerializeField, Min(0.01f)] private float m_chargeTime = 2.0f;
    [SerializeField, Min(0.0f)] private float m_deceleration = 30.0f;
    [SerializeField, Min(0.0f)] private float m_dashGrindSpeed = 25.0f;
    [SerializeField, Min(0.01f)] private float m_dashDuration = 2.0f;

    /// <summary>チャージ中に維持する開始時速度の割合です。</summary>
    public float ChargeSpeedRate => Mathf.Clamp01(m_chargeSpeedRate);

    /// <summary>満充電までの秒数です。</summary>
    public float ChargeTime => Mathf.Max(0.01f, m_chargeTime);

    /// <summary>チャージ開始時の減速度です。</summary>
    public float Deceleration => Mathf.Max(0.0f, m_deceleration);

    /// <summary>レール上で解放した直後の速度です。</summary>
    public float DashGrindSpeed => Mathf.Max(0.0f, m_dashGrindSpeed);

    /// <summary>満充電時にレール上のダッシュを維持する秒数です。</summary>
    public float DashDuration => Mathf.Max(0.01f, m_dashDuration);
}
