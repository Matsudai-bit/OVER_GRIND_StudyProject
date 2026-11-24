#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

/// <summary>
/// SoundDatabase アセットの取得・作成・保存を担当します。
/// </summary>
public static class SoundToolDatabaseProvider
{
    // SoundDatabase を検索するフィルター
    private const string DATABASE_SEARCH_FILTER = "t:SoundDatabase";

    // 新規作成先のフォルダ（SoundManager が Resources から自動ロードするため）
    private const string DATABASE_FOLDER_PATH = "Assets/Resources";

    // 新規作成先のフォルダの親フォルダ
    private const string DATABASE_PARENT_FOLDER_PATH = "Assets";

    // 新規作成先のフォルダ名
    private const string DATABASE_FOLDER_NAME = "Resources";

    // 新規作成するアセットのパス
    private const string DATABASE_ASSET_PATH = "Assets/Resources/SoundDatabase.asset";

    // キャッシュ済みの SoundDatabase
    private static SoundDatabase m_cachedDatabase;

    // 保存していない変更があるか
    private static bool m_hasUnsavedChanges = false;

    /// <summary>
    /// 保存していない変更があるかを取得します。
    /// </summary>
    public static bool HasUnsavedChanges => m_hasUnsavedChanges;

    /// <summary>
    /// SoundDatabase を取得します。
    /// </summary>
    /// <returns>SoundDatabase。見つからない場合は null。</returns>
    public static SoundDatabase GetDatabase()
    {
        // キャッシュが有効ならそのまま返す
        if (m_cachedDatabase != null)
        {
            return m_cachedDatabase;
        }

        // プロジェクト内から検索する
        string[] guids = AssetDatabase.FindAssets(DATABASE_SEARCH_FILTER);

        if (guids.Length == 0)
        {
            return null;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        m_cachedDatabase = AssetDatabase.LoadAssetAtPath<SoundDatabase>(path);

        return m_cachedDatabase;
    }

    /// <summary>
    /// SoundDatabase を Assets/Resources に新規作成します。
    /// </summary>
    /// <returns>作成した SoundDatabase。</returns>
    public static SoundDatabase CreateDatabase()
    {
        // Resources フォルダが無ければ作成する
        if (!AssetDatabase.IsValidFolder(DATABASE_FOLDER_PATH))
        {
            AssetDatabase.CreateFolder(DATABASE_PARENT_FOLDER_PATH, DATABASE_FOLDER_NAME);
        }

        SoundDatabase database = ScriptableObject.CreateInstance<SoundDatabase>();
        AssetDatabase.CreateAsset(database, DATABASE_ASSET_PATH);
        AssetDatabase.SaveAssets();

        m_cachedDatabase = database;

        return database;
    }

    /// <summary>
    /// 変更済みとして記録します。
    /// </summary>
    /// <param name="database">変更した SoundDatabase。</param>
    public static void MarkDirty(SoundDatabase database)
    {
        if (database == null)
        {
            return;
        }

        EditorUtility.SetDirty(database);
        m_hasUnsavedChanges = true;
    }

    /// <summary>
    /// 未保存の変更があれば保存します。
    /// </summary>
    public static void SaveIfNeeded()
    {
        if (!m_hasUnsavedChanges)
        {
            return;
        }

        m_hasUnsavedChanges = false;
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// SoundDatabase を Project ウィンドウ上で選択表示します。
    /// </summary>
    public static void PingDatabase()
    {
        SoundDatabase database = GetDatabase();

        if (database == null)
        {
            return;
        }

        Selection.activeObject = database;
        EditorGUIUtility.PingObject(database);
    }
}

#endif