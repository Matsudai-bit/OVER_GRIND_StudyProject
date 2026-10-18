#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

/// <summary>
/// プレイヤーの攻撃に関するパラメータをInspectorに表示・編集します。
/// </summary>
[CustomEditor(typeof(PlayerAttackParameterAsset))]
public class PlayerAttackParameterAssetEditor : Editor
{
    // Inspector内のセクション間隔
    private const float SECTION_SPACE = 8.0f;


    // アニメーションイベントの受信元
    private SerializedProperty m_animationEventReceiverProperty;

    // 攻撃時に有効化するヒットボックス
    private SerializedProperty m_attackHitboxesProperty;

    // 多段ヒット攻撃の基本ヒットレート（1秒あたりのヒット回数）
    private SerializedProperty m_baseHitsPerSecondProperty;

    /// <summary>
    /// SerializedPropertyを取得します。
    /// </summary>
    private void OnEnable()
    {
        m_animationEventReceiverProperty =
            serializedObject.FindProperty("m_animationEventReceiver");

        m_attackHitboxesProperty =
            serializedObject.FindProperty("m_attackHitboxes");

        m_baseHitsPerSecondProperty =
            serializedObject.FindProperty("m_baseHitsPerSecond");
    }

    /// <summary>
    /// Inspectorを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDescription();

        EditorGUILayout.Space(SECTION_SPACE);

        DrawAttackSection();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// パラメータの説明を表示します。
    /// </summary>
    private void DrawDescription()
    {
        EditorGUILayout.HelpBox(
            "攻撃の処理に使用されるパラメータを設定します",
            MessageType.Info);
    }

    /// <summary>
    /// 攻撃パラメータを表示します。
    /// </summary>
    private void DrawAttackSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "攻撃パラメータ",
            EditorStyles.boldLabel
        );

        EditorGUILayout.PropertyField(
            m_baseHitsPerSecondProperty,
            new GUIContent(
                "アニメーションイベントの受信元",
                "アニメーションイベントの受信元です。"
                )
            );

        EditorGUILayout.PropertyField(
            m_baseHitsPerSecondProperty,
            new GUIContent(
                "多段ヒット設定",
                "多段ヒット攻撃の基本ヒットレート（1秒あたりのヒット回数）です。"
                )
            );

        EditorGUILayout.EndVertical();


        EditorGUILayout.PropertyField(
            m_attackHitboxesProperty,
            new GUIContent(
                "攻撃用ヒットボックス",
                "攻撃用に使用するヒットボックスです。"
                )
            );
    }
}

#endif