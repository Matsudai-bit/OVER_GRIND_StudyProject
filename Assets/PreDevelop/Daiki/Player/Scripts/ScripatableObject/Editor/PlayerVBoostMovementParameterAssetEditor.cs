using UnityEditor;
using UnityEngine;

/// <summary>
/// Vブースト移動パラメータのInspector表示を管理します。
/// </summary>
[CustomEditor(typeof(PlayerVBoostMovementParameterAsset))]
public sealed class PlayerVBoostMovementParameterAssetEditor : Editor
{
    // Inspector上の余白
    private const float SECTION_SPACE = 8.0f;

    // 初速ブーストの最高速度
    private SerializedProperty m_initialBoostMaxMoveSpeedProperty;

    // 初速ブーストの加速時間
    private SerializedProperty m_initialBoostTimeToMaxSpeedProperty;

    // 初速ブーストの停止時間
    private SerializedProperty m_initialBoostTimeToStopProperty;

    // 初速ブーストの回転速度
    private SerializedProperty m_initialBoostRotationSpeedProperty;

    // 初速ブーストの継続時間
    private SerializedProperty m_initialBoostDurationProperty;

    // 安定ブーストの最高速度
    private SerializedProperty m_stableBoostMaxMoveSpeedProperty;

    // 安定ブーストの加速時間
    private SerializedProperty m_stableBoostTimeToMaxSpeedProperty;

    // 安定ブーストの停止時間
    private SerializedProperty m_stableBoostTimeToStopProperty;

    // 安定ブーストの回転速度
    private SerializedProperty m_stableBoostRotationSpeedProperty;

    // 安定ブーストの継続時間
    private SerializedProperty m_stableBoostDurationProperty;

    /// <summary>
    /// SerializedPropertyを取得します。
    /// </summary>
    private void OnEnable()
    {
        m_initialBoostMaxMoveSpeedProperty =
            serializedObject.FindProperty(
                "m_initialBoostMaxMoveSpeed");

        m_initialBoostTimeToMaxSpeedProperty =
            serializedObject.FindProperty(
                "m_initialBoostTimeToMaxSpeed");

        m_initialBoostTimeToStopProperty =
            serializedObject.FindProperty(
                "m_initialBoostTimeToStop");

        m_initialBoostRotationSpeedProperty =
            serializedObject.FindProperty(
                "m_initialBoostRotationSpeed");

        m_initialBoostDurationProperty =
            serializedObject.FindProperty(
                "m_initialBoostDuration");

        m_stableBoostMaxMoveSpeedProperty =
            serializedObject.FindProperty(
                "m_stableBoostMaxMoveSpeed");

        m_stableBoostTimeToMaxSpeedProperty =
            serializedObject.FindProperty(
                "m_stableBoostTimeToMaxSpeed");

        m_stableBoostTimeToStopProperty =
            serializedObject.FindProperty(
                "m_stableBoostTimeToStop");

        m_stableBoostRotationSpeedProperty =
            serializedObject.FindProperty(
                "m_stableBoostRotationSpeed");

        m_stableBoostDurationProperty =
            serializedObject.FindProperty(
                "m_stableBoostDuration");
    }

    /// <summary>
    /// Inspectorを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDescription();

        EditorGUILayout.Space(SECTION_SPACE);

        DrawInitialBoostSection();

        DrawTransitionLabel();

        DrawStableBoostSection();

        EditorGUILayout.Space(SECTION_SPACE);

        DrawBoostPreview();

        DrawParameterWarnings();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// Vブーストの説明を表示します。
    /// </summary>
    private void DrawDescription()
    {
        EditorGUILayout.HelpBox(
            "Vブーストは「初速ブースト」から開始し、" +
            "一定時間後に「安定ブースト」へ移行します。",
            MessageType.Info);
    }

    /// <summary>
    /// 初速ブーストパラメータを表示します。
    /// </summary>
    private void DrawInitialBoostSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "1. 初速ブースト",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            m_initialBoostMaxMoveSpeedProperty,
            new GUIContent(
                "最高速度",
                "Vブースト開始直後に目標とする最高速度です。"));

        EditorGUILayout.PropertyField(
            m_initialBoostTimeToMaxSpeedProperty,
            new GUIContent(
                "最高速度到達時間",
                "現在速度から初速ブーストの最高速度へ到達するまでの時間です。"));

        EditorGUILayout.PropertyField(
            m_initialBoostDurationProperty,
            new GUIContent(
                "継続時間",
                "初速ブーストを継続する時間です。"));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(
            m_initialBoostTimeToStopProperty,
            new GUIContent(
                "停止時間",
                "初速ブースト中に停止するときの減速時間です。"));

        EditorGUILayout.PropertyField(
            m_initialBoostRotationSpeedProperty,
            new GUIContent(
                "回転速度",
                "初速ブースト中の1秒間の最大回転角度です。"));

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// フェーズ間の遷移を表示します。
    /// </summary>
    private void DrawTransitionLabel()
    {
        GUIStyle centeredLabel =
            new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };

        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            "↓",
            centeredLabel);

        EditorGUILayout.LabelField(
            "時間経過で安定ブーストへ",
            centeredLabel);

        EditorGUILayout.Space();
    }

    /// <summary>
    /// 安定ブーストパラメータを表示します。
    /// </summary>
    private void DrawStableBoostSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "2. 安定ブースト",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            m_stableBoostMaxMoveSpeedProperty,
            new GUIContent(
                "最高速度",
                "初速ブースト後に維持する最高速度です。"));

        EditorGUILayout.PropertyField(
            m_stableBoostTimeToMaxSpeedProperty,
            new GUIContent(
                "目標速度到達時間",
                "現在速度から安定ブースト速度へ近づくまでの基準時間です。"));

        EditorGUILayout.PropertyField(
            m_stableBoostDurationProperty,
            new GUIContent(
                "継続時間",
                "安定ブーストを継続する時間です。"));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(
            m_stableBoostTimeToStopProperty,
            new GUIContent(
                "停止時間",
                "安定ブースト中に停止するときの減速時間です。"));

        EditorGUILayout.PropertyField(
            m_stableBoostRotationSpeedProperty,
            new GUIContent(
                "回転速度",
                "安定ブースト中の1秒間の最大回転角度です。"));

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Vブーストの計算結果を表示します。
    /// </summary>
    private void DrawBoostPreview()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "ブースト確認",
            EditorStyles.boldLabel);

        float initialSpeed =
            m_initialBoostMaxMoveSpeedProperty.floatValue;

        float initialAccelerationTime =
            m_initialBoostTimeToMaxSpeedProperty.floatValue;

        float initialDuration =
            m_initialBoostDurationProperty.floatValue;

        float stableSpeed =
            m_stableBoostMaxMoveSpeedProperty.floatValue;

        float stableAccelerationTime =
            m_stableBoostTimeToMaxSpeedProperty.floatValue;

        float stableDuration =
            m_stableBoostDurationProperty.floatValue;

        float totalDuration =
            initialDuration + stableDuration;

        float initialAcceleration =
            CalculateAcceleration(
                initialSpeed,
                initialAccelerationTime);

        float stableAcceleration =
            CalculateAcceleration(
                stableSpeed,
                stableAccelerationTime);

        DrawReadOnlyFloat(
            "初速ブースト加速度",
            initialAcceleration,
            " units/s?");

        DrawReadOnlyFloat(
            "安定ブースト基準加速度",
            stableAcceleration,
            " units/s?");

        DrawReadOnlyFloat(
            "ブースト総時間",
            totalDuration,
            " 秒");

        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            "速度遷移",
            $"{initialSpeed:F2} → {stableSpeed:F2}");

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 設定値に対する警告を表示します。
    /// </summary>
    private void DrawParameterWarnings()
    {
        float initialSpeed =
            m_initialBoostMaxMoveSpeedProperty.floatValue;

        float stableSpeed =
            m_stableBoostMaxMoveSpeedProperty.floatValue;

        float initialAccelerationTime =
            m_initialBoostTimeToMaxSpeedProperty.floatValue;

        float initialDuration =
            m_initialBoostDurationProperty.floatValue;

        if (initialSpeed <= stableSpeed)
        {
            EditorGUILayout.HelpBox(
                "初速ブーストの最高速度が安定ブースト以下です。" +
                "「開始時に一気に速度が跳ね上がる」挙動を作る場合は、" +
                "初速ブーストの最高速度を高くすることを推奨します。",
                MessageType.Warning);
        }

        if (initialAccelerationTime > initialDuration)
        {
            EditorGUILayout.HelpBox(
                "初速ブーストの最高速度到達時間が継続時間より長いため、" +
                "最高速度へ到達する前に安定ブーストへ移行する可能性があります。",
                MessageType.Warning);
        }
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