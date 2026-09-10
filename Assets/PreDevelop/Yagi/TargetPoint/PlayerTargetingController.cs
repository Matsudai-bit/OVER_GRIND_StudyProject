using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤーのターゲット注視機能を管理します。
/// Xボタンを押している間だけターゲット画像を表示し、
/// 対象部位へ一度だけカメラを向けます。
/// ターゲット中もカメラは自由に操作できます。
/// </summary>
public sealed class PlayerTargetingController : MonoBehaviour
{
    private const float MIN_SEARCH_DISTANCE = 0.0001f;

    [Header("Target Search")]

    [SerializeField, Min(0.1f)]
    private float m_targetSearchRadius = 20.0f;

    [SerializeField]
    private LayerMask m_targetLayer;

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

    [SerializeField, Min(0.0f)]
    private float m_cameraFocusDuration = 0.25f;

    private readonly List<TargetableEnemy> m_candidates =
        new List<TargetableEnemy>();

    private InputAction m_targetAction;
    private InputAction m_targetLeftAction;
    private InputAction m_targetRightAction;

    private TargetableEnemy m_currentEnemy;
    private int m_currentTargetPointIndex = -1;

    private TargetReticleBillboard m_targetReticle;
    private Tween m_reticleTween;

    private bool m_isTargeting;

    /// <summary>
    /// 初期化を行います。
    /// </summary>
    private void Awake()
    {
        ResolveInputActions();

        if (m_targetReticlePrefab != null)
        {
            m_targetReticle =
                Instantiate(m_targetReticlePrefab);

            m_targetReticle.gameObject.SetActive(false);
        }
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
                    targetPoint.transform);

            m_targetReticle.transform.position =
                Vector3.Lerp(
                    m_targetReticle.transform.position,
                    targetPosition,
                    Time.deltaTime * 15.0f);
        }

        if (m_playerCamera != null)
        {
            m_playerCamera.UpdateTargetFocus(
                targetPoint.transform);
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
        m_targetAction.started += OnTargetStarted;
        m_targetAction.canceled += OnTargetCanceled;

        m_targetLeftAction.performed += OnTargetLeftPerformed;
        m_targetRightAction.performed += OnTargetRightPerformed;
    }

    /// <summary>
    /// 入力イベントを解除します。
    /// </summary>
    private void OnDisable()
    {
        m_targetAction.started -= OnTargetStarted;
        m_targetAction.canceled -= OnTargetCanceled;

        m_targetLeftAction.performed -= OnTargetLeftPerformed;
        m_targetRightAction.performed -= OnTargetRightPerformed;
    }

    /// <summary>
    /// Xボタンを押したときの処理を行います。
    /// </summary>
    /// <param name="context">入力コンテキスト。</param>
    private void OnTargetStarted(
        InputAction.CallbackContext context)
    {
        StartTargeting();
    }

    /// <summary>
    /// Xボタンを離したときの処理を行います。
    /// </summary>
    /// <param name="context">入力コンテキスト。</param>
    private void OnTargetCanceled(
        InputAction.CallbackContext context)
    {
        StopTargeting();
    }

    /// <summary>
    /// Xボタンを押したときにターゲットを開始します。
    /// </summary>
    private void StartTargeting()
    {
        if (m_isTargeting)
        {
            return;
        }

        TargetableEnemy nearestEnemy =
            FindNearestEnemy();

        if (nearestEnemy == null ||
            nearestEnemy.TargetPoints.Count == 0)
        {
            return;
        }

        m_currentEnemy = nearestEnemy;
        m_currentTargetPointIndex = 0;
        m_isTargeting = true;

        TargetPoint targetPoint =
            GetCurrentTargetPoint();

        if (targetPoint == null)
        {
            StopTargeting();
            return;
        }

        ShowTargetReticle(
            targetPoint.transform,
            false);

        if (m_playerCamera != null)
        {
            m_playerCamera.BeginTargetFocus(
    targetPoint.transform,
    m_cameraFocusDuration);
        }
    }

    /// <summary>
    /// Xボタンを離したときにターゲットを終了します。
    /// </summary>
    private void StopTargeting()
    {
        if (!m_isTargeting)
        {
            return;
        }

        m_isTargeting = false;

        m_currentEnemy = null;
        m_currentTargetPointIndex = -1;

        HideTargetReticle();

        if (m_playerCamera != null)
        {
            m_playerCamera.EndTargetFocus();
        }
    }

    /// <summary>
    /// D-Pad左でTargetPointを切り替えます。
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
    /// D-Pad右でTargetPointを切り替えます。
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
        if (m_currentEnemy == null ||
            m_currentEnemy.TargetPoints.Count == 0)
        {
            return;
        }

        int count =
            m_currentEnemy.TargetPoints.Count;

        m_currentTargetPointIndex =
            (m_currentTargetPointIndex +
             direction +
             count) %
            count;

        TargetPoint targetPoint =
            GetCurrentTargetPoint();

        if (targetPoint == null)
        {
            return;
        }

        ShowTargetReticle(
            targetPoint.transform,
            true);

        if (m_playerCamera != null)
        {
            m_playerCamera.BeginTargetFocus(
     targetPoint.transform,
     m_cameraFocusDuration);
        }
    }

    /// <summary>
    /// 現在選択されているTargetPointを取得します。
    /// </summary>
    /// <returns>現在のTargetPoint。</returns>
    private TargetPoint GetCurrentTargetPoint()
    {
        if (m_currentEnemy == null ||
            m_currentTargetPointIndex < 0 ||
            m_currentTargetPointIndex >=
            m_currentEnemy.TargetPoints.Count)
        {
            return null;
        }

        return
            m_currentEnemy.TargetPoints[
                m_currentTargetPointIndex];
    }

    /// <summary>
    /// 最も近いTargetableEnemyを取得します。
    /// </summary>
    /// <returns>最も近い敵。</returns>
    private TargetableEnemy FindNearestEnemy()
    {
        Collider[] colliders =
            Physics.OverlapSphere(
                transform.position,
                m_targetSearchRadius,
                m_targetLayer);

        TargetableEnemy nearestEnemy = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider collider in colliders)
        {
            TargetableEnemy enemy =
                collider.GetComponentInParent<TargetableEnemy>();

            if (enemy == null ||
                enemy.TargetPoints.Count == 0)
            {
                continue;
            }

            float distance =
                (enemy.transform.position -
                 transform.position).sqrMagnitude;

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestEnemy = enemy;
            }
        }

        return nearestEnemy;
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