using UnityEditor;
using UnityEngine;

/// <summary>
/// S1P3BossStateParameterAssetを調整しやすく表示します。
/// </summary>
[CustomEditor(typeof(S1P3BossStateParameterAsset))]
public sealed class S1P3BossStateParameterAssetEditor : Editor
{
    private SerializedProperty m_energyCannonProperty;
    private SerializedProperty m_dreadAttackProperty;
    private SerializedProperty m_chargeParameterAssetProperty;

    private bool m_showEnergyCannon = true;
    private bool m_showDreadAttack = true;
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

        m_dreadAttackProperty =
            serializedObject.FindProperty("m_dreadAttack");

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

        DrawDreadAttackParameters();

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
    /// ドレッド攻撃状態のパラメータを描画します。
    /// </summary>
    private void DrawDreadAttackParameters()
    {
        m_showDreadAttack =
            EditorGUILayout.Foldout(
                m_showDreadAttack,
                "ドレッド攻撃",
                true);

        if (!m_showDreadAttack)
        {
            return;
        }

        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "オイル飛行",
            EditorStyles.boldLabel);

        DrawRelativeProperty(
            m_dreadAttackProperty,
            "m_ascentDuration",
            "上昇時間",
            "オイルが上方向へ飛び出す時間です。");

        DrawRelativeProperty(
            m_dreadAttackProperty,
            "m_horizontalMoveDuration",
            "水平移動時間",
            "着地点上空まで移動する時間です。");

        DrawRelativeProperty(
            m_dreadAttackProperty,
            "m_fallDuration",
            "落下時間",
            "着地点上空から地面まで落下する時間です。");

        DrawRelativeProperty(
            m_dreadAttackProperty,
            "m_flightHeight",
            "飛翔高度",
            "オイルが飛翔するときの高さです。");

        EditorGUILayout.Space(4.0f);

        EditorGUILayout.LabelField(
            "着地点",
            EditorStyles.boldLabel);

        DrawRelativeProperty(
            m_dreadAttackProperty,
            "m_landingRadius",
            "着地点ランダム半径",
            "Playerを中心に着地点をランダム選択する半径です。");

        DrawRelativeProperty(
            m_dreadAttackProperty,
            "m_groundRaycastStartHeight",
            "地面探索Ray開始高さ",
            "地面探索Rayの開始高さです。");

        DrawRelativeProperty(
            m_dreadAttackProperty,
            "m_groundRaycastDistance",
            "地面探索Ray距離",
            "地面探索Rayの最大距離です。");

        EditorGUILayout.Space(4.0f);

        EditorGUILayout.LabelField(
            "状態終了",
            EditorStyles.boldLabel);

        DrawRelativeProperty(
            m_dreadAttackProperty,
            "m_finishDelay",
            "終了待機時間",
            "全オイル着弾後から状態終了までの待機時間です。");

        EditorGUILayout.Space(4.0f);

        EditorGUILayout.LabelField(
            "着弾予告",
            EditorStyles.boldLabel);

        DrawRelativeProperty(
            m_dreadAttackProperty,
            "m_targetDecalMinScale",
            "最小サイズ",
            "着弾予告Decalの開始サイズです。");

        DrawRelativeProperty(
            m_dreadAttackProperty,
            "m_targetDecalMaxScale",
            "最大サイズ",
            "着弾予告Decalの最大サイズです。");

        DrawRelativeProperty(
            m_dreadAttackProperty,
            "m_targetDecalMinAlpha",
            "最小不透明度",
            "着弾予告Decalの開始時の不透明度です。");

        DrawRelativeProperty(
            m_dreadAttackProperty,
            "m_targetDecalMaxAlpha",
            "最大不透明度",
            "着弾予告Decalの最大不透明度です。");

        DrawRelativeProperty(
            m_dreadAttackProperty,
            "m_decalGroundOffset",
            "地面オフセット",
            "Decalと地面の表示ずれを防ぐオフセットです。");

        EditorGUILayout.Space(4.0f);

        EditorGUILayout.LabelField(
            "着弾痕",
            EditorStyles.boldLabel);

        DrawRelativeProperty(
            m_dreadAttackProperty,
            "m_oilDecalDuration",
            "着弾痕表示時間",
            "着弾後のオイルDecalを表示する時間です。");

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


        SerializedProperty minScaleProperty =
            m_dreadAttackProperty?.FindPropertyRelative(
                "m_targetDecalMinScale");

        SerializedProperty maxScaleProperty =
            m_dreadAttackProperty?.FindPropertyRelative(
                "m_targetDecalMaxScale");

        if (minScaleProperty != null &&
            maxScaleProperty != null &&
            minScaleProperty.floatValue > maxScaleProperty.floatValue)
        {
            EditorGUILayout.HelpBox(
                "ドレッド攻撃の着弾予告最小サイズが最大サイズを上回っています。",
                MessageType.Warning);
        }

        SerializedProperty minAlphaProperty =
            m_dreadAttackProperty?.FindPropertyRelative(
                "m_targetDecalMinAlpha");

        SerializedProperty maxAlphaProperty =
            m_dreadAttackProperty?.FindPropertyRelative(
                "m_targetDecalMaxAlpha");

        if (minAlphaProperty != null &&
            maxAlphaProperty != null &&
            minAlphaProperty.floatValue > maxAlphaProperty.floatValue)
        {
            EditorGUILayout.HelpBox(
                "ドレッド攻撃の着弾予告最小不透明度が最大不透明度を上回っています。",
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
