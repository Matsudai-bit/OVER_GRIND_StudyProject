using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// エフェクトの識別子
public enum EffectID
{
    Explosion
}

[CreateAssetMenu(fileName = "EffectDatabase", menuName = "Data/EffectDatabase")]
public class EffectDatabase : ScriptableObject
{
    [System.Serializable]
    public struct EffectData
    {
        public EffectID m_id;
        public EffectNode_Aoki m_prefab;
    }

    // [SerializeReference] はインターフェース等に使うため、構造体には [SerializeField] を使用します
    [SerializeField]
    public List<EffectData> m_effectList = new List<EffectData>();

    private Dictionary<EffectID, EffectNode_Aoki> m_dict;

    public void Initialize()
    {
        // 検索をOにするため辞書化
        m_dict = m_effectList.ToDictionary(x => x.m_id, x => x.m_prefab);
    }

    public EffectNode_Aoki GetPrefab(EffectID id)
    {
        // 辞書がまだ作られていなければ、自動で初期化する
        if (m_dict == null)
        {
            Initialize();
        }

        if (m_dict.TryGetValue(id, out var prefab))
        {
            return prefab;
        }
        Debug.LogError($"EffectID: {id} がデータベースに見つかりません");
        return null;
    }
}