using UnityEngine;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Text;
using Unity.Profiling;
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 実行時のパフォーマンス情報を表示します。
/// </summary>
[DisallowMultipleComponent]
public sealed class DebugRuntimeStats : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD

    private const float UPDATE_INTERVAL = 0.25f;
    private const double NANOSECONDS_TO_MS = 0.000001;
    private const double BYTES_TO_MB = 1.0 / (1024.0 * 1024.0);

    [SerializeField, Header("初期表示状態")]
    private bool m_isVisible = true;

    [SerializeField, Header("表示位置・サイズ")]
    private Rect m_displayRect = new Rect(10, 10, 320, 310);

    // CPU
    private ProfilerRecorder m_mainThreadRecorder;
    private ProfilerRecorder m_renderThreadRecorder;

    // Rendering
    private ProfilerRecorder m_batchesRecorder;
    private ProfilerRecorder m_drawCallsRecorder;
    private ProfilerRecorder m_setPassCallsRecorder;
    private ProfilerRecorder m_trianglesRecorder;
    private ProfilerRecorder m_verticesRecorder;

    // Memory
    private ProfilerRecorder m_usedMemoryRecorder;
    private ProfilerRecorder m_gcUsedMemoryRecorder;
    private ProfilerRecorder m_gcAllocatedRecorder;

    // 表示情報
    private readonly StringBuilder m_builder = new(512);
    private string m_displayText = string.Empty;

    private float m_elapsedTime;
    private int m_frameCount;

    private void OnEnable()
    {
        InitializeRecorders();
    }

    private void OnDisable()
    {
        DisposeRecorders();
    }

    private void Update()
    {
        // 表示状態の切り替え
        if (Keyboard.current != null &&
            Keyboard.current.f3Key.wasPressedThisFrame)
        {
            m_isVisible = !m_isVisible;
        }

        // 非表示時は表示文字列の更新を行わない
        if (!m_isVisible)
        {
            m_elapsedTime = 0.0f;
            m_frameCount = 0;
            return;
        }

        // FPS計算用の情報を蓄積
        m_elapsedTime += Time.unscaledDeltaTime;
        m_frameCount++;

        if (m_elapsedTime < UPDATE_INTERVAL)
        {
            return;
        }

        float fps = m_frameCount / m_elapsedTime;
        float frameTime = m_elapsedTime / m_frameCount * 1000.0f;

        UpdateDisplayText(fps, frameTime);

        m_elapsedTime = 0.0f;
        m_frameCount = 0;
    }

    private void OnGUI()
    {
        if (!m_isVisible)
        {
            return;
        }

        GUI.Box(m_displayRect, "Runtime Statistics");
        GUILayout.BeginArea(m_displayRect);
        GUILayout.Space(25);
        GUILayout.Label(m_displayText);
        GUILayout.EndArea();
    }

    /// <summary>
    /// パフォーマンス計測を初期化します。
    /// </summary>
    private void InitializeRecorders()
    {
        // CPU
        m_mainThreadRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Internal, "Main Thread");

        m_renderThreadRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Internal, "Render Thread");

        // Rendering
        m_batchesRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Render, "Batches Count");

        m_drawCallsRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Render, "Draw Calls Count");

        m_setPassCallsRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Render, "SetPass Calls Count");

        m_trianglesRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Render, "Triangles Count");

        m_verticesRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Render, "Vertices Count");

        // Memory
        m_usedMemoryRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Memory, "Total Used Memory");

        m_gcUsedMemoryRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Memory, "GC Used Memory");

        m_gcAllocatedRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Memory, "GC Allocated In Frame");
    }

    /// <summary>
    /// 表示するパフォーマンス情報を更新します。
    /// </summary>
    private void UpdateDisplayText(float fps, float frameTime)
    {
        m_builder.Clear();

        m_builder.AppendLine($"FPS : {fps:F1}");
        m_builder.AppendLine($"Frame Time : {frameTime:F2} ms");

        m_builder.AppendLine();
        m_builder.AppendLine("[ CPU ]");

        m_builder.AppendLine(
            $"Main Thread : {FormatValue(m_mainThreadRecorder, NANOSECONDS_TO_MS, "ms")}");

        m_builder.AppendLine(
            $"Render Thread : {FormatValue(m_renderThreadRecorder, NANOSECONDS_TO_MS, "ms")}");

        m_builder.AppendLine();
        m_builder.AppendLine("[ Rendering ]");

        m_builder.AppendLine(
            $"Batches : {FormatCount(m_batchesRecorder)}");

        m_builder.AppendLine(
            $"Draw Calls : {FormatCount(m_drawCallsRecorder)}");

        m_builder.AppendLine(
            $"SetPass Calls : {FormatCount(m_setPassCallsRecorder)}");

        m_builder.AppendLine(
            $"Triangles : {FormatCount(m_trianglesRecorder)}");

        m_builder.AppendLine(
            $"Vertices : {FormatCount(m_verticesRecorder)}");

        m_builder.AppendLine();
        m_builder.AppendLine("[ Memory ]");

        m_builder.AppendLine(
            $"Used Memory : {FormatValue(m_usedMemoryRecorder, BYTES_TO_MB, "MB")}");

        m_builder.AppendLine(
            $"GC Used Memory : {FormatValue(m_gcUsedMemoryRecorder, BYTES_TO_MB, "MB")}");

        m_builder.AppendLine(
            $"GC Allocated : {FormatCount(m_gcAllocatedRecorder)} Bytes");

        m_displayText = m_builder.ToString();
    }

    /// <summary>
    /// 計測値を指定された単位に変換します。
    /// </summary>
    private static string FormatValue(
        ProfilerRecorder recorder,
        double multiplier,
        string unit)
    {
        if (!recorder.Valid)
        {
            return "N/A";
        }

        return $"{recorder.LastValue * multiplier:F2} {unit}";
    }

    /// <summary>
    /// 計測値を整数形式に変換します。
    /// </summary>
    private static string FormatCount(ProfilerRecorder recorder)
    {
        return recorder.Valid
            ? recorder.LastValue.ToString("N0")
            : "N/A";
    }

    /// <summary>
    /// パフォーマンス計測用のリソースを解放します。
    /// </summary>
    private void DisposeRecorders()
    {
        m_mainThreadRecorder.Dispose();
        m_renderThreadRecorder.Dispose();

        m_batchesRecorder.Dispose();
        m_drawCallsRecorder.Dispose();
        m_setPassCallsRecorder.Dispose();
        m_trianglesRecorder.Dispose();
        m_verticesRecorder.Dispose();

        m_usedMemoryRecorder.Dispose();
        m_gcUsedMemoryRecorder.Dispose();
        m_gcAllocatedRecorder.Dispose();
    }

#endif
}