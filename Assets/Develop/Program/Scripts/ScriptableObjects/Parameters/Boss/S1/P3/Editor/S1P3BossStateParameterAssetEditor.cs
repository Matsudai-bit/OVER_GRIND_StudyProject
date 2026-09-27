using UnityEditor;
using UnityEngine;

/// <summary>
/// S1P3BossStateParameterAssetを調整しやすく表示します。
/// </summary>
[CustomEditor(typeof(S1P3BossStateParameterAsset))]
public sealed class S1P3BossStateParameterAssetEditor : Editor
{
    private SerializedProperty m_energyCannonProperty;
    private SerializedProperty m_chargeParameterAssetProperty;

    private bool m_showEnergyCannon = true;
    private bool m_showCharge = true;
    private bool m_showChargeDetails = true;

    private Editor m_chargeParameterEditor;
    private S1P3BossChargeAttackParameterAsset m_currentChargeParameterAsset;

    /// <summary>
    /// SerializedPropertyを取得します。
    /// </summary>
    private void OnEnable()
    {
        m_energyCannonProperty =
            serializedObject.FindProperty("m_energyCannon");

        m_chargeParameterAssetProperty =
            serializedObject.FindProperty("m_chargeParameterAsset");
    }

    /// <summary>
    /// 生成したEditorを破棄します。
    /// </summary>
    private void OnDisable()
    {
        ReleaseChargeParameterEditor();
    }

    /// <summary>
    /// Inspectorを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawEnergyCannonParameters();

        EditorGUILayout.Space(8.0f);

        DrawChargeParameters();

        EditorGUILayout.Space(8.0f);

        DrawValidationMessages();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// エネルギー砲状態のパラメータを描画します。
    /// </summary>
    private void DrawEnergyCannonParameters()
    {
        m_showEnergyCannon =
            EditorGUILayout.Foldout(
                m_showEnergyCannon,
                "エネルギー砲",
                true);

        if (!m_showEnergyCannon)
        {
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.HelpBox(
            "エネルギー集中 → 直線発射 → Player方向への追従、の3段階で使用する状態パラメータです。",
            MessageType.Info);

        DrawRelativeProperty(
            m_energyCannonProperty,
            "m_preparationDuration",
            "予備動作時間",
            "口を開け、コア前へエネルギーを集中させる時間です。");

        DrawRelativeProperty(
            m_energyCannonProperty,
            "m_straightFireDuration",
            "直線発射時間",
            "Player位置へ向けてエネルギー砲を一直線に放つ時間です。");

        DrawRelativeProperty(
            m_energyCannonProperty,
            "m_trackingDuration",
            "追従時間",
            "直線発射後、Player方向へエネルギー砲を動かす時間です。");

        DrawRelativeProperty(
            m_energyCannonProperty,
            "m_trackingSpeed",
            "追従速度",
            "Player方向へエネルギー砲を左右に動かす速度です。");

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 突進状態のパラメータを描画します。
    /// </summary>
    private void DrawChargeParameters()
    {
        m_showCharge =
            EditorGUILayout.Foldout(
                m_showCharge,
                "突進",
                true);

        if (!m_showCharge)
        {
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.HelpBox(
            "既存のS1P3BossChargeAttackParameterAssetをそのまま利用します。",
            MessageType.Info);

        EditorGUILayout.PropertyField(
            m_chargeParameterAssetProperty,
            new GUIContent(
                "突進パラメータ",
                "S1P3の連続突進で使用する既存のParameter Assetです。"));

        EditorGUILayout.EndVertical();

        DrawChargeParameterEditor();
    }

    /// <summary>
    /// 参照している突進パラメータをInspector内へ展開します。
    /// </summary>
    private void DrawChargeParameterEditor()
    {
        S1P3BossChargeAttackParameterAsset chargeParameterAsset =
            m_chargeParameterAssetProperty.objectReferenceValue
            as S1P3BossChargeAttackParameterAsset;

        if (chargeParameterAsset == null)
        {
            ReleaseChargeParameterEditor();

            EditorGUILayout.HelpBox(
                "突進パラメータアセットを設定してください。",
                MessageType.Warning);

            return;
        }

        m_showChargeDetails =
            EditorGUILayout.Foldout(
                m_showChargeDetails,
                "突進パラメータを編集",
                true);

        if (!m_showChargeDetails)
        {
            return;
        }

        EnsureChargeParameterEditor(chargeParameterAsset);

        EditorGUI.indentLevel++;
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        m_chargeParameterEditor?.OnInspectorGUI();

        EditorGUILayout.EndVertical();
        EditorGUI.indentLevel--;
    }

    /// <summary>
    /// 突進パラメータ用Editorを準備します。
    /// </summary>
    private void EnsureChargeParameterEditor(
        S1P3BossChargeAttackParameterAsset chargeParameterAsset)
    {
        if (m_currentChargeParameterAsset == chargeParameterAsset &&
            m_chargeParameterEditor != null)
        {
            return;
        }

        ReleaseChargeParameterEditor();

        m_currentChargeParameterAsset =
            chargeParameterAsset;

        CreateCachedEditor(
            chargeParameterAsset,
            null,
            ref m_chargeParameterEditor);
    }

    /// <summary>
    /// 突進パラメータ用Editorを破棄します。
    /// </summary>
    private void ReleaseChargeParameterEditor()
    {
        if (m_chargeParameterEditor != null)
        {
            DestroyImmediate(m_chargeParameterEditor);
            m_chargeParameterEditor = null;
        }

        m_currentChargeParameterAsset = null;
    }

    /// <summary>
    /// パラメータ設定の注意を表示します。
    /// </summary>
    private void DrawValidationMessages()
    {
        SerializedProperty trackingSpeedProperty =
            m_energyCannonProperty?.FindPropertyRelative(
                "m_trackingSpeed");

        if (trackingSpeedProperty != null &&
            trackingSpeedProperty.floatValue <= 0.0f)
        {
            EditorGUILayout.HelpBox(
                "エネルギー砲の追従速度は仕様資料に具体値がないため、実装時に調整値を設定してください。",
                MessageType.Warning);
        }

        if (m_chargeParameterAssetProperty.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox(
                "突進状態を使用するにはS1P3BossChargeAttackParameterAssetを設定してください。",
                MessageType.Warning);
        }
    }

    /// <summary>
    /// 子プロパティを描画します。
    /// </summary>
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
            parentProperty.FindPropertyRelative(propertyName);

        if (property == null)
        {
            return;
        }

        EditorGUILayout.PropertyField(
            property,
            new GUIContent(label, tooltip));
    }
}
