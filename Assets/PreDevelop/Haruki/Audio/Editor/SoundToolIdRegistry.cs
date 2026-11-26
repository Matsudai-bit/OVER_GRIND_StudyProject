#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// AudioID の enum ファイルの読み取り・生成およびリネーム時のスクリプト参照置換を担当します。
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

    // スクリプト検索対象のルートフォルダ
    private const string ASSETS_ROOT_FOLDER = "Assets";

    // C# スクリプト検索パターン
    private const string CS_SEARCH_PATTERN = "*.cs";

    /// <summary>
    /// AudioID の名前と固定整数値を保持します。
    /// </summary>
    public sealed class IdEntry
    {
        // AudioID 名
        private readonly string m_name;

        // AudioID の固定整数値
        private readonly int m_value;

        /// <summary>
        /// AudioID 名を取得します。
        /// </summary>
        public string Name => m_name;

        /// <summary>
        /// AudioID の固定整数値を取得します。
        /// </summary>
        public int Value => m_value;

        /// <summary>
        /// AudioID 情報を生成します。
        /// </summary>
        /// <param name="name">AudioID 名。</param>
        /// <param name="value">固定整数値。</param>
        public IdEntry(string name, int value)
        {
            m_name = name;
            m_value = value;
        }
    }

    /// <summary>
    /// スクリプト参照スキャンの結果を表します。
    /// </summary>
    public struct ReferenceScanResult
    {
        // 該当ファイル数
        public int m_fileCount;

        // 該当箇所の合計数
        public int m_matchCount;
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
        int maxValue = 0;

        foreach (IdEntry entry in entries)
        {
            if (entry.Value > maxValue)
            {
                maxValue = entry.Value;
            }
        }

        int nextValue = maxValue + 1;
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

        builder.AppendLine(FILE_HEADER_COMMENT);
        builder.AppendLine($"{NEXT_AUDIO_ID_PREFIX}{nextValue}");
        builder.AppendLine();

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
    /// リネーム対象の AudioID が Assets 内の C# スクリプトでどれだけ参照されているかをスキャンします。
    /// </summary>
    /// <param name="renameMap">旧ID名から新ID名への対応マップ。</param>
    /// <returns>該当ファイル数と該当箇所数。</returns>
    public static ReferenceScanResult ScanScriptReferences(IReadOnlyDictionary<string, string> renameMap)
    {
        ReferenceScanResult result = new ReferenceScanResult();

        if (renameMap == null || renameMap.Count == 0)
        {
            return result;
        }

        Regex pattern = BuildReferenceRegex(renameMap);
        string audioIdFullPath = Path.GetFullPath(GetFilePath());

        foreach (string filePath in Directory.GetFiles(ASSETS_ROOT_FOLDER, CS_SEARCH_PATTERN, SearchOption.AllDirectories))
        {
            if (string.Equals(Path.GetFullPath(filePath), audioIdFullPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string content = File.ReadAllText(filePath);
            MatchCollection matches = pattern.Matches(content);

            if (matches.Count > 0)
            {
                result.m_fileCount++;
                result.m_matchCount += matches.Count;
            }
        }

        return result;
    }

    /// <summary>
    /// Assets 内の C# スクリプトに含まれる AudioID.旧名 の参照を一括で安全に置換します。
    /// </summary>
    /// <param name="renameMap">旧ID名から新ID名への対応マップ。</param>
    /// <returns>置換したファイル数と置換箇所数。</returns>
    public static ReferenceScanResult ReplaceScriptReferences(IReadOnlyDictionary<string, string> renameMap)
    {
        ReferenceScanResult result = new ReferenceScanResult();

        if (renameMap == null || renameMap.Count == 0)
        {
            return result;
        }

        Regex pattern = BuildReferenceRegex(renameMap);
        string audioIdFullPath = Path.GetFullPath(GetFilePath());

        foreach (string filePath in Directory.GetFiles(ASSETS_ROOT_FOLDER, CS_SEARCH_PATTERN, SearchOption.AllDirectories))
        {
            if (string.Equals(Path.GetFullPath(filePath), audioIdFullPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string content = File.ReadAllText(filePath);
            int fileMatchCount = 0;

            string replaced = pattern.Replace(content, match =>
            {
                string oldName = match.Groups[1].Value;
                if (renameMap.TryGetValue(oldName, out string newName))
                {
                    fileMatchCount++;
                    return $"{nameof(AudioID)}.{newName}";
                }

                return match.Value;
            });

            if (fileMatchCount > 0)
            {
                bool hasUtf8Bom = HasUtf8Bom(filePath);
                File.WriteAllText(filePath, replaced, new UTF8Encoding(hasUtf8Bom));
                result.m_fileCount++;
                result.m_matchCount += fileMatchCount;
            }
        }

        return result;
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

        string name = Regex.Replace(rawName.Trim(), @"[\s\-]+", "_");
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

        if (!(char.IsLetter(identifier[0]) || identifier[0] == '_'))
        {
            return false;
        }

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
    /// スワップ変更にも対応できる1パス一括置換用の正規表現を構築します。
    /// </summary>
    private static Regex BuildReferenceRegex(IReadOnlyDictionary<string, string> renameMap)
    {
        List<string> escapedNames = new List<string>(renameMap.Count);

        foreach (string oldName in renameMap.Keys)
        {
            escapedNames.Add(Regex.Escape(oldName));
        }

        // 長い名前から先に一致させるため文字長降順で並べる
        escapedNames.Sort((a, b) => b.Length.CompareTo(a.Length));

        string joinedNames = string.Join("|", escapedNames);
        return new Regex($@"(?<![\w]){nameof(AudioID)}\.({joinedNames})(?![\w])", RegexOptions.Compiled);
    }

    /// <summary>
    /// ファイルの先頭に UTF-8 BOM が付いているかを確認します。
    /// </summary>
    private static bool HasUtf8Bom(string filePath)
    {
        byte[] bom = new byte[3];
        using (FileStream stream = File.OpenRead(filePath))
        {
            if (stream.Read(bom, 0, 3) < 3)
            {
                return false;
            }
        }

        return bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF;
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