using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 1つのエフェクトIDに複数の定義を登録できる一覧です。
/// </summary>
[CreateAssetMenu(menuName = "Game/Effects/Particle Effect Catalog", fileName = "EffectCatalog")]
public sealed class EffectCatalog : ScriptableObject
{
    [SerializeField, Header("エフェクト定義の一覧（同じIDを複数登録可能）")]
    private List<EffectDefinition> m_definitions = new List<EffectDefinition>();

    // 同じIDの複数定義を保持する検索マップ（実行時に構築）。
    private Dictionary<EffectId, List<EffectDefinition>> m_map;

    // Inspector変更時はフラグだけを更新し、次の検索時に安全に再構築します。
    [System.NonSerialized]
    private volatile bool m_isMapDirty = true;

    /// <summary>
    /// IDに登録されたすべての定義を取得します。
    /// </summary>
    /// <param name="id">エフェクトID。</param>
    /// <param name="definitions">同じIDの定義一覧。</param>
    /// <returns>
    /// true：1件以上登録されています。
    /// false：登録されていません。
    /// </returns>
    public bool TryGetDefinitions(EffectId id, out IReadOnlyList<EffectDefinition> definitions)
    {
        EnsureMap();

        if (m_map.TryGetValue(id, out List<EffectDefinition> group))
        {
            definitions = group;
            return true;
        }

        definitions = null;
        return false;
    }

    /// <summary>
    /// 従来互換として、IDに登録された先頭の定義を取得します。
    /// </summary>
    /// <param name="id">エフェクトID。</param>
    /// <param name="definition">先頭の定義。</param>
    /// <returns>
    /// true：定義を取得できました。
    /// false：登録されていません。
    /// </returns>
    public bool TryGetDefinition(EffectId id, out EffectDefinition definition)
    {
        if (TryGetDefinitions(id, out IReadOnlyList<EffectDefinition> definitions))
        {
            definition = definitions[0];
            return true;
        }

        definition = null;
        return false;
    }

    /// <summary>
    /// ロード直後にマップを更新対象として扱います。
    /// </summary>
    private void OnEnable()
    {
        m_isMapDirty = true;

#if UNITY_EDITOR
        // 個別のEffectDefinitionアセットが変更された場合にも再構築します。
        EffectDefinition.DefinitionChanged -= OnDefinitionChanged;
        EffectDefinition.DefinitionChanged += OnDefinitionChanged;
#endif
    }

    /// <summary>
    /// EditorでCatalogの一覧が変更されたときにマップを無効化します。
    /// </summary>
    private void OnValidate()
    {
        // OnValidateはメインスレッド以外からも呼ばれるため、検索処理は行いません。
        m_isMapDirty = true;
    }

    /// <summary>
    /// Editorの変更通知の購読を解除します。
    /// </summary>
    private void OnDisable()
    {
#if UNITY_EDITOR
        EffectDefinition.DefinitionChanged -= OnDefinitionChanged;
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// 個別のDefinitionの編集を検出します。
    /// </summary>
    private void OnDefinitionChanged()
    {
        // 通知元のスレッドではUnity APIを呼ばず、次の検索時に再構築します。
        m_isMapDirty = true;
    }
#endif

    /// <summary>
    /// 更新が必要な場合のみ検索マップを再構築します。
    /// </summary>
    private void EnsureMap()
    {
        if (m_map == null || m_isMapDirty)
        {
            BuildMap();
        }
    }

    /// <summary>
    /// 同じIDの定義をグループ化して検索マップを構築します。
    /// </summary>
    private void BuildMap()
    {
        // 構築中に変更通知が来た場合は、次回も再構築できるよう先に解除します。
        m_isMapDirty = false;
        Dictionary<EffectId, List<EffectDefinition>> rebuiltMap =
            new Dictionary<EffectId, List<EffectDefinition>>();

        if (m_definitions != null)
        {
            foreach (EffectDefinition definition in m_definitions)
            {
                if (definition == null || definition.Id == EffectId.NONE || definition.Prefab == null)
                {
                    Debug.LogWarning("[EffectCatalog] Definitionの登録内容を確認してください。", this);
                    continue;
                }

                if (!rebuiltMap.TryGetValue(definition.Id, out List<EffectDefinition> group))
                {
                    group = new List<EffectDefinition>();
                    rebuiltMap.Add(definition.Id, group);
                }

                group.Add(definition);
            }
        }

        m_map = rebuiltMap;
    }
}
