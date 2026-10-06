using Unity.Cinemachine;
using UnityEngine;

/// <summary>通常カメラ・ドリフト操作とロックオンカメラの切り替えを管理します。</summary>
public class PlayerCamera : MonoBehaviour
{
    private const float MIN_TARGET_DISTANCE = 0.0001f;

    [SerializeField] private CinemachineCamera m_cinemachineCamera;
    [SerializeField] private CinemachineInputAxisController m_inputAxisController;



    [Header("Parameter")]

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
    private bool m_restoreInput;

    /// <summary>ドリフト方向へのカメラ制御が有効かを取得します。</summary>
    public bool IsDriftOverrideActive => m_isDriftOverrideActive;

    /// <summary>既存カメラと専用ロックオン制御を接続します。</summary>
    private void Awake()
    {


        // パラメータの生成を行う
        if(m_parameterAsset == null)
        {
            Debug.LogError("PlayerCameraのPlayerCameraParameterAsset参照が未設定です。", this);
            return;
        }
        m_parameter = m_parameterAsset.CreateCameraParameter();

        // パラメータの設定を行う
        m_chargeCameraDirectionInfluence = m_parameter.m_chargeCameraDirectionInfluence;
        m_chargeCameraVerticalAngle = m_parameter.m_chargeCameraVerticalAngle;
        m_chargeCameraVerticalTurnSpeed = m_parameter.m_chargeCameraVerticalTurnSpeed;



        if (m_cinemachineCamera == null)
        {
            Debug.LogError("PlayerCameraのCinemachineCamera参照が未設定です。", this);
            return;
        }

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
    /// 進行方向を向かせるオーバーライドを開始します。
    /// </summary>
    public void BeginDriftLookOverride()
    {
        m_isDriftOverrideActive = true;

        if (m_inputAxisController != null)
        {
            m_inputAxisController.enabled = false;
        }

        if (m_orbitalFollow != null)
        {
            m_overrideStartAngle =
                m_orbitalFollow.HorizontalAxis.Value;
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
    /// 進行方向へカメラを向けます。
    /// </summary>
    public void UpdateDriftLookDirection(
        Vector3 worldDirection,
        float blendRate,
        float turnSpeedDegreesPerSecond,
        float deltaTime)
    {
        if (!m_isDriftOverrideActive ||
            m_orbitalFollow == null)
        {
            return;
        }

        worldDirection.y = 0.0f;

        if (worldDirection.sqrMagnitude <= MIN_TARGET_DISTANCE)
        {
            return;
        }

        worldDirection.Normalize();

        float directionAngle =
            Mathf.Atan2(
                worldDirection.x,
                worldDirection.z) *
            Mathf.Rad2Deg;

        float clampedBlendRate =
            Mathf.Clamp01(blendRate) *
            m_chargeCameraDirectionInfluence;

        float targetAngle =
            Mathf.MoveTowardsAngle(
                m_overrideStartAngle,
                directionAngle,
                Mathf.Abs(
                    Mathf.DeltaAngle(
                        m_overrideStartAngle,
                        directionAngle)) *
                clampedBlendRate);

        float currentAngle =
            m_orbitalFollow.HorizontalAxis.Value;

        float nextAngle =
            Mathf.MoveTowardsAngle(
                currentAngle,
                targetAngle,
                Mathf.Max(turnSpeedDegreesPerSecond, 0.0f) *
                deltaTime);

        m_orbitalFollow.HorizontalAxis.Value =
            nextAngle;

        UpdateChargeCameraHeight(deltaTime);
    }

    /// <summary>
    /// 残り時間に応じて水平角度を補間し、時間終了時には最新の目標方向へ合わせます。
    /// </summary>
    /// <param name="worldDirection">カメラが向くワールド方向。</param>
    /// <param name="remainingTime">呼び出し側で減算する、到達までの残り時間（秒）。0以下で即座に向きます。</param>
    /// <param name="deltaTime">今回の更新で進める時間（秒）。</param>
    public void UpdateDriftLookDirectionOverTime(
        Vector3 worldDirection,
        float remainingTime,
        float deltaTime)
    {
        if (!m_isDriftOverrideActive || m_orbitalFollow == null)
        {
            return;
        }

        worldDirection.y = 0.0f;
        if (worldDirection.sqrMagnitude <= MIN_TARGET_DISTANCE)
        {
            return;
        }

        float directionAngle = Mathf.Atan2(worldDirection.x, worldDirection.z) * Mathf.Rad2Deg;
        float targetAngle = Mathf.LerpAngle(
            m_overrideStartAngle,
            directionAngle,
            m_chargeCameraDirectionInfluence);
        float progress = remainingTime > 0.0f
            ? Mathf.Clamp01(Mathf.Max(deltaTime, 0.0f) / remainingTime)
            : 1.0f;

        // 毎回一定割合で近づけると到達しないため、残り時間に対する割合で補間する。
        m_orbitalFollow.HorizontalAxis.Value = Mathf.LerpAngle(
            m_orbitalFollow.HorizontalAxis.Value, targetAngle, progress);
        UpdateChargeCameraHeight(deltaTime);
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
}
