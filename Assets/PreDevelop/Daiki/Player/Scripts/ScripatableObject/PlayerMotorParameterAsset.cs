using UnityEngine;

/// <summary>
/// プレイヤーの物理移動パラメータを設定します。
/// </summary>
[CreateAssetMenu(
    fileName = "PlayerMotorParameter",
    menuName = "Game/Parameters/Player/Player Motor Parameter")]
public sealed class PlayerMotorParameterAsset : ScriptableObject
{
    // 時間パラメータの最小値
    private const float MIN_TIME = 0.01f;

    // ジャンプ力の最小値
    private const float MIN_JUMP_POWER = 1.0f;

    // 通常移動の最高速度
    [SerializeField, Header("通常移動")]
    [Tooltip("プレイヤーが通常移動するときの最高速度です。")]
    [Min(0.0f)]
    private float m_maxMoveSpeed = 6.0f;

    // 最高速度に到達するまでの時間
    [SerializeField]
    [Tooltip("停止状態から最高速度に到達するまでの時間です。")]
    [Min(MIN_TIME)]
    private float m_timeToMaxSpeed = 2.0f;

    // 停止するまでの時間
    [SerializeField]
    [Tooltip("移動状態から停止するまでの時間です。")]
    [Min(MIN_TIME)]
    private float m_timeToStop = 0.25f;

    // ジャンプ力
    [SerializeField, Header("ジャンプ")]
    [Tooltip("ジャンプ中に使用する上方向の移動力です。")]
    [Min(MIN_JUMP_POWER)]
    private float m_jumpPower = 6.0f;

    // 1秒間に回転できる最大角度
    [SerializeField, Header("回転")]
    [Tooltip("プレイヤーが1秒間に回転できる最大角度です。")]
    [Min(0.0f)]
    private float m_rotationSpeed = 540.0f;

    /// <summary>
    /// 実行時に使用する物理移動パラメータを生成します。
    /// </summary>
    /// <returns>プレイヤーの物理移動パラメータ。</returns>
    public PlayerMotorParameters CreateParameters()
    {
        return new PlayerMotorParameters(
            m_maxMoveSpeed,
            m_jumpPower,
            m_timeToMaxSpeed,
            m_timeToStop,
            m_rotationSpeed);
    }
}