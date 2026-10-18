using UnityEditor;
using UnityEngine;

/// <summary>
/// SaveDataの編集・JSON操作を行うEditorWindowです。
/// </summary>
public sealed class SaveDataWindow : EditorWindow
{
    private const string SAVE_FILE_NAME_EDITOR_PREFS_KEY =
        "SaveDataWindow.SaveFileName";

    // 編集対象
    private SaveData m_targetData;

    // SerializedObject
    private SerializedObject m_serializedTarget;

    // 操作対象のJSONファイル名
    private string m_saveFileName =
        SaveSystem.DEFAULT_SAVE_FILE_NAME;

    [MenuItem("Tools/Save Data Tool")]
    public static void ShowWindow()
    {
        GetWindow<SaveDataWindow>(
            "セーブデータ管理");
    }

    private void OnEnable()
    {
        m_saveFileName = EditorPrefs.GetString(
            SAVE_FILE_NAME_EDITOR_PREFS_KEY,
            SaveSystem.DEFAULT_SAVE_FILE_NAME);
    }

    private void OnDisable()
    {
        EditorPrefs.SetString(
            SAVE_FILE_NAME_EDITOR_PREFS_KEY,
            m_saveFileName);
    }

    private void OnGUI()
    {
        DrawTargetSection();

        EditorGUILayout.Space(10.0f);

        if (m_targetData == null)
        {
            EditorGUILayout.HelpBox(
                "編集するSaveDataを指定してください。",
                MessageType.Info);

            return;
        }

        DrawSaveDataSection();

        EditorGUILayout.Space(10.0f);

        DrawFileSection();

        EditorGUILayout.Space(10.0f);

        DrawUtilitySection();
    }

    /// <summary>
    /// 編集対象選択を表示します。
    /// </summary>
    private void DrawTargetSection()
    {
        EditorGUILayout.LabelField(
            "編集対象",
            EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        SaveData targetData =
            (SaveData)EditorGUILayout.ObjectField(
                "SaveData",
                m_targetData,
                typeof(SaveData),
                true);

        if (EditorGUI.EndChangeCheck())
        {
            SetTargetData(targetData);
        }

        using (new EditorGUI.DisabledScope(
            !EditorApplication.isPlaying))
        {
            if (GUILayout.Button(
                "実行中のSaveDataを取得"))
            {
                FindRuntimeSaveData();
            }
        }
    }

    /// <summary>
    /// SaveDataの編集項目を表示します。
    /// </summary>
    private void DrawSaveDataSection()
    {
        if (m_serializedTarget == null)
        {
            m_serializedTarget =
                new SerializedObject(m_targetData);
        }

        m_serializedTarget.Update();

        EditorGUILayout.LabelField(
            "セーブデータ",
            EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.IntField(
                "データバージョン",
                m_targetData.Version);
        }

        SerializedProperty progressProperty =
            m_serializedTarget.FindProperty(
                "m_progress");

        SerializedProperty settingsProperty =
            m_serializedTarget.FindProperty(
                "m_settings");

        if (progressProperty != null)
        {
            EditorGUILayout.PropertyField(
                progressProperty,
                true);
        }

        if (settingsProperty != null)
        {
            EditorGUILayout.PropertyField(
                settingsProperty,
                true);
        }

        if (m_serializedTarget.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(m_targetData);
        }
    }

    /// <summary>
    /// JSONファイル操作を表示します。
    /// </summary>
    private void DrawFileSection()
    {
        EditorGUILayout.LabelField(
            "JSONファイル",
            EditorStyles.boldLabel);

        m_saveFileName =
            EditorGUILayout.TextField(
                "ファイル名",
                m_saveFileName);

        string path =
            SaveSystem.GetFilePath(m_saveFileName);

        EditorGUILayout.HelpBox(
            $"保存先\n{path}",
            MessageType.None);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(
            "JSONへ保存",
            GUILayout.Height(40.0f)))
        {
            SaveToJson();
        }

        if (GUILayout.Button(
            "JSONからロード",
            GUILayout.Height(40.0f)))
        {
            LoadFromJson();
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// デバッグ用操作を表示します。
    /// </summary>
    private void DrawUtilitySection()
    {
        EditorGUILayout.LabelField(
            "デバッグ操作",
            EditorStyles.boldLabel);

        if (GUILayout.Button(
            "コード上の初期値へリセット"))
        {
            ResetTargetData();
        }

        if (GUILayout.Button(
            "JSONセーブデータを削除"))
        {
            DeleteJson();
        }
    }

    /// <summary>
    /// 編集対象を変更します。
    /// </summary>
    private void SetTargetData(SaveData targetData)
    {
        m_targetData = targetData;

        m_serializedTarget =
            m_targetData != null
                ? new SerializedObject(m_targetData)
                : null;
    }

    /// <summary>
    /// Play中のSaveDataControllerから現在データを取得します。
    /// </summary>
    private void FindRuntimeSaveData()
    {
        SaveDataController controller =
            Object.FindFirstObjectByType<SaveDataController>();

        if (controller == null)
        {
            Debug.LogWarning(
                "[SaveDataWindow] SaveDataControllerが見つかりません。");

            return;
        }

        if (controller.CurrentData == null)
        {
            Debug.LogWarning(
                "[SaveDataWindow] ランタイムSaveDataが存在しません。");

            return;
        }

        SetTargetData(
            controller.CurrentData);

        m_saveFileName =
            controller.SaveFileName;
    }

    /// <summary>
    /// 編集対象をJSONへ保存します。
    /// </summary>
    private void SaveToJson()
    {
        if (m_targetData == null)
        {
            return;
        }

        SaveSystem.TrySave(
            m_targetData,
            m_saveFileName);
    }

    /// <summary>
    /// JSONを編集対象へ読み込みます。
    /// </summary>
    private void LoadFromJson()
    {
        if (m_targetData == null)
        {
            return;
        }

        Undo.RecordObject(
            m_targetData,
            "Load Save Data");

        if (!SaveSystem.TryLoadOverwrite(
            m_targetData,
            m_saveFileName))
        {
            return;
        }

        m_targetData.EnsureInitialized();

        EditorUtility.SetDirty(
            m_targetData);

        m_serializedTarget?.Update();

        SavePersistentAssetIfNeeded();

        GUI.FocusControl(null);

        Repaint();
    }

    /// <summary>
    /// 編集対象をコード上の初期値へ戻します。
    /// </summary>
    private void ResetTargetData()
    {
        if (m_targetData == null)
        {
            return;
        }

        bool result = EditorUtility.DisplayDialog(
            "SaveDataリセット",
            "対象のSaveDataをコード上の初期値へ戻しますか？",
            "リセット",
            "キャンセル");

        if (!result)
        {
            return;
        }

        Undo.RecordObject(
            m_targetData,
            "Reset Save Data");

        m_targetData.ResetData();

        EditorUtility.SetDirty(
            m_targetData);

        m_serializedTarget?.Update();

        SavePersistentAssetIfNeeded();

        Repaint();
    }

    /// <summary>
    /// JSONセーブデータを削除します。
    /// </summary>
    private void DeleteJson()
    {
        string path =
            SaveSystem.GetFilePath(m_saveFileName);

        bool result = EditorUtility.DisplayDialog(
            "セーブデータ削除",
            $"以下のセーブデータを削除しますか？\n\n{path}",
            "削除",
            "キャンセル");

        if (!result)
        {
            return;
        }

        SaveSystem.TryDelete(
            m_saveFileName);
    }

    /// <summary>
    /// Project上のAssetなら変更を保存します。
    /// </summary>
    private void SavePersistentAssetIfNeeded()
    {
        if (m_targetData == null)
        {
            return;
        }

        if (!EditorUtility.IsPersistent(
            m_targetData))
        {
            return;
        }

        AssetDatabase.SaveAssets();
    }
}