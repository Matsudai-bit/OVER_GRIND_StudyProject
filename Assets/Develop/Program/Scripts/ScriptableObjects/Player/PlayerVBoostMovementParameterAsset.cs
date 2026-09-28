using UnityEngine;

/// <summary>
/// プレイヤーのVブースト移動パラメータを設定します。
/// </summary>
[CreateAssetMenu(
    fileName = "PlayerVBoostMovementParameter",
    menuName = "Game/Parameters/Player/V Boost Movement Parameter")]
public sealed class PlayerVBoostMovementParameterAsset : ScriptableObject
{
    // 時間パラメータの最小値
    private const float MIN_TIME = 0.01f;

    // 初速ブーストの最高速度
    [SerializeField, Header("初速ブースト")]
    [Tooltip("Vブースト開始直後の最高速度です。")]
    [Min(0.0f)]
    private float m_initialBoostMaxMoveSpeed = 12.0f;

    // 初速ブーストの加速時間
    [SerializeField]
    [Tooltip("初速ブーストで最高速度に到達するまでの時間です。")]
    [Min(MIN_TIME)]
    private float m_initialBoostTimeToMaxSpeed = 0.1f;

    // 初速ブーストの停止時間
    [SerializeField]
    [Tooltip("初速ブースト中に減速するときの停止時間です。")]
    [Min(MIN_TIME)]
    private float m_initialBoostTimeToStop = 0.15f;

    // 初速ブーストの回転速度
    [SerializeField]
    [Tooltip("初速ブースト中の1秒間の最大回転角度です。")]
    [Min(0.0f)]
    private float m_initialBoostRotationSpeed = 720.0f;

    // 初速ブーストの継続時間
    [SerializeField]
    [Tooltip("初速ブーストを継続する時間です。")]
    [Min(MIN_TIME)]
    private float m_initialBoostDuration = 0.25f;

    // 安定ブーストの最高速度
    [SerializeField, Header("安定ブースト")]
    [Tooltip("初速ブースト後に維持する最高速度です。")]
    [Min(0.0f)]
    private float m_stableBoostMaxMoveSpeed = 8.0f;

    // 安定ブーストの速度到達時間
    [SerializeField]
    [Tooltip("安定ブーストの目標速度へ到達するまでの時間です。")]
    [Min(MIN_TIME)]
    private float m_stableBoostTimeToMaxSpeed = 0.5f;

    // 安定ブーストの停止時間
    [SerializeField]
    [Tooltip("安定ブースト中に減速するときの停止時間です。")]
    [Min(MIN_TIME)]
    private float m_stableBoostTimeToStop = 0.25f;

    // 安定ブーストの回転速度
    [SerializeField]
    [Tooltip("安定ブースト中の1秒間の最大回転角度です。")]
    [Min(0.0f)]
    private float m_stableBoostRotationSpeed = 540.0f;

    // 安定ブーストの継続時間
    [SerializeField]
    [Tooltip("安定ブーストを継続する時間です。")]
    [Min(MIN_TIME)]
    private float m_stableBoostDuration = 2.0f;

    /// <summary>
    /// 初速ブーストの継続時間を取得します。
    /// </summary>
    public float InitialBoostDuration => m_initialBoostDuration;

    /// <summary>
    /// 安定ブーストの継続時間を取得します。
    /// </summary>
    public float StableBoostDuration => m_stableBoostDuration;

    /// <summary>
    /// 初速ブーストの移動パラメータを生成します。
    /// </summary>
    /// <returns>初速ブーストの移動パラメータ。</returns>
    public PlayerMoveParameters CreateInitialBoostParameters()
    {
        return new PlayerMoveParameters(
            m_initialBoostMaxMoveSpeed,
            m_initialBoostTimeToMaxSpeed,
            m_initialBoostTimeToStop,
            m_initialBoostRotationSpeed);
    }

    /// <summary>
    /// 安定ブーストの移動パラメータを生成します。
    /// </summary>
    /// <returns>安定ブーストの移動パラメータ。</returns>
    public PlayerMoveParameters CreateStableBoostParameters()
    {
        return new PlayerMoveParameters(
            m_stableBoostMaxMoveSpeed,
            m_stableBoostTimeToMaxSpeed,
            m_stableBoostTimeToStop,
            m_stableBoostRotationSpeed);
    }
}