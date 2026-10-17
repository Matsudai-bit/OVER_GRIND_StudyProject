#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

/// <summary>
/// プレイヤーのロックオンに関するパラメータをInspectorに表示・編集します。
/// </summary>
[CustomEditor(typeof(PlayerTargetParameterAsset))]
public class PlayerTargetParameterAssetEditor : Editor
{
    // Inspector内のセクション間隔
    private const float SECTION_SPACE = 8.0f;


    // ロックオン対象を探す範囲
    private SerializedProperty m_targetSearchRadiusProperty;

    // ロックオン対象として検索するLayer
    private SerializedProperty m_targetLayerProperty;

    // 対象からのレティクル距離
    private SerializedProperty m_reticleDistanceFromTargetProperty;

    // レティクル移動時間
    private SerializedProperty m_reticleMoveDurationProperty;

    // カメラフォーカス時間
    private SerializedProperty m_cameraFocusDurationProperty;

    /// <summary>
    /// SerializedPropertyを取得します。
    /// </summary>
    private void OnEnable()
    {
        m_targetSearchRadiusProperty =
            serializedObject.FindProperty("m_targetSearchRadius");

        m_targetLayerProperty =
            serializedObject.FindProperty("m_targetLayer");

        m_reticleDistanceFromTargetProperty =
            serializedObject.FindProperty("m_reticleDistanceFromTarget");

        m_reticleMoveDurationProperty =
            serializedObject.FindProperty("m_reticleMoveDuration");

        m_cameraFocusDurationProperty =
            serializedObject.FindProperty("m_cameraFocusDuration");
    }

    /// <summary>
    /// Inspectorを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDescription();

        EditorGUILayout.Space(SECTION_SPACE);

        DrawTargetSection();

        EditorGUILayout.Space(SECTION_SPACE);

        DrawReticleSection();

        EditorGUILayout.Space(SECTION_SPACE);

        DrawCameraSection();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// パラメータの説明を表示します。
    /// </summary>
    private void DrawDescription()
    {
        EditorGUILayout.HelpBox(
            "ロックオンの処理に使用されるパラメータを設定します",
            MessageType.Info);
    }

    /// <summary>
    /// ロックオン対象のパラメータを表示します。
    /// </summary>
    private void DrawTargetSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "ロックオン対象",
            EditorStyles.boldLabel
        );

        EditorGUILayout.PropertyField(
            m_targetSearchRadiusProperty,
            new GUIContent(
                "検索範囲",
                "ロックオン対象を探す範囲です。"
                )
            );

        EditorGUILayout.PropertyField(
            m_targetLayerProperty,
            new GUIContent(
                "検索対象Layer",
                "ロックオン対象として検索するLayerです。"
                )
            );

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// レティクルのパラメータを表示します。
    /// </summary>
    private void DrawReticleSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "ターゲットレティクル",
            EditorStyles.boldLabel
        );

        EditorGUILayout.PropertyField(
            m_reticleDistanceFromTargetProperty,
            new GUIContent(
                "対象からの距離",
                "対象からのレティクル距離です。"
                )
            );

        EditorGUILayout.PropertyField(
            m_reticleMoveDurationProperty,
            new GUIContent(
                "移動時間",
                "レティクルが移動するまでの時間（秒）です。"
                )
            );

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// カメラのパラメータを表示します。
    /// </summary>
    private void DrawCameraSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "カメラ",
            EditorStyles.boldLabel
        );

        EditorGUILayout.PropertyField(
            m_cameraFocusDurationProperty,
            new GUIContent(
                "フォーカス時間",
                "カメラがロックオン対象にフォーカスするまでの時間（秒）です。"
                )
            );

        EditorGUILayout.EndVertical();
    }
}

#endif