using UnityEngine;

/// <summary>
/// ミサイルの調整パラメータを保持します。
/// </summary>
[CreateAssetMenu(
    fileName = "MissileParameter",
    menuName = "Game/Parameters/Missile/Missile Parameter")]
public sealed class MissileParameterAsset : ScriptableObject
{
    // 移動パラメータ
    [SerializeField, Header("移動パラメータ")]
    private MissileMotorParameters m_motorParameters = new();

    // 操舵パラメータ
    [SerializeField, Header("操舵パラメータ")]
    private MissileSteeringParameters m_steeringParameters = new();

    /// <summary>
    /// 移動パラメータを取得します。
    /// </summary>
    public MissileMotorParameters MotorParameters =>
        m_motorParameters;

    /// <summary>
    /// 操舵パラメータを取得します。
    /// </summary>
    public MissileSteeringParameters SteeringParameters =>
        m_steeringParameters;
}
