using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ボスが移動時に使用する生成済み経路を保持します。
/// </summary>
public sealed class BossMoveRoute
{
    // 経路生成に使用した制御点
    private readonly Vector3[] m_controlPoints;

    // 経路追従に使用するサンプル点
    private readonly Vector3[] m_samplePoints;

    /// <summary>
    /// 移動経路を生成します。
    /// </summary>
    /// <param name="controlPoints">経路の制御点。</param>
    /// <param name="samplePoints">経路のサンプル点。</param>
    public BossMoveRoute(
        IReadOnlyList<Vector3> controlPoints,
        IReadOnlyList<Vector3> samplePoints)
    {
        m_controlPoints =
            CopyPoints(
                controlPoints);

        m_samplePoints =
            CopyPoints(
                samplePoints);

        TotalLength =
            CalculateTotalLength(
                m_samplePoints);
    }

    /// <summary>
    /// 経路の制御点を取得します。
    /// </summary>
    public IReadOnlyList<Vector3> ControlPoints =>
        m_controlPoints;

    /// <summary>
    /// 経路のサンプル点を取得します。
    /// </summary>
    public IReadOnlyList<Vector3> SamplePoints =>
        m_samplePoints;

    /// <summary>
    /// 経路全体の長さを取得します。
    /// </summary>
    public float TotalLength { get; }

    /// <summary>
    /// 最終地点を取得します。
    /// </summary>
    public Vector3 FinalPosition =>
        m_samplePoints.Length > 0
            ? m_samplePoints[^1]
            : Vector3.zero;

    /// <summary>
    /// 有効な経路か取得します。
    /// </summary>
    public bool IsValid =>
        m_samplePoints.Length >= 2;

    /// <summary>
    /// 点列をコピーします。
    /// </summary>
    /// <param name="source">コピー元。</param>
    /// <returns>コピーした点列。</returns>
    private static Vector3[] CopyPoints(
        IReadOnlyList<Vector3> source)
    {
        if (source == null ||
            source.Count == 0)
        {
            return Array.Empty<Vector3>();
        }

        Vector3[] points =
            new Vector3[source.Count];

        for (int i = 0;
             i < source.Count;
             i++)
        {
            points[i] =
                source[i];
        }

        return points;
    }

    /// <summary>
    /// 経路全体の長さを計算します。
    /// </summary>
    /// <param name="points">経路点。</param>
    /// <returns>経路全体の長さ。</returns>
    private static float CalculateTotalLength(
        IReadOnlyList<Vector3> points)
    {
        if (points == null ||
            points.Count < 2)
        {
            return 0.0f;
        }

        float totalLength =
            0.0f;

        for (int i = 1;
             i < points.Count;
             i++)
        {
            totalLength +=
                Vector3.Distance(
                    points[i - 1],
                    points[i]);
        }

        return totalLength;
    }
}
