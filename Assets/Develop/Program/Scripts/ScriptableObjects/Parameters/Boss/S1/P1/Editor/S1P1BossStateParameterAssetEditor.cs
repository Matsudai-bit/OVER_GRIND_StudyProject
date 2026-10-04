using UnityEditor;
using UnityEngine;

/// <summary>
/// S1P1BossStateParameterAssetを調整しやすく表示します。
/// </summary>
[CustomEditor(typeof(S1P1BossStateParameterAsset))]
public sealed class S1P1BossStateParameterAssetEditor : Editor
{
    private SerializedProperty m_walkProperty;
    private SerializedProperty m_turnProperty;
    private SerializedProperty m_stompProperty;
    private SerializedProperty m_missileProperty;
    private SerializedProperty m_heatVentProperty;
    private SerializedProperty m_legsCollapsingProperty;

    private bool m_showWalk = true;
    private bool m_showTurn = true;
    private bool m_showStomp = true;
    private bool m_showMissile = true;
    private bool m_showHeatVent = true;
    private bool m_showLegsCollapsing = true;
    private bool m_showMissileBodyParameters = true;

    private Editor m_missileParameterEditor;
    private MissileParameterAsset m_currentMissileParameterAsset;

    /// <summary>
    /// SerializedPropertyを取得します。
    /// </summary>
    private void OnEnable()
    {
        m_walkProperty =
            serializedObject.FindProperty("m_walk");

        m_turnProperty =
            serializedObject.FindProperty("m_turn");

        m_stompProperty =
            serializedObject.FindProperty("m_stomp");

        m_missileProperty =
            serializedObject.FindProperty("m_missile");

        m_heatVentProperty =
            serializedObject.FindProperty("m_heatVent");

        m_legsCollapsingProperty =
            serializedObject.FindProperty("m_legsCollapsing");
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

        DrawWalkParameters();
        EditorGUILayout.Space(6.0f);

        DrawTurnParameters();
        EditorGUILayout.Space(6.0f);

        DrawStompParameters();
        EditorGUILayout.Space(6.0f);

        DrawMissileParameters();
        EditorGUILayout.Space(6.0f);

        DrawHeatVentParameters();
        EditorGUILayout.Space(6.0f);

        DrawLegsCollapsingParameters();
        EditorGUILayout.Space(6.0f);

        DrawValidationMessages();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawWalkParameters()
    {
        m_showWalk =
            EditorGUILayout.Foldout(
                m_showWalk,
                "歩行",
                true);

        if (!m_showWalk)
        {
            return;
        }

        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        DrawRelativeProperty(
            m_walkProperty,
            "m_walkDuration",
            "歩行時間",
            "歩行状態を継続する時間です。");

        DrawRelativeProperty(
            m_walkProperty,
            "m_forwardCheckDistance",
            "前方確認距離",
            "前方が通行可能か確認する距離です。");

        DrawRelativeProperty(
            m_walkProperty,
            "m_blockedIdleDuration",
            "通行不能時の停止時間",
            "前方へ進めない場合に遷移する停止状態の継続時間です。");

        EditorGUILayout.EndVertical();
    }

    private void DrawTurnParameters()
    {
        m_showTurn =
            EditorGUILayout.Foldout(
                m_showTurn,
                "方向転換",
                true);

        if (!m_showTurn)
        {
            return;
        }

        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        DrawRelativeProperty(
            m_turnProperty,
            "m_rotationSpeed",
            "回転速度",
            "方向転換時の回転速度です。単位は度/秒です。");

        EditorGUILayout.EndVertical();
    }

    private void DrawStompParameters()
    {
        m_showStomp =
            EditorGUILayout.Foldout(
                m_showStomp,
                "踏みつけ",
                true);

        if (!m_showStomp)
        {
            return;
        }

        EditorGUILayout.HelpBox(
            "現在、踏みつけ状態に固有の調整パラメータはありません。",
            MessageType.Info);
    }

    private void DrawMissileParameters()
    {
        m_showMissile =
            EditorGUILayout.Foldout(
                m_showMissile,
                "ミサイル",
                true);

        if (!m_showMissile)
        {
            return;
        }

        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        DrawRelativeProperty(
            m_missileProperty,
            "m_launchInterval",
            "発射間隔",
            "次のミサイルを発射するまでの間隔です。");

        DrawRelativeProperty(
            m_missileProperty,
            "m_missileUpwardDuration",
            "上昇時間",
            "発射直後に上方向へ進んでからホーミングへ移行するまでの時間です。");

        DrawRelativeProperty(
            m_missileProperty,
            "m_idleDuration",
            "攻撃終了後の停止時間",
            "全ミサイル発射後の停止状態の継続時間です。");

        SerializedProperty missileParameterAssetProperty =
            m_missileProperty?.FindPropertyRelative(
                "m_missileParameterAsset");

        if (missileParameterAssetProperty != null)
        {
            EditorGUILayout.PropertyField(
                missileParameterAssetProperty,
                new GUIContent(
                    "ミサイル本体パラメータ",
                    "MissileMotorとMissileSteeringで使用するパラメータです。"));
        }

        EditorGUILayout.EndVertical();

        DrawMissileBodyParameterEditor(
            missileParameterAssetProperty);
    }

    private void DrawHeatVentParameters()
    {
        m_showHeatVent =
            EditorGUILayout.Foldout(
                m_showHeatVent,
                "排熱",
                true);

        if (!m_showHeatVent)
        {
            return;
        }

        EditorGUILayout.HelpBox(
            "現在、排熱状態に固有の調整パラメータはありません。今後追加する場合はS1P1BossHeatVentStateParametersへ集約します。",
            MessageType.Info);
    }

    private void DrawLegsCollapsingParameters()
    {
        m_showLegsCollapsing =
            EditorGUILayout.Foldout(
                m_showLegsCollapsing,
                "脚崩壊・フェーズ移行",
                true);

        if (!m_showLegsCollapsing)
        {
            return;
        }

        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        DrawRelativeProperty(
            m_legsCollapsingProperty,
            "m_transitionDuration",
            "フェーズ移行待機時間",
            "脚崩壊開始から次フェーズへ移行するまでの待機時間です。");

        EditorGUILayout.EndVertical();
    }

    private void DrawMissileBodyParameterEditor(
        SerializedProperty missileParameterAssetProperty)
    {
        MissileParameterAsset missileParameterAsset =
            missileParameterAssetProperty?.objectReferenceValue
            as MissileParameterAsset;

        if (missileParameterAsset == null)
        {
            ReleaseMissileParameterEditor();

            EditorGUILayout.HelpBox(
                "ミサイル本体パラメータを設定してください。",
                MessageType.Warning);

            return;
        }

        m_showMissileBodyParameters =
            EditorGUILayout.Foldout(
                m_showMissileBodyParameters,
                "ミサイル本体パラメータを編集",
                true);

        if (!m_showMissileBodyParameters)
        {
            return;
        }

        EnsureMissileParameterEditor(
            missileParameterAsset);

        EditorGUI.indentLevel++;

        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        m_missileParameterEditor?.OnInspectorGUI();

        EditorGUILayout.EndVertical();

        EditorGUI.indentLevel--;
    }

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

    private void DrawValidationMessages()
    {
        SerializedProperty rotationSpeed =
            m_turnProperty?.FindPropertyRelative(
                "m_rotationSpeed");

        if (rotationSpeed != null &&
            Mathf.Approximately(
                rotationSpeed.floatValue,
                0.0f))
        {
            EditorGUILayout.HelpBox(
                "回転速度が0のため、方向転換状態を完了できません。",
                MessageType.Warning);
        }
    }

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
