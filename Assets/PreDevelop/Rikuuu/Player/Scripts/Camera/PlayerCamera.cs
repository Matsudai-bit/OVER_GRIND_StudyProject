using Unity.Cinemachine;
using UnityEngine;

/// <summary>通常カメラ・ドリフト操作とロックオンカメラの切り替えを管理します。</summary>
public class PlayerCamera : MonoBehaviour
{
    private const float MIN_TARGET_DISTANCE = 0.0001f;
    private const float DEFAULT_CAMERA_SENSITIVITY = 1.0f;
    private const float DEFAULT_CHARGE_DIRECTION_INFLUENCE = 1.0f;
    private const float DEFAULT_CHARGE_VERTICAL_ANGLE = 8.0f;
    private const float DEFAULT_CHARGE_VERTICAL_TURN_SPEED = 120.0f;


    // Y軸の入力変換比をX軸に揃えるための補正倍率
    private const float AXIS_Y_GAIN_SCALE = 0.25f;
    private const float AXIS_Y_DIRECTION = -1.0f;


    [SerializeField] private CinemachineCamera m_cinemachineCamera;
    [SerializeField] private CinemachineInputAxisController m_inputAxisController;



    [Header("Parameter")]

    [SerializeField]
    [Tooltip("カメラ制御設定。未設定時は従来Asset、さらに未設定ならスクリプト既定値を使用します。")]
    private PlayerCameraControllerParameterAsset m_controllerParameterAsset;

    [SerializeField]
    [Tooltip("カメラ挙動のパラメータを設定したアセット。")]
    private PlayerCameraParameterAsset m_parameterAsset;

    // アセットから生成した、カメラ制御用のパラメータ
    private PlayerCameraParameter m_parameter;


    [Header("Charge Camera")]

    //[SerializeField, Range(0.0f, 1.0f)]
    //[Tooltip("チャージ中に進行方向へ向く割合。0でチャージ開始時の向き、1で進行方向を向きます。")]
    private float m_chargeCameraDirectionInfluence;

    //[SerializeField, Range(-10.0f, 45.0f)]
    //[Tooltip("チャージ中に維持するカメラの上下角度。Playerを少し上から見る角度です。")]
    private float m_chargeCameraVerticalAngle;

    //[SerializeField, Min(0.0f)]
    //[Tooltip("チャージ中にカメラの高さを目的角度へ戻す速度[度/秒]。")]
    private float m_chargeCameraVerticalTurnSpeed;



    private CinemachineOrbitalFollow m_orbitalFollow;
    private PlayerLockOnCamera m_lockOnCamera;
    private bool m_isDriftOverrideActive;
    private float m_overrideStartAngle;
    private float m_overrideTargetAngle;
    private float m_overrideBlendElapsedTime;
    private bool m_hasOverrideTarget;
    private bool m_restoreInput;

    /// <summary>ドリフト方向へのカメラ制御が有効かを取得します。</summary>
    public bool IsDriftOverrideActive => m_isDriftOverrideActive;

    /// <summary>既存カメラと専用ロックオン制御を接続します。</summary>
    private void Awake()
    {
        if (m_cinemachineCamera == null)
        {
            Debug.LogError("PlayerCameraのCinemachineCamera参照が未設定です。", this);
            return;
        }

        ApplyCameraParameter();

        m_orbitalFollow = m_cinemachineCamera.GetComponent<CinemachineOrbitalFollow>();
        m_lockOnCamera = m_cinemachineCamera.GetComponent<PlayerLockOnCamera>();
        if (m_lockOnCamera == null)
            m_lockOnCamera = m_cinemachineCamera.gameObject.AddComponent<PlayerLockOnCamera>();
    }

    /// <summary>通常カメラへの復帰が完了した後に入力を戻します。</summary>
    private void Update()
    {
        if (m_restoreInput && m_lockOnCamera != null && !m_lockOnCamera.IsControlling)
        {
            m_restoreInput = false;
            if (m_inputAxisController != null)
                m_inputAxisController.enabled = !m_isDriftOverrideActive;
        }
    }

    /// <summary>
    /// チャージ開始時のカメラ方向と進行方向から、固定の注視角度を確定します。
    /// </summary>
    /// <param name="worldDirection">チャージ開始時の進行方向。</param>
    /// <param name="blendRate">開始時のカメラ方向を0、進行方向を1とする補間割合。</param>
    public void BeginDriftLookOverride(
        Vector3 worldDirection,
        float blendRate)
    {
        m_isDriftOverrideActive = true;
        m_hasOverrideTarget = false;
        m_overrideBlendElapsedTime = 0.0f;

        if (m_inputAxisController != null)
        {
            m_inputAxisController.enabled = false;
        }

        if (m_orbitalFollow != null)
        {
            m_overrideStartAngle =
                m_orbitalFollow.HorizontalAxis.Value;

            SetDriftOverrideTarget(worldDirection, blendRate);
        }
    }

    /// <summary>
    /// 進行方向オーバーライドを終了し、
    /// 通常のカメラ操作へ戻します。
    /// </summary>
    public void EndDriftLookOverride()
    {
        m_isDriftOverrideActive = false;
        if (m_lockOnCamera != null && m_lockOnCamera.IsControlling)
            m_restoreInput = true;

        if (m_inputAxisController != null)
        {
            m_inputAxisController.enabled = m_lockOnCamera == null || !m_lockOnCamera.IsControlling;
        }
    }

    /// <summary>
    /// 現在の目標角度へ一定速度でカメラを向け、到達後は停止します。
    /// </summary>
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
                Mathf.Max(turnSpeedDegreesPerSecond, 0.0f) *
                deltaTime);

        m_orbitalFollow.HorizontalAxis.Value =
            nextAngle;

        UpdateChargeCameraHeight(deltaTime);
    }

    /// <summary>
    /// 開始時の水平角度から現在の目標角度へ指定時間で補間します。
    /// </summary>
    /// <param name="duration">目標角度への到達時間（秒）。0以下で即座に向きます。</param>
    /// <param name="deltaTime">今回の更新で進める時間（秒）。</param>
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

        m_overrideBlendElapsedTime += Mathf.Max(deltaTime, 0.0f);
        float progress = duration > 0.0f
            ? Mathf.Clamp01(m_overrideBlendElapsedTime / duration)
            : 1.0f;

        m_orbitalFollow.HorizontalAxis.Value = Mathf.LerpAngle(
            m_overrideStartAngle,
            m_overrideTargetAngle,
            progress);
        UpdateChargeCameraHeight(deltaTime);
    }

    /// <summary>
    /// 開始時のカメラ角度を基準に、現在の体の向きから目標を更新します。
    /// </summary>
    /// <param name="worldDirection">現在の体のワールド方向。</param>
    /// <param name="blendRate">開始方向を0、体の方向を1とする補間割合。</param>
    public void UpdateDriftLookTarget(Vector3 worldDirection, float blendRate)
    {
        if (!m_isDriftOverrideActive || m_orbitalFollow == null) return;
        SetDriftOverrideTarget(worldDirection, blendRate);
    }

    /// <summary>
    /// 開始時のカメラ角度を0、指定方向を1として目標角度を設定します。
    /// </summary>
    /// <param name="worldDirection">チャージ開始時の進行方向。</param>
    /// <param name="blendRate">開始方向と進行方向の補間割合。</param>
    private void SetDriftOverrideTarget(
        Vector3 worldDirection,
        float blendRate)
    {
        worldDirection.y = 0.0f;
        if (worldDirection.sqrMagnitude <= MIN_TARGET_DISTANCE)
        {
            return;
        }

        float directionAngle =
            Mathf.Atan2(worldDirection.x, worldDirection.z) *
            Mathf.Rad2Deg;

        float clampedBlendRate =
            Mathf.Clamp01(blendRate) *
            Mathf.Clamp01(m_chargeCameraDirectionInfluence);

        m_overrideTargetAngle = Mathf.LerpAngle(
            m_overrideStartAngle,
            directionAngle,
            clampedBlendRate);
        m_hasOverrideTarget = true;
    }

    /// <summary>チャージ中のカメラ高さをPlayerの少し上へ戻します。</summary>
    /// <param name="deltaTime">今回の更新で進める時間（秒）。</param>
    private void UpdateChargeCameraHeight(float deltaTime)
    {
        if (m_orbitalFollow == null)
        {
            return;
        }

        m_orbitalFollow.VerticalAxis.Value = Mathf.MoveTowards(
            m_orbitalFollow.VerticalAxis.Value,
            m_chargeCameraVerticalAngle,
            m_chargeCameraVerticalTurnSpeed * Mathf.Max(deltaTime, 0.0f));
    }

    /// <summary>
    /// カメラの水平角度を指定したワールド方向へ即座に向けます。
    /// </summary>
    public void SnapLookDirectionOnce(Vector3 worldDirection)
    {
        if (m_orbitalFollow == null)
        {
            return;
        }

        worldDirection.y = 0.0f;

        if (worldDirection.sqrMagnitude <= MIN_TARGET_DISTANCE)
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


    /// <summary>現在の描画位置からロックオン構図へ移行します。</summary>
    /// <param name="target">注視する部位。</param>
    /// <param name="duration">遷移の滑らかさの下限（秒）。</param>
    /// <param name="playerMoveDirection">既存呼び出しとの互換用。</param>
    public void BeginTargetFocus(Transform target, float duration, Vector3 playerMoveDirection = default)
    {
        if (target == null || m_lockOnCamera == null)
            return;
        m_lockOnCamera.BeginFocus(target, duration);
        if (m_inputAxisController != null)
        {
            m_restoreInput |= m_inputAxisController.enabled;
            m_inputAxisController.enabled = false;
        }
    }

    /// <summary>ロックオン先を更新します。位置計算はCinemachine更新内で行います。</summary>
    public void UpdateTargetFocus(Transform target, Vector3 playerMoveDirection = default)
    {
        if (m_lockOnCamera != null)
            m_lockOnCamera.SetTarget(target);
    }

    /// <summary>通常カメラへ滑らかに戻します。</summary>
    public void EndTargetFocus()
    {
        if (m_lockOnCamera != null)
            m_lockOnCamera.EndFocus();
    }

    /// <summary>互換APIとして共通のロックオン処理を開始します。</summary>
    public void BeginTargetLock(Transform target, float duration, Vector3 playerMoveDirection = default)
        => BeginTargetFocus(target, duration, playerMoveDirection);

    /// <summary>互換APIとして共通のロックオン先を更新します。</summary>
    public void UpdateTargetLock(Transform target, Vector3 playerMoveDirection = default)
        => UpdateTargetFocus(target, playerMoveDirection);

    /// <summary>互換APIとして通常カメラへ復帰します。</summary>
    public void EndTargetLock(float duration) => EndTargetFocus();

    /// <summary>無効化時にロックオンを解除します。</summary>
    private void OnDisable() => EndTargetFocus();



    /// <summary>
    /// カメラのパラメータを適用させる。
    /// </summary>
    public void ApplyCameraParameter()
    {
        if(m_inputAxisController == null)
        {
            Debug.LogAssertion("PlayerCameraのCinemachineInputAxisController参照が未設定です。");
            return;
        }

        float sensitivityX = DEFAULT_CAMERA_SENSITIVITY;
        float sensitivityY = DEFAULT_CAMERA_SENSITIVITY;
        bool reverseY = false;
        m_chargeCameraDirectionInfluence = DEFAULT_CHARGE_DIRECTION_INFLUENCE;
        m_chargeCameraVerticalAngle = DEFAULT_CHARGE_VERTICAL_ANGLE;
        m_chargeCameraVerticalTurnSpeed = DEFAULT_CHARGE_VERTICAL_TURN_SPEED;

        if (m_controllerParameterAsset != null)
        {
            sensitivityX = m_controllerParameterAsset.CameraSensitivityX;
            sensitivityY = m_controllerParameterAsset.CameraSensitivityY;
            reverseY = m_controllerParameterAsset.IsCameraReverseY;
            m_chargeCameraDirectionInfluence = m_controllerParameterAsset.ChargeCameraDirectionInfluence;
            m_chargeCameraVerticalAngle = m_controllerParameterAsset.ChargeCameraVerticalAngle;
            m_chargeCameraVerticalTurnSpeed = m_controllerParameterAsset.ChargeCameraVerticalTurnSpeed;
        }
        else if (m_parameterAsset != null)
        {
            m_parameter = m_parameterAsset.CreateCameraParameter();
            sensitivityX = m_parameter.m_cameraSensitivityX;
            sensitivityY = m_parameter.m_cameraSensitivityY;
            reverseY = m_parameter.m_isCameraReverseY;
            m_chargeCameraDirectionInfluence = m_parameter.m_chargeCameraDirectionInfluence;
            m_chargeCameraVerticalAngle = m_parameter.m_chargeCameraVerticalAngle;
            m_chargeCameraVerticalTurnSpeed = m_parameter.m_chargeCameraVerticalTurnSpeed;
        }

        foreach(var controller in m_inputAxisController.Controllers)
        {
            // X軸カメラ感度の設定
            if(controller.Name == "Look Orbit X")
            {
                controller.Input.Gain = sensitivityX * 100.0f;
            }
            // Y軸カメラ感度の設定
            if(controller.Name == "Look Orbit Y")
            {
                // カメラ反転にチェックがついているかを調べる
                float sign = reverseY ? -1.0f : 1.0f;
                controller.Input.Gain = AXIS_Y_DIRECTION * sign * sensitivityY * 100.0f * AXIS_Y_GAIN_SCALE;
            }
        }
    }
}
