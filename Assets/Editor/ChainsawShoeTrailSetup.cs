using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Stage1の新モデルへ刃痕の参照を設定し、保存前に検証します。</summary>
public static class ChainsawShoeTrailSetup
{
    private const string SCENE_PATH = "Assets/Develop/Scenes/InGame/InGameScene_Stage1.unity";
    private const string MATERIAL_PATH = "Assets/Develop/Program/Shaders/ChainsawShoeTrail.mat";

    /// <summary>バッチ実行用にStage1を設定・検証して保存します。</summary>
    [MenuItem("Tools/Chainsaw Shoes/Configure Stage1")]
    public static void ConfigureStage1()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != SCENE_PATH)
            throw new InvalidOperationException("Open InGameScene_Stage1 before configuring shoe trails.");
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play mode before configuring shoe trails.");
        PlayerMonitor monitor = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<PlayerMonitor>(true)).Single();
        Transform model = monitor.GetComponentsInChildren<Animator>(true)
            .Single(animator => animator.name == "SK_Player").transform;
        Transform[] bones = model.GetComponentsInChildren<Transform>(true);
        Transform left = bones.Single(bone => bone.name == "Toes_L");
        Transform right = bones.Single(bone => bone.name == "Toes_R");
        Shader shader = Shader.Find("OverGraind/ChainsawShoeTrail");
        if (shader == null || ShaderUtil.ShaderHasError(shader))
            throw new InvalidOperationException("Shoe trail shader did not compile.");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, MATERIAL_PATH);
        }
        PlayerChainsawShoeTrail trail = monitor.GetComponent<PlayerChainsawShoeTrail>();
        if (trail == null) trail = monitor.gameObject.AddComponent<PlayerChainsawShoeTrail>();
        SerializedObject serialized = new SerializedObject(trail);
        serialized.FindProperty("m_leftShoe").objectReferenceValue = left;
        serialized.FindProperty("m_rightShoe").objectReferenceValue = right;
        serialized.FindProperty("m_monitor").objectReferenceValue = monitor;
        serialized.FindProperty("m_stateMachine").objectReferenceValue = monitor.GetComponent<PlayerStateMachineComponent>();
        serialized.FindProperty("m_material").objectReferenceValue = material;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(trail);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"SHOE_TRAIL_SETUP_OK left={left.position} right={right.position} player={monitor.transform.position}");
    }
}
