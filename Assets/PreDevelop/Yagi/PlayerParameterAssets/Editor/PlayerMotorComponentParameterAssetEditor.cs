#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

/// <summary>
/// PlayerMotorComponentParameterAssetのInspector表示を管理します。
/// </summary>
[CustomEditor(typeof(PlayerMotorComponentParameterAsset))]
public sealed class PlayerMotorComponentParameterAssetEditor : Editor
{
    // 障害物検知
    private SerializedProperty m_obstacleLayerMaskProperty;
    private SerializedProperty m_obstacleSkinWidthProperty;
    private SerializedProperty m_obstacleDecelerationPerSecondProperty;

    // 高速衝突
    private SerializedProperty m_collisionLiftPreventionMinSpeedProperty;

    // 段差乗り越え
    private SerializedProperty m_maxStepHeightProperty;
    private SerializedProperty m_stepCheckDistanceProperty;
    private SerializedProperty m_stepLowerCheckHeightProperty;
    private SerializedProperty m_stepForwardOffsetProperty;
    private SerializedProperty m_stepClearanceSkinProperty;

    // Foldout状態
    private bool m_showObstacleSettings = true;
    private bool m_showCollisionSettings = true;
    private bool m_showStepSettings = true;

    /// <summary>
    /// SerializedPropertyを取得します。
    /// </summary>
    private void OnEnable()
    {
        m_obstacleLayerMaskProperty =
            serializedObject.FindProperty(
                "m_obstacleLayerMask");

        m_obstacleSkinWidthProperty =
            serializedObject.FindProperty(
                "m_obstacleSkinWidth");

        m_obstacleDecelerationPerSecondProperty =
            serializedObject.FindProperty(
                "m_obstacleDecelerationPerSecond");

        m_collisionLiftPreventionMinSpeedProperty =
            serializedObject.FindProperty(
                "m_collisionLiftPreventionMinSpeed");

        m_maxStepHeightProperty =
            serializedObject.FindProperty(
                "m_maxStepHeight");

        m_stepCheckDistanceProperty =
            serializedObject.FindProperty(
                "m_stepCheckDistance");

        m_stepLowerCheckHeightProperty =
            serializedObject.FindProperty(
                "m_stepLowerCheckHeight");

        m_stepForwardOffsetProperty =
            serializedObject.FindProperty(
                "m_stepForwardOffset");

        m_stepClearanceSkinProperty =
            serializedObject.FindProperty(
                "m_stepClearanceSkin");
    }

    /// <summary>
    /// Inspectorを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "PlayerMotor固有の物理移動設定です。\n" +
            "障害物への停止、高速衝突時の浮き上がり抑制、" +
            "小段差の自動乗り越えをここで調整します。",
            MessageType.Info);

        EditorGUILayout.Space(6.0f);

        DrawObstacleSettings();

        EditorGUILayout.Space(8.0f);

        DrawCollisionSettings();

        EditorGUILayout.Space(8.0f);

        DrawStepSettings();

        EditorGUILayout.Space(8.0f);

        DrawValidationMessages();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 障害物検知設定を描画します。
    /// </summary>
    private void DrawObstacleSettings()
    {
        m_showObstacleSettings =
            EditorGUILayout.Foldout(
                m_showObstacleSettings,
                "障害物検知",
                true);

        if (!m_showObstacleSettings)
        {
            return;
        }

        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.HelpBox(
            "プレイヤーの進行を止める壁などを判定します。\n" +
            "Obstacle Layerに設定されたColliderが対象です。",
            MessageType.Info);

        DrawProperty(
            m_obstacleLayerMaskProperty,
            "障害物レイヤー",
            "壁など、プレイヤーの進行を止めるColliderのLayerです。");

        DrawProperty(
            m_obstacleSkinWidthProperty,
            "停止余白",
            "障害物へ完全に接触する少し手前で止めるための距離です。");

        DrawProperty(
            m_obstacleDecelerationPerSecondProperty,
            "障害物接触時の減速度",
            "障害物へ近づいた際に、1秒あたりどれだけ速度を下げるかを指定します。");

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 高速衝突設定を描画します。
    /// </summary>
    private void DrawCollisionSettings()
    {
        m_showCollisionSettings =
            EditorGUILayout.Foldout(
                m_showCollisionSettings,
                "高速衝突",
                true);

        if (!m_showCollisionSettings)
        {
            return;
        }

        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.HelpBox(
            "高速移動中に壁や段差へ衝突した際、" +
            "物理反発だけで発生する意図しない上向き速度を抑制します。\n" +
            "ジャンプや吹き飛ばしなど、ゲーム処理が意図して与えた上向き速度は対象外です。",
            MessageType.Info);

        DrawProperty(
            m_collisionLiftPreventionMinSpeedProperty,
            "浮き上がり抑制開始速度",
            "この水平速度以上で衝突した場合に、意図しない上向き速度の抑制を行います。");

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 段差乗り越え設定を描画します。
    /// </summary>
    private void DrawStepSettings()
    {
        m_showStepSettings =
            EditorGUILayout.Foldout(
                m_showStepSettings,
                "小段差の自動乗り越え",
                true);

        if (!m_showStepSettings)
        {
            return;
        }

        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.HelpBox(
            "進行方向にある小段差を検出し、" +
            "上面へ安全に移動できる場合のみ自動で乗り越えます。\n" +
            "段差検出にはGround LayerとObstacle Layerの両方を使用します。",
            MessageType.Info);

        EditorGUILayout.Space(4.0f);

        EditorGUILayout.LabelField(
            "乗り越え可能サイズ",
            EditorStyles.boldLabel);

        DrawProperty(
            m_maxStepHeightProperty,
            "最大段差高さ",
            "自動で乗り越えられる段差の最大高さです。0にするとStep Upが無効になります。");

        EditorGUILayout.Space(6.0f);

        EditorGUILayout.LabelField(
            "前方判定",
            EditorStyles.boldLabel);

        DrawProperty(
            m_stepCheckDistanceProperty,
            "前方探索距離",
            "CapsuleCollider前方から段差候補を探索する距離です。");

        DrawProperty(
            m_stepLowerCheckHeightProperty,
            "下部判定高さ",
            "地面すれすれからこの高さまで複数のRayを飛ばし、段差正面や低い突起を検出します。");

        DrawProperty(
            m_stepForwardOffsetProperty,
            "上面探索間隔",
            "段差正面から奥へ上面を探索するときのサンプル間隔です。小さくすると細かく探索できます。");

        EditorGUILayout.Space(6.0f);

        EditorGUILayout.LabelField(
            "安全確認",
            EditorStyles.boldLabel);

        DrawProperty(
            m_stepClearanceSkinProperty,
            "乗り越え余白",
            "段差上へ移動した後、CapsuleColliderが地形へ食い込まないための余白です。");

        EditorGUILayout.Space(6.0f);

        EditorGUILayout.HelpBox(
            "初期調整の目安\n" +
            "・最大段差高さ: 0.20 ～ 0.35 m\n" +
            "・前方探索距離: 0.15 ～ 0.30 m\n" +
            "・下部判定高さ: 0.02 ～ 0.05 m\n" +
            "・上面探索間隔: 0.03 ～ 0.08 m\n" +
            "・乗り越え余白: 0.01 ～ 0.03 m",
            MessageType.None);

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 設定値に問題がありそうな場合に警告を表示します。
    /// </summary>
    private void DrawValidationMessages()
    {
        if (m_obstacleLayerMaskProperty != null &&
            m_obstacleLayerMaskProperty.intValue == 0)
        {
            EditorGUILayout.HelpBox(
                "障害物レイヤーが未設定です。Obstacleとしての事前減速は行われません。",
                MessageType.Warning);
        }

        if (m_collisionLiftPreventionMinSpeedProperty != null &&
            m_collisionLiftPreventionMinSpeedProperty.floatValue <= 0.0f)
        {
            EditorGUILayout.HelpBox(
                "浮き上がり抑制開始速度が0です。低速衝突でも浮き上がり抑制が動作します。",
                MessageType.Warning);
        }

        if (m_maxStepHeightProperty != null &&
            m_maxStepHeightProperty.floatValue <= 0.0f)
        {
            EditorGUILayout.HelpBox(
                "最大段差高さが0のため、Step Upは無効です。",
                MessageType.Info);

            return;
        }

        if (m_stepLowerCheckHeightProperty != null &&
            m_maxStepHeightProperty != null &&
            m_stepLowerCheckHeightProperty.floatValue >=
            m_maxStepHeightProperty.floatValue)
        {
            EditorGUILayout.HelpBox(
                "下部判定高さが最大段差高さ以上です。段差正面の検出帯が広すぎる可能性があります。",
                MessageType.Warning);
        }

        if (m_stepForwardOffsetProperty != null &&
            m_stepCheckDistanceProperty != null &&
            m_stepForwardOffsetProperty.floatValue >
            m_stepCheckDistanceProperty.floatValue)
        {
            EditorGUILayout.HelpBox(
                "上面探索間隔が前方探索距離より大きくなっています。上面を取りこぼす可能性があります。",
                MessageType.Warning);
        }

        if (m_stepClearanceSkinProperty != null &&
            m_maxStepHeightProperty != null &&
            m_stepClearanceSkinProperty.floatValue >
            m_maxStepHeightProperty.floatValue)
        {
            EditorGUILayout.HelpBox(
                "乗り越え余白が最大段差高さより大きくなっています。値が大きすぎる可能性があります。",
                MessageType.Warning);
        }
    }

    /// <summary>
    /// SerializedPropertyを日本語ラベルとTooltip付きで描画します。
    /// </summary>
    /// <param name="property">描画するプロパティ。</param>
    /// <param name="label">Inspector上の表示名。</param>
    /// <param name="tooltip">Tooltip。</param>
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
