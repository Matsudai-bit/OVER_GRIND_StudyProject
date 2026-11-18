using UnityEditor;
using UnityEngine;

/// <summary>
/// Manual 本文の強調色を Inspector でプレビューします。
/// </summary>
[CustomEditor(typeof(ManualTextData))]
public sealed class ManualTextDataEditor : Editor
{
    private GUIStyle m_previewStyle;

    /// <summary>編集欄と変換後の文章を表示します。</summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

        if (m_previewStyle == null)
        {
            m_previewStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                wordWrap = true,
                padding = new RectOffset(6, 6, 6, 6)
            };
        }

        ManualTextData data = (ManualTextData)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("表示プレビュー", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("[[文字]] の部分に強調色を適用します。", MessageType.Info);
        DrawPreview("画像の左側", data.LeftBody, data.HighlightColor);
        DrawPreview("1行目", data.Body, data.HighlightColor);
        DrawPreview("2行目", data.SecondBody, data.HighlightColor);
    }

    /// <summary>指定した本文の変換結果を表示します。</summary>
    private void DrawPreview(string title, string body, Color color)
    {
        EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label(HighlightedTextFormatter.Format(body, color), m_previewStyle,
            GUILayout.MinHeight(32));
        EditorGUILayout.EndVertical();
    }
}
