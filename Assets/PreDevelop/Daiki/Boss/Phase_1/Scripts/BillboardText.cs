using UnityEngine;
using System;


#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 3Dテキストを指定したカメラへ向けます。
/// </summary>
public sealed class BillboardText : MonoBehaviour
{
    /// <summary>
    /// ビルボードの回転方法です。
    /// </summary>
    [Serializable]
    public enum BillboardMode
    {
        /// <summary>
        /// カメラへ完全に向けます。
        /// </summary>
        FULL_ROTATION,

        /// <summary>
        /// Y軸のみ回転させます。
        /// </summary>
        Y_AXIS_ONLY
    }

    /// <summary>
    /// 使用するカメラの種類です。
    /// </summary>
    public enum BillboardCameraMode
    {
        /// <summary>
        /// 通常カメラを使用します。
        /// </summary>
        GAME_CAMERA,

        /// <summary>
        /// Sceneビューカメラを使用します。
        /// </summary>
        SCENE_VIEW_CAMERA
    }

    /// <summary>
    /// 通常使用するカメラです。
    /// </summary>
    [SerializeField]
    private Camera m_gameCamera;

    /// <summary>
    /// 使用するカメラの種類です。
    /// </summary>
    [SerializeField]
    private BillboardCameraMode m_cameraMode =
        BillboardCameraMode.GAME_CAMERA;

    /// <summary>
    /// ビルボードの回転方法です。
    /// </summary>
    [SerializeField]
    private BillboardMode m_billboardMode =
        BillboardMode.FULL_ROTATION;

    /// <summary>
    /// テキストの向きを反転するかどうかです。
    /// </summary>
    [SerializeField]
    private bool m_reverseDirection;

    /// <summary>
    /// 通常カメラを初期化します。
    /// </summary>
    private void Awake()
    {
        InitializeGameCamera();
    }

    /// <summary>
    /// カメラ移動後に向きを更新します。
    /// </summary>
    private void LateUpdate()
    {
        Camera targetCamera = GetTargetCamera();

        if (targetCamera == null)
        {
            return;
        }

        FaceCamera(targetCamera);
    }

    /// <summary>
    /// 通常カメラを初期化します。
    /// </summary>
    private void InitializeGameCamera()
    {
        if (m_gameCamera != null)
        {
            return;
        }

        m_gameCamera = Camera.main;

        if (m_gameCamera == null)
        {
            Debug.LogWarning(
                $"{nameof(BillboardText)}: 通常カメラが設定されていません。",
                this);
        }
    }

    /// <summary>
    /// 使用するカメラを取得します。
    /// </summary>
    /// <returns>使用するカメラ。</returns>
    private Camera GetTargetCamera()
    {
        switch (m_cameraMode)
        {
            case BillboardCameraMode.GAME_CAMERA:
                return m_gameCamera;

            case BillboardCameraMode.SCENE_VIEW_CAMERA:
                return GetSceneViewCamera();

            default:
                return m_gameCamera;
        }
    }

    /// <summary>
    /// Sceneビューカメラを取得します。
    /// </summary>
    /// <returns>Sceneビューカメラ。</returns>
    private Camera GetSceneViewCamera()
    {
#if UNITY_EDITOR
        SceneView sceneView = SceneView.lastActiveSceneView;

        if (sceneView == null)
        {
            return null;
        }

        return sceneView.camera;
#else
        return null;
#endif
    }

    /// <summary>
    /// 指定したカメラへ向きを合わせます。
    /// </summary>
    /// <param name="targetCamera">向きを合わせるカメラ。</param>
    private void FaceCamera(Camera targetCamera)
    {
        Vector3 cameraDirection =
            targetCamera.transform.position - transform.position;

        if (m_billboardMode == BillboardMode.Y_AXIS_ONLY)
        {
            cameraDirection.y = 0.0f;
        }

        if (cameraDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(
            cameraDirection,
            Vector3.up);

        // テキストが裏向きの場合は向きを反転します。
        if (m_reverseDirection)
        {
            transform.Rotate(
                0.0f,
                180.0f,
                0.0f,
                Space.Self);
        }
    }

    /// <summary>
    /// 使用するカメラの種類を変更します。
    /// </summary>
    /// <param name="cameraMode">使用するカメラの種類。</param>
    public void ChangeCameraMode(BillboardCameraMode cameraMode)
    {
        m_cameraMode = cameraMode;
    }
}