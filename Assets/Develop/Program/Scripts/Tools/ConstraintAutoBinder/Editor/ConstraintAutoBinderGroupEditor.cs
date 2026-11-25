using UnityEditor;
using UnityEngine;

/// <summary>
/// ConstraintAutoBinderGroupのInspectorを拡張します。
/// </summary>
[CustomEditor(typeof(ConstraintAutoBinderGroup))]
public sealed class ConstraintAutoBinderGroupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        DrawBinderInformation();

        EditorGUILayout.Space();

        DrawBindingButtons();
    }

    /// <summary>
    /// 対象となるConstraintAutoBinderの情報を表示します。
    /// </summary>
    private void DrawBinderInformation()
    {
        ConstraintAutoBinderGroup binderGroup =
            (ConstraintAutoBinderGroup)target;

        ConstraintAutoBinder[] binders =
            binderGroup.GetBinders();

        EditorGUILayout.LabelField(
            "Constraint Auto Binder Group",
            EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            $"対象のConstraintAutoBinder : {binders.Length}個",
            binders.Length > 0
                ? MessageType.Info
                : MessageType.Warning);
    }

    /// <summary>
    /// 一括バインド操作用のボタンを表示します。
    /// </summary>
    private void DrawBindingButtons()
    {
        ConstraintAutoBinderGroup binderGroup =
            (ConstraintAutoBinderGroup)target;

        if (GUILayout.Button(
                "Capture All Bindings",
                GUILayout.Height(30.0f)))
        {
            CaptureAllBindings(binderGroup);
        }

        EditorGUILayout.Space(4.0f);

        if (GUILayout.Button(
                "Bind All Constraints",
                GUILayout.Height(30.0f)))
        {
            BindAllConstraints(binderGroup);
        }
    }

    /// <summary>
    /// 子階層すべてのバインド情報を保存します。
    /// </summary>
    private void CaptureAllBindings(
        ConstraintAutoBinderGroup binderGroup)
    {
        Undo.RegisterFullObjectHierarchyUndo(
            binderGroup.gameObject,
            "Capture All Constraint Bindings");

        binderGroup.CaptureAllBindings();

        EditorUtility.SetDirty(
            binderGroup);
    }

    /// <summary>
    /// 子階層すべてのConstraintを再バインドします。
    /// </summary>
    private void BindAllConstraints(
        ConstraintAutoBinderGroup binderGroup)
    {
        Undo.RegisterFullObjectHierarchyUndo(
            binderGroup.gameObject,
            "Bind All Constraints");

        binderGroup.BindAllConstraints();

        EditorUtility.SetDirty(
            binderGroup);
    }
}