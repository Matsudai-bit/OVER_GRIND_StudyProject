using UnityEditor;
using UnityEngine;

/// <summary>
/// DebugManager ScriptableObject のカスタムInspector
/// このファイルは Assets/Editor/ フォルダに配置してください
/// </summary>
[CustomEditor(typeof(DebugManager))]
public class DebugManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var manager = (DebugManager)target;

        // ヘッダー
        EditorGUILayout.Space(4);
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("🐛 Debug Manager", titleStyle);
        EditorGUILayout.Space(8);

        // ─── Global Toggle ───────────────────────
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            EditorGUILayout.Space(4);

            Color prevColor = GUI.backgroundColor;
            GUI.backgroundColor = manager.isDebugEnabled
                ? new Color(0.4f, 1f, 0.4f)   // 緑: ON
                : new Color(1f, 0.4f, 0.4f);   // 赤: OFF

            string label = manager.isDebugEnabled ? "■ Debug Mode : ON" : "□ Debug Mode : OFF";
            if (GUILayout.Button(label, GUILayout.Height(36)))
            {
                Undo.RecordObject(manager, "Toggle Debug Mode");
                manager.isDebugEnabled = !manager.isDebugEnabled;
                // EditorPrefsにも反映
                EditorPrefs.SetBool("DebugMenu_IsDebugEnabled", manager.isDebugEnabled);
                EditorUtility.SetDirty(manager);
            }

            GUI.backgroundColor = prevColor;
            EditorGUILayout.Space(4);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8);

        // ─── Category Flags ──────────────────────
        EditorGUI.BeginDisabledGroup(!manager.isDebugEnabled);
        {
            EditorGUILayout.LabelField("カテゴリ別フラグ", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                DrawToggleRow(manager, ref manager.enableLogs,   "📋 ログ出力",   "Debug.Log 系の出力を有効にする");
                DrawToggleRow(manager, ref manager.enableGizmos, "🔷 Gizmo描画", "Scene ビューの Gizmo / Overlay を有効にする");
                DrawToggleRow(manager, ref manager.enableStats,  "📊 統計表示",  "FPS・メモリ等のデバッグ統計を有効にする");
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(8);

        // ─── Status ──────────────────────────────
        EditorGUILayout.LabelField("現在の状態", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            DrawStatus("ログ出力",   manager.LogsActive);
            DrawStatus("Gizmo描画", manager.GizmosActive);
            DrawStatus("統計表示",  manager.StatsActive);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8);

        // ─── Help ─────────────────────────────────
        EditorGUILayout.HelpBox(
            "■ コードからの参照方法\n" +
            "  DebugManager.Instance.IsDebugEnabled\n" +
            "  DebugManager.Log(\"message\");\n\n" +
            "■ #if によるビルド除外\n" +
            "  #if DEBUG_MODE\n" +
            "      // デバッグ処理\n" +
            "  #endif\n\n" +
            "■ Tool > Debug > Toggle Debug Mode でも切替可能",
            MessageType.Info);

        if (GUI.changed)
        {
            EditorUtility.SetDirty(manager);
        }
    }

    private void DrawToggleRow(DebugManager manager, ref bool value, string label, string tooltip)
    {
        EditorGUILayout.BeginHorizontal();
        bool newVal = EditorGUILayout.ToggleLeft(
            new GUIContent(label, tooltip), value);
        if (newVal != value)
        {
            Undo.RecordObject(manager, $"Toggle {label}");
            value = newVal;
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawStatus(string label, bool active)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(80));
        GUIStyle st = new GUIStyle(EditorStyles.label)
        {
            fontStyle = FontStyle.Bold,
            normal = { textColor = active ? Color.green : Color.gray }
        };
        EditorGUILayout.LabelField(active ? "● ACTIVE" : "○ INACTIVE", st);
        EditorGUILayout.EndHorizontal();
    }
}
