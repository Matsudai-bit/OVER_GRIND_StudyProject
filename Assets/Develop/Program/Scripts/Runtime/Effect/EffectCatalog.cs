using System.Collections.Generic;
using UnityEngine;

/// <summary>エフェクトIDと定義アセットの対応を管理します。</summary>
[CreateAssetMenu(menuName = "Game/Effects/Particle Effect Catalog", fileName = "EffectCatalog")]
public sealed class EffectCatalog : ScriptableObject
{
    [SerializeField, Header("エフェクト定義の一覧")]
    private List<EffectDefinition> m_definitions = new List<EffectDefinition>();
    private Dictionary<EffectId, EffectDefinition> m_map;

    /// <summary>IDから設定を取得します。</summary>
    public bool TryGetDefinition(EffectId id, out EffectDefinition definition)
    {
        if (m_map == null)
        {
            BuildMap();
        }
        return m_map.TryGetValue(id, out definition);
    }

    /// <summary>設定を読み直します。</summary>
    private void OnEnable()
    {
        BuildMap();
    }

    /// <summary>IDの重複を検出します。</summary>
    private void BuildMap()
    {
        m_map = new Dictionary<EffectId, EffectDefinition>();
        foreach (EffectDefinition definition in m_definitions)
        {
            if (definition == null || definition.Id == EffectId.NONE || definition.Prefab == null)
            {
                Debug.LogWarning("[EffectCatalog] Definitionの登録内容を確認してください。", this);
                continue;
            }
            if (!m_map.TryAdd(definition.Id, definition))
            {
                Debug.LogError($"[EffectCatalog] IDが重複しています: {definition.Id}", this);
            }
        }
    }
}
