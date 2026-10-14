using UnityEditor;
using UnityEngine;

/// <summary>
/// ConstraintAutoBinderのInspectorを拡張します。
/// </summary>
[CustomEditor(typeof(ConstraintAutoBinder))]
public sealed class ConstraintAutoBinderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        ConstraintAutoBinder binder =
            (ConstraintAutoBinder)target;

        EditorGUILayout.LabelField(
            "Constraint Binding",
            EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "モデル差し替え前にCapture Bindingsを実行してください。\n" +
            "モデル差し替え後はBind Allで新しいボーンへ再接続できます。",
            MessageType.Info);

        if (GUILayout.Button("Capture Bindings"))
        {
            Undo.RecordObject(
                binder,
                "Capture Constraint Bindings");

            binder.CaptureBindings();

            EditorUtility.SetDirty(binder);
        }

        if (GUILayout.Button("Bind All"))
        {
            Undo.RecordObject(
                binder,
                "Bind All Constraints");

            binder.BindAll();

            EditorUtility.SetDirty(binder);
        }
    }
}