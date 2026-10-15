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
        /// バインド情報を設定します。
        /// </summary>
        public void Set(
            Component constraintComponent,
            int sourceIndex,
            string boneName)
        {
            m_constraintComponent = constraintComponent;
            m_sourceIndex = sourceIndex;
            m_boneName = boneName;
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
        Transform constraintRoot =
            m_constraintRoot != null ? m_constraintRoot : transform;

        Component[] components =
            constraintRoot.GetComponentsInChildren<Component>(true);

        m_bindings.Clear();

        foreach (Component component in components)
        {
            if (component is not IConstraint constraint)
            {
                continue;
            }

            CaptureConstraintSources(component, constraint);
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
        if (m_boneRoot == null)
        {
            Debug.LogError(
                $"{nameof(ConstraintAutoBinder)}: " +
                $"{nameof(m_boneRoot)} が設定されていません。",
                this);

            return;
        }

        Dictionary<string, Transform> boneMap = CreateBoneMap();

        int successCount = 0;
        int failedCount = 0;

        foreach (ConstraintBinding binding in m_bindings)
        {
            if (TryBind(binding, boneMap))
            {
                successCount++;
            }
            else
            {
                failedCount++;
            }
        }

        Debug.Log(
            $"{nameof(ConstraintAutoBinder)}: " +
            $"バインド完了 成功:{successCount} / 失敗:{failedCount}",
            this);
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

            if (source.sourceTransform == null)
            {
                Debug.LogWarning(
                    $"{component.name}: " +
                    $"Source[{sourceIndex}] が設定されていないため保存できません。",
                    component);

                continue;
            }

            ConstraintBinding binding = new();

            binding.Set(
                component,
                sourceIndex,
                source.sourceTransform.name);

            m_bindings.Add(binding);
        }
    }

    /// <summary>
    /// 指定されたバインド情報をConstraintへ適用します。
    /// </summary>
    private bool TryBind(
        ConstraintBinding binding,
        IReadOnlyDictionary<string, Transform> boneMap)
    {
        Component component = binding.ConstraintComponent;

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
                "IConstraintではありません。",
                component);

            return false;
        }

        if (!boneMap.TryGetValue(
                binding.BoneName,
                out Transform boneTransform))
        {
            Debug.LogWarning(
                $"{component.name}: " +
                $"ボーン「{binding.BoneName}」が見つかりません。",
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

        // Weightなど既存設定はそのまま維持し、
        // Transformのみ新しいボーンへ差し替えます。
        source.sourceTransform = boneTransform;

        constraint.SetSource(
            binding.SourceIndex,
            source);

        return true;
    }

    /// <summary>
    /// BoneRoot以下のTransformをボーン名から取得できる辞書に変換します。
    /// </summary>
    private Dictionary<string, Transform> CreateBoneMap()
    {
        Dictionary<string, Transform> boneMap = new();

        Transform[] transforms =
            m_boneRoot.GetComponentsInChildren<Transform>(true);

        foreach (Transform targetTransform in transforms)
        {
            string boneName = targetTransform.name;

            if (boneMap.ContainsKey(boneName))
            {
                Debug.LogWarning(
                    $"{nameof(ConstraintAutoBinder)}: " +
                    $"同名Transform「{boneName}」が複数存在します。",
                    targetTransform);

                continue;
            }

            boneMap.Add(
                boneName,
                targetTransform);
        }

        return boneMap;
    }
}