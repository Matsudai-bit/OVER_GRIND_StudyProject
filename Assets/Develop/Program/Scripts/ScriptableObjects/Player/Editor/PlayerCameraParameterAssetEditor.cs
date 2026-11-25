using UnityEditor;
using UnityEngine;

/// <summary>
/// PlayerCameraParameterAssetのInspectorを描画します。
/// デフォルト設定とPlay中のランタイム設定を分けて表示します。
/// </summary>
[CustomEditor(typeof(PlayerCameraParameterAsset))]
public sealed class PlayerCameraParameterAssetEditor
    : Editor
{
    // Inspector内のセクション間隔
    private const float SECTION_SPACE = 8.0f;

    // --------------------------------------------------------
    // Serialized Property
    // --------------------------------------------------------

    // X軸カメラ感度
    private SerializedProperty
        m_cameraMaxSensitivityXProperty;

    private SerializedProperty
        m_cameraMinSensitivityXProperty;

    private SerializedProperty
        m_defaultCameraSensitivityRatioXProperty;

    // Y軸カメラ感度
    private SerializedProperty
        m_cameraMaxSensitivityYProperty;

    private SerializedProperty
        m_cameraMinSensitivityYProperty;

    private SerializedProperty
        m_defaultCameraSensitivityRatioYProperty;

    // Y軸反転
    private SerializedProperty
        m_defaultCameraReverseYProperty;

    // チャージカメラ
    private SerializedProperty
        m_chargeCameraDirectionInfluenceProperty;

    private SerializedProperty
        m_chargeCameraVerticalAngleProperty;

    private SerializedProperty
        m_chargeCameraVerticalTurnSpeedProperty;

    /// <summary>
    /// Inspector描画に使用するSerializedPropertyを取得します。
    /// </summary>
    private void OnEnable()
    {
        // X軸感度
        m_cameraMaxSensitivityXProperty =
            serializedObject.FindProperty(
                "m_cameraMaxSensitivityX");

        m_cameraMinSensitivityXProperty =
            serializedObject.FindProperty(
                "m_cameraMinSensitivityX");

        m_defaultCameraSensitivityRatioXProperty =
            serializedObject.FindProperty(
                "m_defaultCameraSensitivityRatioX");

        // Y軸感度
        m_cameraMaxSensitivityYProperty =
            serializedObject.FindProperty(
                "m_cameraMaxSensitivityY");

        m_cameraMinSensitivityYProperty =
            serializedObject.FindProperty(
                "m_cameraMinSensitivityY");

        m_defaultCameraSensitivityRatioYProperty =
            serializedObject.FindProperty(
                "m_defaultCameraSensitivityRatioY");

        // Y軸反転
        m_defaultCameraReverseYProperty =
            serializedObject.FindProperty(
                "m_defaultCameraReverseY");

        // チャージカメラ
        m_chargeCameraDirectionInfluenceProperty =
            serializedObject.FindProperty(
                "m_chargeCameraDirectionInfluence");

        m_chargeCameraVerticalAngleProperty =
            serializedObject.FindProperty(
                "m_chargeCameraVerticalAngle");

        m_chargeCameraVerticalTurnSpeedProperty =
            serializedObject.FindProperty(
                "m_chargeCameraVerticalTurnSpeed");
    }

    /// <summary>
    /// カメラパラメータ用Inspectorを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDescription();

        EditorGUILayout.Space(
            SECTION_SPACE);

        DrawSensitivitySection();

        EditorGUILayout.Space(
            SECTION_SPACE);

        DrawReverseSection();

        EditorGUILayout.Space(
            SECTION_SPACE);

        DrawChargeSection();

        EditorGUILayout.Space(
            SECTION_SPACE);

        DrawRuntimeSection();

        serializedObject.ApplyModifiedProperties();

        // Play中は設定変更によってランタイム値が変化するため再描画する
        if (Application.isPlaying)
        {
            Repaint();
        }
    }

    /// <summary>
    /// ParameterAssetの責務を説明します。
    /// </summary>
    private void DrawDescription()
    {
        EditorGUILayout.HelpBox(
            "PlayerCameraが参照する唯一のカメラParameterAssetです。\n"
            + "Inspector値はデフォルト設定、"
            + "Play中のユーザー設定はランタイム値として保持されます。\n"
            + "設定変更時はPlayerCameraへ自動反映されます。",
            MessageType.Info);
    }

    /// <summary>
    /// X・Y軸の感度設定を描画します。
    /// </summary>
    private void DrawSensitivitySection()
    {
        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "カメラ感度",
            EditorStyles.boldLabel);

        DrawAxisSensitivity(
            "X軸",
            m_cameraMinSensitivityXProperty,
            m_cameraMaxSensitivityXProperty,
            m_defaultCameraSensitivityRatioXProperty);

        EditorGUILayout.Space(
            SECTION_SPACE);

        DrawAxisSensitivity(
            "Y軸",
            m_cameraMinSensitivityYProperty,
            m_cameraMaxSensitivityYProperty,
            m_defaultCameraSensitivityRatioYProperty);

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 1軸分の感度設定を描画します。
    /// </summary>
    private static void DrawAxisSensitivity(
        string axisLabel,
        SerializedProperty minProperty,
        SerializedProperty maxProperty,
        SerializedProperty defaultRatioProperty)
    {
        EditorGUILayout.LabelField(
            axisLabel,
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            minProperty,
            new GUIContent(
                "最小感度"));

        EditorGUILayout.PropertyField(
            maxProperty,
            new GUIContent(
                "最大感度"));

        EditorGUILayout.PropertyField(
            defaultRatioProperty,
            new GUIContent(
                "デフォルト感度比率",
                "0で最小感度、1で最大感度です。"));
    }

    /// <summary>
    /// Y軸反転のデフォルト設定を描画します。
    /// </summary>
    private void DrawReverseSection()
    {
        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "カメラ反転",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            m_defaultCameraReverseYProperty,
            new GUIContent(
                "Y軸反転",
                "有効にするとY軸操作を反転します。"));

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// チャージ中のカメラ設定を描画します。
    /// </summary>
    private void DrawChargeSection()
    {
        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "チャージカメラ",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            m_chargeCameraDirectionInfluenceProperty,
            new GUIContent(
                "進行方向への影響率",
                "0でチャージ開始時の向き、"
                + "1で進行方向を向きます。"));

        EditorGUILayout.PropertyField(
            m_chargeCameraVerticalAngleProperty,
            new GUIContent(
                "上下角度",
                "Playerを少し上から見る角度です。"));

        EditorGUILayout.PropertyField(
            m_chargeCameraVerticalTurnSpeedProperty,
            new GUIContent(
                "上下回転速度",
                "目的角度へ戻す速度[度/秒]。"));

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Play中に実際に使用されている値を表示します。
    /// </summary>
    private void DrawRuntimeSection()
    {
        PlayerCameraParameterAsset parameterAsset =
            target as PlayerCameraParameterAsset;

        if (parameterAsset == null)
        {
            return;
        }

        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "ランタイム値",
            EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "ランタイム値はPlay中に表示されます。",
                MessageType.Info);

            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUI.BeginDisabledGroup(
            true);

        EditorGUILayout.FloatField(
            "X軸 感度比率",
            parameterAsset.CameraSensitivityRatioX);

        EditorGUILayout.FloatField(
            "X軸 実感度",
            parameterAsset.CameraSensitivityX);

        EditorGUILayout.FloatField(
            "Y軸 感度比率",
            parameterAsset.CameraSensitivityRatioY);

        EditorGUILayout.FloatField(
            "Y軸 実感度",
            parameterAsset.CameraSensitivityY);

        EditorGUILayout.Toggle(
            "Y軸 反転",
            parameterAsset.IsCameraReverseY);

        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndVertical();
    }
}
