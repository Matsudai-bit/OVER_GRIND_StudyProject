using UnityEngine;

/// <summary>
/// ブーストチャージ状態(PlayerBoostChargingState)で使用する
/// 各種パラメータをまとめたアセットです。
/// </summary>
[CreateAssetMenu(
    fileName = "PlayerBoostChargingParameterAsset",
    menuName = "Player/Boost Charging Parameter Asset")]
public sealed class PlayerBoostChargingParameterAsset : ScriptableObject
{
    [Header("チャージ設定")]

    // 最大チャージ時間
    [SerializeField, Min(0.01f)]
    private float m_maxChargeTime = 4.0f;

    // ブーストダッシュに必要な最低チャージ割合
    [SerializeField, Range(0.0f, 1.0f)]
    private float m_minBoostChargeRate = 0.10f;

    // チャージ中の移動速度倍率
    [SerializeField, Min(0.0f)]
    private float m_chargeMoveSpeedRate = 0.75f;

    [Header("ドリフト設定（移動方向の曲がりやすさ）")]

    // チャージ開始直後の、移動方向を変更できる速度（度/秒）
    // 値が小さいほど曲がりにくい
    [Tooltip("チャージ開始直後の曲がりやすさ（度/秒）。小さいほど曲がりにくい。")]
    [SerializeField, Min(0.0f)]
    private float m_driftTurnSpeedAtChargeStart = 15.0f;

    // チャージ完了時点の、移動方向を変更できる速度（度/秒）
    // チャージが進むほどこの値へ近づき、曲がりやすくなる
    [Tooltip("チャージ完了時点の曲がりやすさ（度/秒）。大きいほど曲がりやすい。")]
    [SerializeField, Min(0.0f)]
    private float m_driftTurnSpeedAtFullCharge = 60.0f;

    // プレイヤーの見た目の向きを変更する速度（度/秒）
    [SerializeField, Min(0.0f)]
    private float m_facingRotationSpeed = 50.0f;

    // スティック入力のデッドゾーン
    [SerializeField, Range(0.0f, 1.0f)]
    private float m_steeringDeadZone = 0.1f;

    [Header("カメラ設定")]

    // チャージ中、カメラが目標角度へ追従する速度（度/秒）
    [SerializeField, Min(0.0f)]
    private float m_cameraDriftLookTurnSpeed = 180.0f;

    // チャージ中、カメラがプレイヤーの向きへどれだけ振れるか（0～1）
    [SerializeField, Range(0.0f, 1.0f)]
    private float m_cameraDriftLookBlendRate = 0.75f;

    [Header("その他")]

    // 移動入力が瞬間的に途切れてもIdlingへ遷移しない猶予時間
    [SerializeField, Min(0.0f)]
    private float m_noMoveInputGraceTime = 0.15f;

    // 速度ログの出力間隔
    [SerializeField, Min(0.01f)]
    private float m_speedLogInterval = 0.25f;

    /// <summary>
    /// 最大チャージ時間を取得します。
    /// </summary>
    public float MaxChargeTime => m_maxChargeTime;

    /// <summary>
    /// ブーストダッシュに必要な最低チャージ割合を取得します。
    /// </summary>
    public float MinBoostChargeRate => m_minBoostChargeRate;

    /// <summary>
    /// チャージ中の移動速度倍率を取得します。
    /// </summary>
    public float ChargeMoveSpeedRate => m_chargeMoveSpeedRate;

    /// <summary>
    /// チャージ開始直後の曲がりやすさ（度/秒）を取得します。
    /// </summary>
    public float DriftTurnSpeedAtChargeStart =>
        m_driftTurnSpeedAtChargeStart;

    /// <summary>
    /// チャージ完了時点の曲がりやすさ（度/秒）を取得します。
    /// </summary>
    public float DriftTurnSpeedAtFullCharge =>
        m_driftTurnSpeedAtFullCharge;

    /// <summary>
    /// プレイヤーの見た目の向きを変更する速度（度/秒）を取得します。
    /// </summary>
    public float FacingRotationSpeed => m_facingRotationSpeed;

    /// <summary>
    /// スティック入力のデッドゾーンを取得します。
    /// </summary>
    public float SteeringDeadZone => m_steeringDeadZone;

    /// <summary>
    /// カメラの追従速度（度/秒）を取得します。
    /// </summary>
    public float CameraDriftLookTurnSpeed =>
        m_cameraDriftLookTurnSpeed;

    /// <summary>
    /// カメラのブレンド割合（0～1）を取得します。
    /// </summary>
    public float CameraDriftLookBlendRate =>
        m_cameraDriftLookBlendRate;

    /// <summary>
    /// 移動入力の猶予時間を取得します。
    /// </summary>
    public float NoMoveInputGraceTime => m_noMoveInputGraceTime;

    /// <summary>
    /// 速度ログの出力間隔を取得します。
    /// </summary>
    public float SpeedLogInterval => m_speedLogInterval;
}