using UnityEditor;
using UnityEngine;

/// <summary>
/// S1P2BossStateParameterAssetを調整しやすく表示します。
/// </summary>
[CustomEditor(typeof(S1P2BossStateParameterAsset))]
public sealed class S1P2BossStateParameterAssetEditor : Editor
{
    // 移動状態のパラメータ
    private SerializedProperty m_moveProperty;

    // ミサイル状態のパラメータ
    private SerializedProperty m_missileProperty;

    // 移動ミサイル状態のパラメータ設定
    private SerializedProperty m_moveMissileProperty;

    // 排熱状態のパラメータ
    private SerializedProperty m_heatVentProperty;

    // 各セクションの表示状態
    private bool m_showMove = true;
    private bool m_showMissile = true;
    private bool m_showMoveMissile = true;
    private bool m_showHeatVent = true;

    // ミサイル本体パラメータの表示状態
    private bool m_showSharedMissileBodyParameters = true;
    private bool m_showOverrideMissileBodyParameters = true;

    // 共通ミサイル本体パラメータ用Editor
    private Editor m_sharedMissileParameterEditor;

    // 専用ミサイル本体パラメータ用Editor
    private Editor m_overrideMissileParameterEditor;

    // Editor生成対象
    private MissileParameterAsset m_currentSharedMissileParameterAsset;
    private MissileParameterAsset m_currentOverrideMissileParameterAsset;

    /// <summary>
    /// SerializedPropertyを取得します。
    /// </summary>
    private void OnEnable()
    {
        m_moveProperty =
            serializedObject.FindProperty(
                "m_move");

        m_missileProperty =
            serializedObject.FindProperty(
                "m_missile");

        m_moveMissileProperty =
            serializedObject.FindProperty(
                "m_moveMissile");

        m_heatVentProperty =
            serializedObject.FindProperty(
                "m_heatVent");
    }

    /// <summary>
    /// 生成したEditorを破棄します。
    /// </summary>
    private void OnDisable()
    {
        ReleaseSharedMissileParameterEditor();
        ReleaseOverrideMissileParameterEditor();
    }

    /// <summary>
    /// Inspectorを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawMoveParameters();
        EditorGUILayout.Space(6.0f);

        DrawMissileParameters();
        EditorGUILayout.Space(6.0f);

        DrawMoveMissileParameters();
        EditorGUILayout.Space(6.0f);

        DrawHeatVentParameters();
        EditorGUILayout.Space(6.0f);

        DrawValidationMessages();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 通常の移動状態パラメータを描画します。
    /// </summary>
    private void DrawMoveParameters()
    {
        m_showMove =
            EditorGUILayout.Foldout(
                m_showMove,
                "移動",
                true);

        if (!m_showMove)
        {
            return;
        }

        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        DrawMoveParameterFields(
            m_moveProperty);

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 通常のミサイル状態パラメータを描画します。
    /// </summary>
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

        SerializedProperty missileParameterAssetProperty =
            DrawMissileParameterFields(
                m_missileProperty);

        EditorGUILayout.EndVertical();

        DrawMissileBodyParameterEditor(
            missileParameterAssetProperty,
            ref m_showSharedMissileBodyParameters,
            ref m_sharedMissileParameterEditor,
            ref m_currentSharedMissileParameterAsset,
            "ミサイル本体パラメータを編集");
    }

    /// <summary>
    /// 移動ミサイル状態のパラメータ設定を描画します。
    /// </summary>
    private void DrawMoveMissileParameters()
    {
        m_showMoveMissile =
            EditorGUILayout.Foldout(
                m_showMoveMissile,
                "移動ミサイル",
                true);

        if (!m_showMoveMissile)
        {
            return;
        }

        if (m_moveMissileProperty == null)
        {
            return;
        }

        SerializedProperty useSharedMoveProperty =
            m_moveMissileProperty.FindPropertyRelative(
                "m_useSharedMoveParameters");

        SerializedProperty useSharedMissileProperty =
            m_moveMissileProperty.FindPropertyRelative(
                "m_useSharedMissileParameters");

        SerializedProperty overrideMoveProperty =
            m_moveMissileProperty.FindPropertyRelative(
                "m_moveParameters");

        SerializedProperty overrideMissileProperty =
            m_moveMissileProperty.FindPropertyRelative(
                "m_missileParameters");

        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "共通設定",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            useSharedMoveProperty,
            new GUIContent(
                "移動パラメータを共通で使用",
                "ONの場合は通常の「移動」状態と同じパラメータを使用します。"));

        EditorGUILayout.PropertyField(
            useSharedMissileProperty,
            new GUIContent(
                "ミサイルパラメータを共通で使用",
                "ONの場合は通常の「ミサイル」状態と同じパラメータを使用します。"));

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(4.0f);

        DrawMoveMissileMoveSection(
            useSharedMoveProperty,
            overrideMoveProperty);

        EditorGUILayout.Space(4.0f);

        DrawMoveMissileMissileSection(
            useSharedMissileProperty,
            overrideMissileProperty);
    }

    /// <summary>
    /// 移動ミサイル状態の移動パラメータを描画します。
    /// </summary>
    /// <param name="useSharedProperty">共通使用フラグ。</param>
    /// <param name="overrideProperty">専用パラメータ。</param>
    private void DrawMoveMissileMoveSection(
        SerializedProperty useSharedProperty,
        SerializedProperty overrideProperty)
    {
        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "移動パラメータ",
            EditorStyles.boldLabel);

        if (useSharedProperty != null &&
            useSharedProperty.boolValue)
        {
            EditorGUILayout.HelpBox(
                "通常の「移動」状態で設定したパラメータを使用します。",
                MessageType.Info);

            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.HelpBox(
            "移動ミサイル状態専用の移動パラメータを使用します。",
            MessageType.Info);

        DrawMoveParameterFields(
            overrideProperty);

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 移動ミサイル状態のミサイルパラメータを描画します。
    /// </summary>
    /// <param name="useSharedProperty">共通使用フラグ。</param>
    /// <param name="overrideProperty">専用パラメータ。</param>
    private void DrawMoveMissileMissileSection(
        SerializedProperty useSharedProperty,
        SerializedProperty overrideProperty)
    {
        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "ミサイルパラメータ",
            EditorStyles.boldLabel);

        if (useSharedProperty != null &&
            useSharedProperty.boolValue)
        {
            EditorGUILayout.HelpBox(
                "通常の「ミサイル」状態で設定したパラメータを使用します。",
                MessageType.Info);

            EditorGUILayout.EndVertical();

            ReleaseOverrideMissileParameterEditor();
            return;
        }

        EditorGUILayout.HelpBox(
            "移動ミサイル状態専用のミサイルパラメータを使用します。",
            MessageType.Info);

        SerializedProperty missileParameterAssetProperty =
            DrawMissileParameterFields(
                overrideProperty);

        EditorGUILayout.EndVertical();

        DrawMissileBodyParameterEditor(
            missileParameterAssetProperty,
            ref m_showOverrideMissileBodyParameters,
            ref m_overrideMissileParameterEditor,
            ref m_currentOverrideMissileParameterAsset,
            "専用ミサイル本体パラメータを編集");
    }

    /// <summary>
    /// 排熱状態のパラメータを描画します。
    /// </summary>
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
            "現在、S1P2の排熱状態に固有の調整パラメータはありません。",
            MessageType.Info);
    }

    /// <summary>
    /// 移動パラメータ一式を描画します。
    /// </summary>
    /// <param name="moveProperty">移動パラメータ。</param>
    private void DrawMoveParameterFields(
        SerializedProperty moveProperty)
    {
        if (moveProperty == null)
        {
            return;
        }

        EditorGUILayout.LabelField(
            "経路生成",
            EditorStyles.boldLabel);

        DrawRelativeProperty(
            moveProperty,
            "m_minRoutePointCount",
            "最小経路地点数",
            "生成する経路で使用する地点数の最小値です。");

        DrawRelativeProperty(
            moveProperty,
            "m_maxRoutePointCount",
            "最大経路地点数",
            "生成する経路で使用する地点数の最大値です。");

        DrawRelativeProperty(
            moveProperty,
            "m_requiredFarPointDistance",
            "遠距離地点の要求距離",
            "最低1地点に要求するBossからの距離です。");

        DrawRelativeProperty(
            moveProperty,
            "m_routeSearchAttemptCount",
            "経路探索試行回数",
            "有効な経路を生成する最大試行回数です。");

        DrawRelativeProperty(
            moveProperty,
            "m_routeSampleInterval",
            "経路サンプル間隔",
            "経路を生成・検証するときのサンプル間隔です。");

        DrawRelativeProperty(
            moveProperty,
            "m_initialDirectionGuideDistance",
            "開始方向ガイド距離",
            "開始時の進行方向を曲線へ反映する距離です。");

        EditorGUILayout.Space(4.0f);

        EditorGUILayout.LabelField(
            "経路形状",
            EditorStyles.boldLabel);

        DrawRelativeProperty(
            moveProperty,
            "m_minRouteSegmentDistance",
            "最小区間距離",
            "連続する経路地点間に要求する最低距離です。");

        DrawRelativeProperty(
            moveProperty,
            "m_maxRouteTurnAngle",
            "最大旋回角度",
            "経路地点で許容する最大旋回角度です。");

        EditorGUILayout.Space(4.0f);

        EditorGUILayout.LabelField(
            "移動",
            EditorStyles.boldLabel);

        DrawRelativeProperty(
            moveProperty,
            "m_maxMoveSpeed",
            "最高移動速度",
            "移動状態で使用する最高速度です。");

        DrawRelativeProperty(
            moveProperty,
            "m_timeToMaxSpeed",
            "最高速度到達時間",
            "停止状態から最高速度へ到達する時間です。");

        DrawRelativeProperty(
            moveProperty,
            "m_timeToStop",
            "停止時間",
            "最高速度から停止するまでの時間です。");

        DrawRelativeProperty(
            moveProperty,
            "m_rotationSpeed",
            "回転速度",
            "1秒間に回転できる最大角度です。");

        EditorGUILayout.Space(4.0f);

        EditorGUILayout.LabelField(
            "旋回",
            EditorStyles.boldLabel);

        DrawRelativeProperty(
            moveProperty,
            "m_turnSlowdownStartAngle",
            "旋回減速開始角度",
            "旋回による減速を開始する角度です。");

        DrawRelativeProperty(
            moveProperty,
            "m_turnInPlaceAngle",
            "その場旋回角度",
            "その場旋回へ切り替える角度です。");

        EditorGUILayout.Space(4.0f);

        EditorGUILayout.LabelField(
            "経路追従",
            EditorStyles.boldLabel);

        DrawRelativeProperty(
            moveProperty,
            "m_lookAheadDistance",
            "先読み距離",
            "経路上で移動方向を決めるために先読みする距離です。");

        DrawRelativeProperty(
            moveProperty,
            "m_arrivalDistance",
            "到着判定距離",
            "最終地点へ到着したと判定する距離です。");
    }

    /// <summary>
    /// ミサイルパラメータ一式を描画します。
    /// </summary>
    /// <param name="missileProperty">ミサイルパラメータ。</param>
    /// <returns>ミサイル本体パラメータのSerializedProperty。</returns>
    private SerializedProperty DrawMissileParameterFields(
        SerializedProperty missileProperty)
    {
        if (missileProperty == null)
        {
            return null;
        }

        DrawRelativeProperty(
            missileProperty,
            "m_launchInterval",
            "発射間隔",
            "次のミサイルを発射するまでの間隔です。");

        DrawRelativeProperty(
            missileProperty,
            "m_missileUpwardDuration",
            "上昇時間",
            "発射直後に上方向へ進んでからホーミングへ移行するまでの時間です。");

        DrawRelativeProperty(
            missileProperty,
            "m_idleDuration",
            "攻撃終了後の停止時間",
            "全ミサイル発射後の停止状態の継続時間です。");

        DrawRelativeProperty(
            missileProperty,
            "m_volleyCount",
            "連続発射周回数",
            "LaunchSitesを何周してミサイルを発射するか指定します。");

        SerializedProperty missileParameterAssetProperty =
            missileProperty.FindPropertyRelative(
                "m_missileParameterAsset");

        if (missileParameterAssetProperty != null)
        {
            EditorGUILayout.PropertyField(
                missileParameterAssetProperty,
                new GUIContent(
                    "ミサイル本体パラメータ",
                    "MissileMotorとMissileSteeringで使用するパラメータです。"));
        }

        return missileParameterAssetProperty;
    }

    /// <summary>
    /// ミサイル本体のParameterAssetをInspector内へ展開します。
    /// </summary>
    /// <param name="missileParameterAssetProperty">
    /// ミサイル本体ParameterAsset。
    /// </param>
    /// <param name="isExpanded">表示状態。</param>
    /// <param name="cachedEditor">キャッシュするEditor。</param>
    /// <param name="currentAsset">現在の対象Asset。</param>
    /// <param name="foldoutLabel">Foldoutの表示名。</param>
    private void DrawMissileBodyParameterEditor(
        SerializedProperty missileParameterAssetProperty,
        ref bool isExpanded,
        ref Editor cachedEditor,
        ref MissileParameterAsset currentAsset,
        string foldoutLabel)
    {
        MissileParameterAsset missileParameterAsset =
            missileParameterAssetProperty?.objectReferenceValue
            as MissileParameterAsset;

        if (missileParameterAsset == null)
        {
            ReleaseMissileParameterEditor(
                ref cachedEditor,
                ref currentAsset);

            EditorGUILayout.HelpBox(
                "ミサイル本体パラメータを設定してください。",
                MessageType.Warning);

            return;
        }

        isExpanded =
            EditorGUILayout.Foldout(
                isExpanded,
                foldoutLabel,
                true);

        if (!isExpanded)
        {
            return;
        }

        EnsureMissileParameterEditor(
            missileParameterAsset,
            ref cachedEditor,
            ref currentAsset);

        EditorGUI.indentLevel++;

        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        cachedEditor?.OnInspectorGUI();

        EditorGUILayout.EndVertical();

        EditorGUI.indentLevel--;
    }

    /// <summary>
    /// ミサイルパラメータ用Editorを準備します。
    /// </summary>
    /// <param name="missileParameterAsset">編集するAsset。</param>
    /// <param name="cachedEditor">キャッシュするEditor。</param>
    /// <param name="currentAsset">現在の対象Asset。</param>
    private void EnsureMissileParameterEditor(
        MissileParameterAsset missileParameterAsset,
        ref Editor cachedEditor,
        ref MissileParameterAsset currentAsset)
    {
        if (currentAsset ==
                missileParameterAsset &&
            cachedEditor != null)
        {
            return;
        }

        ReleaseMissileParameterEditor(
            ref cachedEditor,
            ref currentAsset);

        currentAsset =
            missileParameterAsset;

        CreateCachedEditor(
            missileParameterAsset,
            null,
            ref cachedEditor);
    }

    /// <summary>
    /// 共通ミサイル用Editorを破棄します。
    /// </summary>
    private void ReleaseSharedMissileParameterEditor()
    {
        ReleaseMissileParameterEditor(
            ref m_sharedMissileParameterEditor,
            ref m_currentSharedMissileParameterAsset);
    }

    /// <summary>
    /// 移動ミサイル専用ミサイル用Editorを破棄します。
    /// </summary>
    private void ReleaseOverrideMissileParameterEditor()
    {
        ReleaseMissileParameterEditor(
            ref m_overrideMissileParameterEditor,
            ref m_currentOverrideMissileParameterAsset);
    }

    /// <summary>
    /// ミサイルパラメータ用Editorを破棄します。
    /// </summary>
    /// <param name="cachedEditor">破棄するEditor。</param>
    /// <param name="currentAsset">現在の対象Asset。</param>
    private void ReleaseMissileParameterEditor(
        ref Editor cachedEditor,
        ref MissileParameterAsset currentAsset)
    {
        if (cachedEditor != null)
        {
            DestroyImmediate(
                cachedEditor);

            cachedEditor = null;
        }

        currentAsset = null;
    }

    /// <summary>
    /// パラメータ設定の注意を表示します。
    /// </summary>
    private void DrawValidationMessages()
    {
        ValidateMoveParameters(
            m_moveProperty,
            "移動");

        if (m_moveMissileProperty == null)
        {
            return;
        }

        SerializedProperty useSharedMoveProperty =
            m_moveMissileProperty.FindPropertyRelative(
                "m_useSharedMoveParameters");

        SerializedProperty overrideMoveProperty =
            m_moveMissileProperty.FindPropertyRelative(
                "m_moveParameters");

        if (useSharedMoveProperty != null &&
            !useSharedMoveProperty.boolValue)
        {
            ValidateMoveParameters(
                overrideMoveProperty,
                "移動ミサイル専用の移動");
        }
    }

    /// <summary>
    /// 移動パラメータの設定内容を確認します。
    /// </summary>
    /// <param name="moveProperty">確認する移動パラメータ。</param>
    /// <param name="displayName">警告に表示する名称。</param>
    private void ValidateMoveParameters(
        SerializedProperty moveProperty,
        string displayName)
    {
        if (moveProperty == null)
        {
            return;
        }

        SerializedProperty minRoutePointCountProperty =
            moveProperty.FindPropertyRelative(
                "m_minRoutePointCount");

        SerializedProperty maxRoutePointCountProperty =
            moveProperty.FindPropertyRelative(
                "m_maxRoutePointCount");

        if (minRoutePointCountProperty != null &&
            maxRoutePointCountProperty != null &&
            minRoutePointCountProperty.intValue >
            maxRoutePointCountProperty.intValue)
        {
            EditorGUILayout.HelpBox(
                $"{displayName}：最小経路地点数が最大経路地点数を上回っています。",
                MessageType.Warning);
        }

        SerializedProperty turnSlowdownStartAngleProperty =
            moveProperty.FindPropertyRelative(
                "m_turnSlowdownStartAngle");

        SerializedProperty turnInPlaceAngleProperty =
            moveProperty.FindPropertyRelative(
                "m_turnInPlaceAngle");

        if (turnSlowdownStartAngleProperty != null &&
            turnInPlaceAngleProperty != null &&
            turnSlowdownStartAngleProperty.floatValue >
            turnInPlaceAngleProperty.floatValue)
        {
            EditorGUILayout.HelpBox(
                $"{displayName}：旋回減速開始角度が、その場旋回角度を上回っています。",
                MessageType.Warning);
        }
    }

    /// <summary>
    /// 子プロパティを描画します。
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
