using System;
using UnityEngine;

/// <summary>
/// ゲーム設定の変更を通知します。
/// </summary>
[CreateAssetMenu(
    fileName = "GameSettingsChangedEvent",
    menuName = "Game/Event/Game Settings Changed Event")]
public sealed class GameSettingsChangedEvent : ScriptableObject
{
    // ゲーム設定変更イベント
    private event Action<GameSettingsChangedEventData> m_changed;

    /// <summary>
    /// ゲーム設定の変更を通知します。
    /// </summary>
    /// <param name="eventData">ゲーム設定変更情報。</param>
    public void Raise(GameSettingsChangedEventData eventData)
    {
        m_changed?.Invoke(eventData);
    }

    /// <summary>
    /// リスナーを登録します。
    /// </summary>
    /// <param name="listener">登録するリスナー。</param>
    public void RegisterListener(
        Action<GameSettingsChangedEventData> listener)
    {
        if (listener == null)
        {
            return;
        }

        m_changed -= listener;
        m_changed += listener;
    }

    /// <summary>
    /// リスナーを解除します。
    /// </summary>
    /// <param name="listener">解除するリスナー。</param>
    public void UnregisterListener(
        Action<GameSettingsChangedEventData> listener)
    {
        if (listener == null)
        {
            return;
        }

        m_changed -= listener;
    }
}
