using UnityEngine;

/// <summary>
/// ターゲット画像をカメラへ向けるビルボード制御を行います。
/// </summary>
public sealed class TargetReticleBillboard : MonoBehaviour
{
    private Camera m_targetCamera;

    /// <summary>
    /// カメラを取得します。
    /// </summary>
    private void Awake()
    {
        m_targetCamera = Camera.main;
    }

    /// <summary>
    /// ターゲット画像を常にカメラへ向けます。
    /// </summary>
    private void LateUpdate()
    {
        if (m_targetCamera == null)
        {
            return;
        }

        transform.rotation =
            m_targetCamera.transform.rotation;
    }
}