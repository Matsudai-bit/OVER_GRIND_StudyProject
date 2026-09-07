using System;
using UnityEngine;

/// <summary>
/// ˆê‚Â‚ÌUŒ‚‚Åg—p‚·‚éHitboxŒQ‚Ì‹¤’Êî•ñ‚ğŠÇ—‚µ‚Ü‚·B
/// </summary>
[Serializable]
public abstract class AttackHitboxGroupBase
{
    // UŒ‚ID
    [SerializeField]
    private AttackIdentifier m_attackIdentifier;

    /// <summary>
    /// UŒ‚ID‚ğæ“¾‚µ‚Ü‚·B
    /// </summary>
    public AttackIdentifier AttackIdentifier =>
        m_attackIdentifier;

    /// <summary>
    /// “o˜^‚³‚ê‚Ä‚¢‚éHitbox‚ğ‚·‚×‚Ä—LŒø‚É‚µ‚Ü‚·B
    /// </summary>
    public abstract void EnableHitboxes();

    /// <summary>
    /// “o˜^‚³‚ê‚Ä‚¢‚éHitbox‚ğ‚·‚×‚Ä–³Œø‚É‚µ‚Ü‚·B
    /// </summary>
    public abstract void DisableHitboxes();
}
