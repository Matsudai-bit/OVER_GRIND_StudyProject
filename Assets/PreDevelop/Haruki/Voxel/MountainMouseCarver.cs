using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// マウス操作で砂山を削るためのデバッグ用コンポーネント。
/// </summary>
public class MountainMouseCarver : MonoBehaviour
{
    // ---- Inspector 設定項目 ----
    [Header("対象")]
    [Tooltip("削る対象の砂山（未設定時は同一GameObjectから取得）")]
    [SerializeField] private MarchingCubesMountain m_targetMountain;

    [Header("入力設定")]
    [Tooltip("マウスで削る球の半径(m)")]
    [SerializeField] private float m_carveRadius = 1.0f;
    [Tooltip("長押しで連続して削るかどうか")]
    [SerializeField] private bool m_holdToCarve = false;

    // ---- メンバー変数 ----
    private Camera m_cachedCamera; // メインカメラのキャッシュ

    private void Awake()
    {
        if (m_targetMountain == null)
        {
            m_targetMountain = GetComponent<MarchingCubesMountain>();
        }

        m_cachedCamera = Camera.main;
    }

    private void Update()
    {
        ProcessMouseInput();
    }

    /// <summary>
    /// マウス入力を判定し、対象砂山のクリック位置を削ります。
    /// </summary>
    private void ProcessMouseInput()
    {
        if (m_targetMountain == null || Mouse.current == null)
        {
            return;
        }

        Camera activeCamera = GetActiveCamera();
        if (activeCamera == null)
        {
            return;
        }

        bool isPressed = m_holdToCarve
            ? Mouse.current.leftButton.isPressed
            : Mouse.current.leftButton.wasPressedThisFrame;

        if (!isPressed)
        {
            return;
        }

        Ray ray = activeCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.gameObject == m_targetMountain.gameObject)
        {
            m_targetMountain.Carve(hit.point, m_carveRadius);
        }
    }

    /// <summary>
    /// 有効なカメラを取得します。
    /// </summary>
    private Camera GetActiveCamera()
    {
        if (m_cachedCamera == null)
        {
            m_cachedCamera = Camera.main;
        }

        return m_cachedCamera;
    }
}