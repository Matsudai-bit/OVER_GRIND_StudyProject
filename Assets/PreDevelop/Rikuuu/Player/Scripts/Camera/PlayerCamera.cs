using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// プレイヤーのカメラ制御を行います。
/// 通常時はCinemachineInputAxisControllerによる
/// マウス・スティック操作を受け付けます。
/// ロックオン中はCinemachineRotationComposerを使用して
/// 指定されたターゲット方向へカメラを向けます。
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
    /// <param name="worldDirection">向かせたいワールド方向。</param>
    /// <param name="blendRate">ブレンド割合。</param>
    /// <param name="turnSpeedDegreesPerSecond">回転速度。</param>
    /// <param name="deltaTime">経過時間。</param>
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
    /// <param name="worldDirection">向かせたいワールド方向。</param>
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
    /// プレイヤーを追従対象にしたまま、
    /// Rotation Composerだけをターゲット方向へ向けます。
    /// </summary>
    /// <param name="target">ロックオン対象。</param>
    /// <param name="duration">カメラ移動時間。</param>
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

        Vector3 startPosition =
            m_targetLookTransform.position;

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

                    m_targetLookTransform.position =
                        Vector3.Lerp(
                            startPosition,
                            target.position,
                            progress);
                },
                1.0f,
                Mathf.Max(duration, 0.0f))
            .SetEase(Ease.OutCubic);
    }

    /// <summary>
    /// ロックオン中のカメラターゲットを更新します。
    /// </summary>
    /// <param name="target">現在のターゲット。</param>
    public void UpdateTargetLock(Transform target)
    {
        if (target == null ||
            m_targetLookTransform == null)
        {
            return;
        }

        m_targetLookTransform.position =
            Vector3.Lerp(
                m_targetLookTransform.position,
                target.position,
                Time.deltaTime * 12.0f);
    }

    /// <summary>
    /// ロックオンを解除します。
    /// カメラを別方向へ移動させず、
    /// 現在のカメラ状態から直接通常操作へ戻します。
    /// </summary>
    /// <param name="duration">互換性のために保持している引数です。</param>
    public void EndTargetLock(float duration)
    {
        m_targetLookTween?.Kill();
        m_targetLookTween = null;

        ClearCameraLookTarget();
    }

    /// <summary>
    /// CinemachineCameraのLook Atターゲットを設定します。
    /// </summary>
    /// <param name="target">Look Atターゲット。</param>
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

    /// <summary>
    /// CinemachineCameraのLook Atターゲットを解除します。
    /// </summary>
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
    /// Xボタンを押している間はターゲットを注視し続けます。
    /// </summary>
    /// <param name="target">注視するターゲット。</param>
    /// <param name="duration">注視方向へ移動する時間。</param>
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

        m_targetLookTransform.gameObject.SetActive(true);

        SetCameraLookTarget(m_targetLookTransform);

        Vector3 startPosition =
            m_targetLookTransform.position;

        m_targetLookTween =
            DOTween.To(
                () => 0.0f,
                progress =>
                {
                    if (target == null)
                    {
                        return;
                    }

                    m_targetLookTransform.position =
                        Vector3.Lerp(
                            startPosition,
                            target.position,
                            progress);
                },
                1.0f,
                Mathf.Max(duration, 0.0f))
            .SetEase(Ease.OutCubic);
    }

    /// <summary>
    /// 注視中のターゲットを追従します。
    /// </summary>
    /// <param name="target">現在のターゲット。</param>
    public void UpdateTargetFocus(Transform target)
    {
        if (target == null ||
            m_targetLookTransform == null ||
            !m_targetLookTransform.gameObject.activeSelf)
        {
            return;
        }

        m_targetLookTransform.position =
            Vector3.Lerp(
                m_targetLookTransform.position,
                target.position,
                Time.deltaTime * 15.0f);
    }

    /// <summary>
    /// ターゲットへの注視を終了し、
    /// 通常のカメラ操作へ戻します。
    /// </summary>
    public void EndTargetFocus()
    {
        m_targetLookTween?.Kill();
        m_targetLookTween = null;

        ClearCameraLookTarget();
    }
    /// <summary>
    /// オブジェクト破棄時にTweenを停止します。
    /// </summary>
    private void OnDestroy()
    {
        m_targetLookTween?.Kill();
    }
}