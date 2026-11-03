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
/// UnityのSplineをSplineMeshへ同期します。
/// 同一フレーム内の変更をまとめ、値に変化がない場合は再計算を省略します。
/// </summary>
[ExecuteAlways]
[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
[RequireComponent(typeof(SplineContainer))]
[RequireComponent(typeof(SplineMeshSpline))]
public sealed class SplineMeshSynchronizer : MonoBehaviour
{
    private const int MIN_SAMPLES_PER_CURVE = 2;
    private const float TANGENT_EPSILON = 0.000001f;
    private const float OPEN_ENDPOINT_TANGENT_FACTOR = 1.0f / 3.0f;
    private const float INNER_TANGENT_FACTOR = 1.0f / 6.0f;

    [SerializeField, Header("同期元")]
    private int m_splineIndex = 0;

    [SerializeField, Header("同期設定")]
    private bool m_syncAutomatically = true;

    [SerializeField, Min(MIN_SAMPLES_PER_CURVE)]
    private int m_samplesPerCurve = 8;

    // Unity標準のスプライン。
    private SplineContainer m_splineContainer;

    // SplineMesh側のスプライン。
    private SplineMeshSpline m_splineMeshSpline;

    // サンプリング結果の再利用バッファ。
    private Vector3[] m_positions;
    private Vector3[] m_upVectors;

    // 次の更新タイミングで同期するかどうか。
    private bool m_isSyncPending;

#if UNITY_EDITOR
    // Editorの遅延呼び出しが予約済みかどうか。
    private bool m_isEditorSyncScheduled;
#endif

    /// <summary>
    /// スプラインを即時同期します。手動実行にも使用できます。
    /// </summary>
    [ContextMenu("Sync Spline")]
    public void SyncSpline()
    {
        m_isSyncPending = false;

#if UNITY_EDITOR
        CancelEditorSync();
#endif

        CacheComponents();

        if (!TryGetSourceSpline(out UnitySpline sourceSpline))
        {
            return;
        }

        if (sourceSpline.Count < 2)
        {
            return;
        }

        // サンプル数を決定し、既存の配列を再利用する。
        int curveCount = sourceSpline.Closed ? sourceSpline.Count : sourceSpline.Count - 1;
        int sampleSegmentCount = curveCount * m_samplesPerCurve;
        int nodeCount = sampleSegmentCount + 1;
        EnsureSampleBuffer(nodeCount);

        for (int i = 0; i < nodeCount; i++)
        {
            float t = sourceSpline.Closed && i == nodeCount - 1
                ? 0.0f
                : (float)i / sampleSegmentCount;

            EvaluateSourceSpline(sourceSpline, t, out m_positions[i], out m_upVectors[i]);
        }

        // ノード数・ループ状態・位置や向きがすべて同じなら何もしない。
        if (!HasSplineChanges(nodeCount, sourceSpline.Closed))
        {
            return;
        }

        // 閉路の端点連動を一時的に外してからノードを更新する。
        if (m_splineMeshSpline.IsLoop)
        {
            m_splineMeshSpline.IsLoop = false;
        }

        EnsureNodeCount(nodeCount);

        for (int i = 0; i < nodeCount; i++)
        {
            SplineMeshNode node = m_splineMeshSpline.nodes[i];
            Vector3 position = m_positions[i];
            Vector3 direction = CalculateDirection(i, nodeCount, sourceSpline.Closed);
            Vector3 up = m_upVectors[i];

            // SplineNodeのSetterはそれぞれ再計算イベントを発行する。
            // 同値の再設定を省き、通知回数をできるだけ抑える。
            if (!node.Position.Equals(position))
            {
                node.Position = position;
            }

            if (!node.Direction.Equals(direction))
            {
                node.Direction = direction;
            }

            if (!node.Up.Equals(up))
            {
                node.Up = up;
            }

            if (!node.Scale.Equals(Vector2.one))
            {
                node.Scale = Vector2.one;
            }

            if (node.Roll != 0.0f)
            {
                node.Roll = 0.0f;
            }
        }

        // AddNode / RemoveNodeと各SetterでSplineMesh側は曲線を再計算する。
        // RefreshCurves()の重複呼び出しは行わない。
        if (m_splineMeshSpline.IsLoop != sourceSpline.Closed)
        {
            m_splineMeshSpline.IsLoop = sourceSpline.Closed;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(m_splineMeshSpline);
        }
#endif
    }

    /// <summary>
    /// 外部処理から次の更新タイミングでの同期を要求します。
    /// </summary>
    public void RequestSync()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        m_isSyncPending = true;

#if UNITY_EDITOR
        if (!Application.isPlaying && !m_isEditorSyncScheduled)
        {
            m_isEditorSyncScheduled = true;
            EditorApplication.delayCall += HandleDelayedEditorSync;
        }
#endif
    }

    private void OnEnable()
    {
        CacheComponents();

        if (Application.isPlaying)
        {
            UnitySpline.Changed += HandleRuntimeSplineChanged;
        }
#if UNITY_EDITOR
        else
        {
            EditorSplineUtility.AfterSplineWasModified += HandleEditorSplineModified;
        }
#endif

        if (m_syncAutomatically)
        {
            RequestSync();
        }
    }

    private void OnDisable()
    {
        UnitySpline.Changed -= HandleRuntimeSplineChanged;

#if UNITY_EDITOR
        EditorSplineUtility.AfterSplineWasModified -= HandleEditorSplineModified;
        CancelEditorSync();
#endif
        m_isSyncPending = false;
    }

    private void LateUpdate()
    {
        // 変更通知を受けた同一フレーム中は、何回通知されても1回だけ同期する。
        if (!Application.isPlaying || !m_isSyncPending)
        {
            return;
        }

        SyncSpline();
    }

    /// <summary>
    /// 生成対象の値が変化しているか確認します。
    /// </summary>
    /// <returns>true：変更あり。false：変更なし。</returns>
    private bool HasSplineChanges(int nodeCount, bool isClosed)
    {
        if (m_splineMeshSpline.nodes.Count != nodeCount ||
            m_splineMeshSpline.IsLoop != isClosed)
        {
            return true;
        }

        for (int i = 0; i < nodeCount; i++)
        {
            SplineMeshNode node = m_splineMeshSpline.nodes[i];
            if (node == null ||
                !node.Position.Equals(m_positions[i]) ||
                !node.Direction.Equals(CalculateDirection(i, nodeCount, isClosed)) ||
                !node.Up.Equals(m_upVectors[i]) ||
                !node.Scale.Equals(Vector2.one) ||
                node.Roll != 0.0f)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// スプラインのサンプリング結果を読み取ります。
    /// </summary>
    private void EvaluateSourceSpline(UnitySpline spline, float t, out Vector3 position, out Vector3 up)
    {
        SplineUtility.Evaluate(spline, t, out float3 splinePosition, out _, out float3 splineUp);
        position = splinePosition;
        up = splineUp;
        up = up.sqrMagnitude <= TANGENT_EPSILON ? Vector3.up : up.normalized;
    }

    /// <summary>
    /// サンプル位置からSplineMeshの制御点を計算します。
    /// </summary>
    private Vector3 CalculateDirection(int index, int nodeCount, bool isClosed)
    {
        Vector3 currentPosition = m_positions[index];

        if (isClosed && index == nodeCount - 1)
        {
            return CalculateDirection(0, nodeCount, true);
        }

        if (isClosed)
        {
            int uniquePointCount = nodeCount - 1;
            int previousIndex = index == 0 ? uniquePointCount - 1 : index - 1;
            int nextIndex = index + 1 == uniquePointCount ? 0 : index + 1;
            return currentPosition +
                   (m_positions[nextIndex] - m_positions[previousIndex]) * INNER_TANGENT_FACTOR;
        }

        if (index == 0)
        {
            return currentPosition + (m_positions[1] - currentPosition) * OPEN_ENDPOINT_TANGENT_FACTOR;
        }

        if (index == nodeCount - 1)
        {
            return currentPosition + (currentPosition - m_positions[index - 1]) * OPEN_ENDPOINT_TANGENT_FACTOR;
        }

        return currentPosition +
               (m_positions[index + 1] - m_positions[index - 1]) * INNER_TANGENT_FACTOR;
    }

    /// <summary>
    /// ノード数を必要数に揃えます。
    /// </summary>
    private void EnsureNodeCount(int requiredNodeCount)
    {
        while (m_splineMeshSpline.nodes.Count < requiredNodeCount)
        {
            m_splineMeshSpline.AddNode(new SplineMeshNode(Vector3.zero, Vector3.right));
        }

        while (m_splineMeshSpline.nodes.Count > requiredNodeCount)
        {
            int lastIndex = m_splineMeshSpline.nodes.Count - 1;
            m_splineMeshSpline.RemoveNode(m_splineMeshSpline.nodes[lastIndex]);
        }
    }

    /// <summary>
    /// サンプル配列を必要な数だけ確保します。
    /// </summary>
    private void EnsureSampleBuffer(int requiredCount)
    {
        if (m_positions == null || m_positions.Length != requiredCount)
        {
            m_positions = new Vector3[requiredCount];
            m_upVectors = new Vector3[requiredCount];
        }
    }

    private void CacheComponents()
    {
        if (m_splineContainer == null)
        {
            TryGetComponent(out m_splineContainer);
        }

        if (m_splineMeshSpline == null)
        {
            TryGetComponent(out m_splineMeshSpline);
        }
    }

    /// <summary>
    /// 指定された番号のSplineを取得します。
    /// </summary>
    /// <returns>true：取得成功。false：取得失敗。</returns>
    private bool TryGetSourceSpline(out UnitySpline spline)
    {
        spline = null;
        if (m_splineContainer == null || m_splineMeshSpline == null)
        {
            return false;
        }

        if (m_splineIndex < 0 || m_splineIndex >= m_splineContainer.Splines.Count)
        {
            Debug.LogWarning($"{nameof(SplineMeshSynchronizer)}: Spline Index {m_splineIndex} は範囲外です。", this);
            return false;
        }

        spline = m_splineContainer.Splines[m_splineIndex];
        return spline != null;
    }

    private bool IsTargetSpline(UnitySpline spline)
    {
        return TryGetSourceSpline(out UnitySpline targetSpline) &&
               ReferenceEquals(spline, targetSpline);
    }

    private void HandleRuntimeSplineChanged(UnitySpline spline, int knotIndex, SplineModification modification)
    {
        if (m_syncAutomatically && IsTargetSpline(spline))
        {
            RequestSync();
        }
    }

#if UNITY_EDITOR
    private void HandleEditorSplineModified(UnitySpline spline)
    {
        if (m_syncAutomatically && IsTargetSpline(spline))
        {
            RequestSync();
        }
    }

    private void OnValidate()
    {
        m_splineIndex = Mathf.Max(0, m_splineIndex);
        m_samplesPerCurve = Mathf.Max(MIN_SAMPLES_PER_CURVE, m_samplesPerCurve);

        if (!Application.isPlaying && m_syncAutomatically)
        {
            RequestSync();
        }
    }

    private void HandleDelayedEditorSync()
    {
        m_isEditorSyncScheduled = false;

        if (this == null || !isActiveAndEnabled || Application.isPlaying || !m_isSyncPending)
        {
            return;
        }

        SyncSpline();
    }

    private void CancelEditorSync()
    {
        if (!m_isEditorSyncScheduled)
        {
            return;
        }

        EditorApplication.delayCall -= HandleDelayedEditorSync;
        m_isEditorSyncScheduled = false;
    }
#endif
}