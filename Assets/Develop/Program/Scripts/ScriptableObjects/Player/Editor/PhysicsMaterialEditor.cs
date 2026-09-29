#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

/// <summary>
/// PhysicsMaterialのInspectorを調整担当者向けに分かりやすく表示します。
/// </summary>
[CustomEditor(typeof(PhysicsMaterial))]
public class PhysicsMaterialEditor : Editor
{
    private SerializedProperty dynamicFriction;
    private SerializedProperty staticFriction;
    private SerializedProperty bounciness;
    private SerializedProperty frictionCombine;
    private SerializedProperty bounceCombine;

    private void OnEnable()
    {
        dynamicFriction = serializedObject.FindProperty("m_DynamicFriction");
        staticFriction = serializedObject.FindProperty("m_StaticFriction");
        bounciness = serializedObject.FindProperty("m_Bounciness");
        frictionCombine = serializedObject.FindProperty("m_FrictionCombine");
        bounceCombine = serializedObject.FindProperty("m_BounceCombine");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            "摩擦",
            EditorStyles.boldLabel
        );

        EditorGUILayout.PropertyField(
            dynamicFriction,
            new GUIContent(
                "Dynamic Friction",
                "物体が動いている間に発生する摩擦です。値が大きいほど滑りにくくなります。"
            )
        );

        EditorGUILayout.HelpBox(
            "動いている物体に対する摩擦。\n" +
            "↑ 大きくする → 滑りにくい\n" +
            "↓ 小さくする → 滑りやすい",
            MessageType.None
        );

        EditorGUILayout.Space(5);

        EditorGUILayout.PropertyField(
            staticFriction,
            new GUIContent(
                "Static Friction",
                "物体が静止している状態から動き始めるまでの摩擦です。"
            )
        );

        EditorGUILayout.HelpBox(
            "停止している物体が動き出すまでの摩擦。\n" +
            "↑ 大きくする → 動き始めにくい\n" +
            "↓ 小さくする → 動き始めやすい",
            MessageType.None
        );

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField(
            "反発",
            EditorStyles.boldLabel
        );

        EditorGUILayout.PropertyField(
            bounciness,
            new GUIContent(
                "Bounciness",
                "衝突したときの跳ね返りの強さです。"
            )
        );

        EditorGUILayout.HelpBox(
            "衝突時の跳ね返りの強さ。\n" +
            "0 → ほぼ跳ねない\n" +
            "1 → 強く跳ね返る",
            MessageType.None
        );

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField(
            "組み合わせ",
            EditorStyles.boldLabel
        );

        EditorGUILayout.PropertyField(
            frictionCombine,
            new GUIContent(
                "Friction Combine",
                "衝突する2つの物体の摩擦値をどのように組み合わせるかを決めます。"
            )
        );

        EditorGUILayout.PropertyField(
            bounceCombine,
            new GUIContent(
                "Bounce Combine",
                "衝突する2つの物体の反発値をどのように組み合わせるかを決めます。"
            )
        );

        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "調整の目安\n\n" +
            "摩擦を強くしたい → Static / Dynamic Friction ↑\n" +
            "滑りやすくしたい → Static / Dynamic Friction ↓\n" +
            "跳ね返りを強くしたい → Bounciness ↑\n" +
            "跳ね返りを弱くしたい → Bounciness ↓",
            MessageType.Info
        );

        serializedObject.ApplyModifiedProperties();
    }
}

#endif