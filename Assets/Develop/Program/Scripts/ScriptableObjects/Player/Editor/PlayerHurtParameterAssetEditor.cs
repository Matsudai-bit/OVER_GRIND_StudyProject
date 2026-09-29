#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

/// <summary>
/// プレイヤーの被ダメージパラメータをInspectorに表示・編集します。
/// </summary>
[CustomEditor(typeof(PlayerHurtParameterAsset))]
public sealed class PlayerHurtParameterAssetEditor : Editor
{
    // Inspector内のセクション間隔
    private const float SECTION_SPACE = 8.0f;

    // ============================================================
    // ダメージ
    // ============================================================

    // 受けるダメージの倍率
    private SerializedProperty m_damageMultiplierProperty;

    // ============================================================
    // ノックバック
    // ============================================================

    // ノックバック速度
    private SerializedProperty m_knockbackSpeedProperty;

    // 全攻撃共通のノックバック倍率。
    private SerializedProperty m_knockbackRateProperty;

    // 攻撃別設定が渡されなかった場合の設定
    private SerializedProperty m_defaultKnockbackProfileProperty;

    // 被弾時に上方向へ加算する吹き飛び速度
    private SerializedProperty m_knockbackLiftSpeedProperty;

    // 吹き飛び状態を維持する時間
    private SerializedProperty m_knockbackDurationProperty;

    // 被弾後に操作可能になるまでの硬直時間（秒）
    private SerializedProperty m_hitRecoveryDurationProperty;

    // 吹き飛びを終了させる地形・壁のレイヤー
    private SerializedProperty m_hitEnvironmentLayerMaskProperty;

    /// <summary>
    /// SerializedPropertyを取得します。
    /// </summary>
    private void OnEnable()
    {
        m_damageMultiplierProperty =
            serializedObject.FindProperty("m_damageMultiplier");

        m_knockbackSpeedProperty =
            serializedObject.FindProperty("m_knockbackSpeed");

        m_knockbackRateProperty =
            serializedObject.FindProperty("m_knockbackRate");

        m_defaultKnockbackProfileProperty =
            serializedObject.FindProperty("m_defaultKnockbackProfile");

        m_knockbackLiftSpeedProperty =
            serializedObject.FindProperty("m_knockbackLiftSpeed");

        m_knockbackDurationProperty =
            serializedObject.FindProperty("m_knockbackDuration");

        m_hitRecoveryDurationProperty =
            serializedObject.FindProperty("m_hitRecoveryDuration");

        m_hitEnvironmentLayerMaskProperty =
            serializedObject.FindProperty("m_hitEnvironmentLayerMask");
    }

    /// <summary>
    /// Inspectorを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDescription();

        EditorGUILayout.Space(SECTION_SPACE);

        DrawHurtSection();

        EditorGUILayout.Space(SECTION_SPACE);

        DrawKnockbackSection();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// パラメータの説明を表示します。
    /// </summary>
    private void DrawDescription()
    {
        EditorGUILayout.HelpBox(
            "被ダメージ時の処理に使用されるパラメータを設定します",
            MessageType.Info);
    }

    /// <summary>
    /// 被ダメージ時パラメータを表示します。
    /// </summary>
    private void DrawHurtSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "被ダメージ時パラメータ",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            m_damageMultiplierProperty,
            new GUIContent(
                "被ダメージ時倍率",
                "受けるダメージの倍率です。"));

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// ノックバック時パラメータを表示します。
    /// </summary>
    private void DrawKnockbackSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "ノックバック時パラメータ",
            EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            m_knockbackSpeedProperty,
            new GUIContent(
                "ノックバック速度",
                "ノックバック速度です。"));

        EditorGUILayout.PropertyField(
            m_knockbackRateProperty,
            new GUIContent(
                "ノックバック倍率",
                "全攻撃共通のノックバック倍率です。" +
                "攻撃固有の倍率と乗算します。"));

        EditorGUILayout.PropertyField(
            m_defaultKnockbackProfileProperty,
            new GUIContent(
                "攻撃別設定が渡されなかった場合の設定",
                "攻撃別設定が渡されなかった場合の設定です。" +
                "未設定なら従来の速度・時間を使用します。"));

        EditorGUILayout.PropertyField(
            m_knockbackLiftSpeedProperty,
            new GUIContent(
                "上方向へ加算する吹き飛び速度",
                "被弾時に上方向へ加算する吹き飛び速度です。"));

        EditorGUILayout.PropertyField(
            m_knockbackDurationProperty,
            new GUIContent(
                "吹き飛び状態を維持する時間",
                "吹き飛び状態を維持する時間です。"));

        EditorGUILayout.PropertyField(
            m_hitRecoveryDurationProperty,
            new GUIContent(
                "操作可能になるまでの硬直時間",
                "被弾後に操作可能になるまでの硬直時間（秒）です。"));

        EditorGUILayout.PropertyField(
            m_hitEnvironmentLayerMaskProperty,
            new GUIContent(
                "吹き飛びを終了させる地形レイヤー",
                "吹き飛びを終了させる地形・壁のレイヤーです。"));

        EditorGUILayout.EndVertical();
    }
}

#endif