using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Splines;
#endif

using UnitySpline = UnityEngine.Splines.Spline;
using SplineMeshSpline = SplineMesh.Spline;
using SplineMeshNode = SplineMesh.SplineNode;

/// <summary>
/// SplineContainerの形状をサンプリングし、
/// SplineMeshのSplineへ同期します。
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SplineContainer))]
[RequireComponent(typeof(SplineMeshSpline))]
public sealed class SplineMeshSynchronizer : MonoBehaviour
{
    private const int MIN_SAMPLES_PER_CURVE = 2;
    private const float TANGENT_EPSILON = 0.000001f;

    [SerializeField, Header("同期元")]
    private int m_splineIndex = 0;

    [SerializeField, Header("同期設定")]
    private bool m_syncAutomatically = true;

    [SerializeField, Min(MIN_SAMPLES_PER_CURVE)]
    private int m_samplesPerCurve = 8;

    // Unity標準Spline
    private SplineContainer m_splineContainer;

    // SplineMesh側Spline
    private SplineMeshSpline m_splineMeshSpline;

    /// <summary>
    /// Unity Splineの形状をSplineMeshへ同期します。
    /// </summary>
    [ContextMenu("Sync Spline")]
    public void SyncSpline()
    {
        CacheComponents();

        if (!TryGetSourceSpline(out var sourceSpline))
            return;

        if (sourceSpline.Count < 2)
            return;

        var curveCount =
            sourceSpline.Closed
                ? sourceSpline.Count
                : sourceSpline.Count - 1;

        var sampleSegmentCount =
            curveCount * m_samplesPerCurve;

        // Closedの場合でも、
        // SplineMeshでは最後に始点の複製Nodeを必要とする
        var nodeCount =
            sampleSegmentCount + 1;

        m_splineMeshSpline.IsLoop = false;

        EnsureNodeCount(nodeCount);

        var positions =
            new Vector3[nodeCount];

        var upVectors =
            new Vector3[nodeCount];

        // まずSpline上の位置をサンプリング
        for (var i = 0; i < nodeCount; i++)
        {
            float t;

            if (sourceSpline.Closed &&
                i == nodeCount - 1)
            {
                // Closedの最後は始点を複製
                t = 0.0f;
            }
            else
            {
                t =
                    (float)i /
                    sampleSegmentCount;
            }

            EvaluateSourceSpline(
                sourceSpline,
                t,
                out positions[i],
                out upVectors[i]);
        }

        // サンプルした位置からSplineMeshのNodeを生成
        for (var i = 0; i < nodeCount; i++)
        {
            var position =
                positions[i];

            var direction =
                CalculateDirection(
                    positions,
                    i,
                    sourceSpline.Closed);

            var targetNode =
                m_splineMeshSpline.nodes[i];

            targetNode.Position =
                position;

            targetNode.Direction =
                direction;

            targetNode.Up =
                upVectors[i];

            targetNode.Scale =
                Vector2.one;

            targetNode.Roll =
                0.0f;
        }

        // Curve情報を再構築
        m_splineMeshSpline.RefreshCurves();

        // Closedを最後に反映
        m_splineMeshSpline.IsLoop =
            sourceSpline.Closed;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(
                m_splineMeshSpline);
        }
#endif
    }

    /// <summary>
    /// Unity Splineから位置とUp方向を取得します。
    /// </summary>
    private void EvaluateSourceSpline(
        UnitySpline spline,
        float t,
        out Vector3 position,
        out Vector3 up)
    {
        SplineUtility.Evaluate(
            spline,
            t,
            out float3 splinePosition,
            out _,
            out float3 splineUp);

        position =
            splinePosition;

        up =
            splineUp;

        if (up.sqrMagnitude <=
            TANGENT_EPSILON)
        {
            up = Vector3.up;
        }
        else
        {
            up.Normalize();
        }
    }

    /// <summary>
    /// サンプリング点からSplineMesh用Directionを計算します。
    /// </summary>
    private Vector3 CalculateDirection(
        Vector3[] positions,
        int index,
        bool isClosed)
    {
        var currentPosition =
            positions[index];

        // Closedの最後は始点と同じDirection
        if (isClosed &&
            index == positions.Length - 1)
        {
            return CalculateDirection(
                positions,
                0,
                true);
        }

        Vector3 tangent;

        if (isClosed)
        {
            var uniquePointCount =
                positions.Length - 1;

            var previousIndex =
                index - 1;

            if (previousIndex < 0)
            {
                previousIndex =
                    uniquePointCount - 1;
            }

            var nextIndex =
                index + 1;

            if (nextIndex >= uniquePointCount)
            {
                nextIndex = 0;
            }

            tangent =
                positions[nextIndex] -
                positions[previousIndex];

            // Cubic Bezierの制御点として扱うので1/6
            return currentPosition +
                   tangent / 6.0f;
        }

        // Open Splineの始点
        if (index == 0)
        {
            tangent =
                positions[1] -
                positions[0];

            return currentPosition +
                   tangent / 3.0f;
        }

        // Open Splineの終点
        if (index ==
            positions.Length - 1)
        {
            tangent =
                positions[index] -
                positions[index - 1];

            return currentPosition +
                   tangent / 3.0f;
        }

        // 中間Node
        tangent =
            positions[index + 1] -
            positions[index - 1];

        return currentPosition +
               tangent / 6.0f;
    }

    /// <summary>
    /// SplineMesh側のNode数を調整します。
    /// </summary>
    private void EnsureNodeCount(
        int requiredNodeCount)
    {
        while (m_splineMeshSpline.nodes.Count <
               requiredNodeCount)
        {
            m_splineMeshSpline.AddNode(
                new SplineMeshNode(
                    Vector3.zero,
                    Vector3.right));
        }

        while (m_splineMeshSpline.nodes.Count >
               requiredNodeCount)
        {
            var lastNode =
                m_splineMeshSpline.nodes[
                    m_splineMeshSpline.nodes.Count - 1];

            m_splineMeshSpline.RemoveNode(
                lastNode);
        }
    }

    /// <summary>
    /// 必要なComponentを取得します。
    /// </summary>
    private void CacheComponents()
    {
        if (m_splineContainer == null)
        {
            TryGetComponent(
                out m_splineContainer);
        }

        if (m_splineMeshSpline == null)
        {
            TryGetComponent(
                out m_splineMeshSpline);
        }
    }

    /// <summary>
    /// 同期対象のUnity Splineを取得します。
    /// </summary>
    private bool TryGetSourceSpline(
        out UnitySpline spline)
    {
        spline = null;

        if (m_splineContainer == null)
            return false;

        if (m_splineIndex < 0 ||
            m_splineIndex >=
            m_splineContainer.Splines.Count)
        {
            Debug.LogWarning(
                $"{nameof(SplineMeshSynchronizer)}: " +
                $"Spline Index {m_splineIndex} が範囲外です。",
                this);

            return false;
        }

        spline =
            m_splineContainer
                .Splines[m_splineIndex];

        return spline != null;
    }

    /// <summary>
    /// 変更されたSplineが同期対象か確認します。
    /// </summary>
    private bool IsTargetSpline(
        UnitySpline spline)
    {
        if (!TryGetSourceSpline(
                out var sourceSpline))
        {
            return false;
        }

        return ReferenceEquals(
            spline,
            sourceSpline);
    }

    private void OnEnable()
    {
        CacheComponents();

        if (Application.isPlaying)
        {
            UnitySpline.Changed +=
                HandleRuntimeSplineChanged;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorSplineUtility
                    .AfterSplineWasModified +=
                HandleEditorSplineModified;
        }
#endif

        if (m_syncAutomatically)
            SyncSpline();
    }

    private void OnDisable()
    {
        UnitySpline.Changed -=
            HandleRuntimeSplineChanged;

#if UNITY_EDITOR
        EditorSplineUtility
                .AfterSplineWasModified -=
            HandleEditorSplineModified;
#endif
    }

    /// <summary>
    /// RuntimeでSplineが変更された際に同期します。
    /// </summary>
    private void HandleRuntimeSplineChanged(
        UnitySpline spline,
        int knotIndex,
        SplineModification modification)
    {
        if (!m_syncAutomatically)
            return;

        if (!IsTargetSpline(spline))
            return;

        SyncSpline();
    }

#if UNITY_EDITOR

    /// <summary>
    /// Editor上でSplineが変更された際に同期します。
    /// </summary>
    private void HandleEditorSplineModified(
        UnitySpline spline)
    {
        if (!m_syncAutomatically)
            return;

        if (!IsTargetSpline(spline))
            return;

        SyncSpline();
    }

    private void OnValidate()
    {
        m_splineIndex =
            Mathf.Max(
                0,
                m_splineIndex);

        m_samplesPerCurve =
            Mathf.Max(
                MIN_SAMPLES_PER_CURVE,
                m_samplesPerCurve);

        if (Application.isPlaying ||
            !m_syncAutomatically)
        {
            return;
        }

        EditorApplication.delayCall +=
            HandleDelayedValidation;
    }

    /// <summary>
    /// Inspector変更後にSplineを同期します。
    /// </summary>
    private void HandleDelayedValidation()
    {
        if (this == null ||
            !isActiveAndEnabled)
        {
            return;
        }

        SyncSpline();
    }

#endif
}