using UnityEditor;
using UnityEngine;

/// <summary>
/// S1P1BossMissileParameterAssetを調整しやすく表示します。
/// </summary>
[CustomEditor(typeof(S1P1BossMissileParameterAsset))]
public sealed class S1P1BossMissileParameterAssetEditor : Editor
{
    // ミサイル本体パラメータの参照
    private SerializedProperty m_missileParameterAssetProperty;

    // ミサイル攻撃状態パラメータ
    private SerializedProperty m_stateParametersProperty;

    // ミサイル本体パラメータの表示状態
    private bool m_showMissileParameters = true;

    // ミサイル本体パラメータ用Editor
    private Editor m_missileParameterEditor;

    // 現在Editorを生成しているミサイルパラメータ
    private MissileParameterAsset m_currentMissileParameterAsset;

    /// <summary>
    /// SerializedPropertyを取得します。
    /// </summary>
    private void OnEnable()
    {
        m_missileParameterAssetProperty =
            serializedObject.FindProperty(
                "m_missileParameterAsset");

        m_stateParametersProperty =
            serializedObject.FindProperty(
                "m_stateParameters");
    }

    /// <summary>
    /// 生成したEditorを破棄します。
    /// </summary>
    private void OnDisable()
    {
        ReleaseMissileParameterEditor();
    }

    /// <summary>
    /// Inspectorを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawStateParameters();

        EditorGUILayout.Space(8.0f);

        DrawMissileParameterReference();

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(4.0f);

        DrawMissileParameterEditor();
    }

    /// <summary>
    /// ボスのミサイル攻撃状態パラメータを描画します。
    /// </summary>
    private void DrawStateParameters()
    {
        EditorGUILayout.LabelField(
            "ボスミサイル挙動",
            EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        DrawRelativeProperty(
            m_stateParametersProperty,
            "m_launchInterval",
            "発射間隔",
            "次のミサイルを発射するまでの間隔です。");

        DrawRelativeProperty(
            m_stateParametersProperty,
            "m_missileUpwardDuration",
            "上昇時間",
            "発射直後に上方向へ進んでからホーミングへ移行するまでの時間です。");

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// ミサイル本体パラメータの参照を描画します。
    /// </summary>
    private void DrawMissileParameterReference()
    {
        EditorGUILayout.LabelField(
            "ミサイル本体",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            m_missileParameterAssetProperty,
            new GUIContent(
                "ミサイルパラメータ",
                "MissileMotorとMissileSteeringで使用するパラメータアセットです。"));

        if (m_missileParameterAssetProperty.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox(
                "ミサイルパラメータアセットを設定してください。",
                MessageType.Warning);
        }
    }

    /// <summary>
    /// 参照しているミサイルパラメータをInspector内へ展開します。
    /// </summary>
    private void DrawMissileParameterEditor()
    {
        MissileParameterAsset missileParameterAsset =
            m_missileParameterAssetProperty.objectReferenceValue
            as MissileParameterAsset;

        if (missileParameterAsset == null)
        {
            ReleaseMissileParameterEditor();
            return;
        }

        // BeginFoldoutHeaderGroupは子Editor側でも使用しているため、
        // 親側では通常のFoldoutを使用してネストを避けます。
        m_showMissileParameters =
            EditorGUILayout.Foldout(
                m_showMissileParameters,
                "ミサイル本体パラメータを編集",
                true);

        if (!m_showMissileParameters)
        {
            return;
        }

        EnsureMissileParameterEditor(
            missileParameterAsset);

        EditorGUI.indentLevel++;

        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        if (m_missileParameterEditor != null)
        {
            m_missileParameterEditor.OnInspectorGUI();
        }

        EditorGUILayout.EndVertical();

        EditorGUI.indentLevel--;
    }

    /// <summary>
    /// ミサイルパラメータ用Editorを準備します。
    /// </summary>
    /// <param name="missileParameterAsset">
    /// 編集するミサイルパラメータ。
    /// </param>
    private void EnsureMissileParameterEditor(
        MissileParameterAsset missileParameterAsset)
    {
        if (m_currentMissileParameterAsset ==
                missileParameterAsset &&
            m_missileParameterEditor != null)
        {
            return;
        }

        ReleaseMissileParameterEditor();

        m_currentMissileParameterAsset =
            missileParameterAsset;

        CreateCachedEditor(
            missileParameterAsset,
            null,
            ref m_missileParameterEditor);
    }

    /// <summary>
    /// ミサイルパラメータ用Editorを破棄します。
    /// </summary>
    private void ReleaseMissileParameterEditor()
    {
        if (m_missileParameterEditor != null)
        {
            DestroyImmediate(
                m_missileParameterEditor);

            m_missileParameterEditor = null;
        }

        m_currentMissileParameterAsset = null;
    }

    /// <summary>
    /// 子プロパティをラベルとツールチップ付きで描画します。
    /// </summary>
    /// <param name="parentProperty">親プロパティ。</param>
    /// <param name="propertyName">子プロパティ名。</param>
    /// <param name="label">表示名。</param>
    /// <param name="tooltip">説明。</param>
    private void DrawRelativeProperty(
        SerializedProperty parentProperty,
        string propertyName,
        string label,
        string tooltip)
    {
        if (parentProperty == null)
        {
            return;
        }

        SerializedProperty property =
            parentProperty.FindPropertyRelative(
                propertyName);

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
