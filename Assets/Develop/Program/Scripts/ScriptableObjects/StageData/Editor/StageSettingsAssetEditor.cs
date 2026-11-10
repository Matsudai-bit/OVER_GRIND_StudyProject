using UnityEditor;
using UnityEngine;

/// <summary>
/// StageSettingsAssetのInspector表示を拡張します。
/// </summary>
[CustomEditor(typeof(StageSettingsAsset))]
public sealed class StageSettingsAssetEditor :
    Editor
{
    // ステージ名
    private SerializedProperty m_stageName;

    // 制限時間
    private SerializedProperty m_timeLimitSeconds;

    // S+評価時間
    private SerializedProperty m_sPlusTimeSeconds;

    // S評価時間
    private SerializedProperty m_sTimeSeconds;

    // A評価時間
    private SerializedProperty m_aTimeSeconds;

    // B評価時間
    private SerializedProperty m_bTimeSeconds;

    private void OnEnable()
    {
        m_stageName =
            serializedObject.FindProperty(
                "m_stageName");

        m_timeLimitSeconds =
            serializedObject.FindProperty(
                "m_timeLimitSeconds");

        m_sPlusTimeSeconds =
            serializedObject.FindProperty(
                "m_sPlusTimeSeconds");

        m_sTimeSeconds =
            serializedObject.FindProperty(
                "m_sTimeSeconds");

        m_aTimeSeconds =
            serializedObject.FindProperty(
                "m_aTimeSeconds");

        m_bTimeSeconds =
            serializedObject.FindProperty(
                "m_bTimeSeconds");
    }

    /// <summary>
    /// Inspectorを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField(
            "ステージ情報",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            m_stageName,
            new GUIContent("ステージ名"));

        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            "制限時間",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            m_timeLimitSeconds,
            new GUIContent("制限時間（秒）"));

        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            "クリア評価基準",
            EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "クリア時間が各値『未満』の場合にそのランクになります。\n"
            + "B基準以上でも制限時間内にクリアすればC評価です。",
            MessageType.Info);

        DrawRankField(
            "S+",
            m_sPlusTimeSeconds);

        DrawRankField(
            "S",
            m_sTimeSeconds);

        DrawRankField(
            "A",
            m_aTimeSeconds);

        DrawRankField(
            "B",
            m_bTimeSeconds);

        EditorGUILayout.Space();

        DrawPreview();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// ランク時間設定を表示します。
    /// </summary>
    /// <param name="rankName">表示するランク名。</param>
    /// <param name="property">時間Property。</param>
    private void DrawRankField(
        string rankName,
        SerializedProperty property)
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.PrefixLabel(
            $"{rankName} 評価");

        property.floatValue =
            Mathf.Max(
                0.0f,
                EditorGUILayout.FloatField(
                    property.floatValue));

        EditorGUILayout.LabelField(
            "秒未満",
            GUILayout.Width(50.0f));

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 現在の評価範囲を表示します。
    /// </summary>
    private void DrawPreview()
    {
        EditorGUILayout.LabelField(
            "評価範囲",
            EditorStyles.boldLabel);

        float sPlus =
            m_sPlusTimeSeconds.floatValue;

        float s =
            m_sTimeSeconds.floatValue;

        float a =
            m_aTimeSeconds.floatValue;

        float b =
            m_bTimeSeconds.floatValue;

        float limit =
            m_timeLimitSeconds.floatValue;

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.TextField(
                "S+",
                $"0 ～ {FormatSeconds(sPlus)} 未満");

            EditorGUILayout.TextField(
                "S",
                $"{FormatSeconds(sPlus)} ～ {FormatSeconds(s)} 未満");

            EditorGUILayout.TextField(
                "A",
                $"{FormatSeconds(s)} ～ {FormatSeconds(a)} 未満");

            EditorGUILayout.TextField(
                "B",
                $"{FormatSeconds(a)} ～ {FormatSeconds(b)} 未満");

            EditorGUILayout.TextField(
                "C",
                $"{FormatSeconds(b)} ～ {FormatSeconds(limit)}");
        }
    }

    /// <summary>
    /// 秒数を分:秒形式へ変換します。
    /// </summary>
    /// <param name="seconds">変換する秒数。</param>
    /// <returns>表示文字列。</returns>
    private static string FormatSeconds(
        float seconds)
    {
        int minutes =
            Mathf.FloorToInt(
                seconds / 60.0f);

        float remainingSeconds =
            seconds
            - minutes * 60.0f;

        return
            $"{minutes:00}:{remainingSeconds:00.00}";
    }
}
