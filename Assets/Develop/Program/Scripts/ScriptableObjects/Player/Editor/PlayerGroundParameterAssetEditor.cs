#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

/// <summary>
/// プレイヤーの接地に関するパラメータをInspectorに表示・編集します。
/// </summary>
[CustomEditor(typeof(PlayerGroundParameterAsset))]
public sealed class PlayerGroundParameterAssetEditor : Editor
{
    // Inspector内のセクション間隔
    private const float SECTION_SPACE = 8.0f;


    // 接地判定を行う位置
    private SerializedProperty m_groundCheckOriginProperty;

    // 接地判定の半径
    private SerializedProperty m_groundCheckRadiusProperty;

    // 接地対象のレイヤー
    private SerializedProperty m_groundLayerMaskProperty;

    // 接地対象のレイヤー
    private SerializedProperty m_railLayerMaskProperty;

    // 地上・空中の判定半径を設定するアセット
    private SerializedProperty m_railDetectionParameterProperty;

    /// <summary>
    /// SerializedPropertyを取得します。
    /// </summary>
    private void OnEnable()
    {
        m_groundCheckOriginProperty =
            serializedObject.FindProperty("m_groundCheckOrigin");

        m_groundCheckRadiusProperty =
            serializedObject.FindProperty("m_groundCheckRadius");

        m_groundLayerMaskProperty =
            serializedObject.FindProperty("m_groundLayerMask");

        m_railLayerMaskProperty =
            serializedObject.FindProperty("m_railLayerMask");

        m_railDetectionParameterProperty =
            serializedObject.FindProperty("m_railDetectionParameter");
    }

    /// <summary>
    /// Inspectorを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDescription();

        EditorGUILayout.Space(SECTION_SPACE);

        DrawGroundSection();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// パラメータの説明を表示します。
    /// </summary>
    private void DrawDescription()
    {
        EditorGUILayout.HelpBox(
            "接地時の処理に使用されるパラメータを設定します",
            MessageType.Info);
    }

    /// <summary>
    /// 接地時パラメータを表示します。
    /// </summary>
    private void DrawGroundSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "接地時パラメータ",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            m_groundCheckOriginProperty,
            new GUIContent(
                "設定判定位置",
                "接地判定を行う位置です。"
                )
            );

        EditorGUILayout.PropertyField(
            m_groundCheckRadiusProperty,
            new GUIContent(
                "接地判定の半径",
                "接地判定の半径置です。"
                )
            );

        EditorGUILayout.PropertyField(
            m_groundLayerMaskProperty,
            new GUIContent(
                "接地対象のレイヤー",
                "接地対象のレイヤーです。"
                )
            );

        EditorGUILayout.PropertyField(
            m_railLayerMaskProperty,
            new GUIContent(
                "接地対象のレイヤー（レール用）",
                "レール専用の接地対象のレイヤーです。"
                )
            );

        EditorGUILayout.PropertyField(
            m_railDetectionParameterProperty,
            new GUIContent(
                "地上・空中の判定半径を設定するアセット",
                "地上・空中の判定半径を設定するアセットです。\n" +
                "未設定の場合は従来の接地半径を使います。"
                )
            );

        EditorGUILayout.EndVertical();
    }
}

#endif