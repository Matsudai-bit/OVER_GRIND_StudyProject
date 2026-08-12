using UnityEngine;
using System.IO;

[CreateAssetMenu(fileName = "SaveData", menuName = "ScriptableObjects/SaveData")]
public class SaveData : ScriptableObject
{
    // ★ここを追加：ファイル名を自由に設定できる変数
    [Header("セーブファイル設定")]
    [Tooltip("保存するファイル名（例: savedata.json）")]
    public string saveFileName = "savedata.json";

    [Header("ゲーム進行")]
    public int clearedStage = 0;

    [Header("オーディオ設定")]
    [Range(0f, 1f)] public float bgmVolume = 1.0f;
    [Range(0f, 1f)] public float seVolume = 1.0f;
    [Range(0f, 1f)] public float masterVolume = 1.0f;

    [Header("システム設定")]
    public int screenSize = 1;
    public float cameraSensitivity = 1.0f;

    // ★staticを外し、上で設定した saveFileName を使うように変更
    public string GetSavePath()
    {
        // もしファイル名が空欄になっていたら、強制的に "savedata.json" にする安全対策
        string fileName = string.IsNullOrEmpty(saveFileName) ? "savedata.json" : saveFileName;

        return Path.Combine(Application.persistentDataPath, fileName);
    }

    public void SaveToJson()
    {
        string path = GetSavePath();
        string json = JsonUtility.ToJson(this, true);

        File.WriteAllText(path, json);
        Debug.Log($"<color=green>セーブ成功！</color>\n保存先: {path}");
    }

    public void LoadFromJson()
    {
        string path = GetSavePath();
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            JsonUtility.FromJsonOverwrite(json, this);
            Debug.Log($"<color=cyan>ロード成功！</color>\n読込元: {path}");
        }
        else
        {
            Debug.LogWarning("セーブデータ(JSON)が見つかりません: " + path);
        }
    }
}