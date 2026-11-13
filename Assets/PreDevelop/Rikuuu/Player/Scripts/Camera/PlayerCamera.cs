using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Playerの通常カメラ、チャージ中のカメラ補正、
/// ロックオンカメラを統括します。
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerCamera : MonoBehaviour
{
    // --------------------------------------------------------
    // 定数
    // --------------------------------------------------------

    // 方向ベクトルを有効とみなす最小値
    private const float MIN_TARGET_DISTANCE = 0.0001f;

    // ParameterAsset未設定時のカメラ感度
    private const float DEFAULT_CAMERA_SENSITIVITY = 1.0f;

    // 動的カメラ感度の初期値
    private const float DEFAULT_DYNAMIC_CAMERA_SENSITIVITY = 0.5f;

    // 動的感度0.5を従来倍率1.0へ変換する係数
    private const float DYNAMIC_SENSITIVITY_SCALE = 2.0f;

    // Cinemachine Input Gainへ変換する倍率
    private const float CAMERA_INPUT_GAIN_SCALE = 100.0f;

    // ParameterAsset未設定時のチャージカメラ設定
    private const float DEFAULT_CHARGE_DIRECTION_INFLUENCE = 1.0f;
    private const float DEFAULT_CHARGE_VERTICAL_ANGLE = 8.0f;
    private const float DEFAULT_CHARGE_VERTICAL_TURN_SPEED = 120.0f;

    // Y軸入力を既存の操作感へ合わせる補正値
    private const float AXIS_Y_GAIN_SCALE = 0.25f;
    private const float AXIS_Y_DIRECTION = -1.0f;

    // CinemachineInputAxisController内の入力軸名
    private const string LOOK_ORBIT_X_NAME = "Look Orbit X";
    private const string LOOK_ORBIT_Y_NAME = "Look Orbit Y";

    // --------------------------------------------------------
    // Inspector参照
    // --------------------------------------------------------

    // Player用CinemachineCamera
    [SerializeField, Header("Camera Reference")]
    private CinemachineCamera m_cinemachineCamera;

    // 通常時のカメラ入力を受け取るController
    [SerializeField]
    private CinemachineInputAxisController m_inputAxisController;

    // ゲーム中に変化する動的カメラ感度の取得元
    [SerializeField]
    [Tooltip(
        "Playerの動的カメラ感度を受け取ります。"
        + "未設定時はParameterAssetの感度をそのまま使用します。")]
    private PlayerDynamicParameterController m_dynamicParameterController;

    // カメラ設定の唯一のParameterAsset
    [SerializeField, Header("Parameter")]
    [Tooltip(
        "カメラ感度・反転・チャージカメラ設定を保持します。"
        + "ゲーム設定変更もこのAssetへ反映されます。")]
    private PlayerCameraParameterAsset m_parameterAsset;

    // --------------------------------------------------------
    // Ground Collision
    // --------------------------------------------------------

    // カメラを貫通させない地面Layer
    [SerializeField, Header("Ground Collision")]
    [Tooltip("カメラを貫通させない地面のLayer。")]
    private LayerMask m_groundLayerMask = 1 << 3;

    // 地面との判定に使用するカメラ半径
    [SerializeField, Min(0.01f)]
    [Tooltip("地面との判定に使用するカメラ周囲の半径。")]
    private float m_groundCollisionRadius = 0.3f;

    // 地面との接触位置から確保する余白
    [SerializeField, Min(0.0f)]
    [Tooltip("地面との接触位置から追加で確保する余白。")]
    private float m_groundCollisionPadding = 0.05f;

    // Player足元との誤接触を避ける判定開始高さ
    [SerializeField, Min(0.0f)]
    [Tooltip(
        "Player位置から地面判定を開始する高さ。"
        + "足元との誤接触を防ぎます。")]
    private float m_groundCollisionOriginHeight = 1.0f;

    // --------------------------------------------------------
    // Runtime Component
    // --------------------------------------------------------

    // 通常カメラの周回制御
    private CinemachineOrbitalFollow m_orbitalFollow;

    // ロックオン中の専用カメラ制御
    private PlayerLockOnCamera m_lockOnCamera;

    // 通常カメラの地面貫通防止
    private PlayerCameraGroundCollision m_groundCollision;

    // --------------------------------------------------------
    // Runtime Parameter
    // --------------------------------------------------------

    // ParameterAssetから取得した現在のX軸基準感度
    private float m_baseSensitivityX =
        DEFAULT_CAMERA_SENSITIVITY;

    // ParameterAssetから取得した現在のY軸基準感度
    private float m_baseSensitivityY =
        DEFAULT_CAMERA_SENSITIVITY;

    // 現在のY軸反転設定
    private bool m_reverseY;

    // DynamicParameterControllerから受け取る感度倍率
    private float m_dynamicCameraSensitivity =
        DEFAULT_DYNAMIC_CAMERA_SENSITIVITY;

    // チャージ中に進行方向へ向く割合
    private float m_chargeCameraDirectionInfluence =
        DEFAULT_CHARGE_DIRECTION_INFLUENCE;

    // チャージ中に維持する上下角度
    private float m_chargeCameraVerticalAngle =
        DEFAULT_CHARGE_VERTICAL_ANGLE;

    // チャージ中の上下回転速度
    private float m_chargeCameraVerticalTurnSpeed =
        DEFAULT_CHARGE_VERTICAL_TURN_SPEED;

    // --------------------------------------------------------
    // Charge Camera State
    // --------------------------------------------------------

    // チャージ中の方向オーバーライドが有効か
    private bool m_isDriftOverrideActive;

    // オーバーライド開始時の水平角度
    private float m_overrideStartAngle;

    // オーバーライド中の目標水平角度
    private float m_overrideTargetAngle;

    // 時間指定補間の経過時間
    private float m_overrideBlendElapsedTime;

    // 現在有効な目標角度を保持しているか
    private bool m_hasOverrideTarget;

    // ロックオン終了後に通常入力を戻す必要があるか
    private bool m_restoreInput;

    /// <summary>
    /// チャージ中のカメラ方向オーバーライドが有効か取得します。
    /// </summary>
    public bool IsDriftOverrideActive =>
        m_isDriftOverrideActive;

    // --------------------------------------------------------
    // Unity Event
    // --------------------------------------------------------

    /// <summary>
    /// カメラComponentを取得し、衝突設定を初期化します。
    /// </summary>
    private void Awake()
    {
        if (m_cinemachineCamera == null)
        {
            Debug.LogError(
                $"{nameof(PlayerCamera)}の"
                + $"{nameof(CinemachineCamera)}参照が未設定です。",
                this);

            return;
        }

        InitializeCameraComponents();
        ConfigureCameraCollision();
    }

    /// <summary>
    /// Parameter変更通知を購読し、現在値をカメラへ反映します。
    /// </summary>
    private void OnEnable()
    {
        SubscribeParameterEvents();

        // 無効中に設定が変更されていた場合も、
        // Enable時点の最新値を必ず反映する。
        ApplyCameraParameter();
    }

    /// <summary>
    /// ロックオン終了後に通常カメラ入力を復元します。
    /// </summary>
    private void Update()
    {
        if (!m_restoreInput ||
            m_lockOnCamera == null ||
            m_lockOnCamera.IsControlling)
        {
            return;
        }

        m_restoreInput = false;

        if (m_inputAxisController != null)
        {
            // チャージオーバーライド中は通常入力を戻さない
            m_inputAxisController.enabled =
                !m_isDriftOverrideActive;
        }
    }

    /// <summary>
    /// Parameter変更通知を解除し、ロックオンを終了します。
    /// </summary>
    private void OnDisable()
    {
        UnsubscribeParameterEvents();
        EndTargetFocus();
    }

    // --------------------------------------------------------
    // Initialization
    // --------------------------------------------------------

    /// <summary>
    /// CinemachineCamera上の補助Componentを取得します。
    /// 必要なComponentが無い場合は自動追加します。
    /// </summary>
    private void InitializeCameraComponents()
    {
        m_orbitalFollow =
            m_cinemachineCamera
                .GetComponent<CinemachineOrbitalFollow>();

        m_lockOnCamera =
            m_cinemachineCamera
                .GetComponent<PlayerLockOnCamera>();

        if (m_lockOnCamera == null)
        {
            m_lockOnCamera =
                m_cinemachineCamera.gameObject
                    .AddComponent<PlayerLockOnCamera>();
        }

        m_groundCollision =
            m_cinemachineCamera
                .GetComponent<PlayerCameraGroundCollision>();

        if (m_groundCollision == null)
        {
            m_groundCollision =
                m_cinemachineCamera.gameObject
                    .AddComponent<PlayerCameraGroundCollision>();
        }
    }

    /// <summary>
    /// 通常カメラとロックオンカメラへ地面衝突設定を適用します。
    /// </summary>
    private void ConfigureCameraCollision()
    {
        if (m_groundCollision != null)
        {
            m_groundCollision.Configure(
                m_groundLayerMask,
                m_groundCollisionRadius,
                m_groundCollisionPadding,
                m_groundCollisionOriginHeight);
        }

        if (m_lockOnCamera != null)
        {
            m_lockOnCamera.ConfigureCollision(
                m_groundLayerMask,
                m_groundCollisionRadius,
                m_groundCollisionPadding,
                m_groundCollisionOriginHeight);
        }
    }

    /// <summary>
    /// カメラパラメータ関連の変更通知を購読します。
    /// </summary>
    private void SubscribeParameterEvents()
    {
        if (m_parameterAsset != null)
        {
            m_parameterAsset.ParameterChanged +=
                ApplyCameraParameter;
        }

        if (m_dynamicParameterController != null)
        {
            m_dynamicParameterController.CameraSensitivityChanged +=
                ApplyCameraSensitivity;

            m_dynamicCameraSensitivity =
                m_dynamicParameterController.CameraSensitivity;
        }
    }

    /// <summary>
    /// カメラパラメータ関連の変更通知を解除します。
    /// </summary>
    private void UnsubscribeParameterEvents()
    {
        if (m_parameterAsset != null)
        {
            m_parameterAsset.ParameterChanged -=
                ApplyCameraParameter;
        }

        if (m_dynamicParameterController != null)
        {
            m_dynamicParameterController.CameraSensitivityChanged -=
                ApplyCameraSensitivity;
        }
    }

    // --------------------------------------------------------
    // Parameter Apply
    // --------------------------------------------------------

    /// <summary>
    /// ParameterAssetの現在値をPlayerCameraへ反映します。
    /// 設定画面から値が変更された場合も、このメソッドが自動で再実行されます。
    /// </summary>
    public void ApplyCameraParameter()
    {
        // Asset未設定時はスクリプト既定値を使用する
        if (m_parameterAsset == null)
        {
            m_baseSensitivityX =
                DEFAULT_CAMERA_SENSITIVITY;

            m_baseSensitivityY =
                DEFAULT_CAMERA_SENSITIVITY;

            m_reverseY =
                false;

            m_chargeCameraDirectionInfluence =
                DEFAULT_CHARGE_DIRECTION_INFLUENCE;

            m_chargeCameraVerticalAngle =
                DEFAULT_CHARGE_VERTICAL_ANGLE;

            m_chargeCameraVerticalTurnSpeed =
                DEFAULT_CHARGE_VERTICAL_TURN_SPEED;
        }
        else
        {
            // ユーザー設定を含むAssetの現在値を取得する
            m_baseSensitivityX =
                m_parameterAsset.CameraSensitivityX;

            m_baseSensitivityY =
                m_parameterAsset.CameraSensitivityY;

            m_reverseY =
                m_parameterAsset.IsCameraReverseY;

            m_chargeCameraDirectionInfluence =
                m_parameterAsset
                    .ChargeCameraDirectionInfluence;

            m_chargeCameraVerticalAngle =
                m_parameterAsset
                    .ChargeCameraVerticalAngle;

            m_chargeCameraVerticalTurnSpeed =
                m_parameterAsset
                    .ChargeCameraVerticalTurnSpeed;
        }

        // 基準感度・反転設定が変化したため、
        // Cinemachine Input Gainまで即座に再計算する。
        ApplyCameraSensitivity(
            m_dynamicCameraSensitivity);
    }

    /// <summary>
    /// 動的カメラ感度をCinemachine Input Gainへ反映します。
    /// </summary>
    /// <param name="sensitivity">
    /// 動的カメラ感度。0.1～1.0。
    /// </param>
    public void ApplyCameraSensitivity(
        float sensitivity)
    {
        m_dynamicCameraSensitivity =
            Mathf.Clamp(
                sensitivity,
                0.1f,
                1.0f);

        // DynamicParameterController未設定時は、
        // ParameterAssetの基準感度をそのまま使用する。
        float sensitivityMultiplier =
            m_dynamicParameterController != null
                ? m_dynamicCameraSensitivity *
                  DYNAMIC_SENSITIVITY_SCALE
                : DEFAULT_CAMERA_SENSITIVITY;

        if (m_inputAxisController == null)
        {
            return;
        }

        foreach (var controller in
                 m_inputAxisController.Controllers)
        {
            if (controller.Name ==
                LOOK_ORBIT_X_NAME)
            {
                controller.Input.Gain =
                    m_baseSensitivityX *
                    sensitivityMultiplier *
                    CAMERA_INPUT_GAIN_SCALE;

                continue;
            }

            if (controller.Name ==
                LOOK_ORBIT_Y_NAME)
            {
                float reverseSign =
                    m_reverseY
                        ? -1.0f
                        : 1.0f;

                controller.Input.Gain =
                    AXIS_Y_DIRECTION *
                    reverseSign *
                    m_baseSensitivityY *
                    sensitivityMultiplier *
                    CAMERA_INPUT_GAIN_SCALE *
                    AXIS_Y_GAIN_SCALE;
            }
        }
    }

    // --------------------------------------------------------
    // Charge Camera
    // --------------------------------------------------------

    /// <summary>
    /// チャージ中のカメラ方向オーバーライドを開始します。
    /// </summary>
    /// <param name="worldDirection">
    /// チャージ開始時の進行方向。
    /// </param>
    /// <param name="blendRate">
    /// 開始時のカメラ方向を0、進行方向を1とする補間割合。
    /// </param>
    public void BeginDriftLookOverride(
        Vector3 worldDirection,
        float blendRate)
    {
        m_isDriftOverrideActive = true;
        m_hasOverrideTarget = false;
        m_overrideBlendElapsedTime = 0.0f;

        if (m_inputAxisController != null)
        {
            // チャージ中は通常のカメラ入力を停止する
            m_inputAxisController.enabled =
                false;
        }

        if (m_orbitalFollow == null)
        {
            return;
        }

        // 開始角度を固定し、以降の補間基準にする
        m_overrideStartAngle =
            m_orbitalFollow.HorizontalAxis.Value;

        SetDriftOverrideTarget(
            worldDirection,
            blendRate);
    }

    /// <summary>
    /// チャージ中のカメラ方向オーバーライドを終了します。
    /// </summary>
    public void EndDriftLookOverride()
    {
        m_isDriftOverrideActive = false;

        if (m_lockOnCamera != null &&
            m_lockOnCamera.IsControlling)
        {
            // ロックオン中は終了後まで入力復元を遅延する
            m_restoreInput = true;
        }

        if (m_inputAxisController != null)
        {
            m_inputAxisController.enabled =
                m_lockOnCamera == null ||
                !m_lockOnCamera.IsControlling;
        }
    }

    /// <summary>
    /// 現在の目標角度へ一定速度でカメラを向けます。
    /// </summary>
    /// <param name="turnSpeedDegreesPerSecond">
    /// 水平回転速度[度/秒]。
    /// </param>
    /// <param name="deltaTime">経過時間。</param>
    public void UpdateDriftLookDirection(
        float turnSpeedDegreesPerSecond,
        float deltaTime)
    {
        if (!m_isDriftOverrideActive ||
            m_orbitalFollow == null ||
            !m_hasOverrideTarget)
        {
            return;
        }

        float currentAngle =
            m_orbitalFollow.HorizontalAxis.Value;

        float nextAngle =
            Mathf.MoveTowardsAngle(
                currentAngle,
                m_overrideTargetAngle,
                Mathf.Max(
                    turnSpeedDegreesPerSecond,
                    0.0f) *
                deltaTime);

        m_orbitalFollow.HorizontalAxis.Value =
            nextAngle;

        UpdateChargeCameraHeight(
            deltaTime);
    }

    /// <summary>
    /// 開始角度から目標角度へ指定時間で補間します。
    /// </summary>
    /// <param name="duration">
    /// 目標角度への到達時間。0以下で即座に向きます。
    /// </param>
    /// <param name="deltaTime">経過時間。</param>
    public void UpdateDriftLookDirectionOverTime(
        float duration,
        float deltaTime)
    {
        if (!m_isDriftOverrideActive ||
            m_orbitalFollow == null ||
            !m_hasOverrideTarget)
        {
            return;
        }

        m_overrideBlendElapsedTime +=
            Mathf.Max(
                deltaTime,
                0.0f);

        float progress =
            duration > 0.0f
                ? Mathf.Clamp01(
                    m_overrideBlendElapsedTime /
                    duration)
                : 1.0f;

        m_orbitalFollow.HorizontalAxis.Value =
            Mathf.LerpAngle(
                m_overrideStartAngle,
                m_overrideTargetAngle,
                progress);

        UpdateChargeCameraHeight(
            deltaTime);
    }

    /// <summary>
    /// チャージ中の目標カメラ方向を更新します。
    /// </summary>
    /// <param name="worldDirection">
    /// 現在のPlayerのワールド方向。
    /// </param>
    /// <param name="blendRate">
    /// 開始方向を0、指定方向を1とする補間割合。
    /// </param>
    public void UpdateDriftLookTarget(
        Vector3 worldDirection,
        float blendRate)
    {
        if (!m_isDriftOverrideActive ||
            m_orbitalFollow == null)
        {
            return;
        }

        SetDriftOverrideTarget(
            worldDirection,
            blendRate);
    }

    /// <summary>
    /// チャージ開始角度と指定方向から目標水平角度を計算します。
    /// </summary>
    /// <param name="worldDirection">目標方向。</param>
    /// <param name="blendRate">目標方向への補間割合。</param>
    private void SetDriftOverrideTarget(
        Vector3 worldDirection,
        float blendRate)
    {
        // 高低差は水平回転へ影響させない
        worldDirection.y = 0.0f;

        if (worldDirection.sqrMagnitude <=
            MIN_TARGET_DISTANCE)
        {
            return;
        }

        float directionAngle =
            Mathf.Atan2(
                worldDirection.x,
                worldDirection.z) *
            Mathf.Rad2Deg;

        float clampedBlendRate =
            Mathf.Clamp01(
                blendRate) *
            Mathf.Clamp01(
                m_chargeCameraDirectionInfluence);

        m_overrideTargetAngle =
            Mathf.LerpAngle(
                m_overrideStartAngle,
                directionAngle,
                clampedBlendRate);

        m_hasOverrideTarget = true;
    }

    /// <summary>
    /// チャージ中のカメラ上下角度を目標値へ近づけます。
    /// </summary>
    /// <param name="deltaTime">経過時間。</param>
    private void UpdateChargeCameraHeight(
        float deltaTime)
    {
        if (m_orbitalFollow == null)
        {
            return;
        }

        m_orbitalFollow.VerticalAxis.Value =
            Mathf.MoveTowards(
                m_orbitalFollow.VerticalAxis.Value,
                m_chargeCameraVerticalAngle,
                m_chargeCameraVerticalTurnSpeed *
                Mathf.Max(
                    deltaTime,
                    0.0f));
    }

    /// <summary>
    /// カメラの水平角度を指定方向へ即座に向けます。
    /// </summary>
    /// <param name="worldDirection">
    /// 向けるワールド方向。
    /// </param>
    public void SnapLookDirectionOnce(
        Vector3 worldDirection)
    {
        if (m_orbitalFollow == null)
        {
            return;
        }

        worldDirection.y = 0.0f;

        if (worldDirection.sqrMagnitude <=
            MIN_TARGET_DISTANCE)
        {
            return;
        }

        worldDirection.Normalize();

        float targetAngle =
            Mathf.Atan2(
                worldDirection.x,
                worldDirection.z) *
            Mathf.Rad2Deg;

        m_orbitalFollow.HorizontalAxis.Value =
            targetAngle;
    }

    // --------------------------------------------------------
    // Lock On Camera
    // --------------------------------------------------------

    /// <summary>
    /// 現在のカメラ位置からロックオン構図へ移行します。
    /// </summary>
    /// <param name="target">注視対象。</param>
    /// <param name="duration">遷移時間。</param>
    /// <param name="playerMoveDirection">
    /// 既存呼び出しとの互換用。
    /// </param>
    public void BeginTargetFocus(
        Transform target,
        float duration,
        Vector3 playerMoveDirection = default)
    {
        if (target == null ||
            m_lockOnCamera == null)
        {
            return;
        }

        m_lockOnCamera.BeginFocus(
            target,
            duration);

        if (m_inputAxisController != null)
        {
            m_restoreInput |=
                m_inputAxisController.enabled;

            m_inputAxisController.enabled =
                false;
        }
    }

    /// <summary>
    /// ロックオン中の注視対象を更新します。
    /// </summary>
    /// <param name="target">新しい注視対象。</param>
    /// <param name="playerMoveDirection">
    /// 既存呼び出しとの互換用。
    /// </param>
    public void UpdateTargetFocus(
        Transform target,
        Vector3 playerMoveDirection = default)
    {
        if (m_lockOnCamera == null)
        {
            return;
        }

        m_lockOnCamera.SetTarget(
            target);
    }

    /// <summary>
    /// ロックオンを終了して通常カメラへ戻します。
    /// </summary>
    public void EndTargetFocus()
    {
        if (m_lockOnCamera == null)
        {
            return;
        }

        m_lockOnCamera.EndFocus();
    }

    /// <summary>
    /// 互換APIとしてロックオンを開始します。
    /// </summary>
    public void BeginTargetLock(
        Transform target,
        float duration,
        Vector3 playerMoveDirection = default)
    {
        BeginTargetFocus(
            target,
            duration,
            playerMoveDirection);
    }

    /// <summary>
    /// 互換APIとしてロックオン対象を更新します。
    /// </summary>
    public void UpdateTargetLock(
        Transform target,
        Vector3 playerMoveDirection = default)
    {
        UpdateTargetFocus(
            target,
            playerMoveDirection);
    }

    /// <summary>
    /// 互換APIとしてロックオンを終了します。
    /// </summary>
    public void EndTargetLock(
        float duration)
    {
        EndTargetFocus();
    }
}
