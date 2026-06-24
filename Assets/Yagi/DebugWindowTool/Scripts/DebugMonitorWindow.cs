using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/******************************************************************************
 * @file    DebugMonitorWindow.cs
 * @brief   DebugMonitorウィンドウ
 * @author  Ryo Yagi (Modified)
 * @date    2026/06/19
 ******************************************************************************/
public class DebugMonitorWindow : EditorWindow
{
    private Vector2 scroll;
    private Dictionary<Object, bool> foldouts = new();
    private HashSet<string> pinnedKeys = new();
    private string searchText = "";

    // 各列の幅（ドラッグで可変）
    private float objectColumnWidth = 120f;
    private float nameColumnWidth = 150f;

    // 描画用の行カウント（背景のストライプ描画用）
    private int rowIndex = 0;

    [MenuItem("Tools/Debug Monitor")]
    public static void Open()
    {
        GetWindow<DebugMonitorWindow>("Debug Monitor");
    }

    private void OnGUI()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Play中のみ使用できます", MessageType.Info);
            return;
        }

        List<DebugParameterData> fields = DebugMonitorScanner.Scan();

        EditorGUILayout.Space();
        searchText = EditorGUILayout.TextField("Search", searchText);
        EditorGUILayout.Space();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        rowIndex = 0; // 行カウントリセット

        var sortedFields = fields.OrderByDescending(f => pinnedKeys.Contains(f.UniqueKey)).ToList();
        var pinnedFields = sortedFields.Where(f => pinnedKeys.Contains(f.UniqueKey)).ToList();

        // ==========================================
        // ピン留めセクション
        // ==========================================
        if (pinnedFields.Count > 0)
        {
            EditorGUILayout.LabelField("★ Pinned", EditorStyles.boldLabel);
            DrawHeader(true);

            EditorGUI.indentLevel++;
            foreach (var field in pinnedFields)
            {
                if (!string.IsNullOrEmpty(searchText) && !field.Name.ToLower().Contains(searchText.ToLower()))
                    continue;

                DrawField(field, isPinned: true);
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space();
        }

        // ==========================================
        // 通常セクション
        // ==========================================
        DrawHeader(false);

        var groups = sortedFields.GroupBy(x => x.Target);
        foreach (var group in groups)
        {
            Object target = group.Key;
            var filteredGroup = group.Where(field =>
            {
                if (string.IsNullOrEmpty(searchText)) return true;
                return field.Name.ToLower().Contains(searchText.ToLower());
            }).ToList();

            if (filteredGroup.Count == 0) continue;

            if (!foldouts.ContainsKey(target)) foldouts[target] = true;

            // Foldoutの背景も少し色を付ける
            Rect foldoutRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            EditorGUI.DrawRect(foldoutRect, new Color(0.2f, 0.2f, 0.2f, 0.5f));

            foldouts[target] = EditorGUI.Foldout(foldoutRect, foldouts[target], $"{target.GetType().Name} ({target.name})", true);

            if (!foldouts[target]) continue;

            EditorGUI.indentLevel++;
            foreach (var field in filteredGroup)
            {
                DrawField(field, isPinned: false);
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndScrollView();
        Repaint();
    }

    /******************************************************************************
     * @fn      DrawHeader
     * @brief   カラムのヘッダーを描画
     ******************************************************************************/
    private void DrawHeader(bool isPinnedSection)
    {
        // 高さを1行分に固定
        Rect headerRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(EditorGUIUtility.singleLineHeight));

        // ヘッダーの背景色
        EditorGUI.DrawRect(headerRect, new Color(0.15f, 0.15f, 0.15f, 1f));

        float indentSpace = EditorGUI.indentLevel * 15f;
        GUILayout.Space(indentSpace);

        if (isPinnedSection)
        {
            EditorGUILayout.LabelField("Object", EditorStyles.boldLabel, GUILayout.Width(objectColumnWidth));
            DrawSplitter(ref objectColumnWidth);
        }

        EditorGUILayout.LabelField("Variable", EditorStyles.boldLabel, GUILayout.Width(nameColumnWidth));
        DrawSplitter(ref nameColumnWidth);

        EditorGUILayout.LabelField("Value", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        // ヘッダー下部のライン
        Rect lineRect = new Rect(headerRect.x, headerRect.yMax - 1, headerRect.width, 1);
        EditorGUI.DrawRect(lineRect, Color.gray);
    }

    /******************************************************************************
     * @fn      DrawField
     * @brief   型に応じた描画処理（セル分割対応版）
     ******************************************************************************/
    private void DrawField(DebugParameterData field, bool isPinned)
    {
        // 高さを1行分に固定
        Rect rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(EditorGUIUtility.singleLineHeight));

        // 奇数行・偶数行で背景色を変える（ストライプ表示）
        if (rowIndex % 2 == 0)
        {
            EditorGUI.DrawRect(rowRect, new Color(0.3f, 0.3f, 0.3f, 0.2f));
        }
        rowIndex++;

        if (isPinned)
        {
            EditorGUILayout.LabelField($"{field.Name} ({field.TypeName})", GUILayout.Width(nameColumnWidth));
            DrawSplitter(ref objectColumnWidth);
        }

        EditorGUILayout.LabelField($"{field.Name} ({field.TypeName})", GUILayout.Width(nameColumnWidth));
        DrawSplitter(ref nameColumnWidth);

        DrawValueField(field);
        DrawPinButton(field);

        EditorGUILayout.EndHorizontal();
    }

    /******************************************************************************
     * @fn      DrawSplitter
     * @brief   ドラッグで幅を変更できるスプリッターを描画
     ******************************************************************************/
    private void DrawSplitter(ref float width)
    {
        // 【修正箇所】GUILayout.ExpandHeight(true) を削除し、1行分の高さだけ取得するように変更
        Rect rect = GUILayoutUtility.GetRect(2f, EditorGUIUtility.singleLineHeight);
        EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);

        int controlID = GUIUtility.GetControlID(FocusType.Passive);
        Event evt = Event.current;

        switch (evt.GetTypeForControl(controlID))
        {
            case EventType.MouseDown:
                if (rect.Contains(evt.mousePosition))
                {
                    GUIUtility.hotControl = controlID;
                    evt.Use();
                }
                break;
            case EventType.MouseDrag:
                if (GUIUtility.hotControl == controlID)
                {
                    width += evt.delta.x;
                    width = Mathf.Max(50f, width); // 最小幅
                    evt.Use();
                }
                break;
            case EventType.MouseUp:
                if (GUIUtility.hotControl == controlID)
                {
                    GUIUtility.hotControl = 0;
                    evt.Use();
                }
                break;
        }

        // 縦線を描画（セルの区切りを明確に）
        Rect lineRect = new Rect(rect.x, rect.y, 1, rect.height);
        EditorGUI.DrawRect(lineRect, new Color(0.5f, 0.5f, 0.5f, 0.4f));
    }

    /******************************************************************************
     * @fn      DrawValueField
     * @brief   値の描画と更新処理
     ******************************************************************************/
    private void DrawValueField(DebugParameterData field)
    {
        System.Type type = field.Field.FieldType;

        if (type == typeof(int))
        {
            int val = EditorGUILayout.IntField(GUIContent.none, (int)field.Value);
            if (val != (int)field.Value) field.SetValue(val);
        }
        else if (type == typeof(float))
        {
            float val = EditorGUILayout.FloatField(GUIContent.none, (float)field.Value);
            if (!Mathf.Approximately(val, (float)field.Value)) field.SetValue(val);
        }
        else if (type == typeof(bool))
        {
            bool val = EditorGUILayout.Toggle(GUIContent.none, (bool)field.Value);
            if (val != (bool)field.Value) field.SetValue(val);
        }
        else if (type == typeof(string))
        {
            string val = EditorGUILayout.TextField(GUIContent.none, (string)field.Value);
            if (val != (string)field.Value) field.SetValue(val);
        }
        // ↓ ここから追加
        else if (type == typeof(Vector2))
        {
            Vector2 val = EditorGUILayout.Vector2Field(GUIContent.none, (Vector2)field.Value);
            if (val != (Vector2)field.Value) field.SetValue(val);
        }
        else if (type == typeof(Vector3))
        {
            Vector3 val = EditorGUILayout.Vector3Field(GUIContent.none, (Vector3)field.Value);
            if (val != (Vector3)field.Value) field.SetValue(val);
        }
        else if (type == typeof(Vector4))
        {
            Vector4 val = EditorGUILayout.Vector4Field(GUIContent.none, (Vector4)field.Value);
            if (val != (Vector4)field.Value) field.SetValue(val);
        }
        else if (type == typeof(Color))
        {
            Color val = EditorGUILayout.ColorField(GUIContent.none, (Color)field.Value);
            if (val != (Color)field.Value) field.SetValue(val);
        }
        else if (type == typeof(Quaternion))
        {
            // QuaternionはEuler角で表示・編集するほうが直感的
            Quaternion current = (Quaternion)field.Value;
            Vector3 euler = EditorGUILayout.Vector3Field(GUIContent.none, current.eulerAngles);
            if (euler != current.eulerAngles) field.SetValue(Quaternion.Euler(euler));
        }
        else if (type == typeof(Rect))
        {
            Rect val = EditorGUILayout.RectField(GUIContent.none, (Rect)field.Value);
            if (val != (Rect)field.Value) field.SetValue(val);
        }
        else if (type == typeof(Bounds))
        {
            Bounds val = EditorGUILayout.BoundsField(GUIContent.none, (Bounds)field.Value);
            if (val != (Bounds)field.Value) field.SetValue(val);
        }
        // ↑ ここまで追加
        else
        {
            EditorGUILayout.LabelField(field.Value?.ToString() ?? "null", GUILayout.ExpandWidth(true));
        }
    }

    /******************************************************************************
     * @fn      DrawPinButton
     * @brief   ピンボタン描画
     ******************************************************************************/
    private void DrawPinButton(DebugParameterData field)
    {
        bool isPinned = pinnedKeys.Contains(field.UniqueKey);
        string buttonText = isPinned ? "★" : "☆";

        if (GUILayout.Button(buttonText, GUILayout.Width(25)))
        {
            if (isPinned) pinnedKeys.Remove(field.UniqueKey);
            else pinnedKeys.Add(field.UniqueKey);
        }
    }
}

/******************************************************************************
 * @class   DebugMonitorHierarchyTooltip
 * @brief   ヒエラルキー上のツールチップ表示機能 (Unity 6 対応版)
 ******************************************************************************/
[InitializeOnLoad]
public static class DebugMonitorHierarchyTooltip
{
    private static int _hoveredInstanceID = -1;
    private static string _hoveredInfo = "";
    // ホバー行のスクリーン座標Y（ウィンドウ外描画に使用）
    private static Rect _hoveredScreenRect;

    static DebugMonitorHierarchyTooltip()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        // ウィンドウ外に描画するためSceneViewのOnGUIを借りる
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
    {
        Event e = Event.current;

        if (selectionRect.Contains(e.mousePosition))
        {
            if (_hoveredInstanceID != instanceID)
            {
                _hoveredInstanceID = instanceID;
                _hoveredInfo = BuildInfo(instanceID);

                // selectionRect はウィンドウローカル座標なので
                // GUIUtility.GUIToScreenPoint でスクリーン座標へ変換して保存
                Vector2 screenPos = GUIUtility.GUIToScreenPoint(
                    new Vector2(selectionRect.x, selectionRect.y));
                _hoveredScreenRect = new Rect(screenPos.x, screenPos.y,
                    selectionRect.width, selectionRect.height);

                EditorApplication.RepaintHierarchyWindow();
            }
        }
        else if (_hoveredInstanceID == instanceID)
        {
            _hoveredInstanceID = -1;
            _hoveredInfo = "";
            EditorApplication.RepaintHierarchyWindow();
        }
    }

    // EditorWindowを継承した専用のオーバーレイウィンドウで描画
    private static void OnSceneGUI(SceneView sv)
    {
        // SceneViewのOnGUIは借りない → 専用ウィンドウへ委譲
    }

    // ★ 専用のEditorWindowでオーバーレイを描画する
    [InitializeOnLoad]
    public class HierarchyOverlayWindow : EditorWindow
    {
        static HierarchyOverlayWindow()
        {
            EditorApplication.update += EnsureWindow;
        }

        private static HierarchyOverlayWindow _instance;

        private static void EnsureWindow()
        {
            if (_instance != null) return;
            _instance = CreateInstance<HierarchyOverlayWindow>();
            // タイトルバーなし・フォーカス不要の透明ウィンドウ
            _instance.ShowPopup();
            _instance.minSize = Vector2.zero;
        }

        private void OnGUI()
        {
            if (string.IsNullOrEmpty(_hoveredInfo))
            {
                // 非表示時はウィンドウを0サイズに縮小
                position = new Rect(-9999, -9999, 1, 1);
                return;
            }

            GUIStyle style = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleLeft,
            };

            GUIContent content = new GUIContent(_hoveredInfo);
            Vector2 size = style.CalcSize(content);

            // ヒエラルキ行の左側にポップアップ表示
            float x = _hoveredScreenRect.x - size.x - 8f;
            float y = _hoveredScreenRect.y;

            position = new Rect(x, y, size.x + 4f, size.y);

            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height),
                new Color(0.1f, 0.1f, 0.1f, 0.95f));
            GUI.Label(new Rect(0, 0, position.width, position.height),
                content, style);

            Repaint();
        }
    }

    private static string BuildInfo(int instanceID)
    {
#pragma warning disable CS0618
        GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
#pragma warning restore CS0618

        if (go == null) return "";

        var monos = go.GetComponents<MonoBehaviour>();
        if (monos.Length == 0) return "";

        string result = "";
        foreach (var mono in monos)
        {
            if (mono == null) continue;

            var fields = mono.GetType().GetFields(
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                if (field.GetCustomAttribute<DebugParameterFieldAttribute>() == null)
                    continue;

                string valueStr = Application.isPlaying
                    ? (field.GetValue(mono)?.ToString() ?? "null")
                    : "(停止中)";

                result += $"[{field.FieldType.Name}] {field.Name} = {valueStr}\n";
            }
        }

        return result.TrimEnd();
    }
}