using UnityEditor;
using UnityEngine;

/// <summary>
/// 通常時とチャージ残存時の多段ヒットレートを設定するInspectorです。
/// </summary>
[CustomEditor(typeof(PlayerAttackController))]
[CanEditMultipleObjects]
public sealed class PlayerAttackControllerEditor : Editor
{
    private SerializedProperty m_baseHitsPerSecond;
    private SerializedProperty m_chargedHitsPerSecond;

    /// <summary>
    /// 編集対象のシリアライズ済みプロパティを取得します。
    /// </summary>
    private void OnEnable()
    {
        m_baseHitsPerSecond = serializedObject.FindProperty("m_baseHitsPerSecond");
        m_chargedHitsPerSecond = serializedObject.FindProperty("m_chargedHitsPerSecond");
    }

    /// <summary>
    /// 既存の参照設定と、チャージ残量別の攻撃レートを表示します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
        }

        DrawPropertiesExcluding(serializedObject,
            "m_Script", "m_baseHitsPerSecond", "m_chargedHitsPerSecond");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("攻撃レート（Hit/秒）", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(m_baseHitsPerSecond,
            new GUIContent("通常時", "ダッシュ後のチャージ残量が0のときの基準レート。"));
        EditorGUILayout.PropertyField(m_chargedHitsPerSecond,
            new GUIContent("チャージ残存時", "ダッシュ後に継続消費されるチャージが残っている間の基準レート。"));
        EditorGUILayout.HelpBox(
            "ダッシュ動作や溜め入力ではなく、ダッシュ後のチャージ残量で切り替えます。" +
            "攻撃中もチャージを消費し、0になると通常時のレートに戻ります。\n" +
            "実際のヒットレートには、既存の移動速度低下による減衰が適用されます。",
            MessageType.Info);

        serializedObject.ApplyModifiedProperties();
    }
}
