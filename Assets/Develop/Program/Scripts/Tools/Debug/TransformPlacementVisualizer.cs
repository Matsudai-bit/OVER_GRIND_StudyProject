using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Transformの位置と向きをSceneビュー上に可視化します。
/// </summary>
[DisallowMultipleComponent]
public sealed class TransformPlacementVisualizer : MonoBehaviour
{
    [Header("表示設定")]
    [SerializeField]
    private bool m_drawOnlyWhenSelected = false;

    [SerializeField]
    private bool m_showObjectName = true;

    [Header("位置マーカー")]
    [SerializeField]
    private Color m_markerColor = Color.yellow;

    [SerializeField, Min(0.01f)]
    private float m_markerRadius = 0.1f;

    [Header("ローカル軸")]
    [SerializeField]
    private bool m_drawAxes = true;

    [SerializeField, Min(0.01f)]
    private float m_axisLength = 0.5f;

    [Header("前方向")]
    [SerializeField]
    private bool m_drawForward = true;

    [SerializeField, Min(0.01f)]
    private float m_forwardLength = 1.0f;

    [SerializeField]
    private Color m_forwardColor = Color.cyan;

    [Header("ラベル")]
    [SerializeField]
    private Vector3 m_labelOffset = new Vector3(0.0f, 0.2f, 0.0f);

    private void OnDrawGizmos()
    {
        if (m_drawOnlyWhenSelected)
        {
            return;
        }

        DrawVisualizer();
    }

    private void OnDrawGizmosSelected()
    {
        if (!m_drawOnlyWhenSelected)
        {
            return;
        }

        DrawVisualizer();
    }

    /// <summary>
    /// Transformのデバッグ表示を描画します。
    /// </summary>
    private void DrawVisualizer()
    {
        Vector3 position = transform.position;

        DrawPositionMarker(position);

        if (m_drawAxes)
        {
            DrawAxes(position);
        }

        if (m_drawForward)
        {
            DrawForward(position);
        }

#if UNITY_EDITOR
        if (m_showObjectName)
        {
            DrawLabel(position);
        }
#endif
    }

    /// <summary>
    /// Transformの位置を描画します。
    /// </summary>
    private void DrawPositionMarker(Vector3 position)
    {
        Gizmos.color = m_markerColor;

        Gizmos.DrawSphere(
            position,
            m_markerRadius
        );
    }

    /// <summary>
    /// Transformのローカル座標軸を描画します。
    /// </summary>
    private void DrawAxes(Vector3 position)
    {
        // X軸
        Gizmos.color = Color.red;
        Gizmos.DrawLine(
            position,
            position + transform.right * m_axisLength
        );

        // Y軸
        Gizmos.color = Color.green;
        Gizmos.DrawLine(
            position,
            position + transform.up * m_axisLength
        );

        // Z軸
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(
            position,
            position + transform.forward * m_axisLength
        );
    }

    /// <summary>
    /// Transformの前方向を描画します。
    /// </summary>
    private void DrawForward(Vector3 position)
    {
        Vector3 forwardEndPosition =
            position + transform.forward * m_forwardLength;

        Gizmos.color = m_forwardColor;
        Gizmos.DrawLine(position, forwardEndPosition);

#if UNITY_EDITOR
        Handles.color = m_forwardColor;

        Handles.ArrowHandleCap(
            0,
            position,
            transform.rotation,
            m_forwardLength,
            EventType.Repaint
        );
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// GameObject名をSceneビュー上に描画します。
    /// </summary>
    private void DrawLabel(Vector3 position)
    {
        Handles.Label(
            position + m_labelOffset,
            gameObject.name
        );
    }
#endif
}