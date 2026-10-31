// ============================================================================
// SceneYamlSizeAnalyzerWindow.cs
// Unity 6 / Editor only / UTF-8 BOM
//
// Scene (.unity) の UnityYAML を読み取り専用で走査し、サイズの大きい
// YAML ドキュメント・シリアライズ項目・Prefab Override を特定します。
// Editor フォルダーに配置してください。
// ============================================================================

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene の YAML を解析し、容量を消費するデータを表示する Editor ウィンドウです。
/// </summary>
public sealed class SceneYamlSizeAnalyzerWindow : EditorWindow
{
    private const int DEFAULT_ROW_LIMIT = 40;
    private const int MAX_ROW_LIMIT = 200;
    private const float BYTES_PER_MIB = 1024f * 1024f;

    private SceneAsset m_sceneAsset;
    private SceneYamlSizeAnalysis m_analysis;
    private SceneYamlDocument m_selectedDocument;
    private Vector2 m_scrollPosition;
    private int m_rowLimit = DEFAULT_ROW_LIMIT;
    private string m_statusMessage = string.Empty;
    private readonly Dictionary<string, string> m_guidPathCache = new Dictionary<string, string>();

    /// <summary>
    /// 解析ウィンドウを開きます。
    /// </summary>
    [MenuItem("Tools/Scene YAML Size Analyzer")]
    public static void OpenWindow()
    {
        GetWindow<SceneYamlSizeAnalyzerWindow>("Scene YAML Analyzer");
    }

    /// <summary>
    /// 選択中の Scene または現在の Scene を初期対象にします。
    /// </summary>
    private void OnEnable()
    {
        m_sceneAsset = Selection.activeObject as SceneAsset;
        if (m_sceneAsset != null)
        {
            return;
        }

        string scenePath = SceneManager.GetActiveScene().path;
        if (!string.IsNullOrEmpty(scenePath))
        {
            m_sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        }
    }

    /// <summary>
    /// 解析対象の指定、実行、集計結果の表示を行います。
    /// </summary>
    private void OnGUI()
    {
        EditorGUILayout.LabelField("Scene YAML 容量診断", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "ディスク上の .unity ファイルを読み取り専用で解析します。" +
            "Scene や Prefab は変更しません。未保存の変更は結果に含まれません。",
            MessageType.Info);

        EditorGUI.BeginChangeCheck();
        m_sceneAsset = (SceneAsset)EditorGUILayout.ObjectField(
            "対象 Scene", m_sceneAsset, typeof(SceneAsset), false);
        if (EditorGUI.EndChangeCheck())
        {
            m_analysis = null;
            m_selectedDocument = null;
            m_statusMessage = string.Empty;
        }

        m_rowLimit = EditorGUILayout.IntSlider("表示件数", m_rowLimit, 10, MAX_ROW_LIMIT);

        EditorGUILayout.BeginHorizontal();
        using (new EditorGUI.DisabledScope(m_sceneAsset == null))
        {
            if (GUILayout.Button("YAML を解析", GUILayout.Height(28)))
            {
                AnalyzeSelectedScene();
            }
        }

        using (new EditorGUI.DisabledScope(m_analysis == null))
        {
            if (GUILayout.Button("CSV を保存", GUILayout.Height(28)))
            {
                ExportCsvFiles();
            }
        }
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(m_statusMessage))
        {
            EditorGUILayout.HelpBox(m_statusMessage, MessageType.None);
        }

        if (m_analysis == null)
        {
            return;
        }

        m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);
        DrawOverview();
        DrawTypeSummary();
        DrawLargestFields();
        DrawPrefabSummary();
        DrawLargestDocuments();
        DrawSelectedDocument();
        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// SceneAsset の保存済み YAML を検証し、ストリーミング走査します。
    /// </summary>
    private void AnalyzeSelectedScene()
    {
        string assetPath = AssetDatabase.GetAssetPath(m_sceneAsset);
        if (string.IsNullOrEmpty(assetPath) ||
            !assetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
        {
            m_statusMessage = "Scene (.unity) アセットを選択してください。";
            return;
        }

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string absolutePath = Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        if (!File.Exists(absolutePath))
        {
            m_statusMessage = "シーンファイルが見つかりません: " + absolutePath;
            return;
        }

        try
        {
            m_analysis = SceneYamlSizeScanner.Scan(absolutePath, assetPath);
            m_selectedDocument = null;
            m_statusMessage = string.Format(
                CultureInfo.InvariantCulture,
                "解析完了: {0:N0} ドキュメント / {1:N2} MiB。CSV に全件を出力できます。",
                m_analysis.Documents.Count,
                m_analysis.FileBytes / BYTES_PER_MIB);
        }
        catch (OperationCanceledException)
        {
            m_statusMessage = "解析をキャンセルしました。元の Scene は変更されていません。";
        }
        catch (Exception exception)
        {
            m_statusMessage = "解析失敗: " + exception.Message;
            Debug.LogException(exception);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    /// <summary>
    /// 解析結果の基本統計を表示します。
    /// </summary>
    private void DrawOverview()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("全体概要", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Scene", m_analysis.AssetPath);
        EditorGUILayout.LabelField("ファイルサイズ", FormatBytes(m_analysis.FileBytes));
        EditorGUILayout.LabelField("YAML ドキュメント数", m_analysis.Documents.Count.ToString("N0"));
        EditorGUILayout.LabelField("Prefab Instance 数", m_analysis.PrefabInstanceCount.ToString("N0"));
        EditorGUILayout.LabelField("Prefab property Override 数", m_analysis.OverrideCount.ToString("N0"));

        if (m_analysis.PreambleBytes > 0)
        {
            EditorGUILayout.LabelField("YAML ヘッダー等", FormatBytes(m_analysis.PreambleBytes));
        }
    }

    /// <summary>
    /// YAML 型別の合計容量を表示します。
    /// </summary>
    private void DrawTypeSummary()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("型別・容量上位 15 件", EditorStyles.boldLabel);
        foreach (SceneYamlTypeSummary summary in m_analysis.TypeSummaries.Take(15))
        {
            EditorGUILayout.LabelField(
                string.Format("{0}  ({1:N0} 件)", summary.TypeName, summary.Count),
                FormatBytes(summary.Bytes));
        }
    }

    /// <summary>
    /// 全ドキュメント合算で容量が大きいトップレベル項目を表示します。
    /// </summary>
    private void DrawLargestFields()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("シリアライズ項目別・合計容量上位 15 件", EditorStyles.boldLabel);
        foreach (SceneYamlFieldSummary summary in m_analysis.FieldSummaries.Take(15))
        {
            EditorGUILayout.LabelField(summary.TypeName + " / " + summary.FieldName,
                FormatBytes(summary.Bytes));
        }
    }

    /// <summary>
    /// 同じ Prefab が大量配置されている場合の累積容量を表示します。
    /// </summary>
    private void DrawPrefabSummary()
    {
        if (m_analysis.PrefabSummaries.Count == 0)
        {
            return;
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Prefab アセット別・シーン内累積容量上位 15 件", EditorStyles.boldLabel);
        foreach (SceneYamlPrefabSummary summary in m_analysis.PrefabSummaries.Take(15))
        {
            string path = ResolveAssetPath(summary.PrefabGuid);
            string label = string.IsNullOrEmpty(path) ? summary.PrefabGuid : path;
            EditorGUILayout.LabelField(
                string.Format("{0} ({1:N0} インスタンス / {2:N0} overrides)",
                    label, summary.Count, summary.OverrideCount),
                FormatBytes(summary.Bytes));
        }
    }

    /// <summary>
    /// 容量の大きい YAML ドキュメントを表示します。
    /// </summary>
    private void DrawLargestDocuments()
    {
        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("サイズの大きい YAML ドキュメント", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "『GameObject 名』はコンポーネントの m_GameObject を参照して取得します。" +
            "PrefabInstance は元 Prefab のパスを表示します。行を押すと項目別の内訳が確認できます。",
            MessageType.None);

        foreach (SceneYamlDocument document in m_analysis.LargestDocuments.Take(m_rowLimit))
        {
            string label = string.Format(
                CultureInfo.InvariantCulture,
                "{0,7:F2} MiB | {1} | {2} | line {3:N0}",
                document.Bytes / BYTES_PER_MIB,
                document.TypeName,
                GetDocumentLabel(document),
                document.StartLine);

            if (GUILayout.Button(label, EditorStyles.miniButton))
            {
                m_selectedDocument = document;
            }
        }
    }

    /// <summary>
    /// 選択したドキュメントの各プロパティの保存サイズを表示します。
    /// </summary>
    private void DrawSelectedDocument()
    {
        if (m_selectedDocument == null)
        {
            return;
        }

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("選択ドキュメントの詳細", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("型", m_selectedDocument.TypeName);
        EditorGUILayout.LabelField("File ID", m_selectedDocument.FileId);
        EditorGUILayout.LabelField("対象", GetDocumentLabel(m_selectedDocument));
        EditorGUILayout.LabelField("容量", FormatBytes(m_selectedDocument.Bytes));
        EditorGUILayout.LabelField("開始行", m_selectedDocument.StartLine.ToString("N0"));
        EditorGUILayout.LabelField("最長行", FormatBytes(m_selectedDocument.LongestLineBytes));

        if (m_selectedDocument.TypeName == "PrefabInstance")
        {
            EditorGUILayout.LabelField("property Override 件数",
                m_selectedDocument.OverrideCount.ToString("N0"));
        }

        EditorGUILayout.Space(3);
        EditorGUILayout.LabelField("シリアライズ項目別の容量", EditorStyles.boldLabel);
        foreach (KeyValuePair<string, long> field in m_selectedDocument.FieldBytes
                     .OrderByDescending(pair => pair.Value).Take(35))
        {
            EditorGUILayout.LabelField(field.Key, FormatBytes(field.Value));
        }
    }

    /// <summary>
    /// YAML ドキュメントに対応する GameObject 名や Prefab パスを返します。
    /// </summary>
    private string GetDocumentLabel(SceneYamlDocument document)
    {
        if (document.TypeName == "PrefabInstance" &&
            !string.IsNullOrEmpty(document.PrefabGuid))
        {
            string path = ResolveAssetPath(document.PrefabGuid);
            return !string.IsNullOrEmpty(path) ? path : "GUID: " + document.PrefabGuid;
        }

        if (!string.IsNullOrEmpty(document.GameObjectId) &&
            m_analysis.GameObjectNames.TryGetValue(document.GameObjectId, out string name))
        {
            if (document.TypeName == "MonoBehaviour" &&
                !string.IsNullOrEmpty(document.ScriptGuid))
            {
                string scriptPath = ResolveAssetPath(document.ScriptGuid);
                if (!string.IsNullOrEmpty(scriptPath))
                {
                    return name + " / " + Path.GetFileNameWithoutExtension(scriptPath);
                }
            }

            return name;
        }

        if (!string.IsNullOrEmpty(document.Name))
        {
            return document.Name;
        }

        return "fileID: " + document.FileId;
    }

    /// <summary>
    /// GUID の解決結果をキャッシュし、Inspectorの再描画やCSV出力を軽量化します。
    /// </summary>
    private string ResolveAssetPath(string guid)
    {
        if (!m_guidPathCache.TryGetValue(guid, out string path))
        {
            path = AssetDatabase.GUIDToAssetPath(guid);
            m_guidPathCache.Add(guid, path);
        }

        return path;
    }

    /// <summary>
    /// 型別・ドキュメント別・ドキュメント内フィールド別の CSV を出力します。
    /// </summary>
    private void ExportCsvFiles()
    {
        string basePath = EditorUtility.SaveFilePanel(
            "CSV 出力先 (5 ファイル生成)",
            Directory.GetParent(Application.dataPath).FullName,
            "SceneYamlAnalysis",
            "csv");

        if (string.IsNullOrEmpty(basePath))
        {
            return;
        }

        string prefix = Path.Combine(
            Path.GetDirectoryName(basePath),
            Path.GetFileNameWithoutExtension(basePath));

        try
        {
            ExportDocumentCsv(prefix + "_documents.csv");
            ExportFieldCsv(prefix + "_fields.csv");
            ExportTypeCsv(prefix + "_types.csv");
            ExportFieldSummaryCsv(prefix + "_field_totals.csv");
            ExportPrefabSummaryCsv(prefix + "_prefab_totals.csv");
            m_statusMessage = "CSV を5ファイル出力しました: " + prefix + "_*.csv";
            EditorUtility.RevealInFinder(prefix + "_documents.csv");
        }
        catch (Exception exception)
        {
            m_statusMessage = "CSV 出力失敗: " + exception.Message;
            Debug.LogException(exception);
        }
    }

    /// <summary>
    /// サイズの大きい各 YAML ドキュメントの情報を保存します。
    /// </summary>
    private void ExportDocumentCsv(string path)
    {
        using (StreamWriter writer = OpenCsvWriter(path))
        {
            WriteCsvRow(writer,
                "Type", "FileId", "Label", "Bytes", "MiB", "StartLine",
                "LongestLineBytes", "OverrideCount", "LargestField", "LargestFieldBytes");

            foreach (SceneYamlDocument document in m_analysis.LargestDocuments)
            {
                KeyValuePair<string, long> largestField = document.FieldBytes
                    .OrderByDescending(pair => pair.Value).FirstOrDefault();

                WriteCsvRow(writer,
                    document.TypeName,
                    document.FileId,
                    GetDocumentLabel(document),
                    document.Bytes.ToString(CultureInfo.InvariantCulture),
                    (document.Bytes / BYTES_PER_MIB).ToString("F4", CultureInfo.InvariantCulture),
                    document.StartLine.ToString(CultureInfo.InvariantCulture),
                    document.LongestLineBytes.ToString(CultureInfo.InvariantCulture),
                    document.OverrideCount.ToString(CultureInfo.InvariantCulture),
                    largestField.Key ?? string.Empty,
                    largestField.Value.ToString(CultureInfo.InvariantCulture));
            }
        }
    }

    /// <summary>
    /// 各 YAML ドキュメント内のトップレベル項目の容量を保存します。
    /// </summary>
    private void ExportFieldCsv(string path)
    {
        using (StreamWriter writer = OpenCsvWriter(path))
        {
            WriteCsvRow(writer, "Type", "FileId", "Label", "Field", "Bytes", "MiB");

            foreach (SceneYamlDocument document in m_analysis.LargestDocuments)
            {
                string label = GetDocumentLabel(document);
                foreach (KeyValuePair<string, long> field in document.FieldBytes
                             .OrderByDescending(pair => pair.Value))
                {
                    WriteCsvRow(writer,
                        document.TypeName,
                        document.FileId,
                        label,
                        field.Key,
                        field.Value.ToString(CultureInfo.InvariantCulture),
                        (field.Value / BYTES_PER_MIB).ToString("F4", CultureInfo.InvariantCulture));
                }
            }
        }
    }

    /// <summary>
    /// YAML の型ごとの合計容量を保存します。
    /// </summary>
    private void ExportTypeCsv(string path)
    {
        using (StreamWriter writer = OpenCsvWriter(path))
        {
            WriteCsvRow(writer, "Type", "Count", "Bytes", "MiB");
            foreach (SceneYamlTypeSummary summary in m_analysis.TypeSummaries)
            {
                WriteCsvRow(writer,
                    summary.TypeName,
                    summary.Count.ToString(CultureInfo.InvariantCulture),
                    summary.Bytes.ToString(CultureInfo.InvariantCulture),
                    (summary.Bytes / BYTES_PER_MIB).ToString("F4", CultureInfo.InvariantCulture));
            }
        }
    }

    /// <summary>
    /// 型ごとのトップレベルシリアライズ項目を集計したCSVを保存します。
    /// </summary>
    private void ExportFieldSummaryCsv(string path)
    {
        using (StreamWriter writer = OpenCsvWriter(path))
        {
            WriteCsvRow(writer, "Type", "Field", "Bytes", "MiB");
            foreach (SceneYamlFieldSummary summary in m_analysis.FieldSummaries)
            {
                WriteCsvRow(writer,
                    summary.TypeName,
                    summary.FieldName,
                    summary.Bytes.ToString(CultureInfo.InvariantCulture),
                    (summary.Bytes / BYTES_PER_MIB).ToString("F4", CultureInfo.InvariantCulture));
            }
        }
    }

    /// <summary>
    /// 参照するPrefabアセット別のシーン内累積容量をCSVに保存します。
    /// </summary>
    private void ExportPrefabSummaryCsv(string path)
    {
        using (StreamWriter writer = OpenCsvWriter(path))
        {
            WriteCsvRow(writer, "Prefab", "PrefabGuid", "Count", "OverrideCount", "Bytes", "MiB");
            foreach (SceneYamlPrefabSummary summary in m_analysis.PrefabSummaries)
            {
                WriteCsvRow(writer,
                    ResolveAssetPath(summary.PrefabGuid),
                    summary.PrefabGuid,
                    summary.Count.ToString(CultureInfo.InvariantCulture),
                    summary.OverrideCount.ToString(CultureInfo.InvariantCulture),
                    summary.Bytes.ToString(CultureInfo.InvariantCulture),
                    (summary.Bytes / BYTES_PER_MIB).ToString("F4", CultureInfo.InvariantCulture));
            }
        }
    }

    /// <summary>
    /// 日本語がExcelで文字化けしにくい BOM付き UTF-8 Writer を作成します。
    /// </summary>
    private static StreamWriter OpenCsvWriter(string path)
    {
        return new StreamWriter(path, false, new UTF8Encoding(true));
    }

    /// <summary>
    /// CSV の引用符や改行をエスケープして1行を書き込みます。
    /// </summary>
    private static void WriteCsvRow(StreamWriter writer, params string[] values)
    {
        writer.WriteLine(string.Join(",", values.Select(value =>
            "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"")));
    }

    /// <summary>
    /// バイト数を MiB とバイト数の併記で表示します。
    /// </summary>
    private static string FormatBytes(long bytes)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0:N2} MiB ({1:N0} B)", bytes / BYTES_PER_MIB, bytes);
    }
}

/// <summary>
/// UnityYAMLのドキュメントごとの統計情報です。
/// </summary>
internal sealed class SceneYamlDocument
{
    public string TypeName { get; set; } = "Unknown";
    public string FileId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string GameObjectId { get; set; } = string.Empty;
    public string ScriptGuid { get; set; } = string.Empty;
    public string PrefabGuid { get; set; } = string.Empty;
    public long StartLine { get; set; }
    public long Bytes { get; set; }
    public long LongestLineBytes { get; set; }
    public long OverrideCount { get; set; }
    public string CurrentField { get; set; } = "(ヘッダー)";
    public Dictionary<string, long> FieldBytes { get; } = new Dictionary<string, long>();

    /// <summary>
    /// 指定フィールドに1行分の容量を加算します。
    /// </summary>
    public void AddLineBytes(long lineBytes)
    {
        Bytes += lineBytes;
        if (lineBytes > LongestLineBytes)
        {
            LongestLineBytes = lineBytes;
        }

        if (!FieldBytes.TryGetValue(CurrentField, out long currentBytes))
        {
            currentBytes = 0;
        }

        FieldBytes[CurrentField] = currentBytes + lineBytes;
    }
}

/// <summary>
/// YAMLの型別統計です。
/// </summary>
internal sealed class SceneYamlTypeSummary
{
    public string TypeName { get; set; }
    public long Count { get; set; }
    public long Bytes { get; set; }
}

/// <summary>
/// YAML型とフィールドの組み合わせによる合計容量です。
/// </summary>
internal sealed class SceneYamlFieldSummary
{
    public string TypeName { get; set; }
    public string FieldName { get; set; }
    public long Bytes { get; set; }
}

/// <summary>
/// 参照元 PrefabAsset ごとのシーン内累積容量です。
/// </summary>
internal sealed class SceneYamlPrefabSummary
{
    public string PrefabGuid { get; set; }
    public long Count { get; set; }
    public long OverrideCount { get; set; }
    public long Bytes { get; set; }
}

/// <summary>
/// ファイル全体の解析結果です。
/// </summary>
internal sealed class SceneYamlSizeAnalysis
{
    public string AssetPath { get; set; }
    public long FileBytes { get; set; }
    public long PreambleBytes { get; set; }
    public long OverrideCount { get; set; }
    public long PrefabInstanceCount { get; set; }
    public List<SceneYamlDocument> Documents { get; } = new List<SceneYamlDocument>();
    public Dictionary<string, string> GameObjectNames { get; } = new Dictionary<string, string>();
    public List<SceneYamlDocument> LargestDocuments { get; set; } = new List<SceneYamlDocument>();
    public List<SceneYamlTypeSummary> TypeSummaries { get; set; } = new List<SceneYamlTypeSummary>();
    public List<SceneYamlFieldSummary> FieldSummaries { get; set; } = new List<SceneYamlFieldSummary>();
    public List<SceneYamlPrefabSummary> PrefabSummaries { get; set; } = new List<SceneYamlPrefabSummary>();

    /// <summary>
    /// 保存サイズを基準にソートし、Prefab Override数や型別集計を確定します。
    /// </summary>
    public void FinalizeResults()
    {
        LargestDocuments = Documents.OrderByDescending(document => document.Bytes).ToList();
        TypeSummaries = Documents
            .GroupBy(document => document.TypeName)
            .Select(group => new SceneYamlTypeSummary
            {
                TypeName = group.Key,
                Count = group.LongCount(),
                Bytes = group.Sum(document => document.Bytes)
            })
            .OrderByDescending(summary => summary.Bytes)
            .ToList();

        FieldSummaries = Documents
            .SelectMany(document => document.FieldBytes.Select(field => new
            {
                document.TypeName,
                FieldName = field.Key,
                Bytes = field.Value
            }))
            .GroupBy(item => new { item.TypeName, item.FieldName })
            .Select(group => new SceneYamlFieldSummary
            {
                TypeName = group.Key.TypeName,
                FieldName = group.Key.FieldName,
                Bytes = group.Sum(item => item.Bytes)
            })
            .OrderByDescending(summary => summary.Bytes)
            .ToList();

        PrefabSummaries = Documents
            .Where(document => document.TypeName == "PrefabInstance" &&
                               !string.IsNullOrEmpty(document.PrefabGuid))
            .GroupBy(document => document.PrefabGuid)
            .Select(group => new SceneYamlPrefabSummary
            {
                PrefabGuid = group.Key,
                Count = group.LongCount(),
                OverrideCount = group.Sum(document => document.OverrideCount),
                Bytes = group.Sum(document => document.Bytes)
            })
            .OrderByDescending(summary => summary.Bytes)
            .ToList();

        foreach (SceneYamlDocument document in Documents)
        {
            OverrideCount += document.OverrideCount;
            if (document.TypeName == "PrefabInstance")
            {
                PrefabInstanceCount++;
            }

            if (document.TypeName == "GameObject" &&
                !string.IsNullOrEmpty(document.FileId) &&
                !string.IsNullOrEmpty(document.Name))
            {
                GameObjectNames[document.FileId] = document.Name;
            }
        }
    }
}

/// <summary>
/// Scene ファイルを変更せず、ファイルサイズとYAML項目を逐次解析します。
/// ファイル全体をメモリにロードしないため、大規模Sceneにも利用できます。
/// </summary>
internal static class SceneYamlSizeScanner
{
    private const int READ_BUFFER_SIZE = 64 * 1024;
    private const int LINE_PREFIX_SIZE = 2048;
    private const long PROGRESS_INTERVAL_BYTES = 4L * 1024 * 1024;

    /// <summary>
    /// YAML テキストをバイト単位で読み取り、実バイト数に基づく集計結果を返します。
    /// </summary>
    public static SceneYamlSizeAnalysis Scan(string absolutePath, string assetPath)
    {
        SceneYamlSizeAnalysis result = new SceneYamlSizeAnalysis
        {
            AssetPath = assetPath,
            FileBytes = new FileInfo(absolutePath).Length
        };

        byte[] chunk = new byte[READ_BUFFER_SIZE];
        byte[] linePrefix = new byte[LINE_PREFIX_SIZE];
        int prefixLength = 0;
        long lineBytes = 0;
        long lineNumber = 0;
        long lastProgressBytes = 0;
        SceneYamlDocument currentDocument = null;

        try
        {
            using (FileStream stream = new FileStream(
                       absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                       READ_BUFFER_SIZE, FileOptions.SequentialScan))
            {
                int readBytes;
                while ((readBytes = stream.Read(chunk, 0, chunk.Length)) > 0)
                {
                    for (int index = 0; index < readBytes; index++)
                    {
                        byte character = chunk[index];
                        lineBytes++;

                        if (character == (byte)'\n')
                        {
                            lineNumber++;
                            ProcessLine(result, ref currentDocument,
                                linePrefix, prefixLength, lineBytes, lineNumber);
                            prefixLength = 0;
                            lineBytes = 0;
                        }
                        else if (prefixLength < LINE_PREFIX_SIZE)
                        {
                            // メタ情報は行頭だけ使用します。長い配列行でもメモリを増やしません。
                            linePrefix[prefixLength++] = character;
                        }
                    }

                    if (stream.Position - lastProgressBytes >= PROGRESS_INTERVAL_BYTES)
                    {
                        lastProgressBytes = stream.Position;
                        bool canceled = EditorUtility.DisplayCancelableProgressBar(
                            "Scene YAML 解析中",
                            string.Format(CultureInfo.InvariantCulture,
                                "{0:N1} / {1:N1} MiB",
                                stream.Position / 1048576.0,
                                result.FileBytes / 1048576.0),
                            result.FileBytes > 0
                                ? (float)stream.Position / result.FileBytes
                                : 1f);

                        if (canceled)
                        {
                            throw new OperationCanceledException();
                        }
                    }
                }
            }

            // 最終行が LF で終わらないファイルへの対応。
            if (lineBytes > 0)
            {
                lineNumber++;
                ProcessLine(result, ref currentDocument,
                    linePrefix, prefixLength, lineBytes, lineNumber);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        if (result.Documents.Count == 0)
        {
            throw new InvalidDataException(
                "UnityYAML のドキュメント (--- !u!...) が見つかりません。" +
                "Editor > Asset Serialization > Force Text を確認してください。");
        }

        result.FinalizeResults();
        return result;
    }

    /// <summary>
    /// 1行を解析します。サイズはUTF-8で再計算せず、実際に読み込んだバイト数を用います。
    /// </summary>
    private static void ProcessLine(
        SceneYamlSizeAnalysis result,
        ref SceneYamlDocument currentDocument,
        byte[] prefix,
        int prefixLength,
        long totalLineBytes,
        long lineNumber)
    {
        // Unicode文字を含む場合も、文書境界は ASCII プレフィックスで判定できます。
        bool isDocumentStart = StartsWithAscii(prefix, prefixLength, "--- !u!");
        if (isDocumentStart)
        {
            string header = Encoding.UTF8.GetString(prefix, 0, prefixLength).TrimEnd('\r');
            currentDocument = CreateDocumentFromHeader(header, lineNumber);
            result.Documents.Add(currentDocument);
            currentDocument.AddLineBytes(totalLineBytes);
            return;
        }

        if (currentDocument == null)
        {
            result.PreambleBytes += totalLineBytes;
            return;
        }

        // 先頭2つのスペース＋キー名は、UnityYAMLの直下プロパティを表します。
        bool isTopField = prefixLength >= 3 &&
                          prefix[0] == (byte)' ' &&
                          prefix[1] == (byte)' ' &&
                          prefix[2] != (byte)' ' &&
                          prefix[2] != (byte)'-';
        bool isRootType = prefixLength >= 2 &&
                          prefix[0] >= (byte)'A' &&
                          prefix[0] <= (byte)'Z';

        // PrefabInstance の propertyPath は中階層に存在します。
        bool maybeOverride = currentDocument.TypeName == "PrefabInstance" &&
                             HasIndentedKey(prefix, prefixLength, "propertyPath:");

        if (isTopField || isRootType || maybeOverride)
        {
            string line = Encoding.UTF8.GetString(prefix, 0, prefixLength).TrimEnd('\r');
            if (isRootType)
            {
                int colonIndex = line.IndexOf(':');
                if (colonIndex > 0)
                {
                    currentDocument.TypeName = line.Substring(0, colonIndex);
                }
            }

            if (isTopField)
            {
                int colonIndex = line.IndexOf(':', 2);
                if (colonIndex > 2)
                {
                    string key = line.Substring(2, colonIndex - 2);
                    currentDocument.CurrentField = key;
                    string value = line.Substring(colonIndex + 1).Trim();
                    ProcessTopLevelValue(currentDocument, key, value);
                }
            }

            if (maybeOverride)
            {
                currentDocument.OverrideCount++;
            }
        }

        currentDocument.AddLineBytes(totalLineBytes);
    }

    /// <summary>
    /// YAML ドキュメントの Class ID と File ID をヘッダーから取得します。
    /// </summary>
    private static SceneYamlDocument CreateDocumentFromHeader(string header, long lineNumber)
    {
        SceneYamlDocument document = new SceneYamlDocument { StartLine = lineNumber };
        int fileIdStart = header.IndexOf('&');
        if (fileIdStart < 0)
        {
            return document;
        }

        int fileIdEnd = header.IndexOf(' ', fileIdStart);
        if (fileIdEnd < 0)
        {
            fileIdEnd = header.Length;
        }

        document.FileId = header.Substring(fileIdStart + 1, fileIdEnd - fileIdStart - 1);
        return document;
    }

    /// <summary>
    /// 直下プロパティからオブジェクト名、所有 GameObject、参照 GUID を抽出します。
    /// </summary>
    private static void ProcessTopLevelValue(SceneYamlDocument document, string key, string value)
    {
        switch (key)
        {
            case "m_Name":
                document.Name = value.Trim('"', '\'');
                break;

            case "m_GameObject":
                document.GameObjectId = ExtractReferenceValue(value, "fileID:");
                break;

            case "m_Script":
                document.ScriptGuid = ExtractReferenceValue(value, "guid:");
                break;

            case "m_SourcePrefab":
                document.PrefabGuid = ExtractReferenceValue(value, "guid:");
                break;
        }
    }

    /// <summary>
    /// Unity参照 {fileID: ..., guid: ..., type: ...} から指定値を取得します。
    /// </summary>
    private static string ExtractReferenceValue(string value, string key)
    {
        int keyIndex = value.IndexOf(key, StringComparison.Ordinal);
        if (keyIndex < 0)
        {
            return string.Empty;
        }

        int valueStart = keyIndex + key.Length;
        while (valueStart < value.Length && char.IsWhiteSpace(value[valueStart]))
        {
            valueStart++;
        }

        int valueEnd = valueStart;
        while (valueEnd < value.Length &&
               value[valueEnd] != ',' && value[valueEnd] != '}' &&
               !char.IsWhiteSpace(value[valueEnd]))
        {
            valueEnd++;
        }

        return value.Substring(valueStart, valueEnd - valueStart).Trim();
    }

    /// <summary>
    /// 先頭の空白を無視して YAML の key が一致するか判定します。
    /// </summary>
    private static bool HasIndentedKey(byte[] prefix, int prefixLength, string key)
    {
        int offset = 0;
        while (offset < prefixLength && prefix[offset] == (byte)' ')
        {
            offset++;
        }

        if (offset == 0 || prefixLength - offset < key.Length)
        {
            return false;
        }

        for (int index = 0; index < key.Length; index++)
        {
            if (prefix[offset + index] != (byte)key[index])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 行頭のASCIIキーをバイト単位で比較します。
    /// </summary>
    private static bool StartsWithAscii(byte[] prefix, int prefixLength, string ascii)
    {
        if (prefixLength < ascii.Length)
        {
            return false;
        }

        for (int index = 0; index < ascii.Length; index++)
        {
            if (prefix[index] != (byte)ascii[index])
            {
                return false;
            }
        }

        return true;
    }
}
#endif
