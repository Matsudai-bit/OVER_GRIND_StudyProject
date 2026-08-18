using UnityEditor;
using UnityEngine;

/// <summary>
/// 通常移動パラメータのInspector表示を管理します。
/// </summary>
[CustomEditor(typeof(PlayerMovementParameterAsset))]
public sealed class PlayerMovementParameterAssetEditor : Editor
{
    // Inspector上の余白
    private const float SECTION_SPACE = 8.0f;

    // 通常移動の最高速度
    private SerializedProperty m_maxMoveSpeedProperty;

    // 最高速度までの到達時間
    private SerializedProperty m_timeToMaxSpeedProperty;

    // 停止までの時間
    private SerializedProperty m_timeToStopProperty;

    // 回転速度
    private SerializedProperty m_rotationSpeedProperty;

    // ジャンプ力
    private SerializedProperty m_jumpPowerProperty;

    // ジャンプ入力の最大反映時間
    private SerializedProperty m_jumpInputDurationProperty;

    /// <summary>
    /// SerializedPropertyを取得します。
    /// </summary>
    private void OnEnable()
    {
        m_maxMoveSpeedProperty =
            serializedObject.FindProperty("m_maxMoveSpeed");

        m_timeToMaxSpeedProperty =
            serializedObject.FindProperty("m_timeToMaxSpeed");

        m_timeToStopProperty =
            serializedObject.FindProperty("m_timeToStop");

        m_rotationSpeedProperty =
            serializedObject.FindProperty("m_rotationSpeed");

        m_jumpPowerProperty =
            serializedObject.FindProperty("m_jumpPower");

        m_jumpInputDurationProperty =
            serializedObject.FindProperty("m_jumpInputDuration");
    }

    /// <summary>
    /// Inspectorを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDescription();

        EditorGUILayout.Space(SECTION_SPACE);

        DrawMovementSection();

        EditorGUILayout.Space(SECTION_SPACE);

        DrawJumpSection();

        EditorGUILayout.Space(SECTION_SPACE);

        DrawMovementPreview();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// パラメータの説明を表示します。
    /// </summary>
    private void DrawDescription()
    {
        EditorGUILayout.HelpBox(
            "通常移動時の速度・加減速・回転と、" +
            "通常ジャンプのパラメータを設定します。",
            MessageType.Info);
    }

    /// <summary>
    /// 通常移動パラメータを表示します。
    /// </summary>
    private void DrawMovementSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "通常移動",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            m_maxMoveSpeedProperty,
            new GUIContent(
                "最高速度",
                "通常移動時の最高速度です。"));

        EditorGUILayout.PropertyField(
            m_timeToMaxSpeedProperty,
            new GUIContent(
                "最高速度到達時間",
                "停止状態から最高速度へ到達するまでの時間です。"));

        EditorGUILayout.PropertyField(
            m_timeToStopProperty,
            new GUIContent(
                "停止時間",
                "現在速度から停止するまでの時間です。"));

        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            "方向転換",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            m_rotationSpeedProperty,
            new GUIContent(
                "回転速度",
                "1秒間に回転できる最大角度です。"));

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// ジャンプパラメータを表示します。
    /// </summary>
    private void DrawJumpSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "ジャンプ",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            m_jumpPowerProperty,
            new GUIContent(
                "ジャンプ力",
                "ジャンプ中に上方向へ適用する移動力です。"));

        EditorGUILayout.PropertyField(
            m_jumpInputDurationProperty,
            new GUIContent(
                "入力反映時間",
                "ジャンプ入力を上方向の移動へ反映する最大時間です。"));

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 通常移動の計算結果を表示します。
    /// </summary>
    private void DrawMovementPreview()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "調整確認",
            EditorStyles.boldLabel);

        float maxMoveSpeed =
            m_maxMoveSpeedProperty.floatValue;

        float timeToMaxSpeed =
            m_timeToMaxSpeedProperty.floatValue;

        float timeToStop =
            m_timeToStopProperty.floatValue;

        float acceleration =
            CalculateAcceleration(
                maxMoveSpeed,
                timeToMaxSpeed);

        float deceleration =
            CalculateAcceleration(
                maxMoveSpeed,
                timeToStop);

        DrawReadOnlyFloat(
            "加速度",
            acceleration,
            " units/s?");

        DrawReadOnlyFloat(
            "減速度",
            deceleration,
            " units/s?");

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 読み取り専用の数値を表示します。
    /// </summary>
    /// <param name="label">表示名。</param>
    /// <param name="value">表示する値。</param>
    /// <param name="suffix">単位。</param>
    private void DrawReadOnlyFloat(
        string label,
        float value,
        string suffix)
    {
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.TextField(
                label,
                $"{value:F2}{suffix}");
        }
    }

    /// <summary>
    /// 目標速度と時間から加速度を計算します。
    /// </summary>
    /// <param name="targetSpeed">目標速度。</param>
    /// <param name="requiredTime">到達時間。</param>
    /// <returns>加速度。</returns>
    private float CalculateAcceleration(
        float targetSpeed,
        float requiredTime)
    {
        if (requiredTime <= 0.0f)
        {
            return 0.0f;
        }

        return targetSpeed / requiredTime;
    }
}