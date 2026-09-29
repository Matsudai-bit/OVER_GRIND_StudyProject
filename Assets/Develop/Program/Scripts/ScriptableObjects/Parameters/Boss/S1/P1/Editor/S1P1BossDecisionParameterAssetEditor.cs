using UnityEditor;
using UnityEngine;

/// <summary>
/// S1P1BossDecisionParameterAssetのInspector表示を拡張します。
/// </summary>
[CustomEditor(typeof(S1P1BossDecisionParameterAsset))]
public sealed class S1P1BossDecisionParameterAssetEditor : Editor
{
    private SerializedProperty m_stompProperty;
    private SerializedProperty m_missileProperty;
    private SerializedProperty m_heatExhaustProperty;
    private SerializedProperty m_turnProperty;
    private SerializedProperty m_walkProperty;

    private bool m_showStomp = true;
    private bool m_showMissile = true;
    private bool m_showHeatExhaust = true;
    private bool m_showTurn = true;
    private bool m_showWalk = true;

    /// <summary>
    /// SerializedPropertyを取得します。
    /// </summary>
    private void OnEnable()
    {
        m_stompProperty =
            serializedObject.FindProperty("m_stomp");

        m_missileProperty =
            serializedObject.FindProperty("m_missile");

        m_heatExhaustProperty =
            serializedObject.FindProperty("m_heatExhaust");

        m_turnProperty =
            serializedObject.FindProperty("m_turn");

        m_walkProperty =
            serializedObject.FindProperty("m_walk");
    }

    /// <summary>
    /// Inspectorを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDescription();

        EditorGUILayout.Space(8.0f);

        DrawStompParameters();

        EditorGUILayout.Space(4.0f);

        DrawMissileParameters();

        EditorGUILayout.Space(4.0f);

        DrawHeatExhaustParameters();

        EditorGUILayout.Space(4.0f);

        DrawTurnParameters();

        EditorGUILayout.Space(4.0f);

        DrawWalkParameters();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 行動選択システムの説明を表示します。
    /// </summary>
    private static void DrawDescription()
    {
        EditorGUILayout.HelpBox(
            "S1P1 ボスの行動選択パラメータです。\n\n" +
            "行動は以下の順番で判定され、選択された時点で後続の判定は行われません。\n" +
            "各行動は正常終了後、設定したクールタイムが経過するまで再選択されません。\n\n" +
            "1. 踏みつけ\n" +
            "2. ミサイル攻撃\n" +
            "3. 排熱攻撃\n" +
            "4. 方向転換 + 歩行\n" +
            "5. 歩行\n" +
            "6. 停止",
            MessageType.Info);
    }

    /// <summary>
    /// 踏みつけ攻撃のパラメータを描画します。
    /// </summary>
    private void DrawStompParameters()
    {
        m_showStomp = EditorGUILayout.BeginFoldoutHeaderGroup(
            m_showStomp,
            "踏みつけ攻撃");

        if (m_showStomp)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.HelpBox(
                "Playerが指定距離以内に一定時間滞在している場合、" +
                "踏みつけ攻撃の選択確率が上昇します。",
                MessageType.None);

            SerializedProperty distance =
                m_stompProperty.FindPropertyRelative("m_distance");

            SerializedProperty requiredStayDuration =
                m_stompProperty.FindPropertyRelative(
                    "m_requiredStayDuration");

            SerializedProperty nearProbability =
                m_stompProperty.FindPropertyRelative(
                    "m_nearProbability");

            SerializedProperty defaultProbability =
                m_stompProperty.FindPropertyRelative(
                    "m_defaultProbability");

            SerializedProperty coolTime =
                m_stompProperty.FindPropertyRelative(
                    "m_coolTime");

            DrawDistance(
                "足元判定距離",
                distance);

            DrawTime(
                "必要滞在時間",
                requiredStayDuration);

            DrawProbability(
                "条件成立時の確率",
                nearProbability);

            DrawProbability(
                "通常時の確率",
                defaultProbability);

            DrawSeparator();

            DrawTime(
                "クールタイム",
                coolTime);

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    /// <summary>
    /// ミサイル攻撃のパラメータを描画します。
    /// </summary>
    private void DrawMissileParameters()
    {
        m_showMissile = EditorGUILayout.BeginFoldoutHeaderGroup(
            m_showMissile,
            "ミサイル攻撃");

        if (m_showMissile)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.HelpBox(
                "Playerが指定距離以上離れている場合に、" +
                "ミサイル攻撃を選択します。",
                MessageType.None);

            SerializedProperty distance =
                m_missileProperty.FindPropertyRelative(
                    "m_distance");

            SerializedProperty farProbability =
                m_missileProperty.FindPropertyRelative(
                    "m_farProbability");

            SerializedProperty defaultProbability =
                m_missileProperty.FindPropertyRelative(
                    "m_defaultProbability");

            SerializedProperty coolTime =
                m_missileProperty.FindPropertyRelative(
                    "m_coolTime");

            DrawDistance(
                "遠距離判定",
                distance);

            DrawProbability(
                "遠距離時の確率",
                farProbability);

            DrawProbability(
                "通常時の確率",
                defaultProbability);

            DrawSeparator();

            DrawTime(
                "クールタイム",
                coolTime);

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    /// <summary>
    /// 排熱攻撃のパラメータを描画します。
    /// </summary>
    private void DrawHeatExhaustParameters()
    {
        m_showHeatExhaust =
            EditorGUILayout.BeginFoldoutHeaderGroup(
                m_showHeatExhaust,
                "排熱攻撃");

        if (m_showHeatExhaust)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.HelpBox(
                "Playerが指定距離以内にいる場合に、" +
                "排熱攻撃を選択します。",
                MessageType.None);

            SerializedProperty distance =
                m_heatExhaustProperty.FindPropertyRelative(
                    "m_distance");

            SerializedProperty nearProbability =
                m_heatExhaustProperty.FindPropertyRelative(
                    "m_nearProbability");

            SerializedProperty defaultProbability =
                m_heatExhaustProperty.FindPropertyRelative(
                    "m_defaultProbability");

            SerializedProperty coolTime =
                m_heatExhaustProperty.FindPropertyRelative(
                    "m_coolTime");

            DrawDistance(
                "近距離判定",
                distance);

            DrawProbability(
                "近距離時の確率",
                nearProbability);

            DrawProbability(
                "通常時の確率",
                defaultProbability);

            DrawSeparator();

            DrawTime(
                "クールタイム",
                coolTime);

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    /// <summary>
    /// 方向転換のパラメータを描画します。
    /// </summary>
    private void DrawTurnParameters()
    {
        m_showTurn = EditorGUILayout.BeginFoldoutHeaderGroup(
            m_showTurn,
            "方向転換 + 歩行");

        if (m_showTurn)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.HelpBox(
                "Playerが遠距離にいる場合は、" +
                "方向転換を行ってから歩行へ移行します。",
                MessageType.None);

            SerializedProperty distance =
                m_turnProperty.FindPropertyRelative(
                    "m_distance");

            SerializedProperty farProbability =
                m_turnProperty.FindPropertyRelative(
                    "m_farProbability");

            SerializedProperty defaultProbability =
                m_turnProperty.FindPropertyRelative(
                    "m_defaultProbability");

            SerializedProperty coolTime =
                m_turnProperty.FindPropertyRelative(
                    "m_coolTime");

            DrawDistance(
                "遠距離判定",
                distance);

            DrawProbability(
                "遠距離時の確率",
                farProbability);

            DrawProbability(
                "通常時の確率",
                defaultProbability);

            DrawSeparator();

            DrawTime(
                "クールタイム",
                coolTime);

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    /// <summary>
    /// 歩行のパラメータを描画します。
    /// </summary>
    private void DrawWalkParameters()
    {
        m_showWalk = EditorGUILayout.BeginFoldoutHeaderGroup(
            m_showWalk,
            "歩行");

        if (m_showWalk)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.HelpBox(
                "それ以前の行動が選択されなかった場合に、" +
                "この確率で歩行を選択します。",
                MessageType.None);

            SerializedProperty probability =
                m_walkProperty.FindPropertyRelative(
                    "m_probability");

            SerializedProperty coolTime =
                m_walkProperty.FindPropertyRelative(
                    "m_coolTime");

            DrawProbability(
                "選択確率",
                probability);

            DrawSeparator();

            DrawTime(
                "クールタイム",
                coolTime);

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    /// <summary>
    /// 距離パラメータを描画します。
    /// </summary>
    /// <param name="label">表示名。</param>
    /// <param name="property">距離プロパティ。</param>
    private static void DrawDistance(
        string label,
        SerializedProperty property)
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.PrefixLabel(label);

        property.floatValue =
            Mathf.Max(
                0.0f,
                EditorGUILayout.FloatField(
                    property.floatValue));

        GUILayout.Label(
            "m",
            GUILayout.Width(20.0f));

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 時間パラメータを描画します。
    /// </summary>
    /// <param name="label">表示名。</param>
    /// <param name="property">時間プロパティ。</param>
    private static void DrawTime(
        string label,
        SerializedProperty property)
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.PrefixLabel(label);

        property.floatValue =
            Mathf.Max(
                0.0f,
                EditorGUILayout.FloatField(
                    property.floatValue));

        GUILayout.Label(
            "秒",
            GUILayout.Width(20.0f));

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 確率パラメータを百分率で描画します。
    /// </summary>
    /// <param name="label">表示名。</param>
    /// <param name="property">確率プロパティ。</param>
    private static void DrawProbability(
        string label,
        SerializedProperty property)
    {
        float percentage =
            property.floatValue * 100.0f;

        percentage = EditorGUILayout.Slider(
            label,
            percentage,
            0.0f,
            100.0f);

        property.floatValue =
            percentage / 100.0f;
    }

    /// <summary>
    /// パラメータグループ内に区切り線を描画します。
    /// </summary>
    private static void DrawSeparator()
    {
        EditorGUILayout.Space(4.0f);

        Rect rect =
            EditorGUILayout.GetControlRect(
                false,
                1.0f);

        EditorGUI.DrawRect(
            rect,
            new Color(
                0.4f,
                0.4f,
                0.4f,
                0.5f));

        EditorGUILayout.Space(4.0f);
    }
}
