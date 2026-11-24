using UnityEditor;
using UnityEngine;

/// <summary>
/// HintDataAssetに登録されたHint内容のプレビューを表示します。
/// </summary>
[CustomEditor(typeof(HintDataAsset))]
public sealed class HintDataAssetEditor : Editor
{
    private SerializedProperty m_hintsProperty;
    private GUIStyle m_previewStyle;

    private void OnEnable()
    {
        m_hintsProperty = serializedObject.FindProperty("m_hints");
        m_previewStyle = new GUIStyle(EditorStyles.label)
        {
            richText = true,
            wordWrap = true,
            padding = new RectOffset(6, 6, 6, 6)
        };
    }

    /// <summary>
    /// Hint参照と各Hintの表示内容をInspectorへ描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(
            m_hintsProperty,
            new GUIContent("Hint一覧"),
            includeChildren: true);

        EditorGUILayout.Space(6.0f);
        EditorGUILayout.LabelField(
            "表示プレビュー",
            EditorStyles.boldLabel);

        for (int i = 0; i < m_hintsProperty.arraySize; i++)
        {
            SerializedProperty element =
                m_hintsProperty.GetArrayElementAtIndex(i);

            HintTextData hintTextData =
                element.objectReferenceValue as HintTextData;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(
                element,
                new GUIContent($"Hint {i}"));

            if (hintTextData == null)
            {
                EditorGUILayout.HelpBox(
                    "HintTextDataが設定されていません。",
                    MessageType.Warning);
                EditorGUILayout.EndVertical();
                continue;
            }

            EditorGUILayout.LabelField("見出し");
            GUILayout.Label(HighlightedTextFormatter.Format(
                hintTextData.Heading, hintTextData.HighlightColor), m_previewStyle,
                GUILayout.MinHeight(24.0f));
            EditorGUILayout.LabelField("本文");
            GUILayout.Label(HighlightedTextFormatter.Format(
                hintTextData.Body, hintTextData.HighlightColor), m_previewStyle,
                GUILayout.MinHeight(40.0f));

            EditorGUILayout.EndVertical();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
