/// <summary>
/// プレイヤーの物理移動に使用するパラメータを保持します。
/// </summary>
public readonly struct PlayerMotorParameters
{
    /// <summary>
    /// 通常移動の最高速度を取得します。
    /// </summary>
    public float MaxMoveSpeed { get; }

    /// <summary>
    /// ジャンプ力を取得します。
    /// </summary>
    public float JumpPower { get; }

    /// <summary>
    /// 最高速度に到達するまでの時間を取得します。
    /// </summary>
    public float TimeToMaxSpeed { get; }

    /// <summary>
    /// 停止するまでの時間を取得します。
    /// </summary>
    public float TimeToStop { get; }

    /// <summary>
    /// 1秒間に回転できる最大角度を取得します。
    /// </summary>
    public float RotationSpeed { get; }

    /// <summary>
    /// プレイヤーの物理移動パラメータを生成します。
    /// </summary>
    /// <param name="maxMoveSpeed">通常移動の最高速度。</param>
    /// <param name="jumpPower">ジャンプ力。</param>
    /// <param name="timeToMaxSpeed">最高速度に到達するまでの時間。</param>
    /// <param name="timeToStop">停止するまでの時間。</param>
    /// <param name="rotationSpeed">1秒間に回転できる最大角度。</param>
    public PlayerMotorParameters(
        float maxMoveSpeed,
        float jumpPower,
        float timeToMaxSpeed,
        float timeToStop,
        float rotationSpeed)
    {
        MaxMoveSpeed = maxMoveSpeed;
        JumpPower = jumpPower;
        TimeToMaxSpeed = timeToMaxSpeed;
        TimeToStop = timeToStop;
        RotationSpeed = rotationSpeed;
    }
}