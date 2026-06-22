using UnityEngine;
using UnityEngine.Profiling;
#if UNITY_2020_1_OR_NEWER
using Unity.Profiling;
#endif

public class DebugStatsMonitor : MonoBehaviour
{
    private GUIStyle _style;
    private float _deltaTime = 0.0f;

#if UNITY_2020_1_OR_NEWER
    // ドローコール（SetPass CallsやBatches）を取得するためのレコーダー
    private ProfilerRecorder _setPassCallsRecorder;
    private ProfilerRecorder _drawCallsRecorder;

    private void OnEnable()
    {
        // 描画に関する内部カウンターを有効化
        _setPassCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
        _drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
    }

    private void OnDisable()
    {
        _setPassCallsRecorder.Dispose();
        _drawCallsRecorder.Dispose();
    }
#endif

    private void Awake()
    {
        // 画面に表示するテキストの見た目（スタイル）を設定
        _style = new GUIStyle();
        _style.alignment = TextAnchor.UpperLeft;
        _style.fontSize = 20;
        _style.normal.textColor = Color.cyan; // 見やすいようにシアン（水色）に設定
    }

    private void Update()
    {
        // DebugManagerの「統計表示」がOFFなら計算もしない
        if (DebugManager.Instance == null || !DebugManager.Instance.StatsActive) return;

        // 処理時間（FPS）計算用のデルタタイムを計算
        _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
    }

    private void OnGUI()
    {
        // DebugManagerの「統計表示」がOFFなら画面に何も描画しない
        if (DebugManager.Instance == null || !DebugManager.Instance.StatsActive) return;

        // 1. 処理時間 & FPS の計算
        float msec = _deltaTime * 1000.0f;
        float fps = 1.0f / _deltaTime;

        // 2. メモリ使用量の取得（Byte単位で取れるため、MBに変換）
        long totalMemory = Profiler.GetTotalAllocatedMemoryLong();
        double totalMemoryMB = (totalMemory / 1024.0) / 1024.0;

        long reservedMemory = Profiler.GetTotalReservedMemoryLong();
        double reservedMemoryMB = (reservedMemory / 1024.0) / 1024.0;

        // 3. ドローコールの取得
        long drawCalls = 0;
        long setPassCalls = 0;
#if UNITY_2020_1_OR_NEWER
        if (_drawCallsRecorder.Valid) drawCalls = _drawCallsRecorder.LastValue;
        if (_setPassCallsRecorder.Valid) setPassCalls = _setPassCallsRecorder.LastValue;
#endif

        // 表示する文字列を構築
        string text = string.Format(
            "=== PERFORMANCE STATS ===\n" +
            "■ 処理時間: {0:0.0} ms ({1:0.} FPS)\n" +
            "■ メモリ (Allocated): {2:0.0} MB / (Reserved): {3:0.0} MB\n" +
            "■ ドローコール (DrawCalls): {4} / (SetPass): {5}",
            msec, fps, totalMemoryMB, reservedMemoryMB, drawCalls, setPassCalls
        );

        // 画面の左上（座標: 20, 20）から余裕を持ったサイズで描画
        Rect rect = new Rect(20, 20, 500, 200);

        // 背景をうっすら暗くして文字を読みやすくする（お好みで）
        GUI.Box(rect, "");
        GUI.Label(rect, text, _style);
    }
}