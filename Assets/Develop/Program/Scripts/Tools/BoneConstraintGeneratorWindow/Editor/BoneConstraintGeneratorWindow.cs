using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;

/// <summary>
/// 指定したルートボーン以下からParentConstraint付きGameObjectを生成します。
/// </summary>
public sealed class BoneConstraintGeneratorWindow : EditorWindow
{
    private const string DEFAULT_OUTPUT_NAME = "BoneConstraints";
    private const string DEFAULT_SUFFIX = "_Constraint";

    private Transform m_rootBone;
    private Transform m_outputParent;

    private bool m_includeRootBone = true;
    private bool m_includeInactive = true;

    private string m_nameSuffix = DEFAULT_SUFFIX;

    /// <summary>
    /// Bone Constraint Generatorを開きます。
    /// </summary>
    [MenuItem("Tools/Bone Constraint Generator")]
    public static void Open()
    {
        BoneConstraintGeneratorWindow window =
            GetWindow<BoneConstraintGeneratorWindow>();

        window.titleContent =
            new GUIContent("Bone Constraint Generator");
    }

    private void OnGUI()
    {
        DrawTargetSettings();

        EditorGUILayout.Space();

        DrawGenerationSettings();

        EditorGUILayout.Space();

        DrawGenerateButton();
    }

    /// <summary>
    /// 対象設定を描画します。
    /// </summary>
    private void DrawTargetSettings()
    {
        EditorGUILayout.LabelField(
            "対象",
            EditorStyles.boldLabel);

        m_rootBone = (Transform)EditorGUILayout.ObjectField(
            "Root Bone",
            m_rootBone,
            typeof(Transform),
            true);

        m_outputParent = (Transform)EditorGUILayout.ObjectField(
            "Output Parent",
            m_outputParent,
            typeof(Transform),
            true);

        EditorGUILayout.HelpBox(
            "Root Bone以下のTransformを対象として、" +
            "ParentConstraint付きGameObjectを生成します。",
            MessageType.Info);
    }

    /// <summary>
    /// 生成設定を描画します。
    /// </summary>
    private void DrawGenerationSettings()
    {
        EditorGUILayout.LabelField(
            "生成設定",
            EditorStyles.boldLabel);

        m_includeRootBone = EditorGUILayout.Toggle(
            "Include Root Bone",
            m_includeRootBone);

        m_includeInactive = EditorGUILayout.Toggle(
            "Include Inactive",
            m_includeInactive);

        m_nameSuffix = EditorGUILayout.TextField(
            "Name Suffix",
            m_nameSuffix);
    }

    /// <summary>
    /// 生成ボタンを描画します。
    /// </summary>
    private void DrawGenerateButton()
    {
        using (new EditorGUI.DisabledScope(m_rootBone == null))
        {
            if (GUILayout.Button(
                    "Generate Constraints",
                    GUILayout.Height(32.0f)))
            {
                Generate();
            }
        }
    }

    /// <summary>
    /// Constraint付きGameObjectを生成します。
    /// </summary>
    private void Generate()
    {
        if (m_rootBone == null)
        {
            Debug.LogError(
                $"{nameof(BoneConstraintGeneratorWindow)}: " +
                "Root Boneが設定されていません.");

            return;
        }

        Transform outputParent = GetOrCreateOutputParent();

        List<Transform> targetBones = CollectTargetBones();

        foreach (Transform bone in targetBones)
        {
            CreateConstraintObject(
                bone,
                outputParent);
        }

        Selection.activeGameObject = outputParent.gameObject;

        Debug.Log(
            $"{nameof(BoneConstraintGeneratorWindow)}: " +
            $"{targetBones.Count}個のConstraintを生成しました。",
            outputParent);
    }

    /// <summary>
    /// 対象となるボーンを収集します。
    /// </summary>
    private List<Transform> CollectTargetBones()
    {
        List<Transform> bones = new();

        Transform[] transforms =
            m_rootBone.GetComponentsInChildren<Transform>(
                m_includeInactive);

        foreach (Transform targetTransform in transforms)
        {
            if (!m_includeRootBone &&
                targetTransform == m_rootBone)
            {
                continue;
            }

            bones.Add(targetTransform);
        }

        return bones;
    }

    /// <summary>
    /// 生成先Transformを取得します。
    /// 未指定の場合は新規作成します。
    /// </summary>
    private Transform GetOrCreateOutputParent()
    {
        if (m_outputParent != null)
        {
            return m_outputParent;
        }

        GameObject outputObject =
            new(DEFAULT_OUTPUT_NAME);

        Undo.RegisterCreatedObjectUndo(
            outputObject,
            "Create Bone Constraint Root");

        Transform modelRoot = m_rootBone.root;

        if (modelRoot.parent != null)
        {
            outputObject.transform.SetParent(
                modelRoot.parent,
                false);
        }

        return outputObject.transform;
    }

    /// <summary>
    /// 指定ボーンを追従するConstraintオブジェクトを生成します。
    /// </summary>
    private void CreateConstraintObject(
        Transform bone,
        Transform outputParent)
    {
        string objectName =
            bone.name + m_nameSuffix;

        GameObject constraintObject =
            new(objectName);

        Undo.RegisterCreatedObjectUndo(
            constraintObject,
            "Create Bone Constraint");

        Transform constraintTransform =
            constraintObject.transform;

        constraintTransform.SetParent(
            outputParent,
            true);

        constraintTransform.SetPositionAndRotation(
            bone.position,
            bone.rotation);

        constraintTransform.localScale =
            Vector3.one;

        ParentConstraint parentConstraint =
            Undo.AddComponent<ParentConstraint>(
                constraintObject);

        ConstraintSource source = new()
        {
            sourceTransform = bone,
            weight = 1.0f
        };

        int sourceIndex =
            parentConstraint.AddSource(source);

        parentConstraint.SetTranslationOffset(
            sourceIndex,
            Vector3.zero);

        parentConstraint.SetRotationOffset(
            sourceIndex,
            Vector3.zero);

        parentConstraint.weight = 1.0f;
        parentConstraint.constraintActive = true;
        parentConstraint.locked = true;
    }
}