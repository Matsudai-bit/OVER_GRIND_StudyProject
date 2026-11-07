using UnityEditor;
using UnityEngine;

/// <summary>
/// PlayerCameraParameterAssetのInspector表示を拡張します。
/// </summary>
[CustomEditor(typeof(PlayerCameraParameterAsset))]
public sealed class PlayerCameraParameterAssetEditor : Editor
{
    // Inspector内のセクション間隔
    private const float SECTION_SPACE = 8.0f;

    // X軸カメラ最大感度
    private SerializedProperty m_cameraMaxSensitivityXProperty;

    // X軸カメラ最小感度
    private SerializedProperty m_cameraMinSensitivityXProperty;

    // X軸カメラデフォルト感度比率
    private SerializedProperty
        m_defaultCameraSensitivityRatioXProperty;

    // Y軸カメラ最大感度
    private SerializedProperty m_cameraMaxSensitivityYProperty;

    // Y軸カメラ最小感度
    private SerializedProperty m_cameraMinSensitivityYProperty;

    // Y軸カメラデフォルト感度比率
    private SerializedProperty
        m_defaultCameraSensitivityRatioYProperty;

    // Y軸カメラ反転デフォルト設定
    private SerializedProperty
        m_defaultCameraReverseYProperty;

    // チャージ中の方向影響率
    private SerializedProperty
        m_chargeCameraDirectionInfluenceProperty;

    // チャージ中の上下角度
    private SerializedProperty
        m_chargeCameraVerticalAngleProperty;

    // チャージ中の上下回転速度
    private SerializedProperty
        m_chargeCameraVerticalTurnSpeedProperty;

    /// <summary>
    /// SerializedPropertyを取得します。
    /// </summary>
    private void OnEnable()
    {
        m_cameraMaxSensitivityXProperty =
            serializedObject.FindProperty(
                "m_cameraMaxSensitivityX");

        m_cameraMinSensitivityXProperty =
            serializedObject.FindProperty(
                "m_cameraMinSensitivityX");

        m_defaultCameraSensitivityRatioXProperty =
            serializedObject.FindProperty(
                "m_defaultCameraSensitivityRatioX");

        m_cameraMaxSensitivityYProperty =
            serializedObject.FindProperty(
                "m_cameraMaxSensitivityY");

        m_cameraMinSensitivityYProperty =
            serializedObject.FindProperty(
                "m_cameraMinSensitivityY");

        m_defaultCameraSensitivityRatioYProperty =
            serializedObject.FindProperty(
                "m_defaultCameraSensitivityRatioY");

        m_defaultCameraReverseYProperty =
            serializedObject.FindProperty(
                "m_defaultCameraReverseY");

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
    /// Inspectorを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDescription();

        EditorGUILayout.Space(
            SECTION_SPACE);

        DrawCameraParameterSection();

        EditorGUILayout.Space(
            SECTION_SPACE);

        DrawRuntimeCameraParameterSection();

        EditorGUILayout.Space(
            SECTION_SPACE);

        DrawChargeSection();

        serializedObject.ApplyModifiedProperties();

        // Play中はランタイム値が変化するためInspectorを再描画
        if (Application.isPlaying)
        {
            Repaint();
        }
    }

    /// <summary>
    /// パラメータの説明を表示します。
    /// </summary>
    private void DrawDescription()
    {
        EditorGUILayout.HelpBox(
            "カメラ処理に使用されるパラメータを設定します。\n"
            + "実際の感度は最小値～最大値を"
            + "ユーザー設定の0～1で補間して決定します。",
            MessageType.Info);
    }

    /// <summary>
    /// カメラ基本設定を表示します。
    /// </summary>
    private void DrawCameraParameterSection()
    {
        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "X軸カメラ感度",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            m_cameraMinSensitivityXProperty,
            new GUIContent(
                "最小感度"));

        EditorGUILayout.PropertyField(
            m_cameraMaxSensitivityXProperty,
            new GUIContent(
                "最大感度"));

        EditorGUILayout.PropertyField(
            m_defaultCameraSensitivityRatioXProperty,
            new GUIContent(
                "デフォルト感度比率",
                "0で最小感度、1で最大感度です。"));

        EditorGUILayout.Space(
            SECTION_SPACE);

        EditorGUILayout.LabelField(
            "Y軸カメラ感度",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            m_cameraMinSensitivityYProperty,
            new GUIContent(
                "最小感度"));

        EditorGUILayout.PropertyField(
            m_cameraMaxSensitivityYProperty,
            new GUIContent(
                "最大感度"));

        EditorGUILayout.PropertyField(
            m_defaultCameraSensitivityRatioYProperty,
            new GUIContent(
                "デフォルト感度比率",
                "0で最小感度、1で最大感度です。"));

        EditorGUILayout.Space(
            SECTION_SPACE);

        EditorGUILayout.LabelField(
            "カメラ反転",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            m_defaultCameraReverseYProperty,
            new GUIContent(
                "Y軸反転",
                "TRUEの場合はY軸操作を反転します。"));

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// ランタイム中のカメラ設定値を表示します。
    /// </summary>
    private void DrawRuntimeCameraParameterSection()
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
            "ランタイムカメラパラメータ",
            EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "ランタイム値はPlay中に表示されます。",
                MessageType.Info);

            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUI.BeginDisabledGroup(true);

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

    /// <summary>
    /// チャージパラメータを表示します。
    /// </summary>
    private void DrawChargeSection()
    {
        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "チャージパラメータ",
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
}