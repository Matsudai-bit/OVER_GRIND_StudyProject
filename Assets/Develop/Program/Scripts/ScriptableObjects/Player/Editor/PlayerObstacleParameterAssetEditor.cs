#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

/// <summary>
/// プレイヤーの衝突判定に関するパラメータをInspectorに表示・編集します。
/// </summary>
[CustomEditor(typeof(PlayerObstacleParameterAsset))]
public class PlayerObstacleParameterAssetEditor : Editor
{
    // Inspector内のセクション間隔
    private const float SECTION_SPACE = 8.0f;


    // 障害物への食い込みを防ぐための手前バッファ距離
    private SerializedProperty m_obstacleSkinWidthProperty;

    // 障害物に接触している間、1秒あたり減速する速度
    private SerializedProperty m_obstacleDecelerationPerSecondProperty;

    /// <summary>
    /// SerializedPropertyを取得します。
    /// </summary>
    private void OnEnable()
    {
        m_obstacleSkinWidthProperty =
            serializedObject.FindProperty("m_obstacleSkinWidth");

        m_obstacleDecelerationPerSecondProperty =
            serializedObject.FindProperty("m_obstacleDecelerationPerSecond");
    }

    /// <summary>
    /// Inspectorを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDescription();

        EditorGUILayout.Space(SECTION_SPACE);

        DrawObstacleSection();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// パラメータの説明を表示します。
    /// </summary>
    private void DrawDescription()
    {
        EditorGUILayout.HelpBox(
            "接地時の処理に使用されるパラメータを設定します",
            MessageType.Info);
    }

    /// <summary>
    /// 衝突判定時パラメータを表示します。
    /// </summary>
    private void DrawObstacleSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "衝突判定時パラメータ",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            m_obstacleSkinWidthProperty,
            new GUIContent(
                "障害物への食い込みを防ぐための手前バッファ距離",
                "大きいほど障害物の手前で早めに減速しますが、\n" +
                "隙間の狭い通路で引っかかりやすくなります"
                )
            );

        EditorGUILayout.PropertyField(
            m_obstacleDecelerationPerSecondProperty,
            new GUIContent(
                "障害物に接触している間、1秒あたり減速する速度",
                "値が大きいほど、障害物へ接触した際に速く止まります。"
                )
            );

        EditorGUILayout.EndVertical();
    }
}

#endif