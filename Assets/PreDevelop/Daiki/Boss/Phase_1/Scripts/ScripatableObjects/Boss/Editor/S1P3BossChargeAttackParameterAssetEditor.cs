using UnityEditor;
using UnityEngine;

/// <summary>
/// ステージ1フェーズ3の連続突進攻撃パラメータのInspectorを管理します。
/// </summary>
[CustomEditor(typeof(S1P3BossChargeAttackParameterAsset))]
public sealed class S1P3BossChargeAttackParameterAssetEditor : Editor
{
    // Inspector上の余白
    private const float SECTION_SPACE = 8.0f;

    // 1回目の突進パラメータ
    private SerializedProperty m_firstChargeParameterAssetProperty;

    // 2回目の突進パラメータ
    private SerializedProperty m_secondChargeParameterAssetProperty;

    // 3回目の突進パラメータ
    private SerializedProperty m_thirdChargeParameterAssetProperty;

    // 各突進パラメータ用Editor
    private Editor m_firstChargeEditor;
    private Editor m_secondChargeEditor;
    private Editor m_thirdChargeEditor;

    // 各突進パラメータの表示状態
    private bool m_showFirstCharge = true;
    private bool m_showSecondCharge = true;
    private bool m_showThirdCharge = true;

    /// <summary>
    /// SerializedPropertyを取得します。
    /// </summary>
    private void OnEnable()
    {
        m_firstChargeParameterAssetProperty =
            serializedObject.FindProperty(
                "m_firstChargeParameterAsset");

        m_secondChargeParameterAssetProperty =
            serializedObject.FindProperty(
                "m_secondChargeParameterAsset");

        m_thirdChargeParameterAssetProperty =
            serializedObject.FindProperty(
                "m_thirdChargeParameterAsset");
    }

    /// <summary>
    /// Inspectorを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDescription();

        EditorGUILayout.Space(SECTION_SPACE);

        DrawChargeSection(
            1,
            "1回目の突進",
            "プレイヤーを追尾して突進します。",
            m_firstChargeParameterAssetProperty,
            ref m_firstChargeEditor,
            ref m_showFirstCharge);

        DrawTransitionLabel(
            "1回目終了 → 2回目へ");

        DrawChargeSection(
            2,
            "2回目の突進",
            "再度プレイヤーを追尾して突進します。",
            m_secondChargeParameterAssetProperty,
            ref m_secondChargeEditor,
            ref m_showSecondCharge);

        DrawTransitionLabel(
            "2回目終了 → 3回目の目的地を決定");

        DrawChargeSection(
            3,
            "3回目の突進",
            "選択された固定地点を目標に突進します。",
            m_thirdChargeParameterAssetProperty,
            ref m_thirdChargeEditor,
            ref m_showThirdCharge);

        EditorGUILayout.Space(SECTION_SPACE);

        DrawWarnings();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 連続突進攻撃の説明を表示します。
    /// </summary>
    private void DrawDescription()
    {
        EditorGUILayout.HelpBox(
            "フェーズ3では3回の突進を連続して実行します。\n\n" +
            "1回目：プレイヤー追尾 → 突進\n" +
            "2回目：プレイヤー追尾 → 突進\n" +
            "3回目：固定地点を選択 → 突進",
            MessageType.Info);
    }

    /// <summary>
    /// 1回分の突進パラメータを表示します。
    /// </summary>
    /// <param name="chargeNumber">突進番号。</param>
    /// <param name="title">表示タイトル。</param>
    /// <param name="description">突進内容。</param>
    /// <param name="property">Parameter Asset。</param>
    /// <param name="cachedEditor">Parameter Asset用Editor。</param>
    /// <param name="isExpanded">展開状態。</param>
    private void DrawChargeSection(
        int chargeNumber,
        string title,
        string description,
        SerializedProperty property,
        ref Editor cachedEditor,
        ref bool isExpanded)
    {
        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        isExpanded = EditorGUILayout.Foldout(
            isExpanded,
            title,
            true);

        if (!isExpanded)
        {
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            description,
            EditorStyles.wordWrappedMiniLabel);

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(
            property,
            new GUIContent(
                "使用パラメータ",
                $"{chargeNumber}回目の突進で使用するParameter Assetです。"));

        S1BossChargeAttackParameterAsset parameterAsset =
            property.objectReferenceValue as
                S1BossChargeAttackParameterAsset;

        if (parameterAsset == null)
        {
            EditorGUILayout.HelpBox(
                $"{chargeNumber}回目の突進パラメータが設定されていません。",
                MessageType.Error);

            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.Space();

        DrawSeparator();

        EditorGUILayout.Space();

        // BossChargeAttackParameterAssetEditorを
        // このInspector内へそのまま描画
        Editor.CreateCachedEditor(
            parameterAsset,
            typeof(BossChargeAttackParameterAssetEditor),
            ref cachedEditor);

        cachedEditor.OnInspectorGUI();

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 突進間の遷移を表示します。
    /// </summary>
    /// <param name="description">遷移内容。</param>
    private void DrawTransitionLabel(
        string description)
    {
        GUIStyle arrowStyle =
            new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16
            };

        GUIStyle descriptionStyle =
            new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter
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
    /// 区切り線を表示します。
    /// </summary>
    private void DrawSeparator()
    {
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
    }

    /// <summary>
    /// 設定不足の警告を表示します。
    /// </summary>
    private void DrawWarnings()
    {
        bool hasMissingParameter =
            m_firstChargeParameterAssetProperty
                .objectReferenceValue == null ||
            m_secondChargeParameterAssetProperty
                .objectReferenceValue == null ||
            m_thirdChargeParameterAssetProperty
                .objectReferenceValue == null;

        if (!hasMissingParameter)
        {
            return;
        }

        EditorGUILayout.HelpBox(
            "連続突進を実行するには、" +
            "1～3回目すべての突進パラメータを設定してください。",
            MessageType.Error);
    }
}