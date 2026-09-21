using UnityEditor;
using UnityEngine;

/// <summary>
/// MissileParameterAssetを調整しやすく表示します。
/// </summary>
[CustomEditor(typeof(MissileParameterAsset))]
public sealed class MissileParameterAssetEditor : Editor
{
    // 移動パラメータ
    private SerializedProperty m_motorParametersProperty;

    // 誘導パラメータ
    private SerializedProperty m_steeringParametersProperty;

    // 移動パラメータの表示状態
    private bool m_showMotorParameters = true;

    // 誘導パラメータの表示状態
    private bool m_showSteeringParameters = true;

    /// <summary>
    /// SerializedPropertyを取得します。
    /// </summary>
    private void OnEnable()
    {
        m_motorParametersProperty =
            serializedObject.FindProperty(
                "m_motorParameters");

        m_steeringParametersProperty =
            serializedObject.FindProperty(
                "m_steeringParameters");
    }

    /// <summary>
    /// Inspectorを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawMotorParameters();
        EditorGUILayout.Space(6.0f);
        DrawSteeringParameters();
        EditorGUILayout.Space(6.0f);
        DrawValidationMessages();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 移動パラメータを描画します。
    /// </summary>
    private void DrawMotorParameters()
    {
        m_showMotorParameters =
            EditorGUILayout.BeginFoldoutHeaderGroup(
                m_showMotorParameters,
                "移動パラメータ");

        if (m_showMotorParameters)
        {
            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox);

            DrawProperty(
                m_motorParametersProperty,
                "m_initialSpeed",
                "初速",
                "発射した瞬間の速度です。");

            DrawProperty(
                m_motorParametersProperty,
                "m_maxSpeed",
                "最大速度",
                "加速後に到達する最大速度です。");

            DrawProperty(
                m_motorParametersProperty,
                "m_acceleration",
                "加速度",
                "初速から最大速度へ近づく加速度です。");

            EditorGUILayout.Space(4.0f);

            DrawProperty(
                m_motorParametersProperty,
                "m_gravityAcceleration",
                "重力加速度",
                "ミサイルへ下方向に加える加速度です。");

            DrawProperty(
                m_motorParametersProperty,
                "m_rotationSpeed",
                "回転速度",
                "進行方向へ向く回転速度です。");

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    /// <summary>
    /// 誘導パラメータを描画します。
    /// </summary>
    private void DrawSteeringParameters()
    {
        m_showSteeringParameters =
            EditorGUILayout.BeginFoldoutHeaderGroup(
                m_showSteeringParameters,
                "誘導パラメータ");

        if (m_showSteeringParameters)
        {
            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox);

            DrawProperty(
                m_steeringParametersProperty,
                "m_minSteeringAcceleration",
                "最小操舵加速度",
                "操舵開始時に使用する最小の加速度です。");

            DrawProperty(
                m_steeringParametersProperty,
                "m_maxSteeringAcceleration",
                "最大操舵加速度",
                "同じ方向へ追従し続けたときの最大加速度です。");

            DrawProperty(
                m_steeringParametersProperty,
                "m_steeringAccelerationRate",
                "操舵加速度の増加速度",
                "同方向への操舵を継続したときに操舵力を増やす速度です。");

            DrawProperty(
                m_steeringParametersProperty,
                "m_steeringDecelerationRate",
                "操舵加速度の減少速度",
                "操舵方向が変わったときに操舵力を下げる速度です。");

            EditorGUILayout.Space(4.0f);

            DrawProperty(
                m_steeringParametersProperty,
                "m_sameDirectionThreshold",
                "同方向判定閾値",
                "前回の操舵方向との内積がこの値以上なら同方向と判定します。");

            DrawProperty(
                m_steeringParametersProperty,
                "m_verticalSteeringRatio",
                "上下操舵割合",
                "上下方向へ適用する操舵加速度の割合です。0で上下追従なし、1で制限なしです。");

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    /// <summary>
    /// パラメータの矛盾をInspectorへ表示します。
    /// </summary>
    private void DrawValidationMessages()
    {
        SerializedProperty initialSpeed =
            m_motorParametersProperty.FindPropertyRelative(
                "m_initialSpeed");

        SerializedProperty maxSpeed =
            m_motorParametersProperty.FindPropertyRelative(
                "m_maxSpeed");

        SerializedProperty acceleration =
            m_motorParametersProperty.FindPropertyRelative(
                "m_acceleration");

        SerializedProperty minSteeringAcceleration =
            m_steeringParametersProperty.FindPropertyRelative(
                "m_minSteeringAcceleration");

        SerializedProperty maxSteeringAcceleration =
            m_steeringParametersProperty.FindPropertyRelative(
                "m_maxSteeringAcceleration");

        if (initialSpeed.floatValue >
            maxSpeed.floatValue)
        {
            EditorGUILayout.HelpBox(
                "初速が最大速度を上回っています。加速処理では速度が最大速度側へ減速します。",
                MessageType.Warning);
        }

        if (initialSpeed.floatValue <
                maxSpeed.floatValue &&
            Mathf.Approximately(
                acceleration.floatValue,
                0.0f))
        {
            EditorGUILayout.HelpBox(
                "加速度が0のため、初速から最大速度へ変化しません。",
                MessageType.Info);
        }

        if (minSteeringAcceleration.floatValue >
            maxSteeringAcceleration.floatValue)
        {
            EditorGUILayout.HelpBox(
                "最小操舵加速度が最大操舵加速度を上回っています。",
                MessageType.Warning);
        }
    }

    /// <summary>
    /// 子プロパティをラベルとツールチップ付きで描画します。
    /// </summary>
    /// <param name="parentProperty">親プロパティ。</param>
    /// <param name="propertyName">子プロパティ名。</param>
    /// <param name="label">表示名。</param>
    /// <param name="tooltip">説明。</param>
    private void DrawProperty(
        SerializedProperty parentProperty,
        string propertyName,
        string label,
        string tooltip)
    {
        if (parentProperty == null)
        {
            return;
        }

        SerializedProperty property =
            parentProperty.FindPropertyRelative(
                propertyName);

        if (property == null)
        {
            return;
        }

        EditorGUILayout.PropertyField(
            property,
            new GUIContent(
                label,
                tooltip));
    }
}
