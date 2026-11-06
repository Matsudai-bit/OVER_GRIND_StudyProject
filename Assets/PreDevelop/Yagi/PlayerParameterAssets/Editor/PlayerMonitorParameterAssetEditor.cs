#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

/// <summary>
/// PlayerMonitorParameterAssetを調整しやすく表示します。
/// </summary>
[CustomEditor(typeof(PlayerMonitorParameterAsset))]
public sealed class PlayerMonitorParameterAssetEditor : Editor
{
    private const float SECTION_SPACE = 8.0f;
    private const float MIN_GROUND_NORMAL_SMOOTH_SPEED = 0.01f;
    private const float GROUND_PROBE_SKIN = 0.01f;

    private SerializedProperty m_groundCheckRadiusProperty;
    private SerializedProperty m_groundLayerMaskProperty;

    private SerializedProperty m_groundProbeRadiusProperty;
    private SerializedProperty m_groundProbeStartOffsetProperty;
    private SerializedProperty m_groundProbeDistanceProperty;
    private SerializedProperty m_maxClimbSlopeAngleProperty;
    private SerializedProperty m_groundNormalSmoothSpeedProperty;

    private SerializedProperty m_railLayerMaskProperty;
    private SerializedProperty m_railDetectionParameterProperty;

    private bool m_showGroundCheck = true;
    private bool m_showGroundSurface = true;
    private bool m_showRailDetection = true;

    /// <summary>
    /// SerializedPropertyを取得します。
    /// </summary>
    private void OnEnable()
    {
        m_groundCheckRadiusProperty =
            serializedObject.FindProperty("m_groundCheckRadius");

        m_groundLayerMaskProperty =
            serializedObject.FindProperty("m_groundLayerMask");

        m_groundProbeRadiusProperty =
            serializedObject.FindProperty("m_groundProbeRadius");

        m_groundProbeStartOffsetProperty =
            serializedObject.FindProperty("m_groundProbeStartOffset");

        m_groundProbeDistanceProperty =
            serializedObject.FindProperty("m_groundProbeDistance");

        m_maxClimbSlopeAngleProperty =
            serializedObject.FindProperty("m_maxClimbSlopeAngle");

        m_groundNormalSmoothSpeedProperty =
            serializedObject.FindProperty("m_groundNormalSmoothSpeed");

        m_railLayerMaskProperty =
            serializedObject.FindProperty("m_railLayerMask");

        m_railDetectionParameterProperty =
            serializedObject.FindProperty("m_railDetectionParameter");
    }

    /// <summary>
    /// Inspectorを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDescription();

        EditorGUILayout.Space(SECTION_SPACE);

        DrawGroundCheckParameters();

        EditorGUILayout.Space(SECTION_SPACE);

        DrawGroundSurfaceParameters();

        EditorGUILayout.Space(SECTION_SPACE);

        DrawRailDetectionParameters();

        EditorGUILayout.Space(SECTION_SPACE);

        DrawValidationMessages();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// Parameter Asset全体の役割を表示します。
    /// </summary>
    private void DrawDescription()
    {
        EditorGUILayout.HelpBox(
            "PlayerMonitorが使用する接地・坂道・レール検知設定です。\n" +
            "接地判定は『地面があるか』、接地面検出は『どの面に立っているか』を判定します。",
            MessageType.Info);
    }

    /// <summary>
    /// 接地判定パラメータを描画します。
    /// </summary>
    private void DrawGroundCheckParameters()
    {
        m_showGroundCheck =
            EditorGUILayout.Foldout(
                m_showGroundCheck,
                "接地判定",
                true);

        if (!m_showGroundCheck)
        {
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.HelpBox(
            "プレイヤーの足元に地面が存在するかを判定します。\n" +
            "ジャンプ可能判定や地上・空中状態の切り替えに使用します。",
            MessageType.Info);

        DrawProperty(
            m_groundCheckRadiusProperty,
            "接地判定半径",
            "Ground Check Originを中心に、地面の存在を確認する球判定の半径です。");

        DrawProperty(
            m_groundLayerMaskProperty,
            "地面レイヤー",
            "接地対象として扱うLayerを指定します。床や坂を含むLayerを設定してください。");

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 接地面・坂道パラメータを描画します。
    /// </summary>
    private void DrawGroundSurfaceParameters()
    {
        m_showGroundSurface =
            EditorGUILayout.Foldout(
                m_showGroundSurface,
                "接地面検出・坂道",
                true);

        if (!m_showGroundSurface)
        {
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.HelpBox(
            "足元へSphereCastを行い、現在プレイヤーを支えている面を選択します。\n" +
            "取得した面の法線を使い、平地・坂道に沿った移動方向を計算します。",
            MessageType.Info);

        EditorGUILayout.LabelField(
            "Ground Probe",
            EditorStyles.boldLabel);

        DrawProperty(
            m_groundProbeRadiusProperty,
            "Probe半径",
            "接地面を調べるSphereCastの半径です。小さすぎると継ぎ目を拾いにくく、大きすぎると周囲の面を拾いやすくなります。");

        DrawProperty(
            m_groundProbeStartOffsetProperty,
            "Probe開始高さ",
            "Ground Check OriginからSphereCast開始位置を上方向へずらす距離です。");

        DrawProperty(
            m_groundProbeDistanceProperty,
            "Probe探索距離",
            "Ground Check Originより下方向へ、接地面を探索する追加距離です。");

        DrawGroundProbePreview();

        EditorGUILayout.Space(4.0f);

        EditorGUILayout.LabelField(
            "坂道",
            EditorStyles.boldLabel);

        DrawProperty(
            m_maxClimbSlopeAngleProperty,
            "最大登坂角度",
            "地面として扱い、プレイヤーが移動できる最大斜面角度です。これを超える面は支持面候補から除外します。");

        DrawProperty(
            m_groundNormalSmoothSpeedProperty,
            "法線追従速度",
            "平面と坂などで支持面が切り替わった際に、移動用の接地面法線が追従する最大角速度です。単位は度/秒です。");

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// レール搭乗判定パラメータを描画します。
    /// </summary>
    private void DrawRailDetectionParameters()
    {
        m_showRailDetection =
            EditorGUILayout.Foldout(
                m_showRailDetection,
                "レール搭乗判定",
                true);

        if (!m_showRailDetection)
        {
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.HelpBox(
            "プレイヤー周辺に搭乗可能なレールが存在するかを判定します。\n" +
            "地上時と空中時で判定半径を分ける場合は、レール搭乗判定パラメータを設定します。",
            MessageType.Info);

        DrawProperty(
            m_railLayerMaskProperty,
            "レールレイヤー",
            "レールとして検出するLayerを指定します。");

        DrawProperty(
            m_railDetectionParameterProperty,
            "レール搭乗判定パラメータ",
            "地上・空中それぞれのレール搭乗判定半径を設定するParameter Assetです。");

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Ground Probeの実効値を表示します。
    /// </summary>
    private void DrawGroundProbePreview()
    {
        float probeRadius =
            Mathf.Max(
                0.01f,
                m_groundProbeRadiusProperty.floatValue);

        float requestedStartOffset =
            Mathf.Max(
                0.0f,
                m_groundProbeStartOffsetProperty.floatValue);

        float effectiveStartOffset =
            Mathf.Max(
                requestedStartOffset,
                probeRadius + GROUND_PROBE_SKIN);

        float probeDistance =
            Mathf.Max(
                0.01f,
                m_groundProbeDistanceProperty.floatValue);

        float totalCastDistance =
            effectiveStartOffset + probeDistance;

        EditorGUILayout.Space(4.0f);

        EditorGUILayout.LabelField(
            "Ground Probe確認",
            EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.FloatField(
                new GUIContent(
                    "実効開始高さ",
                    "SphereCast開始時に地面へめり込まないよう、Probe半径を考慮して実際に使用される開始高さです。"),
                effectiveStartOffset);

            EditorGUILayout.FloatField(
                new GUIContent(
                    "Cast総距離",
                    "SphereCast開始位置から下方向へ調べる合計距離です。"),
                totalCastDistance);
        }
    }

    /// <summary>
    /// パラメータ設定の注意を表示します。
    /// </summary>
    private void DrawValidationMessages()
    {
        bool hasWarning = false;

        if (m_groundLayerMaskProperty.intValue == 0)
        {
            EditorGUILayout.HelpBox(
                "地面レイヤーが未設定です。接地判定と接地面検出ができません。",
                MessageType.Warning);

            hasWarning = true;
        }

        float minimumStartOffset =
            Mathf.Max(
                0.01f,
                m_groundProbeRadiusProperty.floatValue) +
            GROUND_PROBE_SKIN;

        if (m_groundProbeStartOffsetProperty.floatValue <
            minimumStartOffset)
        {
            EditorGUILayout.HelpBox(
                $"Probe開始高さがProbe半径より小さいため、実行時は {minimumStartOffset:F2} 以上に補正されます。",
                MessageType.Info);
        }

        if (m_groundNormalSmoothSpeedProperty.floatValue <
            MIN_GROUND_NORMAL_SMOOTH_SPEED)
        {
            EditorGUILayout.HelpBox(
                "法線追従速度が0に近いため、支持面が切り替わっても移動用法線が追従しません。0より大きい値を設定してください。",
                MessageType.Warning);

            hasWarning = true;
        }

        if (m_maxClimbSlopeAngleProperty.floatValue >= 80.0f)
        {
            EditorGUILayout.HelpBox(
                "最大登坂角度が非常に大きいため、壁に近い急斜面まで地面として扱う可能性があります。",
                MessageType.Warning);

            hasWarning = true;
        }

        if (m_railLayerMaskProperty.intValue == 0)
        {
            EditorGUILayout.HelpBox(
                "レールレイヤーが未設定です。レール搭乗判定は行われません。",
                MessageType.Warning);

            hasWarning = true;
        }

        if (m_railDetectionParameterProperty.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox(
                "レール搭乗判定パラメータが未設定の場合、地上・空中ともに接地判定半径をレール検出半径として使用します。",
                MessageType.Info);
        }

        if (!hasWarning)
        {
            EditorGUILayout.HelpBox(
                "現在の設定に明確な問題はありません。",
                MessageType.None);
        }
    }

    /// <summary>
    /// SerializedPropertyを指定ラベルとTooltipで描画します。
    /// </summary>
    private void DrawProperty(
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

#endif
