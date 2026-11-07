using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

/// <summary>
/// 子階層に存在するConstraintとモデルのボーンを再バインドします。
/// </summary>
[DisallowMultipleComponent]
public sealed class ConstraintAutoBinder : MonoBehaviour
{
    /// <summary>
    /// Constraintとボーンのバインド情報です。
    /// </summary>
    [Serializable]
    private sealed class ConstraintBinding
    {
        [SerializeField]
        private Component m_constraintComponent;

        [SerializeField]
        private int m_sourceIndex;

        [SerializeField]
        private string m_boneName;

        [SerializeField]
        private string m_bonePath;

        /// <summary>
        /// Constraintコンポーネントを取得します。
        /// </summary>
        public Component ConstraintComponent => m_constraintComponent;

        /// <summary>
        /// ConstraintSourceのインデックスを取得します。
        /// </summary>
        public int SourceIndex => m_sourceIndex;

        /// <summary>
        /// バインド対象のボーン名を取得します。
        /// </summary>
        public string BoneName => m_boneName;

        /// <summary>
        /// BoneRootからの相対パスを取得します。
        /// </summary>
        public string BonePath => m_bonePath;

        /// <summary>
        /// バインド情報を設定します。
        /// </summary>
        public void Set(
            Component constraintComponent,
            int sourceIndex,
            string boneName,
            string bonePath)
        {
            m_constraintComponent = constraintComponent;
            m_sourceIndex = sourceIndex;
            m_boneName = boneName;
            m_bonePath = bonePath;
        }
    }

    [Header("ボーン検索")]
    [SerializeField]
    private Transform m_boneRoot;

    [Header("Constraint検索")]
    [SerializeField]
    private Transform m_constraintRoot;

    [Header("バインド情報")]
    [SerializeField]
    private List<ConstraintBinding> m_bindings = new();

    /// <summary>
    /// 現在設定されているConstraintSourceを取得し、
    /// 再バインド用の情報として保存します。
    /// </summary>
    public void CaptureBindings()
    {
        if (!ValidateBoneRoot())
        {
            return;
        }

        Transform constraintRoot =
            m_constraintRoot != null
                ? m_constraintRoot
                : transform;

        Component[] components =
            constraintRoot.GetComponentsInChildren<Component>(true);

        m_bindings.Clear();

        foreach (Component component in components)
        {
            if (component is not IConstraint constraint)
            {
                continue;
            }

            CaptureConstraintSources(
                component,
                constraint);
        }

        Debug.Log(
            $"{nameof(ConstraintAutoBinder)}: " +
            $"{m_bindings.Count}件のバインド情報を保存しました。",
            this);
    }

    /// <summary>
    /// 保存されているバインド情報を使用して
    /// ConstraintSourceを再設定します。
    /// </summary>
    public void BindAll()
    {
        if (!ValidateBoneRoot())
        {
            return;
        }

        if (m_bindings.Count == 0)
        {
            Debug.LogWarning(
                $"{nameof(ConstraintAutoBinder)}: " +
                "バインド情報がありません。" +
                "先にCapture Bindingsを実行してください。",
                this);

            return;
        }

        Dictionary<string, List<Transform>> boneMap =
            CreateBoneMap();

        int successCount = 0;
        int failedCount = 0;

        foreach (ConstraintBinding binding in m_bindings)
        {
            if (TryBind(binding, boneMap))
            {
                successCount++;
                continue;
            }

            failedCount++;
        }

        Debug.Log(
            $"{nameof(ConstraintAutoBinder)}: " +
            $"バインド完了 成功:{successCount} / 失敗:{failedCount}",
            this);
    }

    /// <summary>
    /// BoneRootが設定されているか確認します。
    /// </summary>
    private bool ValidateBoneRoot()
    {
        if (m_boneRoot != null)
        {
            return true;
        }

        Debug.LogError(
            $"{nameof(ConstraintAutoBinder)}: " +
            $"{nameof(m_boneRoot)} が設定されていません。",
            this);

        return false;
    }

    /// <summary>
    /// 現在のConstraintからSource情報を取得します。
    /// </summary>
    private void CaptureConstraintSources(
        Component component,
        IConstraint constraint)
    {
        for (int sourceIndex = 0;
             sourceIndex < constraint.sourceCount;
             sourceIndex++)
        {
            ConstraintSource source =
                constraint.GetSource(sourceIndex);

            Transform sourceTransform =
                source.sourceTransform;

            if (sourceTransform == null)
            {
                Debug.LogWarning(
                    $"{component.name}: " +
                    $"Source[{sourceIndex}] が設定されていないため保存できません。",
                    component);

                continue;
            }

            if (!TryGetRelativeBonePath(
                    sourceTransform,
                    out string bonePath))
            {
                Debug.LogWarning(
                    $"{component.name}: " +
                    $"Source[{sourceIndex}] のTransform " +
                    $"「{sourceTransform.name}」は、" +
                    $"BoneRoot「{m_boneRoot.name}」配下ではありません。",
                    component);

                continue;
            }

            ConstraintBinding binding = new();

            binding.Set(
                component,
                sourceIndex,
                sourceTransform.name,
                bonePath);

            m_bindings.Add(binding);
        }
    }

    /// <summary>
    /// 指定されたバインド情報をConstraintへ適用します。
    /// </summary>
    private bool TryBind(
        ConstraintBinding binding,
        IReadOnlyDictionary<string, List<Transform>> boneMap)
    {
        Component component =
            binding.ConstraintComponent;

        if (component == null)
        {
            Debug.LogWarning(
                $"{nameof(ConstraintAutoBinder)}: " +
                "Constraintコンポーネントが存在しません。",
                this);

            return false;
        }

        if (component is not IConstraint constraint)
        {
            Debug.LogWarning(
                $"{component.name}: " +
                "IConstraintを実装しているコンポーネントではありません。",
                component);

            return false;
        }

        Transform boneTransform =
            FindBone(binding, boneMap);

        if (boneTransform == null)
        {
            Debug.LogWarning(
                $"{component.name}: " +
                $"対象ボーンを特定できません。" +
                $" Name = {binding.BoneName}," +
                $" Path = {binding.BonePath}",
                component);

            return false;
        }

        if (binding.SourceIndex < 0 ||
            binding.SourceIndex >= constraint.sourceCount)
        {
            Debug.LogWarning(
                $"{component.name}: " +
                $"Source[{binding.SourceIndex}] が存在しません。",
                component);

            return false;
        }

        ConstraintSource source =
            constraint.GetSource(binding.SourceIndex);

        // Weightなどの既存設定は維持し、
        // Transformのみ新しいボーンへ差し替えます。
        source.sourceTransform = boneTransform;

        constraint.SetSource(
            binding.SourceIndex,
            source);

        return true;
    }

    /// <summary>
    /// 保存されている名前とパスから対象ボーンを取得します。
    /// </summary>
    private Transform FindBone(
        ConstraintBinding binding,
        IReadOnlyDictionary<string, List<Transform>> boneMap)
    {
        if (!boneMap.TryGetValue(
                binding.BoneName,
                out List<Transform> sameNameBones))
        {
            return null;
        }

        // 名前が一意なら階層パスを確認せず採用する。
        if (sameNameBones.Count == 1)
        {
            return sameNameBones[0];
        }

        // 同名ボーンが存在する場合のみ、
        // 保存している相対パスから正しいボーンを特定する。
        foreach (Transform bone in sameNameBones)
        {
            if (!TryGetRelativeBonePath(
                    bone,
                    out string bonePath))
            {
                continue;
            }

            if (bonePath == binding.BonePath)
            {
                return bone;
            }
        }

        return null;
    }

    /// <summary>
    /// BoneRoot以下のTransformを名前ごとに分類します。
    /// 同名Transformは同じリストに格納します。
    /// </summary>
    private Dictionary<string, List<Transform>> CreateBoneMap()
    {
        Dictionary<string, List<Transform>> boneMap = new();

        Transform[] transforms =
            m_boneRoot.GetComponentsInChildren<Transform>(true);

        foreach (Transform targetTransform in transforms)
        {
            string boneName =
                targetTransform.name;

            if (!boneMap.TryGetValue(
                    boneName,
                    out List<Transform> sameNameBones))
            {
                sameNameBones = new List<Transform>();

                boneMap.Add(
                    boneName,
                    sameNameBones);
            }

            sameNameBones.Add(
                targetTransform);
        }

        return boneMap;
    }

    /// <summary>
    /// BoneRootから指定Transformまでの相対パスを取得します。
    /// </summary>
    private bool TryGetRelativeBonePath(
        Transform targetTransform,
        out string bonePath)
    {
        bonePath = string.Empty;

        if (targetTransform == null ||
            m_boneRoot == null)
        {
            return false;
        }

        // BoneRoot自身の場合は空文字列をパスとして扱う。
        if (targetTransform == m_boneRoot)
        {
            return true;
        }

        if (!targetTransform.IsChildOf(m_boneRoot))
        {
            return false;
        }

        Stack<string> pathElements = new();

        Transform currentTransform =
            targetTransform;

        while (currentTransform != null &&
               currentTransform != m_boneRoot)
        {
            pathElements.Push(
                currentTransform.name);

            currentTransform =
                currentTransform.parent;
        }

        bonePath =
            string.Join(
                "/",
                pathElements);

        return true;
    }
}