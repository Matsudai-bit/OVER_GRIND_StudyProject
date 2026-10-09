using System;
using System.Collections.Generic;

/// <summary>
/// 螳溯｡梧凾縺ｫ荳縺､縺ｮ謾ｻ謦・〒菴ｿ逕ｨ縺吶ｋAttackHitbox鄒､繧剃ｿ晄戟縺励∪縺吶・
/// </summary>
public sealed class AttackHitboxRuntimeGroup
{
    // 謾ｻ謦オD
    private readonly AttackIdentifier m_attackIdentifier;

    // 謾ｻ謦・〒菴ｿ逕ｨ縺吶ｋHitbox荳隕ｧ
    private readonly IReadOnlyList<AttackHitbox> m_hitboxes;

    /// <summary>
    /// 謾ｻ謦オD繧貞叙蠕励＠縺ｾ縺吶・
    /// </summary>
    public AttackIdentifier AttackIdentifier =>
        m_attackIdentifier;

    /// <summary>
    /// 謾ｻ謦・〒菴ｿ逕ｨ縺吶ｋHitbox荳隕ｧ繧貞叙蠕励＠縺ｾ縺吶・
    /// </summary>
    public IReadOnlyList<AttackHitbox> Hitboxes =>
        m_hitboxes;

    /// <summary>
    /// 螳溯｡梧凾Hitbox諠・ｱ繧堤函謌舌＠縺ｾ縺吶・
    /// </summary>
    /// <param name="attackIdentifier">謾ｻ謦オD縲・/param>
    /// <param name="hitboxes">謾ｻ謦・〒菴ｿ逕ｨ縺吶ｋHitbox荳隕ｧ縲・/param>
    /// <exception cref="ArgumentNullException">
    /// 蠢・ｦ√↑諠・ｱ縺系ull縺ｮ蝣ｴ蜷医↓逋ｺ逕溘＠縺ｾ縺吶・
    /// </exception>
    public AttackHitboxRuntimeGroup(
        AttackIdentifier attackIdentifier,
        IReadOnlyList<AttackHitbox> hitboxes)
    {
        m_attackIdentifier =
            attackIdentifier ??
            throw new ArgumentNullException(
                nameof(attackIdentifier));

        m_hitboxes =
            hitboxes ??
            throw new ArgumentNullException(
                nameof(hitboxes));
    }

    /// <summary>
    /// 逋ｻ骭ｲ縺輔ｌ縺ｦ縺・ｋHitbox繧偵☆縺ｹ縺ｦ譛牙柑縺ｫ縺励∪縺吶・
    /// </summary>
    public void EnableHitboxes()
    {
        foreach (AttackHitbox hitbox in m_hitboxes)
        {
            if (hitbox == null)
            {
                continue;
            }

            // 繝繝｡繝ｼ繧ｸ險ｭ螳壹′譛ｪ逋ｻ骭ｲ縺ｧ繧ゅ√げ繝ｫ繝ｼ繝励・謾ｻ謦オD繧定｢ｫ蠑ｾ蜈医∈貂｡縺励∪縺吶・
            hitbox.EnableHitbox(
                new AttackDamageData(
                    hitbox.CurrentDamage,
                    m_attackIdentifier));
        }
    }

    /// <summary>
    /// 逋ｻ骭ｲ縺輔ｌ縺ｦ縺・ｋHitbox繧偵☆縺ｹ縺ｦ辟｡蜉ｹ縺ｫ縺励∪縺吶・
    /// </summary>
    public void DisableHitboxes()
    {
        foreach (AttackHitbox hitbox in m_hitboxes)
        {
            if (hitbox == null)
            {
                continue;
            }

            hitbox.DisableHitbox();
        }
    }
}
