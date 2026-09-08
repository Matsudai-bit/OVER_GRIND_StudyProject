using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;

//エフェクトの識別子
public enum EffectID
{
    Explosion,
    Slash,
    MagicAura
}

[CreateAssetMenu(fileName = "EffectDatabase", menuName = "Data/EffectDatabase")]
public class EffectDatabase : ScriptableObject
{
    [System.Serializable]
    public struct EffectData
    {
        public EffectID m_id;
        public EffectNode_Aoki m_prefab; //nodeの指定
    }


    [SerializeReference] private List<EffectData> m_effectList;

    private Dictionary<EffectID, EffectNode_Aoki> m_dict;

    public void Initialize()
    {
        // 検索を０にするため辞書化
        m_dict = m_effectList.ToDictionary(x => x.m_id , x=> x.m_prefab);
    }

    public EffectNode_Aoki GetPrefab(EffectID id)
    {
        if (m_dict != null && m_dict.TryGetValue(id, out var prefab))
        {
            return prefab;
        }
        Debug.LogError($"EffectID: {id} がデータベースに見つかりません");
        return null;
    }
}
