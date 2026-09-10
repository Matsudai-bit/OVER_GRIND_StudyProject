#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// Inspector/Unity上から EffectID を追加・生成する専用ウィンドウ
/// </summary>
public class EffectIDGeneratorWindow : EditorWindow
{
    private string m_newIDName = "";

    [MenuItem("Tools/VFX/EffectID 編集・追加ツール")]
    public static void ShowWindow()
    {
        var window = GetWindow<EffectIDGeneratorWindow>("EffectID 編集");
        window.minSize = new Vector2(350, 180);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("EffectID の追加", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("新しいエフェクト名を入力して追加すると、自動で EffectID.cs が書き換わります。", MessageType.Info);

        EditorGUILayout.Space(10);
        m_newIDName = EditorGUILayout.TextField("追加するID名:", m_newIDName);

        EditorGUILayout.Space(15);
        if (GUILayout.Button("EffectID に追加する", GUILayout.Height(30)))
        {
            AddID();
        }
    }

    private void AddID()
    {
        if (string.IsNullOrWhiteSpace(m_newIDName))
        {
            EditorUtility.DisplayDialog("エラー", "ID名を入力してください。", "OK");
            return;
        }

        // 英数字とアンダースコア以外を除去
        string sanitizedID = System.Text.RegularExpressions.Regex.Replace(m_newIDName.Trim(), @"[^\w]", "");

        if (sanitizedID != m_newIDName.Trim())
        {
            EditorUtility.DisplayDialog("エラー", "ID名には半角英数字とアンダースコア（_）のみ使用できます。", "OK");
            return;
        }

        // 既存のEnum項目を取得
        List<string> existingNames = new List<string>(Enum.GetNames(typeof(EffectID)));

        if (existingNames.Contains(sanitizedID))
        {
            EditorUtility.DisplayDialog("警告", $"'{sanitizedID}' はすでに存在します。", "OK");
            return;
        }

        existingNames.Add(sanitizedID);

        // Enumファイルの書き換え処理
        GenerateEnumFile(existingNames);

        m_newIDName = "";
        GUI.FocusControl(null); // フォーカス解除

        AssetDatabase.Refresh(); // Unityアセットの再読み込み（自動コンパイル開始）
        EditorUtility.DisplayDialog("完了", $"'{sanitizedID}' を EffectID に追加しました！\nコンパイル完了後にプルダウンで選択可能", "OK");
    }

    private void GenerateEnumFile(List<string> idList)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("// 自動生成されたファイルです。手動で直接編集しないでください。");
        sb.AppendLine("public enum EffectID");
        sb.AppendLine("{");

        foreach (var id in idList)
        {
            sb.AppendLine($"    {id},");
        }

        sb.AppendLine("}");

        string fullPath = GetEffectIDFilePath();
        File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);
    }

    private string GetEffectIDFilePath()
    {
        // プロジェクト内から EffectID.cs を検索
        string[] guids = AssetDatabase.FindAssets("EffectID t:MonoScript");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileName(path) == "EffectID.cs")
            {
                return path;
            }
        }

        // 見つからなかった場合のデフォルトパス
        return "Assets/PreDevelop/AOKI/Scripts/Effects_AokiScripts/EffectID.cs";
    }
}
#endif