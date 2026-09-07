using UnityEngine;

/// <summary>
/// プレイヤーの通常移動パラメータを設定します。
/// </summary>
[CreateAssetMenu(
    fileName = "PlayerMovementParameter",
    menuName = "Game/Parameters/Player/Movement Parameter")]
public sealed class PlayerMovementParameterAsset : ScriptableObject
{
    // 時間パラメータの最小値
    private const float MIN_TIME = 0.01f;

    // ジャンプ力の最小値
    private const float MIN_JUMP_POWER = 0.01f;

    // 通常移動の最高速度
    [SerializeField, Header("通常移動")]
    [Tooltip("通常移動時の最高速度です。")]
    [Min(0.0f)]
    private float m_maxMoveSpeed = 6.0f;

    // 最高速度までの到達時間
    [SerializeField]
    [Tooltip("通常移動で最高速度に到達するまでの時間です。")]
    [Min(MIN_TIME)]
    private float m_timeToMaxSpeed = 2.0f;

    // 停止までの時間
    [SerializeField]
    [Tooltip("通常移動から停止するまでの時間です。")]
    [Min(MIN_TIME)]
    private float m_timeToStop = 0.25f;

    // 通常移動中の回転速度
    [SerializeField]
    [Tooltip("通常移動中の1秒間の最大回転角度です。")]
    [Min(0.0f)]
    private float m_rotationSpeed = 540.0f;

    // ジャンプ力
    [SerializeField, Header("ジャンプ")]
    [Tooltip("ジャンプ中に使用する上方向の移動力です。")]
    [Min(MIN_JUMP_POWER)]
    private float m_jumpPower = 6.0f;

    // ジャンプ入力を反映する最大時間
    [SerializeField]
    [Tooltip("ジャンプ入力を上方向の移動へ反映する最大時間です。")]
    [Min(MIN_TIME)]
    private float m_jumpInputDuration = 0.5f;

    // 落下中の重力倍率
    [SerializeField]
    [Tooltip("落下中に適用する重力倍率です。大きいほど早く落下します。")]
    [Min(1.0f)]
    private float m_fallGravityMultiplier = 2.5f;

    // ジャンプ早期解除時の重力倍率
    [SerializeField]
    [Tooltip("上昇中にジャンプ入力を離した際に適用する重力倍率です。")]
    [Min(1.0f)]
    private float m_lowJumpMultiplier = 4.0f;

    // 落下速度の上限
    [SerializeField]
    [Tooltip("落下速度の上限(絶対値)です。")]
    [Min(0.0f)]
    private float m_maxFallSpeed = 20.0f;

    /// <summary>
    /// ジャンプ力を取得します。
    /// </summary>
    public float JumpPower => m_jumpPower;

    /// <summary>
    /// ジャンプ入力の最大反映時間を取得します。
    /// </summary>
    public float JumpInputDuration => m_jumpInputDuration;

    /// <summary>
    /// 落下中に適用する重力倍率を取得します。
    /// </summary>
    public float FallGravityMultiplier => m_fallGravityMultiplier;

    /// <summary>
    /// ジャンプ入力を早期に離した際に適用する重力倍率を取得します。
    /// </summary>
    public float LowJumpMultiplier => m_lowJumpMultiplier;

    /// <summary>
    /// 落下速度の上限(絶対値)を取得します。
    /// </summary>
    public float MaxFallSpeed => m_maxFallSpeed;

    /// <summary>
    /// 通常移動パラメータを生成します。
    /// </summary>
    /// <returns>通常移動パラメータ。</returns>
    public PlayerMoveParameters CreateMoveParameters()
    {
        return new PlayerMoveParameters(
            m_maxMoveSpeed,
            m_timeToMaxSpeed,
            m_timeToStop,
            m_rotationSpeed);
    }
}