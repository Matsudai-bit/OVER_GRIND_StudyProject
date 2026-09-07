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

    // 落下中の重力倍率
    private SerializedProperty m_fallGravityMultiplierProperty;

    // ジャンプ早期解除時の重力倍率
    private SerializedProperty m_lowJumpMultiplierProperty;

    // 落下速度の上限
    private SerializedProperty m_maxFallSpeedProperty;

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

        m_fallGravityMultiplierProperty =
        serializedObject.FindProperty("m_fallGravityMultiplier");

        m_lowJumpMultiplierProperty =
            serializedObject.FindProperty("m_lowJumpMultiplier");

        m_maxFallSpeedProperty =
            serializedObject.FindProperty("m_maxFallSpeed");
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

        EditorGUILayout.Space(SECTION_SPACE);

        DrawJumpPreview();

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

        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            "落下調整",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            m_fallGravityMultiplierProperty,
            new GUIContent(
                "落下重力倍率",
                "落下中に適用する重力倍率です。大きいほど早く落下します。"));

        EditorGUILayout.PropertyField(
            m_lowJumpMultiplierProperty,
            new GUIContent(
                "早期解除重力倍率",
                "上昇中にジャンプ入力を離した際に適用する重力倍率です。"));

        EditorGUILayout.PropertyField(
            m_maxFallSpeedProperty,
            new GUIContent(
                "最大落下速度",
                "落下速度の上限(絶対値)です。"));

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
    /// ジャンプの計算結果を表示します。
    /// </summary>
    private void DrawJumpPreview()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "ジャンプ調整確認",
            EditorStyles.boldLabel);

        float jumpPower =
            m_jumpPowerProperty.floatValue;

        float gravity =
            Mathf.Abs(Physics.gravity.y);

        float fallGravityMultiplier =
            m_fallGravityMultiplierProperty.floatValue;

        // 上昇中(通常重力)の頂点到達時間・高さ
        float timeToApex =
            gravity > 0.0f
                ? jumpPower / gravity
                : 0.0f;

        float maxHeight =
            (jumpPower * jumpPower) /
            Mathf.Max(2.0f * gravity, 0.0001f);

        // 落下時の実効重力を考慮した落下時間の目安
        float effectiveFallGravity =
            gravity * Mathf.Max(fallGravityMultiplier, 0.0001f);

        float timeToFall =
            effectiveFallGravity > 0.0f
                ? Mathf.Sqrt(
                    2.0f * maxHeight / effectiveFallGravity)
                : 0.0f;

        DrawReadOnlyFloat(
            "最大到達高さ",
            maxHeight,
            " m");

        DrawReadOnlyFloat(
            "頂点到達時間",
            timeToApex,
            " s");

        DrawReadOnlyFloat(
            "落下時間(目安)",
            timeToFall,
            " s");

        EditorGUILayout.HelpBox(
            "最大到達高さ・頂点到達時間は、" +
            "ジャンプ力(初速)と重力のみから計算した理論値です。" +
            "入力反映時間や早期解除重力倍率の影響は含みません。",
            MessageType.Info);

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