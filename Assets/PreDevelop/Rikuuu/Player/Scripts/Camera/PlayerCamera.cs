using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// プレイヤーのカメラ制御を行います。
/// 通常時はCinemachineInputAxisControllerによる
/// マウス・スティック操作を受け付けます。
/// ロックオン中はCinemachineOrbitalFollowの軌道角度(HorizontalAxis)を
/// 「Target→Playerの延長線上」と「Playerの移動方向」をブレンドした角度に配置することで
/// Playerの斜め後ろにカメラを保ちつつ、CinemachineRotationComposerで
/// 対象とプレイヤーを一直線上に収めます。
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

    [Header("Lock-On Look At")]

    [SerializeField, Min(0.1f)]
    [Tooltip("ロックオン中の注視点(Player-Target中間点)とカメラの最低距離。" +
             "これより近づくと注視点をカメラから離す方向へ補正し、" +
             "急激な向きの変化や画面がPlayerで埋まる現象を防ぎます。")]
    private float minLookAtDistanceFromCamera = 2.0f;

    [Header("Lock-On Orbit")]

    [SerializeField, Min(1.0f)]
    [Tooltip("ロックオン中、カメラの軌道角度(位置)を目標角度へ追従させる速度[度/秒]。" +
             "この値が遅いと、ロックオン開始直後や斜め移動時にカメラ位置の追従が" +
             "間に合わずPlayer/Targetが画面外へ出る原因になります。")]
    private float lockOnOrbitTurnSpeedDegreesPerSecond = 480.0f;

    [SerializeField, Range(0.0f, 1.0f)]
    [Tooltip("ロックオン中の軌道角度計算に、Playerの移動方向をどれだけ加味するか。" +
             "0だとTarget方向のみで角度を決定するため、後退・斜め後退移動時に" +
             "Playerの背面がカメラに向かず全身が見えづらくなります。" +
             "値を上げるほどPlayerの移動方向側へカメラが回り込み、全身が収まりやすくなります。")]
    private float lockOnMoveDirectionInfluence = 0.4f;

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
    /// PlayerとTargetの中間点を、カメラから一定距離以上離れるようクランプして算出します。
    /// ロックオン中に注視点がカメラへ接近しすぎると、わずかな移動で向きが激変し、
    /// 画面がPlayerで埋まる／視界外へ振り切れる原因になるため、ここで補正します。
    /// </summary>
    private Vector3 ComputeClampedTargetLookPosition(
        Vector3 playerPos,
        Vector3 targetPos)
    {
        Vector3 midPoint = Vector3.Lerp(playerPos, targetPos, 0.5f);

        if (m_cinemachineCamera == null)
        {
            return midPoint;
        }

        Vector3 cameraPosition = m_cinemachineCamera.transform.position;
        Vector3 offsetFromCamera = midPoint - cameraPosition;
        float distanceFromCamera = offsetFromCamera.magnitude;

        if (distanceFromCamera < minLookAtDistanceFromCamera)
        {
            Vector3 direction =
                distanceFromCamera > MIN_TARGET_DISTANCE
                    ? offsetFromCamera / distanceFromCamera
                    : m_cinemachineCamera.transform.forward;

            midPoint =
                cameraPosition +
                direction * minLookAtDistanceFromCamera;
        }

        return midPoint;
    }

    /// <summary>
    /// ロックオン中のカメラ軌道角度の目標値を、
    /// 「Target方向」と「Playerの移動方向」をブレンドして算出します。
    /// Target方向のみを基準にすると、Playerが後退・斜め後退で移動した際に
    /// カメラがPlayerの背面へ回り込めず、全身が見えづらくなるため、
    /// 移動方向を一部加味してカメラが追随しやすいようにします。
    /// </summary>
    private float ComputeLockOnOrbitTargetAngle(
        Vector3 dirToTarget,
        Vector3 playerMoveDirection)
    {
        float targetDirAngle =
            Mathf.Atan2(dirToTarget.x, dirToTarget.z) *
            Mathf.Rad2Deg;

        Vector3 flatMoveDirection = playerMoveDirection;
        flatMoveDirection.y = 0.0f;

        if (flatMoveDirection.sqrMagnitude <= MIN_TARGET_DISTANCE ||
            lockOnMoveDirectionInfluence <= 0.0f)
        {
            return targetDirAngle;
        }

        float moveDirAngle =
            Mathf.Atan2(flatMoveDirection.x, flatMoveDirection.z) *
            Mathf.Rad2Deg;

        return Mathf.LerpAngle(
            targetDirAngle,
            moveDirAngle,
            lockOnMoveDirectionInfluence);
    }

    /// <summary>
    /// カメラの軌道角度(HorizontalAxis)を、Target方向とPlayer移動方向を
    /// ブレンドした角度へ即座にスナップさせます。ロックオン開始直後にカメラ位置が
    /// 目標角度と大きくズレていると、追従が間に合わず画面外へ出てしまうため、
    /// 開始時点で位置を合わせておく必要があります。
    /// </summary>
    private void SnapLockOnOrbitHeading(
        Vector3 playerPos,
        Vector3 targetPos,
        Vector3 playerMoveDirection)
    {
        if (m_orbitalFollow == null)
        {
            return;
        }

        Vector3 dirToTarget = targetPos - playerPos;
        dirToTarget.y = 0.0f;

        if (dirToTarget.sqrMagnitude <= MIN_TARGET_DISTANCE)
        {
            return;
        }

        m_orbitalFollow.HorizontalAxis.Value =
            ComputeLockOnOrbitTargetAngle(dirToTarget, playerMoveDirection);
    }

    /// <summary>
    /// カメラの軌道角度(HorizontalAxis)を、Target方向とPlayer移動方向を
    /// ブレンドした角度へ一定速度[度/秒]で追従させます。
    /// 位置(Position)を担うOrbitalFollowを直接動かすことで、
    /// 移動方向に応じてPlayerの斜め後ろにカメラを保ち続けます。
    /// </summary>
    private void UpdateLockOnOrbitHeading(
        Vector3 playerPos,
        Vector3 targetPos,
        Vector3 playerMoveDirection,
        float deltaTime)
    {
        if (m_orbitalFollow == null)
        {
            return;
        }

        Vector3 dirToTarget = targetPos - playerPos;
        dirToTarget.y = 0.0f;

        if (dirToTarget.sqrMagnitude <= MIN_TARGET_DISTANCE)
        {
            return;
        }

        float targetAngle =
            ComputeLockOnOrbitTargetAngle(dirToTarget, playerMoveDirection);

        m_orbitalFollow.HorizontalAxis.Value =
            Mathf.MoveTowardsAngle(
                m_orbitalFollow.HorizontalAxis.Value,
                targetAngle,
                lockOnOrbitTurnSpeedDegreesPerSecond * deltaTime);
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

        float targetAngle = Mathf.Atan2(worldDirection.x, worldDirection.z) * Mathf.Rad2Deg;
        float progress = remainingTime > 0.0f
            ? Mathf.Clamp01(Mathf.Max(deltaTime, 0.0f) / remainingTime)
            : 1.0f;

        // 毎回一定割合で近づけると到達しないため、残り時間に対する割合で補間する。
        m_orbitalFollow.HorizontalAxis.Value = Mathf.LerpAngle(
            m_orbitalFollow.HorizontalAxis.Value, targetAngle, progress);
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
    /// <param name="target">ロックオン対象。</param>
    /// <param name="duration">注視点が中間位置へ遷移するまでの時間。</param>
    /// <param name="playerMoveDirection">
    /// ロックオン開始時点のPlayer移動方向(ワールド空間)。
    /// 未指定の場合はTarget方向のみでカメラ位置を決定します。
    /// </param>
    public void BeginTargetLock(
        Transform target,
        float duration,
        Vector3 playerMoveDirection = default)
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

        // ★修正点: カメラの軌道位置を「Target方向 + 移動方向」の目標角度へ即座にスナップする
        // これを行わないと、離れた敵をロックした瞬間に注視点だけが先に動き、
        // カメラ位置の追従が間に合わず画面外に出てしまいます
        SnapLockOnOrbitHeading(playerPos, target.position, playerMoveDirection);

        Vector3 targetLookPos = ComputeClampedTargetLookPosition(playerPos, target.position);

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
                    Vector3 currentTargetLookPos = ComputeClampedTargetLookPosition(currentPlayerPos, target.position);

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
    /// <param name="target">ロックオン対象。</param>
    /// <param name="playerMoveDirection">
    /// 現在のPlayer移動方向(ワールド空間、正規化不要)。
    /// 後退・斜め後退移動時にPlayerの全身が画面内に収まるよう、
    /// カメラ軌道角度の計算に反映されます。未指定の場合はTarget方向のみで決定します。
    /// </param>
    public void UpdateTargetLock(
        Transform target,
        Vector3 playerMoveDirection = default)
    {
        if (target == null ||
            m_targetLookTransform == null)
        {
            return;
        }

        Vector3 playerPos = GetTrackingPosition();
        Vector3 targetLookPos = ComputeClampedTargetLookPosition(playerPos, target.position);

        m_targetLookTransform.position =
            Vector3.Lerp(
                m_targetLookTransform.position,
                targetLookPos,
                Time.deltaTime * 12.0f);

        // ★修正点: Target方向だけでなくPlayerの移動方向もブレンドしてカメラ軌道角度を追従させる
        // これにより後退・斜め後退移動時にもカメラがPlayerの背後寄りへ回り込みやすくなります
        UpdateLockOnOrbitHeading(playerPos, target.position, playerMoveDirection, Time.deltaTime);
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
    /// <param name="target">注視対象。</param>
    /// <param name="duration">注視点が中間位置へ遷移するまでの時間。</param>
    /// <param name="playerMoveDirection">
    /// 注視開始時点のPlayer移動方向(ワールド空間)。
    /// 未指定の場合はTarget方向のみでカメラ位置を決定します。
    /// </param>
    public void BeginTargetFocus(
        Transform target,
        float duration,
        Vector3 playerMoveDirection = default)
    {
        if (target == null ||
            m_cinemachineCamera == null ||
            m_targetLookTransform == null)
        {
            return;
        }

        m_targetLookTween?.Kill();

        Vector3 playerPos = GetTrackingPosition();

        // ★修正点: BeginTargetLock同様、カメラ軌道位置を即座にスナップする
        SnapLockOnOrbitHeading(playerPos, target.position, playerMoveDirection);

        Vector3 targetLookPos = ComputeClampedTargetLookPosition(playerPos, target.position);

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
                    Vector3 currentTargetLookPos = ComputeClampedTargetLookPosition(currentPlayerPos, target.position);

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
    /// <param name="target">注視対象。</param>
    /// <param name="playerMoveDirection">
    /// 現在のPlayer移動方向(ワールド空間、正規化不要)。
    /// UpdateTargetLock同様、カメラ軌道角度の計算に反映されます。
    /// </param>
    public void UpdateTargetFocus(
        Transform target,
        Vector3 playerMoveDirection = default)
    {
        if (target == null ||
            m_targetLookTransform == null ||
            !m_targetLookTransform.gameObject.activeSelf)
        {
            return;
        }

        Vector3 playerPos = GetTrackingPosition();
        Vector3 targetLookPos = ComputeClampedTargetLookPosition(playerPos, target.position);

        m_targetLookTransform.position =
            Vector3.Lerp(
                m_targetLookTransform.position,
                targetLookPos,
                Time.deltaTime * 15.0f);

        // ★修正点: UpdateTargetLockと同様、移動方向を加味してカメラ軌道位置を追従させる
        UpdateLockOnOrbitHeading(playerPos, target.position, playerMoveDirection, Time.deltaTime);
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
