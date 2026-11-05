using System.Collections.Generic;
using UnityEngine;

/// <summary>Prefab内にある複数のParentConstraintへアンカーを一括設定します。</summary>
public sealed class EffectAttachmentController
{
    private readonly EffectAttachmentSlot[] m_slots;

    /// <summary>スロット一覧を登録します。</summary>
    public EffectAttachmentController(EffectAttachmentSlot[] slots)
    {
        m_slots = slots ?? System.Array.Empty<EffectAttachmentSlot>();
    }

    /// <summary>すべてのスロットを検証し、追従先を割り当てます。</summary>
    /// <returns>true：割り当て成功。false：ID未登録・設定重複など。</returns>
    public bool TryBind(EffectAnchorRegistry anchors)
    {
        if (m_slots.Length == 0)
        {
            return true;
        }
        if (anchors == null)
        {
            Debug.LogWarning("[EffectAttachmentController] AnchorRegistryが指定されていません。");
            return false;
        }

        // 途中失敗で半分だけ設定されないよう先に全件検証します。
        Dictionary<EffectAttachmentSlot, Transform> resolved =
            new Dictionary<EffectAttachmentSlot, Transform>();
        for (int i = 0; i < m_slots.Length; i++)
        {
            EffectAttachmentSlot slot = m_slots[i];
            if (slot == null || !slot.CanBind() ||
                !anchors.TryGetTarget(slot.AnchorId, out Transform target))
            {
                Debug.LogWarning($"[EffectAttachmentController] 追従先を解決できません: {slot}");
                return false;
            }
            for (int j = 0; j < i; j++)
            {
                EffectAttachmentSlot other = m_slots[j];
                if (other != null && other.Constraint == slot.Constraint &&
                    other.SourceIndex == slot.SourceIndex)
                {
                    Debug.LogError($"[EffectAttachmentController] Source操作の重複: {slot}");
                    return false;
                }
            }
            resolved.Add(slot, target);
        }

        foreach (KeyValuePair<EffectAttachmentSlot, Transform> entry in resolved)
        {
            if (!entry.Key.TryBind(entry.Value))
            {
                ClearBindings();
                return false;
            }
        }
        return true;
    }

    /// <summary>割り当てた参照をすべて解除します。</summary>
    public void ClearBindings()
    {
        foreach (EffectAttachmentSlot slot in m_slots)
        {
            if (slot != null)
            {
                slot.ClearBinding();
            }
        }
    }
}
