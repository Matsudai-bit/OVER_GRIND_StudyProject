using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerObstacleParamater",
    menuName = "Game/Parameters/Player/Obstacle Parameter")]
public class PlayerObstacleParameterAsset : ScriptableObject
{
    [SerializeField, Header("障害物検知")]
    [Min(0.0f), Tooltip("障害物への食い込みを防ぐための手前バッファ距離")]
    private float m_obstacleSkinWidth = 0.05f;

    [SerializeField]
    [Min(0.0f)]
    private float m_obstacleDecelerationPerSecond = 20.0f;

    /// <summary>
    /// 衝突判定パラメータを生成します。
    /// </summary>
    /// <returns>衝突判定パラメータ。</returns>
    public PlayerObstacleParameter CreateObstacleParameter()
    {
        return new PlayerObstacleParameter(
            m_obstacleSkinWidth,
            m_obstacleDecelerationPerSecond
        );
    }
}
