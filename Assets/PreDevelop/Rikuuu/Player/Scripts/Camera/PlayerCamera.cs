using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// プレイヤーのカメラ制御を行います。
/// 通常時はCinemachineInputAxisControllerによる
/// マウス・スティック操作を受け付けます。
/// ロックオン中はCinemachineRotationComposerを使用して
/// 指定されたターゲット方向へカメラを向けつつ、対象とプレイヤーを一直線上に収めます。
/// </summary>
public class PlayerCamera : MonoBehaviour
{
    private const float MIN_TARGET_DISTANCE = 0.0001f;

    [Header("Camera")]

    [SerializeField, Min(0.1f)]
    private float sensitivity = 1.0f;

    [SerializeField]
    private CinemachineCamera m_cinemachineCamera;

    [SerializeField]
    private CinemachineInputAxisController m_inputAxisController;

    private CinemachineOrbitalFollow m_orbitalFollow;
    private CinemachineRotationComposer m_rotationComposer;

    private bool m_isDriftOverrideActive;
    private float m_overrideStartAngle;

    private Transform m_targetLookTransform;
    private Tween m_targetLookTween;

    /// <summary>
    /// 進行方向オーバーライドが有効かどうかを取得します。
    /// </summary>
    public bool IsDriftOverrideActive =>
        m_isDriftOverrideActive;

    /// <summary>
    /// 初期化を行います。
    /// </summary>
    private void Awake()
    {
        ResolveCameraComponents();
        CreateTargetLookTransform();
    }

    /// <summary>
    /// Cinemachineの各コンポーネントへの参照を取得します。
    /// </summary>
    private void ResolveCameraComponents()
    {
        if (m_cinemachineCamera == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerCamera)}] " +
                $"{nameof(CinemachineCamera)}が設定されていません。",
                this);

            return;
        }

        m_orbitalFollow =
            m_cinemachineCamera.GetComponent<CinemachineOrbitalFollow>();

        if (m_orbitalFollow == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerCamera)}] " +
                $"{nameof(CinemachineOrbitalFollow)}が見つかりません。",
                this);
        }

        m_rotationComposer =
            m_cinemachineCamera.GetComponent<CinemachineRotationComposer>();

        if (m_rotationComposer == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerCamera)}] " +
                $"{nameof(CinemachineRotationComposer)}が見つかりません。",
                this);
        }
    }

    /// <summary>
    /// ロックオン時にカメラが見るためのTransformを作成します。
    /// </summary>
    private void CreateTargetLookTransform()
    {
        GameObject targetLookObject =
            new GameObject("TargetCameraLookPoint");

        targetLookObject.transform.SetParent(transform);
        targetLookObject.transform.localPosition = Vector3.zero;
        targetLookObject.transform.localRotation = Quaternion.identity;

        m_targetLookTransform =
            targetLookObject.transform;

        targetLookObject.SetActive(false);
    }

    /// <summary>
    /// 追従対象（プレイヤー）の現在位置を取得します。
    /// </summary>
    private Vector3 GetTrackingPosition()
    {
        if (m_cinemachineCamera != null && m_cinemachineCamera.Target.TrackingTarget != null)
        {
            return m_cinemachineCamera.Target.TrackingTarget.position;
        }
        return transform.position;
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

        if (m_inputAxisController != null)
        {
            m_inputAxisController.enabled = true;
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
            Mathf.Clamp01(blendRate);

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

    /// <summary>
    /// 指定したターゲットへカメラをロックオンします。
    /// </summary>
    public void BeginTargetLock(
        Transform target,
        float duration)
    {
        if (target == null ||
            m_cinemachineCamera == null ||
            m_rotationComposer == null ||
            m_targetLookTransform == null)
        {
            return;
        }

        m_targetLookTween?.Kill();

        Vector3 playerPos = GetTrackingPosition();
        Vector3 targetLookPos = Vector3.Lerp(playerPos, target.position, 0.5f);

        // ★修正点: カメラが「現在向いている方向」の注視点を初期位置にして即座標を更新する
        // これにより、一瞬古い位置や原点を向いてしまう違和感を防止します
        float targetDistance = Vector3.Distance(playerPos, targetLookPos);
        Vector3 startPosition = playerPos + m_cinemachineCamera.transform.forward * targetDistance;

        m_targetLookTransform.position = startPosition;
        m_targetLookTransform.gameObject.SetActive(true);

        SetCameraLookTarget(m_targetLookTransform);

        m_targetLookTween =
            DOTween.To(
                () => 0.0f,
                progress =>
                {
                    if (target == null)
                    {
                        return;
                    }

                    Vector3 currentPlayerPos = GetTrackingPosition();
                    Vector3 currentTargetLookPos = Vector3.Lerp(currentPlayerPos, target.position, 0.5f);

                    m_targetLookTransform.position =
                        Vector3.Lerp(
                            startPosition,
                            currentTargetLookPos,
                            progress);
                },
                1.0f,
                Mathf.Max(duration, 0.0f))
            .SetEase(Ease.OutCubic);
    }

    /// <summary>
    /// ロックオン中のカメラターゲットを更新します。
    /// </summary>
    public void UpdateTargetLock(Transform target)
    {
        if (target == null ||
            m_targetLookTransform == null)
        {
            return;
        }

        Vector3 playerPos = GetTrackingPosition();
        Vector3 targetLookPos = Vector3.Lerp(playerPos, target.position, 0.5f);

        m_targetLookTransform.position =
            Vector3.Lerp(
                m_targetLookTransform.position,
                targetLookPos,
                Time.deltaTime * 12.0f);

        if (m_orbitalFollow != null)
        {
            Vector3 dirToTarget = target.position - playerPos;
            dirToTarget.y = 0.0f;

            if (dirToTarget.sqrMagnitude > MIN_TARGET_DISTANCE)
            {
                float targetAngle = Mathf.Atan2(dirToTarget.x, dirToTarget.z) * Mathf.Rad2Deg;
                float currentAngle = m_orbitalFollow.HorizontalAxis.Value;

                m_orbitalFollow.HorizontalAxis.Value =
                    Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * 10.0f);
            }
        }
    }

    /// <summary>
    /// ロックオンを解除します。
    /// </summary>
    public void EndTargetLock(float duration)
    {
        m_targetLookTween?.Kill();
        m_targetLookTween = null;

        ClearCameraLookTarget();
    }

    private void SetCameraLookTarget(Transform target)
    {
        CameraTarget cameraTarget =
            m_cinemachineCamera.Target;

        cameraTarget.CustomLookAtTarget = true;
        cameraTarget.LookAtTarget = target;

        m_cinemachineCamera.Target =
            cameraTarget;

        if (m_inputAxisController != null)
        {
            m_inputAxisController.enabled = false;
        }
    }

    private void ClearCameraLookTarget()
    {
        CameraTarget cameraTarget =
            m_cinemachineCamera.Target;

        cameraTarget.CustomLookAtTarget = false;
        cameraTarget.LookAtTarget = null;

        m_cinemachineCamera.Target =
            cameraTarget;

        m_targetLookTransform.gameObject.SetActive(false);

        if (m_inputAxisController != null)
        {
            m_inputAxisController.enabled = true;
        }
    }

    /// <summary>
    /// 指定したターゲットへの注視を開始します。
    /// </summary>
    public void BeginTargetFocus(
        Transform target,
        float duration)
    {
        if (target == null ||
            m_cinemachineCamera == null ||
            m_targetLookTransform == null)
        {
            return;
        }

        m_targetLookTween?.Kill();

        Vector3 playerPos = GetTrackingPosition();
        Vector3 targetLookPos = Vector3.Lerp(playerPos, target.position, 0.5f);

        // ★修正点: BeginTargetLock同様、現在カメラの向きを初期座標としてセットする
        float targetDistance = Vector3.Distance(playerPos, targetLookPos);
        Vector3 startPosition = playerPos + m_cinemachineCamera.transform.forward * targetDistance;

        m_targetLookTransform.position = startPosition;
        m_targetLookTransform.gameObject.SetActive(true);

        SetCameraLookTarget(m_targetLookTransform);

        m_targetLookTween =
            DOTween.To(
                () => 0.0f,
                progress =>
                {
                    if (target == null)
                    {
                        return;
                    }

                    Vector3 currentPlayerPos = GetTrackingPosition();
                    Vector3 currentTargetLookPos = Vector3.Lerp(currentPlayerPos, target.position, 0.5f);

                    m_targetLookTransform.position =
                        Vector3.Lerp(
                            startPosition,
                            currentTargetLookPos,
                            progress);
                },
                1.0f,
                Mathf.Max(duration, 0.0f))
            .SetEase(Ease.OutCubic);
    }

    /// <summary>
    /// 注視中のターゲットを追従します。
    /// </summary>
    public void UpdateTargetFocus(Transform target)
    {
        if (target == null ||
            m_targetLookTransform == null ||
            !m_targetLookTransform.gameObject.activeSelf)
        {
            return;
        }

        Vector3 playerPos = GetTrackingPosition();
        Vector3 targetLookPos = Vector3.Lerp(playerPos, target.position, 0.5f);

        m_targetLookTransform.position =
            Vector3.Lerp(
                m_targetLookTransform.position,
                targetLookPos,
                Time.deltaTime * 15.0f);

        if (m_orbitalFollow != null)
        {
            Vector3 dirToTarget = target.position - playerPos;
            dirToTarget.y = 0.0f;

            if (dirToTarget.sqrMagnitude > MIN_TARGET_DISTANCE)
            {
                float targetAngle = Mathf.Atan2(dirToTarget.x, dirToTarget.z) * Mathf.Rad2Deg;
                m_orbitalFollow.HorizontalAxis.Value =
                    Mathf.LerpAngle(m_orbitalFollow.HorizontalAxis.Value, targetAngle, Time.deltaTime * 10.0f);
            }
        }
    }

    /// <summary>
    /// ターゲットへの注視を終了します。
    /// </summary>
    public void EndTargetFocus()
    {
        m_targetLookTween?.Kill();
        m_targetLookTween = null;

        ClearCameraLookTarget();
    }

    private void OnDestroy()
    {
        m_targetLookTween?.Kill();
    }
}