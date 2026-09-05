using UnityEditor;
using UnityEngine;

/// <summary>
/// ボス突進攻撃パラメータのInspector表示を管理します。
/// </summary>
[CustomEditor(typeof(S1BossChargeAttackParameterAsset))]
public sealed class BossChargeAttackParameterAssetEditor : Editor
{
    // Inspector上の余白
    private const float SECTION_SPACE = 8.0f;

    // 回転状態の継続時間
    private SerializedProperty m_preparationDurationProperty;

    // 回転状態の回転速度
    private SerializedProperty m_rotationSpeedProperty;

    // 突進状態の移動速度
    private SerializedProperty m_chargeSpeedProperty;

    // 突進状態の最大移動距離
    private SerializedProperty m_maxChargeDistanceProperty;

    // 突進状態の停止余白
    private SerializedProperty m_stopMarginProperty;

    // 終了状態の継続時間
    private SerializedProperty m_endDurationProperty;

    // 回転状態のアニメーションBool名
    private SerializedProperty
        m_preparationAnimationBoolNameProperty;

    // 突進状態のアニメーションBool名
    private SerializedProperty
        m_chargeAnimationBoolNameProperty;

    // 終了状態のアニメーションBool名
    private SerializedProperty
        m_endAnimationBoolNameProperty;

    // 攻撃識別子
    private SerializedProperty m_attackIdentifierProperty;

    // システム設定の表示状態
    private bool m_showSystemParameters;

    /// <summary>
    /// SerializedPropertyを取得します。
    /// </summary>
    private void OnEnable()
    {
        m_preparationDurationProperty =
            serializedObject.FindProperty(
                "m_preparationDuration");

        m_rotationSpeedProperty =
            serializedObject.FindProperty(
                "m_rotationSpeed");

        m_chargeSpeedProperty =
            serializedObject.FindProperty(
                "m_chargeSpeed");

        m_maxChargeDistanceProperty =
            serializedObject.FindProperty(
                "m_maxChargeDistance");

        m_stopMarginProperty =
            serializedObject.FindProperty(
                "m_stopMargin");

        m_endDurationProperty =
            serializedObject.FindProperty(
                "m_endDuration");

        m_preparationAnimationBoolNameProperty =
            serializedObject.FindProperty(
                "m_preparationAnimationBoolName");

        m_chargeAnimationBoolNameProperty =
            serializedObject.FindProperty(
                "m_chargeAnimationBoolName");

        m_endAnimationBoolNameProperty =
            serializedObject.FindProperty(
                "m_endAnimationBoolName");

        m_attackIdentifierProperty =
            serializedObject.FindProperty(
                "m_attackIdentifier");
    }

    /// <summary>
    /// Inspectorを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDescription();

        EditorGUILayout.Space(SECTION_SPACE);

        DrawRotationSection();

        DrawTransitionLabel(
            "回転完了後、現在向いている方向へ突進");

        DrawChargeSection();

        DrawTransitionLabel(
            "突進終了後、停止して終了状態へ移行");

        DrawEndSection();

        EditorGUILayout.Space(SECTION_SPACE);

        DrawChargePreview();

        EditorGUILayout.Space(SECTION_SPACE);

        DrawSystemParameters();

        DrawParameterWarnings();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 突進攻撃の概要を表示します。
    /// </summary>
    private void DrawDescription()
    {
        EditorGUILayout.HelpBox(
            "突進攻撃は「回転状態 → 突進状態 → 終了状態」の" +
            "3段階で実行されます。",
            MessageType.Info);
    }

    /// <summary>
    /// 回転状態のパラメータを表示します。
    /// </summary>
    private void DrawRotationSection()
    {
        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "1. 回転状態",
            EditorStyles.boldLabel);

        EditorGUILayout.LabelField(
            "突進前にプレイヤー方向を追従します。",
            EditorStyles.miniLabel);

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(
            m_preparationDurationProperty,
            new GUIContent(
                "回転継続時間",
                "プレイヤー方向を追従して回転する時間です。"));

        EditorGUILayout.PropertyField(
            m_rotationSpeedProperty,
            new GUIContent(
                "回転速度",
                "プレイヤー方向へ向く際の回転速度です。"));

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 突進状態のパラメータを表示します。
    /// </summary>
    private void DrawChargeSection()
    {
        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "2. 突進状態",
            EditorStyles.boldLabel);

        EditorGUILayout.LabelField(
            "回転終了時に向いている方向へ直線突進します。",
            EditorStyles.miniLabel);

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(
            m_chargeSpeedProperty,
            new GUIContent(
                "突進速度",
                "突進中の移動速度です。"));

        EditorGUILayout.PropertyField(
            m_maxChargeDistanceProperty,
            new GUIContent(
                "最大突進距離",
                "1回の突進で移動できる最大距離です。"));

        EditorGUILayout.PropertyField(
            m_stopMarginProperty,
            new GUIContent(
                "停止余白",
                "障害物や移動可能範囲の境界手前で" +
                "停止するための余白です。"));

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 終了状態のパラメータを表示します。
    /// </summary>
    private void DrawEndSection()
    {
        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "3. 終了状態",
            EditorStyles.boldLabel);

        EditorGUILayout.LabelField(
            "突進を停止し、次の行動へ移るまで待機します。",
            EditorStyles.miniLabel);

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(
            m_endDurationProperty,
            new GUIContent(
                "終了硬直時間",
                "突進終了後に次の行動へ移るまでの時間です。"));

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 状態間の遷移を表示します。
    /// </summary>
    /// <param name="description">
    /// 遷移内容。
    /// </param>
    private void DrawTransitionLabel(
        string description)
    {
        GUIStyle centeredLabel =
            new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };

        GUIStyle centeredMiniLabel =
            new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };

        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            "↓",
            centeredLabel);

        EditorGUILayout.LabelField(
            description,
            centeredMiniLabel);

        EditorGUILayout.Space();
    }

    /// <summary>
    /// 突進攻撃の調整結果を表示します。
    /// </summary>
    private void DrawChargePreview()
    {
        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "突進攻撃確認",
            EditorStyles.boldLabel);

        float preparationDuration =
            m_preparationDurationProperty.floatValue;

        float chargeSpeed =
            m_chargeSpeedProperty.floatValue;

        float maxChargeDistance =
            m_maxChargeDistanceProperty.floatValue;

        float endDuration =
            m_endDurationProperty.floatValue;

        float maxChargeDuration = 0.0f;

        if (chargeSpeed > 0.0f)
        {
            maxChargeDuration =
                maxChargeDistance /
                chargeSpeed;
        }

        float maxTotalDuration =
            preparationDuration +
            maxChargeDuration +
            endDuration;

        DrawReadOnlyFloat(
            "最大突進移動時間",
            maxChargeDuration,
            " 秒");

        DrawReadOnlyFloat(
            "最大攻撃時間",
            maxTotalDuration,
            " 秒");

        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            "状態遷移",
            $"回転 {preparationDuration:F2}秒" +
            $" → 突進 最大{maxChargeDuration:F2}秒" +
            $" → 終了 {endDuration:F2}秒");

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// プログラマ向けシステム設定を表示します。
    /// </summary>
    private void DrawSystemParameters()
    {
        m_showSystemParameters =
            EditorGUILayout.Foldout(
                m_showSystemParameters,
                "システム設定（プログラマ向け）",
                true);

        if (!m_showSystemParameters)
        {
            return;
        }

        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.HelpBox(
            "以下は攻撃機構との紐付けに使用する設定です。" +
            "通常のバランス調整では変更しません。",
            MessageType.Info);

        EditorGUILayout.LabelField(
            "回転状態",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            m_preparationAnimationBoolNameProperty,
            new GUIContent(
                "Animation Bool",
                "回転状態で有効にするAnimator Bool名です。"));

        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            "突進状態",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            m_chargeAnimationBoolNameProperty,
            new GUIContent(
                "Animation Bool",
                "突進状態で有効にするAnimator Bool名です。"));

        EditorGUILayout.PropertyField(
            m_attackIdentifierProperty,
            new GUIContent(
                "Attack Identifier",
                "突進中に有効化する攻撃判定です。"));

        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            "終了状態",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            m_endAnimationBoolNameProperty,
            new GUIContent(
                "Animation Bool",
                "終了状態で有効にするAnimator Bool名です。"));

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// パラメータ設定に問題がある場合に警告を表示します。
    /// </summary>
    private void DrawParameterWarnings()
    {
        if (m_chargeSpeedProperty.floatValue <= 0.0f)
        {
            EditorGUILayout.HelpBox(
                "突進速度が0以下のため、突進を実行できません。",
                MessageType.Error);
        }

        if (m_maxChargeDistanceProperty.floatValue <= 0.0f)
        {
            EditorGUILayout.HelpBox(
                "最大突進距離が0以下のため、突進できません。",
                MessageType.Error);
        }

        if (m_attackIdentifierProperty.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox(
                "Attack Identifierが設定されていません。" +
                "突進中の攻撃判定が有効化されません。",
                MessageType.Warning);
        }
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
}