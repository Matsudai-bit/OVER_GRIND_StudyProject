using UnityEditor;
using UnityEngine;

/// <summary>
/// ボス突進攻撃パラメータのInspector表示を管理します。
/// </summary>
[CustomEditor(typeof(S1BossChargeAttackParameterAsset))]
public sealed class BossChargeAttackParameterAssetEditor :
    Editor
{
    // Inspector上の余白
    private const float SECTION_SPACE = 8.0f;

    // 狙い確定状態の継続時間
    private SerializedProperty m_targetLockDurationProperty;

    // 回転速度
    private SerializedProperty m_rotationSpeedProperty;

    // 突進速度
    private SerializedProperty m_chargeSpeedProperty;

    // 最大突進距離
    private SerializedProperty m_maxChargeDistanceProperty;

    // 対象位置を越えて進む距離
    private SerializedProperty m_targetOverrunDistanceProperty;

    // NavMesh境界から確保する停止余白
    private SerializedProperty m_stopMarginProperty;

    // 加速時間
    private SerializedProperty m_accelerationDurationProperty;

    // 加速イージング
    private SerializedProperty m_accelerationCurveProperty;

    // 減速開始距離
    private SerializedProperty m_decelerationDistanceProperty;

    // 減速イージング
    private SerializedProperty m_decelerationCurveProperty;

    // 終了動作時間
    private SerializedProperty m_endDurationProperty;

    // 突進攻撃中を示すAnimator Bool名
    private SerializedProperty m_animationActiveBoolNameProperty;

    // 突進攻撃フェーズを示すAnimator Int名
    private SerializedProperty m_animationPhaseIntNameProperty;

    // 左右旋回方向を示すAnimator Int名
    private SerializedProperty m_animationTurnDirectionIntNameProperty;

    // 攻撃識別子
    private SerializedProperty m_attackIdentifierProperty;

    // システム設定の表示状態
    private bool m_showSystemParameters;

    /// <summary>
    /// SerializedPropertyを取得します。
    /// </summary>
    private void OnEnable()
    {
        m_targetLockDurationProperty =
            serializedObject.FindProperty(
                "m_targetLockDuration");

        m_rotationSpeedProperty =
            serializedObject.FindProperty(
                "m_rotationSpeed");

        m_chargeSpeedProperty =
            serializedObject.FindProperty(
                "m_chargeSpeed");

        m_maxChargeDistanceProperty =
            serializedObject.FindProperty(
                "m_maxChargeDistance");

        m_targetOverrunDistanceProperty =
            serializedObject.FindProperty(
                "m_targetOverrunDistance");

        m_stopMarginProperty =
            serializedObject.FindProperty(
                "m_stopMargin");

        m_accelerationDurationProperty =
            serializedObject.FindProperty(
                "m_accelerationDuration");

        m_accelerationCurveProperty =
            serializedObject.FindProperty(
                "m_accelerationCurve");

        m_decelerationDistanceProperty =
            serializedObject.FindProperty(
                "m_decelerationDistance");

        m_decelerationCurveProperty =
            serializedObject.FindProperty(
                "m_decelerationCurve");

        m_endDurationProperty =
            serializedObject.FindProperty(
                "m_endDuration");

        m_animationActiveBoolNameProperty =
            serializedObject.FindProperty(
                "m_animationActiveBoolName");

        m_animationPhaseIntNameProperty =
            serializedObject.FindProperty(
                "m_animationPhaseIntName");

        m_animationTurnDirectionIntNameProperty =
            serializedObject.FindProperty(
                "m_animationTurnDirectionIntName");

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

        EditorGUILayout.Space(
            SECTION_SPACE);

        DrawTurnSection();

        DrawTransitionLabel(
            "目標方向へ向いたら、その方向を固定");

        DrawTargetLockSection();

        DrawTransitionLabel(
            "一定時間その方向を向いた後に突進");

        DrawChargeSection();

        DrawTransitionLabel(
            "突進終了後、停止して終了動作へ移行");

        DrawEndSection();

        EditorGUILayout.Space(
            SECTION_SPACE);

        DrawAnimationPhasePreview();

        EditorGUILayout.Space(
            SECTION_SPACE);

        DrawSystemParameters();

        DrawWarnings();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 突進攻撃の概要を表示します。
    /// </summary>
    private void DrawDescription()
    {
        EditorGUILayout.HelpBox(
            "突進攻撃は「回転 → 狙い確定 → 突進 → 終了動作」の4段階で実行します。\n" +
            "回転中は目標方向を追従し、目標方向へ向いた時点で方向を固定します。\n" +
            "狙い確定中は固定した方向を一定時間向き続け、その後に直線突進します。",
            MessageType.Info);
    }

    /// <summary>
    /// 回転状態のパラメータを描画します。
    /// </summary>
    private void DrawTurnSection()
    {
        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "1. 回転",
            EditorStyles.boldLabel);

        EditorGUILayout.LabelField(
            "目標方向へ旋回します。左右のAnimationは旋回方向で分岐します。",
            EditorStyles.wordWrappedMiniLabel);

        EditorGUILayout.Space();

        DrawProperty(
            m_rotationSpeedProperty,
            "回転速度",
            "目標方向へ向く際の回転速度です。");

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 狙い確定状態のパラメータを描画します。
    /// </summary>
    private void DrawTargetLockSection()
    {
        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "2. 狙い確定",
            EditorStyles.boldLabel);

        EditorGUILayout.LabelField(
            "回転完了時の方向を固定し、その方向を向いたまま待機します。",
            EditorStyles.wordWrappedMiniLabel);

        EditorGUILayout.Space();

        DrawProperty(
            m_targetLockDurationProperty,
            "狙い確定時間",
            "方向を固定してから突進を開始するまでの待機時間です。");

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 突進状態のパラメータを描画します。
    /// </summary>
    private void DrawChargeSection()
    {
        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "3. 突進",
            EditorStyles.boldLabel);

        EditorGUILayout.LabelField(
            "狙い確定で固定した方向へ直線突進します。",
            EditorStyles.wordWrappedMiniLabel);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            "移動",
            EditorStyles.boldLabel);

        DrawProperty(
            m_chargeSpeedProperty,
            "最高速度",
            "突進中の最高移動速度です。");

        DrawProperty(
            m_maxChargeDistanceProperty,
            "最大突進距離",
            "1回の突進で移動できる最大距離です。");

        DrawProperty(
            m_targetOverrunDistanceProperty,
            "対象超過距離",
            "目標位置を越えて進ませる追加距離です。");

        DrawProperty(
            m_stopMarginProperty,
            "NavMesh停止余白",
            "NavMesh境界の手前で停止するために確保する距離です。");

        EditorGUILayout.Space(
            SECTION_SPACE);

        EditorGUILayout.LabelField(
            "加速",
            EditorStyles.boldLabel);

        DrawProperty(
            m_accelerationDurationProperty,
            "加速時間",
            "突進開始から最高速度へ到達するまでの時間です。");

        DrawProperty(
            m_accelerationCurveProperty,
            "加速イージング",
            "加速時間0～1に対する速度進行率です。");

        EditorGUILayout.Space(
            SECTION_SPACE);

        EditorGUILayout.LabelField(
            "減速",
            EditorStyles.boldLabel);

        DrawProperty(
            m_decelerationDistanceProperty,
            "減速開始距離",
            "停止地点の何m手前から減速を開始するか指定します。");

        DrawProperty(
            m_decelerationCurveProperty,
            "減速イージング",
            "減速区間0～1に対する速度倍率です。");

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 終了動作のパラメータを描画します。
    /// </summary>
    private void DrawEndSection()
    {
        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "4. 終了動作",
            EditorStyles.boldLabel);

        EditorGUILayout.LabelField(
            "突進を停止し、次の行動へ移るまで待機します。",
            EditorStyles.wordWrappedMiniLabel);

        EditorGUILayout.Space();

        DrawProperty(
            m_endDurationProperty,
            "終了硬直時間",
            "突進終了後に次の行動へ移るまでの時間です。");

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 状態間の遷移を表示します。
    /// </summary>
    private void DrawTransitionLabel(
        string description)
    {
        GUIStyle arrowStyle =
            new GUIStyle(
                EditorStyles.boldLabel)
            {
                alignment =
                    TextAnchor.MiddleCenter,
                fontSize =
                    16
            };

        GUIStyle descriptionStyle =
            new GUIStyle(
                EditorStyles.miniLabel)
            {
                alignment =
                    TextAnchor.MiddleCenter
            };

        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            "↓",
            arrowStyle);

        EditorGUILayout.LabelField(
            description,
            descriptionStyle);

        EditorGUILayout.Space();
    }

    /// <summary>
    /// Animatorへ渡される行動フェーズを表示します。
    /// </summary>
    private void DrawAnimationPhasePreview()
    {
        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "Animation Phase",
            EditorStyles.boldLabel);

        EditorGUILayout.LabelField(
            "0  PREPARATION",
            "回転");

        EditorGUILayout.LabelField(
            "1  TARGET_LOCK",
            "狙い確定");

        EditorGUILayout.LabelField(
            "2  EXECUTION",
            "突進");

        EditorGUILayout.LabelField(
            "3  RECOVERY",
            "終了動作");

        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            "Turn Direction",
            EditorStyles.boldLabel);

        EditorGUILayout.LabelField(
            "-1  LEFT",
            "左旋回");

        EditorGUILayout.LabelField(
            " 1  RIGHT",
            "右旋回");

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// プログラマ向けシステム設定を描画します。
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

        DrawProperty(
            m_animationActiveBoolNameProperty,
            "Active Bool",
            "突進攻撃全体が実行中であることを示すAnimator Bool名です。");

        DrawProperty(
            m_animationPhaseIntNameProperty,
            "Phase Int",
            "回転・狙い確定・突進・終了動作を示すAnimator Int名です。");

        DrawProperty(
            m_animationTurnDirectionIntNameProperty,
            "Turn Direction Int",
            "LEFT=-1、RIGHT=1として左右旋回を示すAnimator Int名です。");

        DrawProperty(
            m_attackIdentifierProperty,
            "Attack Identifier",
            "突進中に有効化する攻撃判定の識別子です。");

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// パラメータ設定の警告を表示します。
    /// </summary>
    private void DrawWarnings()
    {
        if (m_decelerationDistanceProperty != null &&
            m_maxChargeDistanceProperty != null &&
            m_decelerationDistanceProperty.floatValue >
            m_maxChargeDistanceProperty.floatValue)
        {
            EditorGUILayout.HelpBox(
                "減速開始距離が最大突進距離を上回っています。" +
                "突進開始直後から減速する可能性があります。",
                MessageType.Warning);
        }

        if (m_attackIdentifierProperty != null &&
            m_attackIdentifierProperty.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox(
                "Attack Identifierが設定されていません。",
                MessageType.Warning);
        }

        if (m_targetLockDurationProperty == null)
        {
            EditorGUILayout.HelpBox(
                "m_targetLockDurationが見つかりません。" +
                "S1BossChargeAttackParameterAssetを4段階仕様へ更新してください。",
                MessageType.Error);
        }

        if (m_animationTurnDirectionIntNameProperty == null)
        {
            EditorGUILayout.HelpBox(
                "m_animationTurnDirectionIntNameが見つかりません。" +
                "左右旋回用Animator IntをParameterAssetへ追加してください。",
                MessageType.Error);
        }
    }

    /// <summary>
    /// SerializedPropertyを描画します。
    /// </summary>
    private static void DrawProperty(
        SerializedProperty property,
        string label,
        string tooltip)
    {
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
