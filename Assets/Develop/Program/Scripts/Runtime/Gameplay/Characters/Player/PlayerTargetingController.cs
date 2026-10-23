using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤーのターゲット注視機能を管理します。
/// 右スティック押し込みでターゲット機能を切り替え、
/// ターゲット中は右スティック左右入力で対象を切り替えます。
/// </summary>
public sealed class PlayerTargetingController : MonoBehaviour
{
    private const float MIN_SEARCH_DISTANCE = 0.0001f;

    [SerializeField, Header("Parameter Asset")]
    private PlayerTargetingControllerParameterAsset m_parameterAsset;

    [SerializeField, Header("Target Search")]
    [Tooltip("シーン内のTargetPointと個別の選択可能距離を管理するManagerです。")]
    private TargetPointManager m_targetPointManager;

    [Header("Target Reticle")]

    [SerializeField]
    private TargetReticleBillboard m_targetReticlePrefab;

    [SerializeField, Min(0.0f)]
    private float m_reticleDistanceFromTarget = 0.15f;

    [SerializeField, Min(0.0f)]
    private float m_reticleMoveDuration = 0.2f;

    [Header("Camera")]

    [SerializeField]
    private PlayerCamera m_playerCamera;

    [SerializeField, Min(0.05f)]
    [Tooltip("ロックオンボタンを押してから、ロックオン構図へ到達するまでの時間（秒）。")]
    private float m_cameraFocusDuration = 0.25f;

    [SerializeField]
    [Tooltip("Playerの移動方向を取得するためのMotor参照。" +
             "カメラがTarget方向だけでなくPlayerの移動方向も考慮して" +
             "位置を決定できるようにするために使用します。未設定の場合は移動方向を渡しません。")]
    private PlayerMotor m_playerMotor;

    private readonly List<TargetPoint> m_targetCandidates = new();

    private InputAction m_targetAction;
    private InputAction m_targetLeftAction;
    private InputAction m_targetRightAction;

    private TargetPoint m_currentTargetPoint;

    private TargetReticleBillboard m_targetReticle;
    private Tween m_reticleTween;

    private bool m_isTargeting;

    /// <summary>
    /// 現在ターゲット中の対象Transformを取得します。
    /// </summary>
    /// <returns>
    /// ターゲット中かつ対象が有効な場合は対象Transform、
    /// それ以外の場合はnull。
    /// </returns>
    public Transform GetCurrentTargetTransform()
    {
        if (!m_isTargeting)
        {
            return null;
        }

        TargetPoint targetPoint = GetCurrentTargetPoint();
        return targetPoint != null
            ? targetPoint.TargetTransform
            : null;
    }

    /// <summary>
    /// 初期化を行います。
    /// </summary>
    private void Awake()
    {
        ApplyParameterAsset();
        ResolveInputActions();

        if (m_targetReticlePrefab != null)
        {
            m_targetReticle =
                Instantiate(m_targetReticlePrefab);

            m_targetReticle.gameObject.SetActive(false);
        }
    }

    /// <summary>設定済みの場合にParameterAssetの値を適用します。</summary>
    private void ApplyParameterAsset()
    {
        if (m_parameterAsset == null)
        {
            return;
        }

        m_targetReticlePrefab = m_parameterAsset.TargetReticlePrefab;
        m_reticleDistanceFromTarget = m_parameterAsset.ReticleDistanceFromTarget;
        m_reticleMoveDuration = m_parameterAsset.ReticleMoveDuration;
        m_cameraFocusDuration = m_parameterAsset.CameraFocusDuration;
    }

    /// <summary>
    /// 現在のPlayerの移動方向を取得します。
    /// m_playerMotorが未設定の場合はVector3.zeroを返し、
    /// PlayerCamera側はTarget方向のみで角度を決定します。
    /// </summary>
    private Vector3 GetPlayerMoveDirection()
    {
        return
            m_playerMotor != null
                ? m_playerMotor.HorizontalDirection
                : Vector3.zero;
    }

    /// <summary>
    /// ターゲット中の対象を追従します。
    /// </summary>
    private void LateUpdate()
    {
        if (!m_isTargeting)
        {
            return;
        }

        TargetPoint targetPoint =
            GetCurrentTargetPoint();

        if (targetPoint == null)
        {
            StopTargeting();
            return;
        }

        if (m_targetReticle != null)
        {
            Vector3 targetPosition =
                GetReticlePosition(
                    targetPoint.TargetTransform);

            m_targetReticle.transform.position =
                Vector3.Lerp(
                    m_targetReticle.transform.position,
                    targetPosition,
                    Time.deltaTime * 15.0f);
        }

        if (m_playerCamera != null)
        {
            // ★修正点: Playerの移動方向を渡すことで、
            // 後退・斜め後退移動時にもカメラがPlayerの背後寄りへ回り込みやすくする
            m_playerCamera.UpdateTargetFocus(
                targetPoint.TargetTransform,
                GetPlayerMoveDirection());
        }
    }

    /// <summary>
    /// Project-wide Input Actionsから入力を取得します。
    /// </summary>
    private void ResolveInputActions()
    {
        InputActionMap playerActionMap =
            InputSystem.actions.FindActionMap(
                "Player",
                true);

        m_targetAction =
            playerActionMap.FindAction(
                "Target",
                true);

        m_targetLeftAction =
            playerActionMap.FindAction(
                "TargetLeft",
                true);

        m_targetRightAction =
            playerActionMap.FindAction(
                "TargetRight",
                true);
    }

    /// <summary>
    /// 入力イベントを登録します。
    /// </summary>
    private void OnEnable()
    {
        m_targetAction.performed += OnTargetPerformed;

        m_targetLeftAction.performed += OnTargetLeftPerformed;
        m_targetRightAction.performed += OnTargetRightPerformed;
    }

    /// <summary>
    /// 入力イベントを解除します。
    /// </summary>
    private void OnDisable()
    {
        StopTargeting();
        m_targetAction.performed -= OnTargetPerformed;

        m_targetLeftAction.performed -= OnTargetLeftPerformed;
        m_targetRightAction.performed -= OnTargetRightPerformed;
    }

    /// <summary>
    /// 右スティックを押し込んだときにターゲット機能を切り替えます。
    /// </summary>
    /// <param name="context">入力コンテキスト。</param>
    private void OnTargetPerformed(
        InputAction.CallbackContext context)
    {
        if (m_isTargeting)
        {
            StopTargeting();
            return;
        }

        StartTargeting();
    }

    /// <summary>
    /// 右スティック押し込みでターゲットを開始します。
    /// </summary>
    private void StartTargeting()
    {
        if (m_isTargeting)
        {
            return;
        }

        if (m_targetPointManager == null)
        {
            Debug.LogWarning(
                $"{nameof(TargetPointManager)}が設定されていません。",
                this);
            return;
        }

        m_targetPointManager.GetTargetablePoints(
            transform.position,
            m_targetCandidates);

        if (m_targetCandidates.Count == 0)
        {
            return;
        }

        m_currentTargetPoint =
            FindClosestTargetToScreenCenter(Camera.main);

        if (m_currentTargetPoint == null)
        {
            return;
        }

        m_isTargeting = true;

        TargetPoint targetPoint =
            GetCurrentTargetPoint();

        if (targetPoint == null)
        {
            StopTargeting();
            return;
        }

        ShowTargetReticle(
            targetPoint.TargetTransform,
            false);

        if (m_playerCamera != null)
        {
            // ★修正点: 開始時点の移動方向も渡し、カメラ軌道の初期スナップに反映する
            m_playerCamera.BeginTargetFocus(
                targetPoint.TargetTransform,
                m_cameraFocusDuration,
                GetPlayerMoveDirection());
        }
    }

    /// <summary>
    /// 右スティック押し込みでターゲットを終了します。
    /// </summary>
    private void StopTargeting()
    {
        if (!m_isTargeting)
        {
            return;
        }

        m_isTargeting = false;

        m_currentTargetPoint = null;
        m_targetCandidates.Clear();

        HideTargetReticle();

        if (m_playerCamera != null)
        {
            m_playerCamera.EndTargetFocus();
        }
    }

    /// <summary>
    /// 右スティックを左へ倒してTargetPointを切り替えます。
    /// </summary>
    /// <param name="context">入力コンテキスト。</param>
    private void OnTargetLeftPerformed(
        InputAction.CallbackContext context)
    {
        if (!m_isTargeting)
        {
            return;
        }

        ChangeTargetPoint(-1);
    }

    /// <summary>
    /// 右スティックを右へ倒してTargetPointを切り替えます。
    /// </summary>
    /// <param name="context">入力コンテキスト。</param>
    private void OnTargetRightPerformed(
        InputAction.CallbackContext context)
    {
        if (!m_isTargeting)
        {
            return;
        }

        ChangeTargetPoint(1);
    }

    /// <summary>
    /// TargetPointを指定した方向へ切り替えます。
    /// </summary>
    /// <param name="direction">
    /// 左の場合は-1、右の場合は1を指定します。
    /// </param>
    private void ChangeTargetPoint(int direction)
    {
        if (m_targetPointManager == null ||
            m_currentTargetPoint == null ||
            direction == 0)
        {
            return;
        }

        m_targetPointManager.GetTargetablePoints(
            transform.position,
            m_targetCandidates);

        if (m_targetCandidates.Count == 0)
        {
            StopTargeting();
            return;
        }

        TargetPoint nextTargetPoint =
            FindClosestTargetInScreenDirection(
                Camera.main,
                direction);

        if (nextTargetPoint == null)
        {
            return;
        }

        m_currentTargetPoint = nextTargetPoint;
        TargetPoint targetPoint = m_currentTargetPoint;

        if (targetPoint == null)
        {
            return;
        }

        ShowTargetReticle(
            targetPoint.TargetTransform,
            true);

        if (m_playerCamera != null)
        {
            // ★修正点: TargetPoint切り替え時も移動方向を渡す
            m_playerCamera.BeginTargetFocus(
                targetPoint.TargetTransform,
                m_cameraFocusDuration,
                GetPlayerMoveDirection());
        }
    }

    /// <summary>
    /// 画面中央との2D距離が最も近いTargetPointを取得します。
    /// カメラ後方にある対象は選択しません。
    /// </summary>
    /// <param name="targetCamera">スクリーン座標への変換に使用するカメラ。</param>
    /// <returns>画面中央に最も近いTargetPoint。選択可能な対象がない場合はnull。</returns>
    private TargetPoint FindClosestTargetToScreenCenter(
        Camera targetCamera)
    {
        if (targetCamera == null)
        {
            return null;
        }

        Vector2 screenCenter =
            targetCamera.pixelRect.center;
        TargetPoint closestTargetPoint = null;
        float closestSqrDistance = float.PositiveInfinity;

        foreach (TargetPoint candidate in m_targetCandidates)
        {
            if (!TryGetScreenPosition(
                    targetCamera,
                    candidate,
                    out Vector2 candidateScreenPosition))
            {
                continue;
            }

            float sqrDistance =
                (candidateScreenPosition - screenCenter).sqrMagnitude;

            if (sqrDistance >= closestSqrDistance)
            {
                continue;
            }

            closestSqrDistance = sqrDistance;
            closestTargetPoint = candidate;
        }

        return closestTargetPoint;
    }

    /// <summary>
    /// 現在対象より指定した画面左右方向にあり、
    /// 現在対象との2D距離が最も近いTargetPointを取得します。
    /// 指定方向に候補がない場合は反対側の端へ循環します。
    /// </summary>
    /// <param name="targetCamera">スクリーン座標への変換に使用するカメラ。</param>
    /// <param name="direction">左は-1、右は1。</param>
    /// <returns>指定方向で最も近いTargetPoint。候補がない場合はnull。</returns>
    private TargetPoint FindClosestTargetInScreenDirection(
        Camera targetCamera,
        int direction)
    {
        if (targetCamera == null ||
            direction == 0 ||
            !TryGetScreenPosition(
                targetCamera,
                m_currentTargetPoint,
                out Vector2 currentScreenPosition))
        {
            return null;
        }

        TargetPoint closestTargetPoint = null;
        float closestSqrDistance = float.PositiveInfinity;
        TargetPoint wrapTargetPoint = null;
        float wrapTargetScreenX = direction < 0
            ? float.NegativeInfinity
            : float.PositiveInfinity;

        foreach (TargetPoint candidate in m_targetCandidates)
        {
            if (candidate == m_currentTargetPoint ||
                !TryGetScreenPosition(
                    targetCamera,
                    candidate,
                    out Vector2 candidateScreenPosition))
            {
                continue;
            }

            bool isCloserToWrapEdge = direction < 0
                ? candidateScreenPosition.x > wrapTargetScreenX
                : candidateScreenPosition.x < wrapTargetScreenX;

            if (isCloserToWrapEdge)
            {
                wrapTargetScreenX = candidateScreenPosition.x;
                wrapTargetPoint = candidate;
            }

            Vector2 screenOffset =
                candidateScreenPosition - currentScreenPosition;

            if (screenOffset.x * direction <= 0.0f)
            {
                continue;
            }

            float sqrDistance = screenOffset.sqrMagnitude;
            if (sqrDistance >= closestSqrDistance)
            {
                continue;
            }

            closestSqrDistance = sqrDistance;
            closestTargetPoint = candidate;
        }

        return closestTargetPoint != null
            ? closestTargetPoint
            : wrapTargetPoint;
    }

    /// <summary>
    /// TargetPointをスクリーン座標へ変換し、カメラ前方にあるか確認します。
    /// </summary>
    /// <param name="targetCamera">スクリーン座標への変換に使用するカメラ。</param>
    /// <param name="targetPoint">変換対象のTargetPoint。</param>
    /// <param name="screenPosition">変換後のスクリーン座標。</param>
    /// <returns>対象が有効かつカメラ前方にある場合はtrue。</returns>
    private bool TryGetScreenPosition(
        Camera targetCamera,
        TargetPoint targetPoint,
        out Vector2 screenPosition)
    {
        screenPosition = default;

        if (targetCamera == null ||
            !IsTargetPointAvailable(targetPoint))
        {
            return false;
        }

        Vector3 projectedPosition =
            targetCamera.WorldToScreenPoint(targetPoint.Position);

        if (projectedPosition.z <= 0.0f)
        {
            return false;
        }

        screenPosition = projectedPosition;
        return true;
    }

    /// <summary>
    /// 現在選択されているTargetPointを取得します。
    /// </summary>
    /// <returns>現在のTargetPoint。</returns>
    private TargetPoint GetCurrentTargetPoint()
    {
        if (!IsTargetPointAvailable(m_currentTargetPoint) ||
            m_targetPointManager == null ||
            !m_targetPointManager.IsTargetable(
                m_currentTargetPoint,
                transform.position))
        {
            return null;
        }

        return m_currentTargetPoint;
    }

    /// <summary>
    /// TargetPointがロックオン可能な状態か確認します。
    /// </summary>
    /// <param name="targetPoint">確認対象の部位。</param>
    /// <returns>ロックオン可能な場合はtrue。</returns>
    private bool IsTargetPointAvailable(TargetPoint targetPoint)
    {
        return targetPoint != null &&
               targetPoint.IsActive &&
               targetPoint.TargetTransform != null &&
               targetPoint.gameObject.activeInHierarchy;
    }

    /// <summary>
    /// ターゲット画像をTargetPointへ移動させます。
    /// </summary>
    /// <param name="target">対象となるTargetPoint。</param>
    /// <param name="useTween">
    /// 切り替え時のTweenを使用するかどうか。
    /// </param>
    private void ShowTargetReticle(
        Transform target,
        bool useTween)
    {
        if (m_targetReticle == null ||
            target == null)
        {
            return;
        }

        m_reticleTween?.Kill();

        Vector3 targetPosition =
            GetReticlePosition(target);

        m_targetReticle.gameObject.SetActive(true);

        if (!useTween)
        {
            m_targetReticle.transform.position =
                targetPosition;

            return;
        }

        Vector3 startPosition =
            m_targetReticle.transform.position;

        m_reticleTween =
            DOTween.To(
                () => 0.0f,
                progress =>
                {
                    if (target == null)
                    {
                        return;
                    }

                    Vector3 currentTargetPosition =
                        GetReticlePosition(target);

                    m_targetReticle.transform.position =
                        Vector3.Lerp(
                            startPosition,
                            currentTargetPosition,
                            progress);
                },
                1.0f,
                m_reticleMoveDuration)
            .SetEase(Ease.OutCubic);
    }

    /// <summary>
    /// ターゲット画像を非表示にします。
    /// </summary>
    private void HideTargetReticle()
    {
        m_reticleTween?.Kill();
        m_reticleTween = null;

        if (m_targetReticle != null)
        {
            m_targetReticle.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// TargetPointよりカメラ側に配置する
    /// ターゲット画像の座標を取得します。
    /// </summary>
    /// <param name="target">TargetPoint。</param>
    /// <returns>ターゲット画像のワールド座標。</returns>
    private Vector3 GetReticlePosition(Transform target)
    {
        Camera targetCamera =
            Camera.main;

        if (targetCamera == null)
        {
            return target.position;
        }

        Vector3 directionToCamera =
            targetCamera.transform.position -
            target.position;

        if (directionToCamera.sqrMagnitude <=
            MIN_SEARCH_DISTANCE)
        {
            return target.position;
        }

        directionToCamera.Normalize();

        return
            target.position +
            directionToCamera *
            m_reticleDistanceFromTarget;
    }

    /// <summary>
    /// オブジェクト破棄時にTweenを停止します。
    /// </summary>
    private void OnDestroy()
    {
        m_reticleTween?.Kill();

        if (m_targetReticle != null)
        {
            Destroy(m_targetReticle.gameObject);
        }
    }
}
