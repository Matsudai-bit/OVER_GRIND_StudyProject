/// @ using :: 使用エンジン
using UnityEngine;
using System.IO;

///　@　className ::　セーブシステムクラス
///　@  name :: Aoki Hayate
///　@  date :: 2026/08/27

///　セーブシステムクラス
public static class SaveSystem
{
    /// <summary>
    /// ファイルの保存先パスを取得する
    /// </summary>
    public static string GetFilePath(string fileName)
    {
        string name = string.IsNullOrEmpty(fileName) ? "savedata.json" : fileName;
        return Path.Combine(Application.persistentDataPath, name);
    }

    /// <summary>
    /// セーブ処理 : ScriptableObjectの内容をJSON化して保存
    /// </summary>
    public static void Save(SaveData dataToSave, string fileName)
    {
        if (dataToSave == null)
        {
            Debug.LogError("[SaveSystem] セーブ対象の SaveData が null です。");
            return;
        }

        string path = GetFilePath(fileName);
        string json = JsonUtility.ToJson(dataToSave, true);

        File.WriteAllText(path, json);
        Debug.Log($"<color=green>【セーブ成功】</color> 保存先: {path}");
    }

    /// <summary>
    /// ロード処理 : ファイルから読み込んで新しい ScriptableObject インスタンスを作成して返す
    /// </summary>
    public static SaveData Load(string fileName)
    {
        string path = GetFilePath(fileName);
        SaveData data = ScriptableObject.CreateInstance<SaveData>();

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            JsonUtility.FromJsonOverwrite(json, data);
            Debug.Log($"<color=cyan>【ロード成功】</color> 読込元: {path}");
        }
        else
        {
            Debug.LogWarning($"[SaveSystem] セーブデータが存在しません。初期値で作成します: {path}");
        }

        return data;
    }

    /// <summary>
    /// ロード処理：既存の ScriptableObject インスタンスにデータを上書きする
    /// </summary>
    public static void LoadOverwrite(SaveData targetData, string fileName)
    {
        if (targetData == null)
        {
            Debug.LogError("[SaveSystem] 上書き対象の SaveData が null です。");
            return;
        }

        string path = GetFilePath(fileName);

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            JsonUtility.FromJsonOverwrite(json, targetData);
            Debug.Log($"<color=cyan>【ロード成功】</color> 読込元: {path}");
        }
        else
        {
            Debug.LogWarning($"[SaveSystem] セーブデータ(JSON)が見つかりません: {path}");
        }
    }
}