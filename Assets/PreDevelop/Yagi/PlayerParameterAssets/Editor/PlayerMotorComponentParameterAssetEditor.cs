#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

/// <summary>
/// PlayerMotorComponentParameterAssetを調整しやすく表示します。
/// </summary>
[CustomEditor(typeof(PlayerMotorComponentParameterAsset))]
public sealed class PlayerMotorComponentParameterAssetEditor : Editor
{
    private SerializedProperty m_obstacleLayerMaskProperty;
    private SerializedProperty m_obstacleSkinWidthProperty;
    private SerializedProperty m_obstacleDecelerationPerSecondProperty;

    private SerializedProperty m_maxStepHeightProperty;
    private SerializedProperty m_stepCheckDistanceProperty;
    private SerializedProperty m_stepLowerCheckHeightProperty;
    private SerializedProperty m_stepForwardOffsetProperty;
    private SerializedProperty m_stepClearanceSkinProperty;

    private bool m_showObstacle = true;
    private bool m_showStep = true;

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
            "PlayerMotor本体の衝突処理を調整します。\n" +
            "通常の移動速度・加減速とは別に、障害物への停止と小段差の自動乗り越えを設定します。",
            MessageType.Info);

        EditorGUILayout.Space(6.0f);

        DrawObstacleParameters();

        EditorGUILayout.Space(8.0f);

        DrawStepParameters();

        EditorGUILayout.Space(8.0f);

        DrawValidationMessages();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 障害物検知設定を描画します。
    /// </summary>
    private void DrawObstacleParameters()
    {
        m_showObstacle =
            EditorGUILayout.Foldout(
                m_showObstacle,
                "障害物検知",
                true);

        if (!m_showObstacle)
        {
            return;
        }

        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.HelpBox(
            "プレイヤーの進行を止める壁などを判定します。\n" +
            "坂や通常の床は原則としてObstacle Layerに含めないでください。",
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
            "障害物が前方にある場合に、1秒あたりどれだけ速度を下げるかを指定します。");

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 小段差乗り越え設定を描画します。
    /// </summary>
    private void DrawStepParameters()
    {
        m_showStep =
            EditorGUILayout.Foldout(
                m_showStep,
                "小段差の自動乗り越え",
                true);

        if (!m_showStep)
        {
            return;
        }

        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        EditorGUILayout.HelpBox(
            "前方下部にColliderがあり、最大段差高さより上が空いていて、" +
            "その奥に登坂可能な上面がある場合だけ自動でStep Upします。\n" +
            "ジャンプではなく、必要な高さだけRigidbody位置を補正します。",
            MessageType.Info);

        EditorGUILayout.HelpBox(
            "段差判定のLayerは、PlayerMonitorのGround Layerと" +
            "MotorのObstacle Layerを自動的に合成して使用します。",
            MessageType.None);

        EditorGUILayout.Space(4.0f);

        EditorGUILayout.LabelField(
            "乗り越え可能サイズ",
            EditorStyles.boldLabel);

        DrawProperty(
            m_maxStepHeightProperty,
            "最大段差高さ",
            "自動で乗り越えられる段差の最大高さです。0にするとStep Upは無効になります。");

        EditorGUILayout.Space(4.0f);

        EditorGUILayout.LabelField(
            "前方判定",
            EditorStyles.boldLabel);

        DrawProperty(
            m_stepCheckDistanceProperty,
            "前方探索距離",
            "CapsuleCollider前面から、段差の垂直面を探す距離です。");

        DrawProperty(
            m_stepLowerCheckHeightProperty,
            "下部判定高さ",
            "地面すれすれからこの高さまで複数のRayで段差正面を確認します。低い突起の取りこぼしを減らすための検出帯の高さです。");

        DrawProperty(
            m_stepForwardOffsetProperty,
            "上面探索オフセット",
            "段差の正面よりどれだけ奥で上面を探すかを指定します。");

        EditorGUILayout.Space(4.0f);

        EditorGUILayout.LabelField(
            "安全確認",
            EditorStyles.boldLabel);

        DrawProperty(
            m_stepClearanceSkinProperty,
            "乗り越え余白",
            "段差上へ移動した際に、CapsuleColliderが床や壁へめり込まないための余白です。");

        EditorGUILayout.Space(4.0f);

        EditorGUILayout.HelpBox(
            "初期調整の目安\n" +
            "・最大段差高さ: 0.20 ～ 0.35 m\n" +
            "・前方探索距離: 0.15 ～ 0.30 m\n" +
            "・下部判定高さ: 0.02 ～ 0.05 m（範囲内を複数Rayで確認）\n" +
            "・上面探索オフセット: 0.03 ～ 0.08 m",
            MessageType.None);

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 設定値の注意を表示します。
    /// </summary>
    private void DrawValidationMessages()
    {
        if (m_obstacleLayerMaskProperty != null &&
            m_obstacleLayerMaskProperty.intValue == 0)
        {
            EditorGUILayout.HelpBox(
                "障害物レイヤーが未設定です。壁への事前減速は行われません。",
                MessageType.Warning);
        }

        if (m_maxStepHeightProperty != null &&
            m_maxStepHeightProperty.floatValue <= 0.0f)
        {
            EditorGUILayout.HelpBox(
                "最大段差高さが0のため、Step Upは無効です。",
                MessageType.Info);
        }

        if (m_stepLowerCheckHeightProperty != null &&
            m_maxStepHeightProperty != null &&
            m_stepLowerCheckHeightProperty.floatValue >=
            m_maxStepHeightProperty.floatValue &&
            m_maxStepHeightProperty.floatValue > 0.0f)
        {
            EditorGUILayout.HelpBox(
                "下部判定高さが最大段差高さ以上です。下部判定の検出帯が広すぎる可能性があります。",
                MessageType.Warning);
        }

        if (m_stepForwardOffsetProperty != null &&
            m_stepCheckDistanceProperty != null &&
            m_stepForwardOffsetProperty.floatValue >
            m_stepCheckDistanceProperty.floatValue)
        {
            EditorGUILayout.HelpBox(
                "上面探索オフセットが前方探索距離より大きくなっています。必要以上に奥の地面を拾う可能性があります。",
                MessageType.Warning);
        }
    }

    /// <summary>
    /// プロパティを日本語ラベルとTooltip付きで描画します。
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
