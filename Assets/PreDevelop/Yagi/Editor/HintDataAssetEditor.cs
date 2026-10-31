using UnityEditor;
using UnityEngine;

/// <summary>
/// HintDataAssetに登録されたHint内容のプレビューを表示します。
/// </summary>
[CustomEditor(typeof(HintDataAsset))]
public sealed class HintDataAssetEditor : Editor
{
    private SerializedProperty m_hintsProperty;

    private void OnEnable()
    {
        m_hintsProperty = serializedObject.FindProperty("m_hints");
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

            SerializedObject hintObject =
                new SerializedObject(hintTextData);
            hintObject.Update();

            SerializedProperty heading =
                hintObject.FindProperty("m_heading");
            SerializedProperty body =
                hintObject.FindProperty("m_body");

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.LabelField("見出し");
            EditorGUILayout.TextArea(heading.stringValue, GUILayout.MinHeight(24.0f));
            EditorGUILayout.LabelField("本文");
            EditorGUILayout.TextArea(body.stringValue, GUILayout.MinHeight(40.0f));
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
