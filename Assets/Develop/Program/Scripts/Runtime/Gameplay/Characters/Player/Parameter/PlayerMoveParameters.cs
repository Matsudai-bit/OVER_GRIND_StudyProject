/// <summary>
/// プレイヤーの水平移動に使用するパラメータを保持します。
/// </summary>
public readonly struct PlayerMoveParameters
{
    /// <summary>
    /// 移動パラメータを生成します。
    /// </summary>
    /// <param name="maxMoveSpeed">最高移動速度。</param>
    /// <param name="timeToMaxSpeed">最高速度までの到達時間。</param>
    /// <param name="timeToStop">停止までの時間。</param>
    /// <param name="rotationSpeed">1秒間の最大回転角度。</param>
    public PlayerMoveParameters(
        float maxMoveSpeed,
        float timeToMaxSpeed,
        float timeToStop,
        float rotationSpeed)
    {
        MaxMoveSpeed = maxMoveSpeed;
        TimeToMaxSpeed = timeToMaxSpeed;
        TimeToStop = timeToStop;
        RotationSpeed = rotationSpeed;
    }

    /// <summary>
    /// 最高移動速度を取得します。
    /// </summary>
    public float MaxMoveSpeed { get; }

    /// <summary>
    /// 最高速度までの到達時間を取得します。
    /// </summary>
    public float TimeToMaxSpeed { get; }

    /// <summary>
    /// 停止までの時間を取得します。
    /// </summary>
    public float TimeToStop { get; }

    /// <summary>
    /// 1秒間の最大回転角度を取得します。
    /// </summary>
    public float RotationSpeed { get; }
}