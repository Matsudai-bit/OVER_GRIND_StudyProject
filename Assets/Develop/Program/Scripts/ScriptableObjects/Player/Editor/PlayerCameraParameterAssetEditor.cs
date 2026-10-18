using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerCameraParameterAsset))]
public class PlayerCameraParameterAssetEditor : Editor
{
    // Inspectorの配置に関するパラメータ --------------------------------

    // Inspector内のセクション間隔
    private const float SECTION_SPACE = 8.0f;

    // 設定するパラメータ -----------------------------------------------

    // X軸カメラ感度。
    private SerializedProperty m_cameraSensitivityXProperty;

    // Y軸カメラ感度。
    private SerializedProperty m_cameraSensitivityYProperty;

    // Y軸カメラの操作を反転させるかどうか。
    private SerializedProperty m_isCameraReverseYProperty;


    // チャージ中に進行方向へ向く割合。0でチャージ開始時の向き、1で進行方向を向きます。
    private SerializedProperty m_chargeCameraDirectionInfluenceProperty;

    // チャージ中に維持するカメラの上下角度。Playerを少し上から見る角度です。
    private SerializedProperty m_chargeCameraVerticalAngleProperty;

    // チャージ中にカメラの高さを目的角度へ戻す速度[度/秒]。
    private SerializedProperty m_chargeCameraVerticalTurnSpeedProperty;

    /// <summary>
    /// SerializedPropertyを取得します。
    /// </summary>
    private void OnEnable()
    {
        m_cameraSensitivityXProperty =
            serializedObject.FindProperty("m_cameraSensitivityX");
        m_cameraSensitivityYProperty =
            serializedObject.FindProperty("m_cameraSensitivityY");
        m_isCameraReverseYProperty =
            serializedObject.FindProperty("m_isCameraReverseY");

        m_chargeCameraDirectionInfluenceProperty =
            serializedObject.FindProperty("m_chargeCameraDirectionInfluence");
        m_chargeCameraVerticalAngleProperty =
            serializedObject.FindProperty("m_chargeCameraVerticalAngle");
        m_chargeCameraVerticalTurnSpeedProperty =
            serializedObject.FindProperty("m_chargeCameraVerticalTurnSpeed");
    }

    /// <summary>
    /// Inspectorを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDescription();

        EditorGUILayout.Space(SECTION_SPACE);

        DrawCameraParameter();

        EditorGUILayout.Space(SECTION_SPACE);

        DrawChargeSection();

        serializedObject.ApplyModifiedProperties();
    }

    // ------------------------------------------------------------------

    /// <summary>
    /// パラメータの説明を表示します。
    /// </summary>
    private void DrawDescription()
    {
        EditorGUILayout.HelpBox(
            "カメラ処理に使用されるパラメータを設定します",
            MessageType.Info);
    }

    /// <summary>
    /// カメラの基礎パラメータを表示します。
    /// </summary>
    private void DrawCameraParameter()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "カメラパラメータ",
            EditorStyles.boldLabel
        );

        EditorGUILayout.PropertyField(
            m_cameraSensitivityXProperty,
            new GUIContent(
                "X軸カメラ感度。"
                )
            );

        EditorGUILayout.PropertyField(
            m_cameraSensitivityYProperty,
            new GUIContent(
                "Y軸カメラ感度。"
                )
            );

        EditorGUILayout.PropertyField(
            m_isCameraReverseYProperty,
            new GUIContent(
                "Y軸カメラの操作を反転させるかどうか。",
                " TRUE の場合は反転させる、 FALSE の場合は反転させません。"
                )
            );

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// チャージパラメータを表示します。
    /// </summary>
    private void DrawChargeSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "チャージパラメータ",
            EditorStyles.boldLabel
        );

        EditorGUILayout.PropertyField(
            m_chargeCameraDirectionInfluenceProperty,
            new GUIContent(
                "チャージ中に進行方向へ向く割合。",
                "0でチャージ開始時の向き、1で進行方向を向きます。"
                )
            );

        EditorGUILayout.PropertyField(
            m_chargeCameraVerticalAngleProperty,
            new GUIContent(
                "チャージ中に維持するカメラの上下角度。",
                "Playerを少し上から見る角度です。"
                )
            );

        EditorGUILayout.PropertyField(
            m_chargeCameraVerticalTurnSpeedProperty,
            new GUIContent(
                "チャージ中にカメラの高さを目的角度へ戻す速度[度/秒]。"
                )
            );

        EditorGUILayout.EndVertical();
    }
}
