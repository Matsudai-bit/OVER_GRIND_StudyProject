#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

/// <summary>
/// PlayerMovementParameterAssetを調整しやすく表示します。
/// </summary>
[CustomEditor(typeof(PlayerMovementParameterAsset))]
public sealed class PlayerMovementParameterAssetEditor
    : Editor
{
    private const float SECTION_SPACE = 8.0f;

    // 通常移動
    private SerializedProperty m_maxMoveSpeedProperty;
    private SerializedProperty m_timeToMaxSpeedProperty;
    private SerializedProperty m_timeToStopProperty;
    private SerializedProperty m_reverseBrakeMultiplierProperty;
    private SerializedProperty m_rotationSpeedProperty;

    // ジャンプ
    private SerializedProperty m_jumpPowerProperty;
    private SerializedProperty m_jumpInputDurationProperty;

    // 空中移動
    private SerializedProperty m_airMaxMoveSpeedProperty;
    private SerializedProperty m_airTimeToMaxSpeedProperty;
    private SerializedProperty m_airReverseBrakeMultiplierProperty;
    private SerializedProperty m_airReverseStopTimeProperty;
    private SerializedProperty m_airRotationSpeedProperty;
    private SerializedProperty m_airResistanceProperty;
    private SerializedProperty m_fallGravityMultiplierProperty;
    private SerializedProperty m_lowJumpMultiplierProperty;
    private SerializedProperty m_maxFallSpeedProperty;

    // 攻撃
    private SerializedProperty m_attackDecelerationMultiplierProperty;
    private SerializedProperty m_attackHitMovementSpeedMultiplierProperty;

    /// <summary>
    /// SerializedPropertyを取得します。
    /// </summary>
    private void OnEnable()
    {
        m_maxMoveSpeedProperty =
            serializedObject.FindProperty(
                "m_maxMoveSpeed");

        m_timeToMaxSpeedProperty =
            serializedObject.FindProperty(
                "m_timeToMaxSpeed");

        m_timeToStopProperty =
            serializedObject.FindProperty(
                "m_timeToStop");

        m_reverseBrakeMultiplierProperty =
            serializedObject.FindProperty(
                "m_reverseBrakeMultiplier");

        m_rotationSpeedProperty =
            serializedObject.FindProperty(
                "m_rotationSpeed");

        m_jumpPowerProperty =
            serializedObject.FindProperty(
                "m_jumpPower");

        m_jumpInputDurationProperty =
            serializedObject.FindProperty(
                "m_jumpInputDuration");

        m_airMaxMoveSpeedProperty =
            serializedObject.FindProperty(
                "m_airMaxMoveSpeed");

        m_airTimeToMaxSpeedProperty =
            serializedObject.FindProperty(
                "m_airTimeToMaxSpeed");

        m_airReverseBrakeMultiplierProperty =
            serializedObject.FindProperty(
                "m_airReverseBrakeMultiplier");

        m_airReverseStopTimeProperty =
            serializedObject.FindProperty(
                "m_airReverseStopTime");

        m_airRotationSpeedProperty =
            serializedObject.FindProperty(
                "m_airRotationSpeed");

        m_airResistanceProperty =
            serializedObject.FindProperty(
                "m_airResistance");

        m_fallGravityMultiplierProperty =
            serializedObject.FindProperty(
                "m_fallGravityMultiplier");

        m_lowJumpMultiplierProperty =
            serializedObject.FindProperty(
                "m_lowJumpMultiplier");

        m_maxFallSpeedProperty =
            serializedObject.FindProperty(
                "m_maxFallSpeed");

        m_attackDecelerationMultiplierProperty =
            serializedObject.FindProperty(
                "m_attackDecelerationMultiplier");

        m_attackHitMovementSpeedMultiplierProperty =
            serializedObject.FindProperty(
                "m_attackHitMovementSpeedMultiplier");
    }

    /// <summary>
    /// Inspectorを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "地上移動・ジャンプ・空中水平操作・攻撃中移動を設定します。\n" +
            "空中操作と空気抵抗は独立して調整できます。",
            MessageType.Info);

        EditorGUILayout.Space(
            SECTION_SPACE);

        DrawGroundMovement();

        EditorGUILayout.Space(
            SECTION_SPACE);

        DrawJump();

        EditorGUILayout.Space(
            SECTION_SPACE);

        DrawAirMovement();

        EditorGUILayout.Space(
            SECTION_SPACE);

        DrawAttack();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 地上移動パラメータを描画します。
    /// </summary>
    private void DrawGroundMovement()
    {
        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "通常移動",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            m_maxMoveSpeedProperty,
            new GUIContent(
                "最高速度"));

        EditorGUILayout.PropertyField(
            m_timeToMaxSpeedProperty,
            new GUIContent(
                "最高速度到達時間"));

        EditorGUILayout.PropertyField(
            m_timeToStopProperty,
            new GUIContent(
                "停止時間"));

        EditorGUILayout.PropertyField(
            m_reverseBrakeMultiplierProperty,
            new GUIContent(
                "逆入力ブレーキ倍率"));

        EditorGUILayout.PropertyField(
            m_rotationSpeedProperty,
            new GUIContent(
                "回転速度"));

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// ジャンプパラメータを描画します。
    /// </summary>
    private void DrawJump()
    {
        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "ジャンプ",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            m_jumpPowerProperty,
            new GUIContent(
                "ジャンプ力"));

        EditorGUILayout.PropertyField(
            m_jumpInputDurationProperty,
            new GUIContent(
                "ジャンプ入力反映時間"));

        EditorGUILayout.PropertyField(
            m_fallGravityMultiplierProperty,
            new GUIContent(
                "落下重力倍率"));

        EditorGUILayout.PropertyField(
            m_lowJumpMultiplierProperty,
            new GUIContent(
                "早期解除重力倍率"));

        EditorGUILayout.PropertyField(
            m_maxFallSpeedProperty,
            new GUIContent(
                "最大落下速度"));

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 空中水平移動パラメータを描画します。
    /// </summary>
    private void DrawAirMovement()
    {
        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "空中水平移動",
            EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "ジャンプ上昇中と下降・滑空中の両方で使用します。\n" +
            "空気抵抗は入力中にも適用されます。",
            MessageType.None);

        EditorGUILayout.PropertyField(
            m_airMaxMoveSpeedProperty,
            new GUIContent(
                "空中最高速度"));

        EditorGUILayout.PropertyField(
            m_airTimeToMaxSpeedProperty,
            new GUIContent(
                "最高速度到達時間"));

        EditorGUILayout.PropertyField(
            m_airReverseStopTimeProperty,
            new GUIContent(
                "逆入力停止時間"));

        EditorGUILayout.PropertyField(
            m_airReverseBrakeMultiplierProperty,
            new GUIContent(
                "逆入力ブレーキ倍率"));

        EditorGUILayout.PropertyField(
            m_airRotationSpeedProperty,
            new GUIContent(
                "空中回転速度"));

        EditorGUILayout.PropertyField(
            m_airResistanceProperty,
            new GUIContent(
                "空気抵抗"));

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 攻撃中移動パラメータを描画します。
    /// </summary>
    private void DrawAttack()
    {
        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "攻撃",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            m_attackDecelerationMultiplierProperty,
            new GUIContent(
                "攻撃中減速倍率"));

        EditorGUILayout.PropertyField(
            m_attackHitMovementSpeedMultiplierProperty,
            new GUIContent(
                "ヒット中移動速度倍率"));

        EditorGUILayout.EndVertical();
    }
}

#endif
