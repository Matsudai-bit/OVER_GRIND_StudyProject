using UnityEditor;
using UnityEngine;

/// <summary>
/// ボス突進攻撃パラメータのInspector表示を管理します。
/// </summary>
[CustomEditor(typeof(S1BossChargeAttackParameterAsset))]
public sealed class BossChargeAttackParameterAssetEditor :
    Editor
{
    private const float SECTION_SPACE =
        8.0f;

    private SerializedProperty
        m_preparationDurationProperty;

    private SerializedProperty
        m_rotationSpeedProperty;

    private SerializedProperty
        m_chargeSpeedProperty;

    private SerializedProperty
        m_maxChargeDistanceProperty;

    private SerializedProperty
        m_targetOverrunDistanceProperty;

    private SerializedProperty
        m_stopMarginProperty;

    private SerializedProperty
        m_accelerationDurationProperty;

    private SerializedProperty
        m_accelerationCurveProperty;

    private SerializedProperty
        m_decelerationDistanceProperty;

    private SerializedProperty
        m_decelerationCurveProperty;

    private SerializedProperty
        m_endDurationProperty;

    private SerializedProperty
        m_animationActiveBoolNameProperty;

    private SerializedProperty
        m_animationPhaseIntNameProperty;

    private SerializedProperty
        m_attackIdentifierProperty;

    private bool m_showSystemParameters;

    /// <summary>
    /// SerializedPropertyを取得します。
    /// </summary>
    private void OnEnable()
    {
        m_preparationDurationProperty =
            serializedObject.FindProperty(
                "m_preparationDuration");

        m_rotationSpeedProperty =
            serializedObject.FindProperty(
                "m_rotationSpeed");

        m_chargeSpeedProperty =
            serializedObject.FindProperty(
                "m_chargeSpeed");

        m_maxChargeDistanceProperty =
            serializedObject.FindProperty(
                "m_maxChargeDistance");

        m_targetOverrunDistanceProperty =
            serializedObject.FindProperty(
                "m_targetOverrunDistance");

        m_stopMarginProperty =
            serializedObject.FindProperty(
                "m_stopMargin");

        m_accelerationDurationProperty =
            serializedObject.FindProperty(
                "m_accelerationDuration");

        m_accelerationCurveProperty =
            serializedObject.FindProperty(
                "m_accelerationCurve");

        m_decelerationDistanceProperty =
            serializedObject.FindProperty(
                "m_decelerationDistance");

        m_decelerationCurveProperty =
            serializedObject.FindProperty(
                "m_decelerationCurve");

        m_endDurationProperty =
            serializedObject.FindProperty(
                "m_endDuration");

        m_animationActiveBoolNameProperty =
            serializedObject.FindProperty(
                "m_animationActiveBoolName");

        m_animationPhaseIntNameProperty =
            serializedObject.FindProperty(
                "m_animationPhaseIntName");

        m_attackIdentifierProperty =
            serializedObject.FindProperty(
                "m_attackIdentifier");
    }

    /// <summary>
    /// Inspectorを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDescription();

        EditorGUILayout.Space(
            SECTION_SPACE);

        DrawPreparationSection();

        DrawChargeSection();

        DrawAccelerationSection();

        DrawDecelerationSection();

        DrawEndSection();

        EditorGUILayout.Space(
            SECTION_SPACE);

        DrawSystemParameters();

        DrawWarnings();

        serializedObject
            .ApplyModifiedProperties();
    }

    /// <summary>
    /// 突進攻撃の概要を表示します。
    /// </summary>
    private void DrawDescription()
    {
        EditorGUILayout.HelpBox(
            "予備動作 → 加速 → 定速 → 減速 → 終了動作\n" +
            "Playerの指定距離先を停止地点とし、" +
            "NavMesh境界が先にある場合は境界手前で停止します。",
            MessageType.Info);
    }

    /// <summary>
    /// 予備動作設定を表示します。
    /// </summary>
    private void DrawPreparationSection()
    {
        EditorGUILayout.LabelField(
            "1. 予備動作",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            m_preparationDurationProperty,
            new GUIContent(
                "予備動作時間"));

        EditorGUILayout.PropertyField(
            m_rotationSpeedProperty,
            new GUIContent(
                "回転速度"));

        EditorGUILayout.Space(
            SECTION_SPACE);
    }

    /// <summary>
    /// 突進基本設定を表示します。
    /// </summary>
    private void DrawChargeSection()
    {
        EditorGUILayout.LabelField(
            "2. 突進",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            m_chargeSpeedProperty,
            new GUIContent(
                "最高速度"));

        EditorGUILayout.PropertyField(
            m_maxChargeDistanceProperty,
            new GUIContent(
                "最大突進距離"));

        EditorGUILayout.PropertyField(
            m_targetOverrunDistanceProperty,
            new GUIContent(
                "対象通過距離",
                "Player位置からさらに何m先で停止するか"));

        EditorGUILayout.PropertyField(
            m_stopMarginProperty,
            new GUIContent(
                "NavMesh停止余白"));

        EditorGUILayout.Space(
            SECTION_SPACE);
    }

    /// <summary>
    /// 加速設定を表示します。
    /// </summary>
    private void DrawAccelerationSection()
    {
        EditorGUILayout.LabelField(
            "3. 加速",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            m_accelerationDurationProperty,
            new GUIContent(
                "加速時間"));

        EditorGUILayout.PropertyField(
            m_accelerationCurveProperty,
            new GUIContent(
                "加速イージング"));

        EditorGUILayout.Space(
            SECTION_SPACE);
    }

    /// <summary>
    /// 減速設定を表示します。
    /// </summary>
    private void DrawDecelerationSection()
    {
        EditorGUILayout.LabelField(
            "4. 減速",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            m_decelerationDistanceProperty,
            new GUIContent(
                "減速開始距離",
                "停止地点の何m手前から減速するか"));

        EditorGUILayout.PropertyField(
            m_decelerationCurveProperty,
            new GUIContent(
                "減速イージング"));

        EditorGUILayout.Space(
            SECTION_SPACE);
    }

    /// <summary>
    /// 終了設定を表示します。
    /// </summary>
    private void DrawEndSection()
    {
        EditorGUILayout.LabelField(
            "5. 終了動作",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            m_endDurationProperty,
            new GUIContent(
                "終了硬直時間"));

        EditorGUILayout.Space(
            SECTION_SPACE);
    }

    /// <summary>
    /// システム設定を表示します。
    /// </summary>
    private void DrawSystemParameters()
    {
        m_showSystemParameters =
            EditorGUILayout.Foldout(
                m_showSystemParameters,
                "システム設定（プログラマ向け）",
                true);

        if (!m_showSystemParameters)
        {
            return;
        }

        EditorGUILayout.PropertyField(
            m_animationActiveBoolNameProperty,
            new GUIContent(
                "Active Bool"));

        EditorGUILayout.PropertyField(
            m_animationPhaseIntNameProperty,
            new GUIContent(
                "Phase Int"));

        EditorGUILayout.PropertyField(
            m_attackIdentifierProperty,
            new GUIContent(
                "Attack Identifier"));
    }

    /// <summary>
    /// 設定値の警告を表示します。
    /// </summary>
    private void DrawWarnings()
    {
        if (m_decelerationDistanceProperty
                .floatValue >
            m_maxChargeDistanceProperty
                .floatValue)
        {
            EditorGUILayout.HelpBox(
                "減速開始距離が最大突進距離より大きいため、" +
                "突進開始直後から減速する場合があります。",
                MessageType.Warning);
        }

        if (m_attackIdentifierProperty
                .objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox(
                "Attack Identifierが設定されていません。",
                MessageType.Warning);
        }
    }
}