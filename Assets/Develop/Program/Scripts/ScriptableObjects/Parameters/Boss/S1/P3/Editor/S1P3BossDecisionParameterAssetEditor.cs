using UnityEditor;
using UnityEngine;

/// <summary>
/// S1P3BossDecisionParameterAssetのInspector表示を拡張します。
/// </summary>
[CustomEditor(typeof(S1P3BossDecisionParameterAsset))]
public sealed class S1P3BossDecisionParameterAssetEditor : Editor
{
    private SerializedProperty m_biteProperty;
    private SerializedProperty m_chargeProperty;
    private SerializedProperty m_dreadAttackProperty;
    private SerializedProperty m_energyCannonProperty;

    private bool m_showBite = true;
    private bool m_showCharge = true;
    private bool m_showDreadAttack = true;
    private bool m_showEnergyCannon = true;

    /// <summary>
    /// SerializedPropertyを取得します。
    /// </summary>
    private void OnEnable()
    {
        m_biteProperty =
            serializedObject.FindProperty("m_bite");

        m_chargeProperty =
            serializedObject.FindProperty("m_charge");

        m_dreadAttackProperty =
            serializedObject.FindProperty("m_dreadAttack");

        m_energyCannonProperty =
            serializedObject.FindProperty("m_energyCannon");
    }

    /// <summary>
    /// Inspectorを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDescription();

        EditorGUILayout.Space(8.0f);
        DrawBiteParameters();

        EditorGUILayout.Space(4.0f);
        DrawChargeParameters();

        EditorGUILayout.Space(4.0f);
        DrawDreadAttackParameters();

        EditorGUILayout.Space(4.0f);
        DrawEnergyCannonParameters();

        EditorGUILayout.Space(8.0f);
        DrawValidationMessages();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 行動選択システムの説明を表示します。
    /// </summary>
    private static void DrawDescription()
    {
        EditorGUILayout.HelpBox(
            "S1P3 ボスの行動選択パラメータです。\n\n" +
            "仕様書上の行動選択順序に合わせ、上から順番に判定する想定です。\n" +
            "選択された時点で後続の判定は行いません。\n\n" +
            "1. 噛み潰し\n" +
            "2. 突進\n" +
            "3. ドレッド攻撃\n" +
            "4. エネルギー砲\n\n" +
            "ダウンはエネルギー砲終了後の直接遷移として扱うため、" +
            "確率による意思決定パラメータには含めません。",
            MessageType.Info);
    }

    /// <summary>
    /// 噛み潰し攻撃のパラメータを描画します。
    /// </summary>
    private void DrawBiteParameters()
    {
        m_showBite = EditorGUILayout.BeginFoldoutHeaderGroup(
            m_showBite,
            "噛み潰し");

        if (m_showBite)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.HelpBox(
                "Playerが口内にいる場合は100%、それ以外は0%で選択する仕様です。" +
                "噛み潰し終了後は突進へ遷移する想定です。",
                MessageType.None);

            SerializedProperty inMouthProbability =
                m_biteProperty.FindPropertyRelative(
                    "m_inMouthProbability");

            SerializedProperty defaultProbability =
                m_biteProperty.FindPropertyRelative(
                    "m_defaultProbability");

            SerializedProperty coolTime =
                m_biteProperty.FindPropertyRelative(
                    "m_coolTime");

            DrawProbability(
                "口内にいる時の確率",
                inMouthProbability);

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
    /// 連続突進攻撃のパラメータを描画します。
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
                "前回の突進後、別の行動を指定回数選択した場合に100%で選択する仕様です。",
                MessageType.None);

            SerializedProperty requiredOtherActionCount =
                m_chargeProperty.FindPropertyRelative(
                    "m_requiredOtherActionCount");

            SerializedProperty readyProbability =
                m_chargeProperty.FindPropertyRelative(
                    "m_readyProbability");

            SerializedProperty defaultProbability =
                m_chargeProperty.FindPropertyRelative(
                    "m_defaultProbability");

            SerializedProperty coolTime =
                m_chargeProperty.FindPropertyRelative(
                    "m_coolTime");

            requiredOtherActionCount.intValue =
                Mathf.Max(
                    0,
                    EditorGUILayout.IntField(
                        "必要な他行動選択回数",
                        requiredOtherActionCount.intValue));

            DrawProbability(
                "条件成立時の確率",
                readyProbability);

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
    /// ドレッド攻撃のパラメータを描画します。
    /// </summary>
    private void DrawDreadAttackParameters()
    {
        m_showDreadAttack =
            EditorGUILayout.BeginFoldoutHeaderGroup(
                m_showDreadAttack,
                "ドレッド攻撃");

        if (m_showDreadAttack)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.HelpBox(
                "PlayerとBossの距離が判定距離より近い場合は80%、" +
                "それ以外は40%で選択する仕様です。",
                MessageType.None);

            SerializedProperty distance =
                m_dreadAttackProperty.FindPropertyRelative(
                    "m_distance");

            SerializedProperty nearProbability =
                m_dreadAttackProperty.FindPropertyRelative(
                    "m_nearProbability");

            SerializedProperty defaultProbability =
                m_dreadAttackProperty.FindPropertyRelative(
                    "m_defaultProbability");

            SerializedProperty coolTime =
                m_dreadAttackProperty.FindPropertyRelative(
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
    /// エネルギー砲のパラメータを描画します。
    /// </summary>
    private void DrawEnergyCannonParameters()
    {
        m_showEnergyCannon =
            EditorGUILayout.BeginFoldoutHeaderGroup(
                m_showEnergyCannon,
                "エネルギー砲");

        if (m_showEnergyCannon)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.HelpBox(
                "PlayerとBossの距離が判定距離より遠い場合は100%で選択します。" +
                "終了後はダウン状態へ直接遷移する想定です。",
                MessageType.None);

            SerializedProperty distance =
                m_energyCannonProperty.FindPropertyRelative(
                    "m_distance");

            SerializedProperty farProbability =
                m_energyCannonProperty.FindPropertyRelative(
                    "m_farProbability");

            SerializedProperty defaultProbability =
                m_energyCannonProperty.FindPropertyRelative(
                    "m_defaultProbability");

            SerializedProperty coolTime =
                m_energyCannonProperty.FindPropertyRelative(
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
    /// 未確定パラメータに関する警告を表示します。
    /// </summary>
    private void DrawValidationMessages()
    {
        DrawCoolTimeWarning(
            m_biteProperty,
            "噛み潰し");

        DrawCoolTimeWarning(
            m_chargeProperty,
            "突進");

        DrawCoolTimeWarning(
            m_dreadAttackProperty,
            "ドレッド攻撃");

        DrawCoolTimeWarning(
            m_energyCannonProperty,
            "エネルギー砲");
    }

    /// <summary>
    /// クールタイムが未設定の場合に警告を表示します。
    /// </summary>
    /// <param name="parentProperty">行動パラメータ。</param>
    /// <param name="actionName">行動名。</param>
    private static void DrawCoolTimeWarning(
        SerializedProperty parentProperty,
        string actionName)
    {
        SerializedProperty coolTime =
            parentProperty?.FindPropertyRelative(
                "m_coolTime");

        if (coolTime == null ||
            coolTime.floatValue > 0.0f)
        {
            return;
        }

        EditorGUILayout.HelpBox(
            $"{actionName}のクールタイムは仕様書に具体秒数がないため0秒で初期化しています。" +
            "必要な場合は調整値を設定してください。",
            MessageType.Warning);
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
