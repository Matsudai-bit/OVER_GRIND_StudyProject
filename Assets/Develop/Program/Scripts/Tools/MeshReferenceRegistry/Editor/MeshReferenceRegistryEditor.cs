using UnityEditor;
using UnityEngine;

/// <summary>
/// MeshReferenceRegistryÇÃInspectorÇägí£ÇµÇ‹Ç∑ÅB
/// </summary>
[CustomEditor(typeof(MeshReferenceRegistry))]
public sealed class MeshReferenceRegistryEditor : Editor
{
    /// <summary>
    /// InspectorÇï`âÊÇµÇ‹Ç∑ÅB
    /// </summary>
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        MeshReferenceRegistry registry =
            (MeshReferenceRegistry)target;

        if (GUILayout.Button("Capture Paths"))
        {
            Undo.RecordObject(
                registry,
                "Capture Mesh Reference Paths");

            registry.CapturePaths();

            EditorUtility.SetDirty(registry);
        }

        if (GUILayout.Button("Rebind"))
        {
            Undo.RecordObject(
                registry,
                "Rebind Mesh References");

            registry.Rebind();

            EditorUtility.SetDirty(registry);
        }
    }
}