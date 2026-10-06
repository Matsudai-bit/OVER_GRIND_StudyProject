using UnityEngine;

/// <summary>
/// プレイヤーの衝突判定に関するパラメータを保持します。
/// </summary>
public readonly struct PlayerObstacleParameter
{
    /// <summary>
    /// 接地時パラメータを生成します。
    /// </summary>
    /// <param name="obstacleSkinWidth">             障害物への食い込みを防ぐための手前バッファ距離 </param>
    /// <param name="obstacleDecelerationPerSecond"> 障害物に接触している間、1秒あたり減速する速度  </param>
    public PlayerObstacleParameter(
        float obstacleSkinWidth,
        float obstacleDecelerationPerSecond)
    {
        m_obstacleSkinWidth = obstacleSkinWidth;
        m_obstacleDecelerationPerSecond = obstacleDecelerationPerSecond;
    }

    /// <summary>
    /// 障害物への食い込みを防ぐための手前バッファ距離を取得します。
    /// </summary>
    public float m_obstacleSkinWidth { get; }

    /// <summary>
    /// 障害物に接触している間、1秒あたり減速する速度を取得します。
    /// </summary>
    public float m_obstacleDecelerationPerSecond {  get; }
}
