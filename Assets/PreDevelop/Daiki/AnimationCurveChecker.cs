#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

public static class AnimationCurveChecker
{
    [MenuItem("Tools/Animation/Print Position Curves")]
    private static void PrintPositionCurves()
    {
        AnimationClip clip = Selection.activeObject as AnimationClip;

        if (clip == null)
        {
            Debug.LogError(
                "ProjectウィンドウでAnimationClipを選択してください。");
            return;
        }

        EditorCurveBinding[] bindings =
            AnimationUtility.GetCurveBindings(clip);

        Debug.Log($"--- {clip.name} Position Curves ---", clip);

        foreach (EditorCurveBinding binding in bindings)
        {
            bool isPosition =
                binding.propertyName.Contains("m_LocalPosition") ||
                binding.propertyName.Contains("RootT");

            if (!isPosition)
            {
                continue;
            }

            Debug.Log(
                $"Path: [{binding.path}] " +
                $"Type: [{binding.type.Name}] " +
                $"Property: [{binding.propertyName}]",
                clip);
        }
    }
}

#endif