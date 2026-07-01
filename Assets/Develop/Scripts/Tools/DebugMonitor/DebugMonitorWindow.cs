using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;

/******************************************************************************
 * @file    DebugMonitorWindow.cs
 * @brief   DebugMonitorウィンドウ
 * @author  Ryo Yagi (Modified)
 * @date    2026/06/19
 ******************************************************************************/
public class DebugMonitorWindow : EditorWindow
{
    private Vector2 scroll;                                             // スクロール位置の記憶
    private Dictionary<UnityEngine.Object, bool> foldouts = new();      // 折り畳み状態か否か
    private HashSet<string> pinnedKeys = new();                         // ピン止め中のUniqueKey一覧
    private string searchText = "";                                     // 検索ワード（検索欄）

    // 各列の幅（ドラッグで可変）
    private float objectColumnWidth = 120f;                             // オブジェクト列の幅（初期値）
    private float nameColumnWidth = 150f;                               // 変数名列の幅（初期値）

    // 描画用の行カウント（背景のストライプ描画用）
    private int rowIndex = 0;                                           // ストライプ描画用の行カウント

    // プリセット管理
    private Dictionary<string, HashSet<string>> _presets = new();       // プリセット一覧
    private string _presetInputName = "";                               // プリセット名入力欄の中身
    private const string PresetFilePath = "Assets/Editor/DebugMonitor/Presets.json";

    [MenuItem("Tools/Debug Monitor")]
    public static void Open()
    {
        GetWindow<DebugMonitorWindow>("Debug Monitor");
    }

    private void OnEnable()     // 最初に１回だけ呼び出される
    {
        LoadPresets();          // プリセットをロード
    }

    private void OnGUI()        // 毎フレーム呼び出される
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Play中のみ使用できます", MessageType.Info);
            return;
        }

        List<DebugParameterData> fields = DebugMonitorScanner.Scan();   // 全てのオブジェクトにあるDebugParameterFieldをスキャン

        // 検索欄の描画
        EditorGUILayout.Space();
        searchText = EditorGUILayout.TextField("Search", searchText);
        EditorGUILayout.Space();
        // プリセット欄の描画
        DrawPresetSection(); // ← 追加
        EditorGUILayout.Space();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        rowIndex = 0; // 行カウントリセット

        // ピン留め済みのものを先頭にソート
        var sortedFields = fields.OrderByDescending(f => pinnedKeys.Contains(f.UniqueKey)).ToList();
        // ピン留め済みのフィールドを抽出
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
            UnityEngine.Object target = group.Key;
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
 * @fn      DrawPresetSection
 * @brief   プリセットUI描画
 ******************************************************************************/
    private void DrawPresetSection()
    {
        EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);

        // 保存欄
        EditorGUILayout.BeginHorizontal();
        _presetInputName = EditorGUILayout.TextField(_presetInputName);

        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_presetInputName));
        if (GUILayout.Button("現在のピンを保存", GUILayout.Width(120)))
        {
            SavePreset(_presetInputName);
            _presetInputName = "";      // 入力欄をクリア
            GUI.FocusControl(null);
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();

        if (_presets.Count == 0)
        {
            EditorGUILayout.LabelField("保存済みプリセットはありません",
                EditorStyles.miniLabel);
            return;
        }

        // プリセット一覧
        EditorGUILayout.Space(2);
        foreach (var presetName in _presets.Keys.ToList())
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(presetName, GUILayout.ExpandWidth(true));

            if (GUILayout.Button("読込", GUILayout.Width(40)))
            {
                LoadPreset(presetName);
            }

            // 削除は誤操作防止のため確認ダイアログを挟む
            if (GUILayout.Button("削除", GUILayout.Width(40)))
            {
                if (EditorUtility.DisplayDialog(
                    "プリセット削除",
                    $"「{presetName}」を削除しますか？",
                    "削除", "キャンセル"))
                {
                    DeletePreset(presetName);
                }
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    /******************************************************************************
     * @fn      SavePreset
     * @brief   現在のピン留めをプリセットとして保存
     *
     * @param   presetName : プリセット名
     ******************************************************************************/
    private void SavePreset(string presetName)
    {
        // ピン止め中の変数がない場合は保存しない
        if (pinnedKeys.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "保存失敗",
                "ピン留めされている変数がありません",
                "OK");
            return;
        }

        // 同名プリセットが存在する場合は上書き確認
        if (_presets.ContainsKey(presetName))
        {
            if (!EditorUtility.DisplayDialog(
                "上書き確認",
                $"「{presetName}」は既に存在します。上書きしますか？",
                "上書き", "キャンセル"))
            {
                return;
            }
        }

        // プリセット（pinnedKeys）を保存する
        _presets[presetName] = new HashSet<string>(pinnedKeys);
        SerializePresets();
    }

    /******************************************************************************
     * @fn      LoadPreset
     * @brief   プリセットのピン留めを復元
     *
     * @param   presetName : プリセット名
     ******************************************************************************/
    private void LoadPreset(string presetName)
    {

        if (!_presets.TryGetValue(presetName, out var keys)) return;
        // 現在のピン留めをクリアしてプリセットのピン留めをロード
        pinnedKeys = new HashSet<string>(keys);
    }

    /******************************************************************************
     * @fn      DeletePreset
     * @brief   プリセットを削除
     *
     * @param   presetName : プリセット名
     ******************************************************************************/
    private void DeletePreset(string presetName)
    {
        _presets.Remove(presetName);
        SerializePresets(); // JSON保存・更新
    }

    /******************************************************************************
     * @fn      SerializePresets
     * @brief   プリセットをEditorPrefsへJSON保存
     ******************************************************************************/
    private void SerializePresets()
    {
        // Dictionaryを直接JsonUtilityで扱えないため、シリアライズ用のリストに変換
        var serializable = new SerializablePresetList();
        foreach (var kvp in _presets)
        {
            serializable.presets.Add(new SerializablePreset
            {
                name = kvp.Key,
                keys = kvp.Value.ToList()
            });
        }

        // JSONに変換して保存
        string json = JsonUtility.ToJson(serializable, true); // true で整形出力

        // フォルダが存在しない場合は作成
        string dir = System.IO.Path.GetDirectoryName(PresetFilePath);
        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);

        System.IO.File.WriteAllText(PresetFilePath, json);

        // Unityのアセットデータベースに反映・変更を通知
        AssetDatabase.Refresh();
    }

    /******************************************************************************
     * @fn      LoadPresets
     * @brief   EditorPrefsからプリセットを復元
     ******************************************************************************/
    private void LoadPresets()
    {
        _presets.Clear();

        if (!System.IO.File.Exists(PresetFilePath)) return;

        string json = System.IO.File.ReadAllText(PresetFilePath);
        if (string.IsNullOrEmpty(json)) return;

        var serializable = JsonUtility.FromJson<SerializablePresetList>(json);
        if (serializable == null) return;

        foreach (var preset in serializable.presets)
        {
            _presets[preset.name] = new HashSet<string>(preset.keys);
        }
    }

    /******************************************************************************
     * シリアライズ用データクラス
     * JsonUtilityはDictionaryを直接扱えないためリスト形式に変換する
     ******************************************************************************/
    [Serializable]
    private class SerializablePreset
    {
        public string name;
        public List<string> keys = new();
    }

    [Serializable]
    private class SerializablePresetList

    {
        public List<SerializablePreset> presets = new();
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

        // ピンされているものにはObject列を表示する
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
            // Object列 → オブジェクト名を表示
            EditorGUILayout.LabelField(field.Target.name, GUILayout.Width(objectColumnWidth));
            DrawSplitter(ref objectColumnWidth);
        }

        // Variable列 → 変数名（型名）を表示
        EditorGUILayout.LabelField($"{field.Name} ({field.TypeName})", GUILayout.Width(nameColumnWidth));
        DrawSplitter(ref nameColumnWidth);

        DrawValueField(field);  // 値の描画と編集場所
        DrawPinButton(field);   // ピン用の☆ボタン

        EditorGUILayout.EndHorizontal();
    }

    /******************************************************************************
     * @fn      DrawSplitter
     * @brief   ドラッグで幅を変更できるスプリッターを描画
     ******************************************************************************/
    private void DrawSplitter(ref float width)
    {
        // ドラッグ可能なスプリッターの描画
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
        System.Type type = field.Field.FieldType;   // 該当変数の型を取得

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