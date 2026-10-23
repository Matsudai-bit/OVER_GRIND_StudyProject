using System;
using UnityEngine;

[CreateAssetMenu(fileName = "GamepadRumbleRequestEvent", menuName = "Game/Events/Gamepad Rumble Request Event")]
public class GamepadRumbleRequestEvent : ScriptableObject
{
    public event Action<GamepadRumbleAsset> OnRequested;

    /// <summary>
    /// ƒQ[ƒ€‘¤‚©‚çU“®—v‹‚ğ”­s‚µ‚Ü‚·
    /// </summary>
    public void Raise(GamepadRumbleAsset rumbleAsset)
    {
        if (rumbleAsset == null) return;
        OnRequested?.Invoke(rumbleAsset);
    }
}