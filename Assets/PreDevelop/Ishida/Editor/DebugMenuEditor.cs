using UnityEditor;
using UnityEngine;

/// <summary>
/// Unity上部メニュー「Tool > Debug」からデバッグON/OFFを操作するエディタ拡張
/// このファイルは Assets/Editor/ フォルダに配置してください
/// </summary>
public static class DebugMenuEditor
{
    // EditorPrefs キー（プロジェクト固有にするためにプロジェクト名をプレフィクスとして使用）
    private const string PrefsKey = "DebugMenu_IsDebugEnabled";

    // ─────────────────────────────────────────
    // メニュー項目
    // ─────────────────────────────────────────

    [MenuItem("Tool/Debug/Toggle Debug Mode", priority = 0)]
    private static void ToggleDebugMode()
    {
        bool current = GetDebugEnabled();
        SetDebugEnabled(!current);
    }

    /// <summary>
    /// チェックマークの表示状態をメニューに反映する
    /// </summary>
    [MenuItem("Tool/Debug/Toggle Debug Mode", validate = true)]
    private static bool ToggleDebugModeValidate()
    {
        Menu.SetChecked("Tool/Debug/Toggle Debug Mode", GetDebugEnabled());
        return true; // 常に有効
    }

    // ─────────────────────────────────────────

    [MenuItem("Tool/Debug/Enable Debug Mode", priority = 11)]
    private static void EnableDebug()  => SetDebugEnabled(true);

    [MenuItem("Tool/Debug/Disable Debug Mode", priority = 12)]
    private static void DisableDebug() => SetDebugEnabled(false);

    // ─────────────────────────────────────────

    [MenuItem("Tool/Debug/Open Debug Settings", priority = 30)]
    private static void OpenDebugSettings()
    {
        var manager = LoadOrCreateDebugManager();
        if (manager != null)
            Selection.activeObject = manager; // Inspectorに表示
    }

    // ─────────────────────────────────────────
    // 内部処理
    // ─────────────────────────────────────────

    /// <summary>
    /// EditorPrefs と ScriptableObject の両方に値をセットして同期する
    /// </summary>
    private static void SetDebugEnabled(bool value)
    {
        // 1) EditorPrefs に保存（エディタ再起動後も保持）
        EditorPrefs.SetBool(PrefsKey, value);

        // 2) ScriptableObject にも反映
        var manager = LoadOrCreateDebugManager();
        if (manager != null)
        {
            manager.isDebugEnabled = value;
            EditorUtility.SetDirty(manager);   // 変更をアセットに書き込む
            AssetDatabase.SaveAssets();
        }

        // 3) Define Symbol の切り替え（コードレベルで #if DEBUG_MODE を使いたい場合）
        SetDebugDefineSymbol(value);

        string state = value ? "ON" : "OFF";
        Debug.Log($"[DebugMenuEditor] Debug Mode → {state}");
    }

    private static bool GetDebugEnabled()
    {
        // EditorPrefs を正としてチェックマークを制御
        return EditorPrefs.GetBool(PrefsKey, false);
    }

    /// <summary>
    /// ScriptableObject をロード。存在しなければ自動生成する
    /// </summary>
    private static DebugManager LoadOrCreateDebugManager()
    {
        // AssetDatabase から検索
        var guids = AssetDatabase.FindAssets("t:DebugManager");
        if (guids.Length > 0)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<DebugManager>(path);
        }

        // 見つからなければ Resources/Settings/ に自動生成
        const string dir  = "Assets/Resources/Settings";
        const string file = "Assets/Resources/Settings/DebugManager.asset";

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/Resources", "Settings");

        var newAsset = ScriptableObject.CreateInstance<DebugManager>();
        AssetDatabase.CreateAsset(newAsset, file);
        AssetDatabase.SaveAssets();

        Debug.Log($"[DebugMenuEditor] DebugManager.asset を自動生成しました → {file}");
        return newAsset;
    }

    /// <summary>
    /// Scripting Define Symbol "DEBUG_MODE" を追加/削除する
    /// #if DEBUG_MODE ... #endif で囲めばビルド時にも除外できる
    /// </summary>
    private static void SetDebugDefineSymbol(bool enable)
    {
        var target = EditorUserBuildSettings.selectedBuildTargetGroup;
#if UNITY_2023_1_OR_NEWER
        // Unity 2023+ 推奨API
        var defines = new System.Collections.Generic.HashSet<string>(
            PlayerSettings.GetScriptingDefineSymbols(
                UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(target))
            .Split(';'));
#else
        var defines = new System.Collections.Generic.HashSet<string>(
            PlayerSettings.GetScriptingDefineSymbolsForGroup(target).Split(';'));
#endif

        bool changed;
        if (enable)
            changed = defines.Add("DEBUG_MODE");
        else
            changed = defines.Remove("DEBUG_MODE");

        if (!changed) return; // 変化なし

        string joined = string.Join(";", defines);

#if UNITY_2023_1_OR_NEWER
        PlayerSettings.SetScriptingDefineSymbols(
            UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(target), joined);
#else
        PlayerSettings.SetScriptingDefineSymbolsForGroup(target, joined);
#endif
    }

    // ─────────────────────────────────────────
    // エディタ起動時の初期同期
    // ─────────────────────────────────────────

    /// <summary>
    /// Unityエディタ起動時に EditorPrefs → ScriptableObject を同期する
    /// </summary>
    [InitializeOnLoadMethod]
    private static void OnEditorLoad()
    {
        bool savedValue = EditorPrefs.GetBool(PrefsKey, false);

        var manager = LoadOrCreateDebugManager();
        if (manager != null && manager.isDebugEnabled != savedValue)
        {
            manager.isDebugEnabled = savedValue;
            EditorUtility.SetDirty(manager);
            AssetDatabase.SaveAssets();
        }
    }
}
