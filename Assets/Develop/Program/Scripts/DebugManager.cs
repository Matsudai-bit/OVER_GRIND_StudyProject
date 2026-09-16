using UnityEngine;

/// <summary>
/// デバッグフラグを管理するScriptableObject
/// Assets/Settings/DebugManager.asset として保存してください
/// </summary>
[CreateAssetMenu(fileName = "DebugManager", menuName = "Settings/DebugManager")]
public class DebugManager : ScriptableObject
{
    private static DebugManager _instance;

    /// <summary>
    /// シングルトンインスタンス（Resourcesフォルダから自動ロード）
    /// </summary>
    public static DebugManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<DebugManager>("Settings/DebugManager");

#if UNITY_EDITOR
                // エディタ上でResourcesに無い場合はAssetDatabaseから検索
                if (_instance == null)
                {
                    var guids = UnityEditor.AssetDatabase.FindAssets("t:DebugManager");
                    if (guids.Length > 0)
                    {
                        var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                        _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<DebugManager>(path);
                    }
                }
#endif

                if (_instance == null)
                    Debug.LogWarning("[DebugManager] DebugManager.asset が見つかりません。Resources/Settings/ に配置してください。");
            }
            return _instance;
        }
    }

    // ─────────────────────────────────────────
    // デバッグフラグ群
    // ─────────────────────────────────────────

    [Header("Global")]
    [Tooltip("デバッグ機能の全体ON/OFF")]
    public bool isDebugEnabled = false;

    [Header("カテゴリ別フラグ")]
    [Tooltip("ログ出力を有効にする")]
    public bool enableLogs = true;

    [Tooltip("GizmoやOverlayの描画を有効にする")]
    public bool enableGizmos = true;

    [Tooltip("FPS・メモリなどの統計表示を有効にする")]
    public bool enableStats = false;

    // ─────────────────────────────────────────
    // 便利プロパティ（isDebugEnabled と AND）
    // ─────────────────────────────────────────

    public bool LogsActive    => isDebugEnabled && enableLogs;
    public bool GizmosActive  => isDebugEnabled && enableGizmos;
    public bool StatsActive   => isDebugEnabled && enableStats;

    // ─────────────────────────────────────────
    // ユーティリティ
    // ─────────────────────────────────────────

    /// <summary>
    /// デバッグが有効なときだけログを出す
    /// </summary>
    public static void Log(string message)
    {
        if (Instance != null && Instance.LogsActive)
            Debug.Log($"[DEBUG] {message}");
    }

    public static void LogWarning(string message)
    {
        if (Instance != null && Instance.LogsActive)
            Debug.LogWarning($"[DEBUG] {message}");
    }

    public static void LogError(string message)
    {
        if (Instance != null && Instance.LogsActive)
            Debug.LogError($"[DEBUG] {message}");
    }
}
