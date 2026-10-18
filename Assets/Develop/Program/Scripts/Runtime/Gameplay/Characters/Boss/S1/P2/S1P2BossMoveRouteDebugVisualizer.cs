using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// S1P2ボスの生成済み移動経路をGizmosで表示します。
/// </summary>
[DisallowMultipleComponent]
public sealed class S1P2BossMoveRouteDebugVisualizer :
    MonoBehaviour
{
    // 経路生成
    [SerializeField, Header("参照")]
    private S1P2BossMoveRoutePlanner m_routePlanner;

    // デバッグ表示を有効にするか
    [SerializeField, Header("表示設定")]
    private bool m_isDebugVisible = true;

    // 実際に追従する経路を表示するか
    [SerializeField]
    private bool m_drawRoute = true;

    // 経路生成に使用した制御点を表示するか
    [SerializeField]
    private bool m_drawControlPoints = true;

    // 経路追従用のサンプル点を表示するか
    [SerializeField]
    private bool m_drawSamplePoints = false;

    // 経路の色
    [SerializeField, Header("色")]
    private Color m_routeColor =
        Color.yellow;

    // 制御点の色
    [SerializeField]
    private Color m_controlPointColor =
        Color.cyan;

    // サンプル点の色
    [SerializeField]
    private Color m_samplePointColor =
        Color.green;

    // 最終地点の色
    [SerializeField]
    private Color m_finalPointColor =
        Color.red;

    // 制御点の表示半径
    [SerializeField, Header("サイズ"), Min(0.0f)]
    private float m_controlPointRadius = 0.5f;

    // サンプル点の表示半径
    [SerializeField, Min(0.0f)]
    private float m_samplePointRadius = 0.15f;

    // 最終地点の表示半径
    [SerializeField, Min(0.0f)]
    private float m_finalPointRadius = 0.75f;

    /// <summary>
    /// デバッグ表示のON/OFFを設定します。
    /// </summary>
    /// <param name="isVisible">表示するか。</param>
    public void SetVisible(
        bool isVisible)
    {
        m_isDebugVisible =
            isVisible;
    }

    /// <summary>
    /// デバッグ表示が有効か取得します。
    /// </summary>
    public bool IsVisible =>
        m_isDebugVisible;

    /// <summary>
    /// 生成済み経路をGizmosで描画します。
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!m_isDebugVisible ||
            m_routePlanner == null)
        {
            return;
        }

        BossMoveRoute route =
            m_routePlanner.LastGeneratedRoute;

        if (route == null ||
            !route.IsValid)
        {
            return;
        }

        if (m_drawRoute)
        {
            DrawRoute(
                route.SamplePoints);
        }

        if (m_drawControlPoints)
        {
            DrawControlPoints(
                route.ControlPoints);
        }

        if (m_drawSamplePoints)
        {
            DrawSamplePoints(
                route.SamplePoints);
        }

        DrawFinalPoint(
            route.FinalPosition);
    }

    /// <summary>
    /// 実際に追従する経路を描画します。
    /// </summary>
    /// <param name="samplePoints">経路サンプル点。</param>
    private void DrawRoute(
        IReadOnlyList<Vector3> samplePoints)
    {
        if (samplePoints == null ||
            samplePoints.Count < 2)
        {
            return;
        }

        Gizmos.color =
            m_routeColor;

        for (int i = 1;
             i < samplePoints.Count;
             i++)
        {
            Gizmos.DrawLine(
                samplePoints[i - 1],
                samplePoints[i]);
        }
    }

    /// <summary>
    /// 経路生成に使用した制御点を描画します。
    /// </summary>
    /// <param name="controlPoints">制御点。</param>
    private void DrawControlPoints(
        IReadOnlyList<Vector3> controlPoints)
    {
        if (controlPoints == null)
        {
            return;
        }

        Gizmos.color =
            m_controlPointColor;

        for (int i = 0;
             i < controlPoints.Count;
             i++)
        {
            Gizmos.DrawWireSphere(
                controlPoints[i],
                m_controlPointRadius);
        }
    }

    /// <summary>
    /// 経路追従用のサンプル点を描画します。
    /// </summary>
    /// <param name="samplePoints">サンプル点。</param>
    private void DrawSamplePoints(
        IReadOnlyList<Vector3> samplePoints)
    {
        if (samplePoints == null)
        {
            return;
        }

        Gizmos.color =
            m_samplePointColor;

        for (int i = 0;
             i < samplePoints.Count;
             i++)
        {
            Gizmos.DrawSphere(
                samplePoints[i],
                m_samplePointRadius);
        }
    }

    /// <summary>
    /// 経路の最終地点を描画します。
    /// </summary>
    /// <param name="finalPosition">最終地点。</param>
    private void DrawFinalPoint(
        Vector3 finalPosition)
    {
        Gizmos.color =
            m_finalPointColor;

        Gizmos.DrawWireSphere(
            finalPosition,
            m_finalPointRadius);
    }

    /// <summary>
    /// Inspector設定時に参照を取得します。
    /// </summary>
    private void Reset()
    {
        m_routePlanner =
            GetComponent<S1P2BossMoveRoutePlanner>();

        if (m_routePlanner == null)
        {
            m_routePlanner =
                GetComponentInParent<
                    S1P2BossMoveRoutePlanner>();
        }
    }
}