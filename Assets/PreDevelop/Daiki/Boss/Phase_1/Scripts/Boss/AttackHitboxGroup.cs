using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ˆê‚Â‚ÌUŒ‚‚Åg—p‚·‚éHitboxŒQ‚ğŠÇ—‚µ‚Ü‚·B
/// </summary>
[Serializable]
public sealed class AttackHitboxGroup : MonoBehaviour
{
    // UŒ‚ID
    [SerializeField]
    private AttackIdentifier m_attackIdentifier;

    // UŒ‚‚Åg—p‚·‚éHitboxˆê——
    [SerializeField]
    private List<AttackHitbox> m_hitboxes = new();

    /// <summary>
    /// UŒ‚ID‚ğæ“¾‚µ‚Ü‚·B
    /// </summary>
    public AttackIdentifier AttackIdentifier => m_attackIdentifier;

    /// <summary>
    /// UŒ‚‚Åg—p‚·‚éHitboxˆê——‚ğæ“¾‚µ‚Ü‚·B
    /// </summary>
    public IReadOnlyList<AttackHitbox> Hitboxes => m_hitboxes;

    /// <summary>
    /// “o˜^‚³‚ê‚Ä‚¢‚éHitbox‚ğ‚·‚×‚Ä—LŒø‚É‚µ‚Ü‚·B
    /// </summary>
    public void EnableHitboxes()
    {
        foreach (AttackHitbox hitbox in m_hitboxes)
        {
            if (hitbox == null)
            {
                continue;
            }

            hitbox.EnableHitbox();
        }
    }

    /// <summary>
    /// “o˜^‚³‚ê‚Ä‚¢‚éHitbox‚ğ‚·‚×‚Ä–³Œø‚É‚µ‚Ü‚·B
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