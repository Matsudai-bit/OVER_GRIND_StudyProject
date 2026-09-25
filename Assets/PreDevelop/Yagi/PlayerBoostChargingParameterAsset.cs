using UnityEngine;
using UnityEngine.Serialization;

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

    // 移動中にチャージを開始した場合の最大チャージ時間
    // 既存の最大チャージ時間の値を引き継ぐ
    [SerializeField, Min(0.01f)]
    [FormerlySerializedAs("m_maxChargeTime")]
    private float m_movingStartChargeTime = 4.0f;

    // 停止中にチャージを開始した場合の最大チャージ時間
    [SerializeField, Min(0.01f)]
    private float m_stationaryStartChargeTime = 4.0f;

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

    // 停止中チャージ時の回転速度（度/秒）
    // チャージ率による補正は行わず、常に一定の速度で回転する
    [Tooltip("停止中チャージ時の回転速度（度/秒）。チャージ率によって変化しません。")]
    [SerializeField, Min(0.0f)]
    private float m_stationaryChargeRotationSpeed = 90.0f;

    // 移動中開始のチャージ中、モデルが移動方向に対して
    // どれだけ真横を向くかの角度（度）
    // 旋回入力がある側へこの角度分だけモデルを向ける
    [Tooltip("移動中開始のチャージ中、旋回入力側へモデルを向ける角度（度）。90で真横。")]
    [SerializeField, Range(0.0f, 180.0f)]
    private float m_movingChargeSidewaysLookAngle = 90.0f;

    [Header("カメラ設定")]

    [Tooltip("移動中開始のチャージ中、左右入力側へカメラを向ける角度（度）。移動方向を基準に、90で真横、0で正面。")]
    [SerializeField, Range(0.0f, 180.0f)]
    private float m_movingChargeCameraLookAngle = 90.0f;

    [Tooltip("左右入力の開始・反転から、カメラが目標角度に到達するまでの時間（秒）。0で即座に向きます。")]
    [SerializeField, Min(0.0f)]
    private float m_movingChargeCameraLookDuration = 0.5f;

    // 入力を離したときと、停止中開始のチャージで使用する追従速度（度/秒）
    [Tooltip("入力を離したときの戻り速度と、停止中開始のチャージで使用する追従速度（度/秒）。")]
    [SerializeField, Min(0.0f)]
    private float m_cameraDriftLookTurnSpeed = 180.0f;

    // 停止中開始のチャージで、カメラがプレイヤーの向きへどれだけ振れるか（0～1）
    [Tooltip("停止中開始のチャージで使用する追従割合。移動中開始ではカメラ角度を優先します。")]
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
    /// 移動中にチャージを開始した場合の最大チャージ時間を取得します。
    /// </summary>
    public float MovingStartChargeTime =>
        m_movingStartChargeTime;

    /// <summary>
    /// 停止中にチャージを開始した場合の最大チャージ時間を取得します。
    /// </summary>
    public float StationaryStartChargeTime =>
        m_stationaryStartChargeTime;

    /// <summary>
    /// ブーストダッシュに必要な最低チャージ割合を取得します。
    /// </summary>
    public float MinBoostChargeRate =>
        m_minBoostChargeRate;

    /// <summary>
    /// チャージ中の移動速度倍率を取得します。
    /// </summary>
    public float ChargeMoveSpeedRate =>
        m_chargeMoveSpeedRate;

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
    public float FacingRotationSpeed =>
        m_facingRotationSpeed;

    /// <summary>
    /// スティック入力のデッドゾーンを取得します。
    /// </summary>
    public float SteeringDeadZone =>
        m_steeringDeadZone;

    /// <summary>
    /// 停止中チャージ時の回転速度（度/秒）を取得します。
    /// </summary>
    public float StationaryChargeRotationSpeed =>
        m_stationaryChargeRotationSpeed;

    /// <summary>
    /// 移動中開始のチャージ中、旋回入力側へモデルを向ける角度（度）を取得します。
    /// </summary>
    public float MovingChargeSidewaysLookAngle =>
        m_movingChargeSidewaysLookAngle;

    /// <summary>
    /// 移動中開始のチャージで、入力側へカメラを向ける角度（度）を取得します。
    /// </summary>
    public float MovingChargeCameraLookAngle =>
        m_movingChargeCameraLookAngle;

    /// <summary>
    /// 左右入力の開始・反転からカメラが目標角度へ到達するまでの時間（秒）を取得します。
    /// </summary>
    public float MovingChargeCameraLookDuration =>
        m_movingChargeCameraLookDuration;

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
    public float NoMoveInputGraceTime =>
        m_noMoveInputGraceTime;

    /// <summary>
    /// 速度ログの出力間隔を取得します。
    /// </summary>
    public float SpeedLogInterval =>
        m_speedLogInterval;
}
