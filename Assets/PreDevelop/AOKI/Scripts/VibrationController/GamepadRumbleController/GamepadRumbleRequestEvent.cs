using System;
using UnityEngine;

/// <summary>
/// ゲームパッドの振動要求を通知します。
/// </summary>
[CreateAssetMenu(fileName = "GamepadRumbleRequestEvent", menuName = "Game/Events/Gamepad Rumble Request Event")]
public class GamepadRumbleRequestEvent : ScriptableObject
{
    // 振動要求イベント
    public event Action<GamepadRumbleAsset> OnRequested;

    /// <summary>
    /// 振動要求イベントを発行します。
    /// </summary>
    /// <param name="rumbleAsset">再生する振動アセット。</param>
    public void Raise(GamepadRumbleAsset rumbleAsset)
    {
        if (rumbleAsset == null)
        {
            return;
        }

        OnRequested?.Invoke(rumbleAsset);
    }
}