using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;



/******************************************************************************
 * @file    DebugMonitorWindow.cs
 * @brief   DebugMonitorウィンドウ
 * @author  Ryo Yagi
 * @date    2026/06/19
 *
 ******************************************************************************/
public class DebugMonitorWindow : EditorWindow
{
    private Vector2 scroll;

    // オブジェクトごとのFoldout開閉状態を保持
    private Dictionary<Object, bool> foldouts = new();

    // ピン留めされたフィールドのUniqueKeyを管理
    private HashSet<string> pinnedKeys = new();

    // 検索ボックスの入力文字列
    private string searchText = "";

    [MenuItem("Tools/Debug Monitor")]
    public static void Open()
    {
        GetWindow<DebugMonitorWindow>("Debug Monitor");
    }

    private void OnGUI()
    {
        // Playモード中のみ利用可能
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Play中のみ使用できます",
                MessageType.Info);

            return;
        }

        // DebugMonitor属性付きフィールドを収集
        List<DebugParameterData> fields =
            DebugMonitorScanner.Scan();

        EditorGUILayout.Space();

        searchText =
            EditorGUILayout.TextField(
                "Search",
                searchText);

        EditorGUILayout.Space();

        scroll =
            EditorGUILayout.BeginScrollView(scroll);

        // ピン留め項目を常に先頭へ表示
        var sortedFields = fields
            .OrderByDescending(f =>
                pinnedKeys.Contains(f.UniqueKey))
            .ToList();

        // ピン留め済み項目のみ抽出
        var pinnedFields = sortedFields
            .Where(f => pinnedKeys.Contains(f.UniqueKey))
            .ToList();

        if (pinnedFields.Count > 0)
        {
            EditorGUILayout.LabelField(
                "★ Pinned",
                EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            foreach (var field in pinnedFields)
            {
                // 名前検索フィルタ
                if (!string.IsNullOrEmpty(searchText) &&
                    !field.Name.ToLower()
                        .Contains(searchText.ToLower()))
                    continue;

                // ピン留め一覧では所属オブジェクト名も表示
                DrawField(field, showObjectName: true);
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "",
                GUI.skin.horizontalSlider);
            EditorGUILayout.Space();
        }

        // 対象オブジェクトごとにグループ化
        var groups =
            sortedFields.GroupBy(x => x.Target);

        foreach (var group in groups)
        {
            Object target = group.Key;

            // 検索結果に一致する項目のみ残す
            var filteredGroup =
                group.Where(field =>
                {
                    if (string.IsNullOrEmpty(searchText))
                        return true;

                    return field.Name
                        .ToLower()
                        .Contains(searchText.ToLower());
                })
                .ToList();

            if (filteredGroup.Count == 0)
                continue;

            // 初回表示時は展開状態にする
            if (!foldouts.ContainsKey(target))
                foldouts[target] = true;

            foldouts[target] =
                EditorGUILayout.Foldout(
                    foldouts[target],
                    $"{target.GetType().Name} ({target.name})",
                    true);

            if (!foldouts[target])
                continue;

            EditorGUI.indentLevel++;

            foreach (var field in filteredGroup)
            {
                DrawField(field);
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndScrollView();

        // 値変化を即時反映するため毎フレーム再描画
        Repaint();
    }

    /******************************************************************************
     * @fn      DrawField
     * @brief   型に応じた描画処理
     *
     * @param   field : 表示対象
     *
     ******************************************************************************/
    private void DrawField(
        DebugParameterData field,
        bool showObjectName = false)
    {
        // ピン留めセクションではオブジェクト名をラベルに付与
        string label = showObjectName
            ? $"[{field.Target.GetType().Name}] {field.Target.name} / {field.Name} ({field.TypeName})"
            : $"{field.Name} ({field.TypeName})";

        EditorGUILayout.BeginHorizontal();

        // int
        if (field.Field.FieldType == typeof(int))
        {
            int value = (int)field.Value;

            int newValue =
                EditorGUILayout.IntField(
                    label,
                    value);

            if (newValue != value)
            {
                field.SetValue(newValue);
            }

            DrawPinButton(field);
            EditorGUILayout.EndHorizontal();
            return;
        }

        // float
        if (field.Field.FieldType == typeof(float))
        {
            float value = (float)field.Value;

            float newValue =
                EditorGUILayout.FloatField(
                    label,
                    value);

            if (!Mathf.Approximately(
                newValue,
                value))
            {
                field.SetValue(newValue);
            }

            DrawPinButton(field);
            EditorGUILayout.EndHorizontal();
            return;
        }

        // bool
        if (field.Field.FieldType == typeof(bool))
        {
            bool value = (bool)field.Value;

            bool newValue =
                EditorGUILayout.Toggle(
                    label,
                    value);

            if (newValue != value)
            {
                field.SetValue(newValue);
            }

            DrawPinButton(field);
            EditorGUILayout.EndHorizontal();
            return;
        }

        // string
        if (field.Field.FieldType == typeof(string))
        {
            string value =
                (string)field.Value;

            string newValue =
                EditorGUILayout.TextField(
                    label,
                    value);

            if (newValue != value)
            {
                field.SetValue(newValue);
            }

            DrawPinButton(field);
            EditorGUILayout.EndHorizontal();
            return;
        }

        // 未対応型
        EditorGUILayout.LabelField(
            label,
            field.Value?.ToString() ?? "null");

        DrawPinButton(field);
        EditorGUILayout.EndHorizontal();
    }

    /******************************************************************************
 * @fn      DrawPinButton
 * @brief   ピンボタン描画
 *
 * @param   field : 対象フィールド
 *
 ******************************************************************************/
    private void DrawPinButton(
        DebugParameterData field)
    {
        bool isPinned =
            pinnedKeys.Contains(
                field.UniqueKey);

        string buttonText =
            isPinned ? "★" : "☆";

        if (GUILayout.Button(
            buttonText,
            GUILayout.Width(25)))
        {
            if (isPinned)
            {
                pinnedKeys.Remove(
                    field.UniqueKey);
            }
            else
            {
                pinnedKeys.Add(
                    field.UniqueKey);
            }
        }
    }
}