using System;
using System.IO;
using UnityEngine;

/// <summary>
/// セーブデータのファイル入出力を行います。
/// </summary>
public static class SaveSystem
{
    public const string DEFAULT_SAVE_FILE_NAME = "savedata.json";

    /// <summary>
    /// セーブファイルのパスを取得します。
    /// </summary>
    public static string GetFilePath(string fileName)
    {
        string saveFileName =
            string.IsNullOrWhiteSpace(fileName)
                ? DEFAULT_SAVE_FILE_NAME
                : fileName;

        return Path.Combine(
            Application.persistentDataPath,
            saveFileName);
    }

    /// <summary>
    /// セーブファイルが存在するか確認します。
    /// </summary>
    public static bool Exists(string fileName)
    {
        return File.Exists(GetFilePath(fileName));
    }

    /// <summary>
    /// セーブデータをJSONへ保存します。
    /// </summary>
    /// <returns>
    /// true：保存に成功しました。
    /// false：保存に失敗しました。
    /// </returns>
    public static bool TrySave(
        SaveData data,
        string fileName)
    {
        if (data == null)
        {
            Debug.LogError(
                "[SaveSystem] SaveDataがnullです。");

            return false;
        }

        string path = GetFilePath(fileName);

        try
        {
            string json = JsonUtility.ToJson(
                data,
                true);

            File.WriteAllText(path, json);

            Debug.Log(
                $"[SaveSystem] セーブしました: {path}");

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[SaveSystem] セーブに失敗しました。\n"
                + $"Path: {path}\n"
                + exception);

            return false;
        }
    }

    /// <summary>
    /// JSONの内容を指定したSaveDataへ上書きします。
    /// </summary>
    /// <returns>
    /// true：ロードに成功しました。
    /// false：ロードに失敗しました。
    /// </returns>
    public static bool TryLoadOverwrite(
        SaveData targetData,
        string fileName)
    {
        if (targetData == null)
        {
            Debug.LogError(
                "[SaveSystem] ロード先のSaveDataがnullです。");

            return false;
        }

        string path = GetFilePath(fileName);

        if (!File.Exists(path))
        {
            Debug.Log(
                $"[SaveSystem] セーブデータが存在しません: {path}");

            return false;
        }

        try
        {
            string json = File.ReadAllText(path);

            JsonUtility.FromJsonOverwrite(
                json,
                targetData);

            targetData.EnsureInitialized();

            Debug.Log(
                $"[SaveSystem] ロードしました: {path}");

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[SaveSystem] ロードに失敗しました。\n"
                + $"Path: {path}\n"
                + exception);

            return false;
        }
    }

    /// <summary>
    /// セーブファイルを削除します。
    /// </summary>
    /// <returns>
    /// true：削除、またはファイルが存在しません。
    /// false：削除に失敗しました。
    /// </returns>
    public static bool TryDelete(string fileName)
    {
        string path = GetFilePath(fileName);

        if (!File.Exists(path))
        {
            return true;
        }

        try
        {
            File.Delete(path);

            Debug.Log(
                $"[SaveSystem] セーブデータを削除しました: {path}");

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[SaveSystem] セーブデータの削除に失敗しました。\n"
                + $"Path: {path}\n"
                + exception);

            return false;
        }
    }
}
