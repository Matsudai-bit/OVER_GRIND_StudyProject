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
    private float objectColumnWidth = 80f;                             // オブジェクト列の幅（初期値）
    private float nameColumnWidth = 80f;                               // 変数名列の幅（初期値）

    // 描画用の行カウント（背景のストライプ描画用）
    private int rowIndex = 0;                                           // ストライプ描画用の行カウント

    // プリセット管理
    private Dictionary<string, HashSet<string>> _presets = new();       // プリセット一覧
    private string _presetInputName = "";                               // プリセット名入力欄の中身
    private const string PresetFilePath = "Assets/Editor/DebugMonitor/Presets.json";


    // チェック中のプリセット名セット
    private HashSet<string> _checkedPresets = new();
    // プリセットプルダウンの開閉状態
    private bool _presetFoldout = false;
    // プリセットセクションの折りたたみ状態（文字列キー）
    private Dictionary<string, bool> _presetFoldouts = new();

    // プリセットごとの色（循環して使用）
    private static readonly Color[] PresetColors = new Color[]
    {
    new Color(0.2f, 0.4f, 0.8f, 0.3f),  // 青
    new Color(0.8f, 0.4f, 0.2f, 0.3f),  // 橙
    new Color(0.4f, 0.7f, 0.2f, 0.3f),  // 緑
    new Color(0.7f, 0.2f, 0.7f, 0.3f),  // 紫
    new Color(0.8f, 0.7f, 0.1f, 0.3f),  // 黄
    };

    // ドロップダウンの開閉状態
    private bool _presetDropdownOpen = false;
    // Presetセクションの一括表示フラグ
    private bool _presetsVisible = true;

    // セクション背景色
    private static readonly Color PresetSectionColor = new Color(0.15f, 0.20f, 0.30f, 0.6f); // 青みがかった暗色
    private static readonly Color VariableSectionColor = new Color(0.20f, 0.15f, 0.15f, 0.6f); // 赤みがかった暗色

    private GUIStyle presetSectionStyle;
    private GUIStyle variableSectionStyle;

    private float typeColumnWidth = 60f;   // 型名列の幅（初期値）

    private void InitializeStyles()
    {
        if (presetSectionStyle != null) return;

        presetSectionStyle = new GUIStyle(EditorStyles.helpBox);
        presetSectionStyle.normal.background = Texture2D.whiteTexture;
        presetSectionStyle.padding = new RectOffset(8, 8, 8, 8);
        presetSectionStyle.margin = new RectOffset(0, 0, 4, 4);

        variableSectionStyle = new GUIStyle(EditorStyles.helpBox);
        variableSectionStyle.normal.background = Texture2D.whiteTexture;
        variableSectionStyle.padding = new RectOffset(8, 8, 8, 8);
        variableSectionStyle.margin = new RectOffset(0, 0, 4, 4);
    }


    [MenuItem("Tools/Debug Monitor")]
    public static void Open()
    {
        GetWindow<DebugMonitorWindow>("Debug Monitor");
    }

    private void OnEnable()     // 最初に１回だけ呼び出される
    {
        LoadPresets();          // プリセットをロード
    }

    private void OnGUI()
    {
        InitializeStyles();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Play中のみ使用できます", MessageType.Info);
            return;
        }

        List<DebugParameterData> allFields = DebugMonitorScanner.Scan();

        scroll = EditorGUILayout.BeginScrollView(scroll);

        rowIndex = 0;

        EditorGUILayout.Space();

        searchText = EditorGUILayout.TextField("Search", searchText);

        EditorGUILayout.Space();

        //----------------------------------------------------------
        // Preset
        //----------------------------------------------------------

        Color oldColor = GUI.color;

        GUI.color = PresetSectionColor;
        EditorGUILayout.BeginVertical(presetSectionStyle);
        GUI.color = oldColor;

        DrawPresetSection();

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        //----------------------------------------------------------
        // Variable
        //----------------------------------------------------------

        GUI.color = VariableSectionColor;
        EditorGUILayout.BeginVertical(variableSectionStyle);
        GUI.color = oldColor;

        var sortedFields = allFields
            .OrderByDescending(f => pinnedKeys.Contains(f.UniqueKey))
            .ToList();

        var pinnedFields = sortedFields
            .Where(f => pinnedKeys.Contains(f.UniqueKey))
            .ToList();

        if (pinnedFields.Count > 0)
        {
            EditorGUILayout.LabelField("★ Pinned", EditorStyles.boldLabel);

            DrawHeader(true);

            EditorGUI.indentLevel++;

            foreach (var field in pinnedFields)
            {
                if (!string.IsNullOrEmpty(searchText) &&
                    !field.Name.ToLower().Contains(searchText.ToLower()))
                    continue;

                DrawField(field, true);
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space();
        }

        DrawHeader(false);

        var groups = sortedFields.GroupBy(x => x.Target);

        foreach (var group in groups)
        {
            UnityEngine.Object target = group.Key;

            var filteredGroup = group.Where(f =>
            {
                if (string.IsNullOrEmpty(searchText))
                    return true;

                return f.Name.ToLower().Contains(searchText.ToLower());
            }).ToList();

            if (filteredGroup.Count == 0)
                continue;

            if (!foldouts.ContainsKey(target))
                foldouts[target] = true;

            Rect foldoutRect = EditorGUILayout.GetControlRect(
                true,
                EditorGUIUtility.singleLineHeight);

            EditorGUI.DrawRect(
                foldoutRect,
                new Color(0.2f, 0.2f, 0.2f, 0.5f));

            foldouts[target] = EditorGUI.Foldout(
                foldoutRect,
                foldouts[target],
                $"{target.GetType().Name} ({target.name})",
                true);

            if (!foldouts[target])
                continue;

            EditorGUI.indentLevel++;

            foreach (var field in filteredGroup)
            {
                DrawField(field, false);
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndScrollView();

        Repaint();
    }
    /******************************************************************************
 * @fn      DrawPresetSection
 * @brief   プリセットUI描画
 ******************************************************************************/
    private void DrawPresetSection()
    {
        // 第1段階：Presetsセクション全体の開閉
        _presetFoldout = EditorGUILayout.Foldout(
            _presetFoldout, "Presets", true, EditorStyles.boldLabel);
        if (!_presetFoldout) return;

        EditorGUI.indentLevel++;

        // 保存欄
        EditorGUILayout.BeginHorizontal();
        _presetInputName = EditorGUILayout.TextField(_presetInputName);
        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_presetInputName));
        if (GUILayout.Button("現在のピンを保存", GUILayout.Width(120)))
        {
            SavePreset(_presetInputName);
            _presetInputName = "";
            GUI.FocusControl(null);
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();

        if (_presets.Count == 0)
        {
            EditorGUILayout.LabelField("保存済みプリセットはありません", EditorStyles.miniLabel);
            EditorGUI.indentLevel--;
            return;
        }

        EditorGUILayout.Space(2);

        // ▼ プリセット選択ドロップダウンボタン
        Rect dropdownRect = EditorGUILayout.GetControlRect(
            false, EditorGUIUtility.singleLineHeight);

        string buttonLabel = _checkedPresets.Count == 0
            ? "▼ プリセットを選択"
            : $"▼ {string.Join(", ", _checkedPresets)}";

        if (GUI.Button(dropdownRect, buttonLabel, EditorStyles.popup))
        {
            GenericMenu menu = new GenericMenu();

            foreach (var presetName in _presets.Keys.ToList())
            {
                string capturedName = presetName;
                bool isChecked = _checkedPresets.Contains(capturedName);

                menu.AddItem(
                    new GUIContent(capturedName),
                    isChecked,
                    () =>
                    {
                        if (_checkedPresets.Contains(capturedName))
                            _checkedPresets.Remove(capturedName);
                        else
                            _checkedPresets.Add(capturedName);
                    });
            }

            menu.AddSeparator("");

            foreach (var presetName in _presets.Keys.ToList())
            {
                string capturedName = presetName;
                menu.AddItem(
                    new GUIContent($"削除/{capturedName}"),
                    false,
                    () =>
                    {
                        if (EditorUtility.DisplayDialog(
                            "プリセット削除",
                            $"「{capturedName}」を削除しますか？",
                            "削除", "キャンセル"))
                        {
                            _checkedPresets.Remove(capturedName);
                            DeletePreset(capturedName);
                        }
                    });
            }

            menu.DropDown(dropdownRect);
        }

        EditorGUILayout.Space(2);

        // チェック中プリセットが1つ以上あるときだけ一括バーを表示
        if (_checkedPresets.Count > 0)
        {
            Rect allToggleRect = EditorGUILayout.GetControlRect(
                false, EditorGUIUtility.singleLineHeight);
            EditorGUI.DrawRect(allToggleRect, new Color(0.25f, 0.25f, 0.25f, 1f));

            string allBarLabel = _presetsVisible ? "▼ Preset 一括非表示" : "▶ Preset 一括表示";
            if (GUI.Button(allToggleRect, allBarLabel, EditorStyles.boldLabel))
                _presetsVisible = !_presetsVisible;

            EditorGUILayout.Space(2);
        }

        // 一括非表示中は何も描画しない
        if (!_presetsVisible)
        {
            EditorGUI.indentLevel--;
            return;
        }

        // チェック中プリセットの変数を展開
        int displayColorIndex = 0;
        foreach (var presetName in _presets.Keys.ToList())
        {
            Color presetColor = PresetColors[displayColorIndex % PresetColors.Length];
            displayColorIndex++;

            if (!_checkedPresets.Contains(presetName)) continue;
            if (!_presets.TryGetValue(presetName, out var presetKeys)) continue;

            if (!_presetFoldouts.ContainsKey(presetName))
                _presetFoldouts[presetName] = true;

            // プリセット名行をFoldoutで描画
            Rect labelRect = EditorGUILayout.GetControlRect(
                false, EditorGUIUtility.singleLineHeight);
            EditorGUI.DrawRect(labelRect, new Color(
                presetColor.r, presetColor.g, presetColor.b, 0.4f));
            _presetFoldouts[presetName] = EditorGUI.Foldout(
                labelRect, _presetFoldouts[presetName],
                $"★ {presetName}", true, EditorStyles.boldLabel);

            if (!_presetFoldouts[presetName]) continue;

            if (Application.isPlaying)
            {
                var allFields = DebugMonitorScanner.Scan();
                var presetFields = allFields
                    .Where(f => presetKeys.Contains(f.UniqueKey))
                    .Where(f => string.IsNullOrEmpty(searchText) ||
                                f.Name.ToLower().Contains(searchText.ToLower()))
                    .ToList();

                if (presetFields.Count > 0)
                {
                    EditorGUI.indentLevel++;

                    Rect headerRect = EditorGUILayout.GetControlRect(
     false, EditorGUIUtility.singleLineHeight);
                    EditorGUI.DrawRect(headerRect, new Color(
                        presetColor.r, presetColor.g, presetColor.b, 0.5f));
                    float hx = headerRect.x + EditorGUI.indentLevel * 15f;
                    EditorGUI.LabelField(
                        new Rect(hx, headerRect.y, objectColumnWidth, headerRect.height),
                        "Object", EditorStyles.boldLabel);
                    EditorGUI.LabelField(
                        new Rect(hx + objectColumnWidth + 2f, headerRect.y,
                                 nameColumnWidth, headerRect.height),
                        "Variable", EditorStyles.boldLabel);
                    EditorGUI.LabelField(
                        new Rect(hx + objectColumnWidth + nameColumnWidth + 4f, headerRect.y,
                                 typeColumnWidth, headerRect.height),
                        "Type", EditorStyles.boldLabel);
                    EditorGUI.LabelField(
                        new Rect(hx + objectColumnWidth + nameColumnWidth + typeColumnWidth + 6f,
                                 headerRect.y, headerRect.width, headerRect.height),
                        "Value", EditorStyles.boldLabel);

                    foreach (var field in presetFields)
                    {
                        Rect fieldRect = EditorGUILayout.GetControlRect(
                            false, EditorGUIUtility.singleLineHeight);
                        EditorGUI.DrawRect(fieldRect, new Color(
                            presetColor.r, presetColor.g, presetColor.b,
                            rowIndex % 2 == 0 ? 0.15f : 0.08f));
                        rowIndex++;

                        float rx = fieldRect.x + EditorGUI.indentLevel * 15f;

                        // Object列
                        EditorGUI.LabelField(
                            new Rect(rx, fieldRect.y, objectColumnWidth, fieldRect.height),
                            field.Target.name);

                        // Variable列（変数名のみ）
                        EditorGUI.LabelField(
                            new Rect(rx + objectColumnWidth + 2f, fieldRect.y,
                                     nameColumnWidth, fieldRect.height),
                            field.Name);

                        // Type列（型名のみ）
                        EditorGUI.LabelField(
                            new Rect(rx + objectColumnWidth + nameColumnWidth + 4f, fieldRect.y,
                                     typeColumnWidth, fieldRect.height),
                            field.TypeName);

                        // Value列
                        float valueWidth = fieldRect.xMax
                            - (rx + objectColumnWidth + nameColumnWidth + typeColumnWidth + 6f) - 30f;
                        DrawValueFieldInRect(field,
                            new Rect(rx + objectColumnWidth + nameColumnWidth + typeColumnWidth + 6f,
                                     fieldRect.y, valueWidth, fieldRect.height));

                        // ピンボタン
                        if (GUI.Button(
                            new Rect(fieldRect.xMax - 28f, fieldRect.y, 25f, fieldRect.height),
                            pinnedKeys.Contains(field.UniqueKey) ? "★" : "☆"))
                        {
                            if (pinnedKeys.Contains(field.UniqueKey))
                                pinnedKeys.Remove(field.UniqueKey);
                            else
                                pinnedKeys.Add(field.UniqueKey);
                        }
                    }

                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space(4);
                }
                else
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField("（対応する変数が見つかりません）",
                        EditorStyles.miniLabel);
                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("（Play中のみ表示されます）",
                    EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
            }
        }

        EditorGUI.indentLevel--;
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
        Rect headerRect = EditorGUILayout.BeginHorizontal(
            GUILayout.Height(EditorGUIUtility.singleLineHeight));
        EditorGUI.DrawRect(headerRect, new Color(0.15f, 0.15f, 0.15f, 1f));

        float indentSpace = EditorGUI.indentLevel * 15f;
        GUILayout.Space(indentSpace);

        if (isPinnedSection)
        {
            EditorGUILayout.LabelField("Object", EditorStyles.boldLabel,
                GUILayout.Width(objectColumnWidth));
            DrawSplitter(ref objectColumnWidth);
        }

        EditorGUILayout.LabelField("Variable", EditorStyles.boldLabel,
            GUILayout.Width(nameColumnWidth));
        DrawSplitter(ref nameColumnWidth);

        // Type列を追加
        EditorGUILayout.LabelField("Type", EditorStyles.boldLabel,
            GUILayout.Width(typeColumnWidth));
        DrawSplitter(ref typeColumnWidth);

        EditorGUILayout.LabelField("Value", EditorStyles.boldLabel,
            GUILayout.ExpandWidth(true));

        // ピンボタン分のスペースを確保（常に右端に固定）
        GUILayout.Space(29f);

        EditorGUILayout.EndHorizontal();

        Rect lineRect = new Rect(headerRect.x, headerRect.yMax - 1,
            headerRect.width, 1);
        EditorGUI.DrawRect(lineRect, Color.gray);
    }

    /******************************************************************************
     * @fn      DrawField
     * @brief   型に応じた描画処理（セル分割対応版）
     ******************************************************************************/
    private void DrawField(DebugParameterData field, bool isPinned)
    {
        Rect rowRect = EditorGUILayout.BeginHorizontal(
            GUILayout.Height(EditorGUIUtility.singleLineHeight));

        if (rowIndex % 2 == 0)
            EditorGUI.DrawRect(rowRect, new Color(0.3f, 0.3f, 0.3f, 0.2f));
        rowIndex++;

        if (isPinned)
        {
            EditorGUILayout.LabelField(field.Target.name,
                GUILayout.Width(objectColumnWidth));
            DrawSplitter(ref objectColumnWidth);
        }

        // Variable列（変数名のみ）
        EditorGUILayout.LabelField(field.Name,
            GUILayout.Width(nameColumnWidth));
        DrawSplitter(ref nameColumnWidth);

        // Type列（型名のみ）
        EditorGUILayout.LabelField(field.TypeName,
            GUILayout.Width(typeColumnWidth));
        DrawSplitter(ref typeColumnWidth);

        // Value列（残り幅いっぱいに広げる）
        DrawValueField(field);

        // ピンボタン（常に右端に固定・幅を明示）
        DrawPinButton(field);

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

    private void DrawValueFieldInRect(DebugParameterData field, Rect rect)
    {
        System.Type type = field.Field.FieldType;

        if (type == typeof(int))
        {
            int val = EditorGUI.IntField(rect, GUIContent.none, (int)field.Value);
            if (val != (int)field.Value) field.SetValue(val);
        }
        else if (type == typeof(float))
        {
            float val = EditorGUI.FloatField(rect, GUIContent.none, (float)field.Value);
            if (!Mathf.Approximately(val, (float)field.Value)) field.SetValue(val);
        }
        else if (type == typeof(bool))
        {
            bool val = EditorGUI.Toggle(rect, GUIContent.none, (bool)field.Value);
            if (val != (bool)field.Value) field.SetValue(val);
        }
        else if (type == typeof(string))
        {
            string val = EditorGUI.TextField(rect, GUIContent.none, (string)field.Value);
            if (val != (string)field.Value) field.SetValue(val);
        }
        else if (type == typeof(Vector2))
        {
            Vector2 val = EditorGUI.Vector2Field(rect, GUIContent.none, (Vector2)field.Value);
            if (val != (Vector2)field.Value) field.SetValue(val);
        }
        else if (type == typeof(Vector3))
        {
            Vector3 val = EditorGUI.Vector3Field(rect, GUIContent.none, (Vector3)field.Value);
            if (val != (Vector3)field.Value) field.SetValue(val);
        }
        else if (type == typeof(Color))
        {
            Color val = EditorGUI.ColorField(rect, GUIContent.none, (Color)field.Value);
            if (val != (Color)field.Value) field.SetValue(val);
        }
        else if (type == typeof(Quaternion))
        {
            Quaternion current = (Quaternion)field.Value;
            Vector3 euler = EditorGUI.Vector3Field(rect, GUIContent.none, current.eulerAngles);
            if (euler != current.eulerAngles) field.SetValue(Quaternion.Euler(euler));
        }
        else
        {
            EditorGUI.LabelField(rect, field.Value?.ToString() ?? "null");
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

        if (GUILayout.Button(buttonText,
            GUILayout.Width(25f),
            GUILayout.ExpandWidth(false)))
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