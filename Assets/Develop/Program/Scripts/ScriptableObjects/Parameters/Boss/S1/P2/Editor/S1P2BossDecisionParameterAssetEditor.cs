using UnityEditor;
using UnityEngine;

/// <summary>
/// S1P2BossDecisionParameterAssetのInspector表示を拡張します。
/// </summary>
[CustomEditor(typeof(S1P2BossDecisionParameterAsset))]
public sealed class S1P2BossDecisionParameterAssetEditor : Editor
{
    private SerializedProperty m_napeJetProperty;
    private SerializedProperty m_missileProperty;
    private SerializedProperty m_heatExhaustProperty;
    private SerializedProperty m_chargeProperty;
    private SerializedProperty m_tailSlamProperty;
    private SerializedProperty m_moveProperty;

    private bool m_showNapeJet = true;
    private bool m_showMissile = true;
    private bool m_showHeatExhaust = true;
    private bool m_showCharge = true;
    private bool m_showTailSlam = true;
    private bool m_showMove = true;

    /// <summary>
    /// SerializedPropertyを取得します。
    /// </summary>
    private void OnEnable()
    {
        m_napeJetProperty =
            serializedObject.FindProperty("m_napeJet");

        m_missileProperty =
            serializedObject.FindProperty("m_missile");

        m_heatExhaustProperty =
            serializedObject.FindProperty("m_heatExhaust");

        m_chargeProperty =
            serializedObject.FindProperty("m_charge");

        m_tailSlamProperty =
            serializedObject.FindProperty("m_tailSlam");

        m_moveProperty =
            serializedObject.FindProperty("m_move");
    }

    /// <summary>
    /// Inspectorを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDescription();

        EditorGUILayout.Space(8.0f);
        DrawNapeJetParameters();

        EditorGUILayout.Space(4.0f);
        DrawMissileParameters();

        EditorGUILayout.Space(4.0f);
        DrawHeatExhaustParameters();

        EditorGUILayout.Space(4.0f);
        DrawChargeParameters();

        EditorGUILayout.Space(4.0f);
        DrawTailSlamParameters();

        EditorGUILayout.Space(4.0f);
        DrawMoveParameters();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 行動選択システムの説明を表示します。
    /// </summary>
    private static void DrawDescription()
    {
        EditorGUILayout.HelpBox(
            "S1P2 ボスの行動選択パラメータです。\n\n" +
            "うなじ噴射は最優先行動ですが、現在は意思決定パラメータのみ定義しています。\n" +
            "通常行動は上から順番に判定し、選択された時点で後続の判定は行いません。\n\n" +
            "1. うなじ噴射（後からイベント連携）\n" +
            "2. ミサイル攻撃\n" +
            "3. 排熱攻撃\n" +
            "4. 突進\n" +
            "5. 尻尾叩きつけ\n" +
            "6. 移動\n" +
            "7. 停止",
            MessageType.Info);
    }

    /// <summary>
    /// うなじ噴射のパラメータを描画します。
    /// </summary>
    private void DrawNapeJetParameters()
    {
        m_showNapeJet = EditorGUILayout.BeginFoldoutHeaderGroup(
            m_showNapeJet,
            "うなじ噴射");

        if (m_showNapeJet)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.HelpBox(
                "現在はパラメータのみです。結合部へのAttackHitイベント連携後にConditionへ接続してください。",
                MessageType.Warning);

            SerializedProperty triggerDelay =
                m_napeJetProperty.FindPropertyRelative(
                    "m_triggerDelay");

            SerializedProperty probability =
                m_napeJetProperty.FindPropertyRelative(
                    "m_probability");

            DrawTime(
                "発動待機時間",
                triggerDelay);

            DrawProbability(
                "条件成立時の確率",
                probability);

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
                "Playerが背中にいる場合を最優先し、それ以外はBossとの水平距離で確率を切り替えます。",
                MessageType.None);

            SerializedProperty distance =
                m_missileProperty.FindPropertyRelative(
                    "m_distance");

            SerializedProperty farProbability =
                m_missileProperty.FindPropertyRelative(
                    "m_farProbability");

            SerializedProperty onBackProbability =
                m_missileProperty.FindPropertyRelative(
                    "m_onBackProbability");

            SerializedProperty defaultProbability =
                m_missileProperty.FindPropertyRelative(
                    "m_defaultProbability");

            SerializedProperty coolTime =
                m_missileProperty.FindPropertyRelative(
                    "m_coolTime");

            DrawDistance("遠距離判定", distance);
            DrawProbability("遠距離時の確率", farProbability);
            DrawProbability("背中にいる時の確率", onBackProbability);
            DrawProbability("通常時の確率", defaultProbability);

            DrawSeparator();
            DrawTime("クールタイム", coolTime);

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    /// <summary>
    /// 排熱攻撃のパラメータを描画します。
    /// </summary>
    private void DrawHeatExhaustParameters()
    {
        m_showHeatExhaust = EditorGUILayout.BeginFoldoutHeaderGroup(
            m_showHeatExhaust,
            "排熱攻撃");

        if (m_showHeatExhaust)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.HelpBox(
                "Playerが背中にいる場合を最優先し、それ以外はBossとの水平距離で確率を切り替えます。",
                MessageType.None);

            SerializedProperty distance =
                m_heatExhaustProperty.FindPropertyRelative(
                    "m_distance");

            SerializedProperty onBackProbability =
                m_heatExhaustProperty.FindPropertyRelative(
                    "m_onBackProbability");

            SerializedProperty nearProbability =
                m_heatExhaustProperty.FindPropertyRelative(
                    "m_nearProbability");

            SerializedProperty defaultProbability =
                m_heatExhaustProperty.FindPropertyRelative(
                    "m_defaultProbability");

            SerializedProperty coolTime =
                m_heatExhaustProperty.FindPropertyRelative(
                    "m_coolTime");

            DrawDistance("近距離判定", distance);
            DrawProbability("背中にいる時の確率", onBackProbability);
            DrawProbability("近距離時の確率", nearProbability);
            DrawProbability("通常時の確率", defaultProbability);

            DrawSeparator();
            DrawTime("クールタイム", coolTime);

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    /// <summary>
    /// 突進攻撃のパラメータを描画します。
    /// </summary>
    private void DrawChargeParameters()
    {
        m_showCharge = EditorGUILayout.BeginFoldoutHeaderGroup(
            m_showCharge,
            "突進");

        if (m_showCharge)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.HelpBox(
                "仕様上の「方向転換 + 突進」枠です。方向転換Stateは使用せず、突進Stateを直接選択します。",
                MessageType.None);

            SerializedProperty probability =
                m_chargeProperty.FindPropertyRelative(
                    "m_probability");

            SerializedProperty coolTime =
                m_chargeProperty.FindPropertyRelative(
                    "m_coolTime");

            DrawProbability("選択確率", probability);

            DrawSeparator();
            DrawTime("クールタイム", coolTime);

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    /// <summary>
    /// 尻尾叩きつけ攻撃のパラメータを描画します。
    /// </summary>
    private void DrawTailSlamParameters()
    {
        m_showTailSlam = EditorGUILayout.BeginFoldoutHeaderGroup(
            m_showTailSlam,
            "尻尾叩きつけ");

        if (m_showTailSlam)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.HelpBox(
                "S1P2BossReferencesの尻尾地点とPlayerの水平距離で尻尾周辺を判定します。",
                MessageType.None);

            SerializedProperty distance =
                m_tailSlamProperty.FindPropertyRelative(
                    "m_distance");

            SerializedProperty nearProbability =
                m_tailSlamProperty.FindPropertyRelative(
                    "m_nearProbability");

            SerializedProperty defaultProbability =
                m_tailSlamProperty.FindPropertyRelative(
                    "m_defaultProbability");

            SerializedProperty coolTime =
                m_tailSlamProperty.FindPropertyRelative(
                    "m_coolTime");

            DrawDistance("尻尾周辺判定", distance);

            if (distance.floatValue <= 0.0f)
            {
                EditorGUILayout.HelpBox(
                    "仕様書には尻尾周辺の距離が指定されていません。使用前に判定距離を設定してください。",
                    MessageType.Warning);
            }

            DrawProbability("尻尾周辺時の確率", nearProbability);
            DrawProbability("通常時の確率", defaultProbability);

            DrawSeparator();
            DrawTime("クールタイム", coolTime);

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    /// <summary>
    /// 移動のパラメータを描画します。
    /// </summary>
    private void DrawMoveParameters()
    {
        m_showMove = EditorGUILayout.BeginFoldoutHeaderGroup(
            m_showMove,
            "移動");

        if (m_showMove)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            SerializedProperty probability =
                m_moveProperty.FindPropertyRelative(
                    "m_probability");

            DrawProbability("選択確率", probability);

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    /// <summary>
    /// 距離パラメータを描画します。
    /// </summary>
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
