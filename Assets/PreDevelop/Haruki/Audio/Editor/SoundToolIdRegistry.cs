#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// AudioID の enum ファイルの読み取りと生成を担当します。
/// </summary>
public static class SoundToolIdRegistry
{
    /// <summary>
    /// 追加直後の ID 名を画面間で受け渡す SessionState のキーです。
    /// </summary>
    public const string RECENT_ADDED_IDS_KEY = "SoundTool_RecentAddedIds";

    // 次の採番値を記録するコメントの接頭辞
    private const string NEXT_AUDIO_ID_PREFIX = "// Next AudioID Value: ";

    // AudioID.cs が見つからない場合の既定パス
    private const string DEFAULT_AUDIO_ID_PATH = "Assets/AudioID.cs";

    // 生成ファイル先頭の注意書き
    private const string FILE_HEADER_COMMENT = "// 自動生成用ファイルです。直接編集しないでください。";

    // enum 内のインデント
    private const string ENUM_INDENT = "    ";

    /// <summary>
    /// AudioID の名前と固定整数値を保持します。
    /// </summary>
    public sealed class IdEntry
    {
        /// <summary>
        /// AudioID 名を取得します。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// AudioID の固定整数値を取得します。
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// AudioID 情報を生成します。
        /// </summary>
        /// <param name="name">AudioID 名。</param>
        /// <param name="value">固定整数値。</param>
        public IdEntry(string name, int value)
        {
            Name = name;
            Value = value;
        }
    }

    /// <summary>
    /// 現在定義されている AudioID と固定値を取得します。
    /// </summary>
    /// <returns>AudioID 一覧。</returns>
    public static List<IdEntry> GetCurrentEntries()
    {
        List<IdEntry> entries = new List<IdEntry>();

        foreach (AudioID audioId in Enum.GetValues(typeof(AudioID)))
        {
            entries.Add(new IdEntry(audioId.ToString(), (int)audioId));
        }

        return entries;
    }

    /// <summary>
    /// 次に使用する AudioID の整数値を取得します。削除した番号は再利用しません。
    /// </summary>
    /// <param name="entries">現在の AudioID 一覧。</param>
    /// <returns>次に使用する整数値。</returns>
    public static int GetNextValue(IReadOnlyList<IdEntry> entries)
    {
        // 現在の最大値 + 1 を求める
        int maxValue = 0;

        foreach (IdEntry entry in entries)
        {
            if (entry.Value > maxValue)
            {
                maxValue = entry.Value;
            }
        }

        int nextValue = maxValue + 1;

        // ファイルに記録された採番値があれば大きい方を採用する
        string path = GetFilePath();

        if (!File.Exists(path))
        {
            return nextValue;
        }

        foreach (string line in File.ReadAllLines(path))
        {
            if (!line.StartsWith(NEXT_AUDIO_ID_PREFIX, StringComparison.Ordinal))
            {
                continue;
            }

            string valueText = line.Substring(NEXT_AUDIO_ID_PREFIX.Length);

            if (int.TryParse(valueText, out int storedNextValue))
            {
                return Mathf.Max(nextValue, storedNextValue);
            }
        }

        return nextValue;
    }

    /// <summary>
    /// AudioID の enum ファイルを生成します。
    /// </summary>
    /// <param name="entries">出力する AudioID 一覧。</param>
    /// <param name="nextValue">次に使用する固定値。</param>
    public static void GenerateFile(IReadOnlyList<IdEntry> entries, int nextValue)
    {
        StringBuilder builder = new StringBuilder();

        // ヘッダーと採番値
        builder.AppendLine(FILE_HEADER_COMMENT);
        builder.AppendLine($"{NEXT_AUDIO_ID_PREFIX}{nextValue}");
        builder.AppendLine();

        // enum 本体
        builder.AppendLine("public enum AudioID");
        builder.AppendLine("{");
        builder.AppendLine($"{ENUM_INDENT}None = 0,");

        foreach (IdEntry entry in entries)
        {
            if (entry.Name == nameof(AudioID.None) || string.IsNullOrWhiteSpace(entry.Name))
            {
                continue;
            }

            builder.AppendLine($"{ENUM_INDENT}{entry.Name} = {entry.Value},");
        }

        builder.AppendLine("}");

        File.WriteAllText(GetFilePath(), builder.ToString(), new UTF8Encoding(true));
    }

    /// <summary>
    /// 入力文字列を AudioID 名として使える形に整えます。
    /// </summary>
    /// <param name="rawName">入力された文字列。</param>
    /// <returns>整形後の名前。</returns>
    public static string SanitizeName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return "";
        }

        // 空白とハイフンはアンダースコアへ置き換える
        string name = Regex.Replace(rawName.Trim(), @"[\s\-]+", "_");

        // 識別子に使えない文字を取り除く
        return Regex.Replace(name, @"[^\w]", "");
    }

    /// <summary>
    /// 文字列が C# の識別子として使用可能かを確認します。
    /// </summary>
    /// <param name="identifier">確認対象の文字列。</param>
    /// <returns>
    /// true：使用可能です。
    /// false：使用できません。
    /// </returns>
    public static bool IsValidIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return false;
        }

        // 先頭は英字またはアンダースコア
        if (!(char.IsLetter(identifier[0]) || identifier[0] == '_'))
        {
            return false;
        }

        // 2文字目以降は英数字またはアンダースコア
        for (int i = 1; i < identifier.Length; i++)
        {
            if (!(char.IsLetterOrDigit(identifier[i]) || identifier[i] == '_'))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// AudioID.cs の Assets パスを取得します。
    /// </summary>
    /// <returns>AudioID.cs のパス。</returns>
    private static string GetFilePath()
    {
        string[] guids = AssetDatabase.FindAssets("AudioID t:MonoScript");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (Path.GetFileNameWithoutExtension(path) == nameof(AudioID))
            {
                return path;
            }
        }

        return DEFAULT_AUDIO_ID_PATH;
    }
}

#endif