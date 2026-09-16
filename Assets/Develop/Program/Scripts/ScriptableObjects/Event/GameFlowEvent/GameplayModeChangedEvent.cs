using System;
using UnityEngine;

/// <summary>
/// ゲームプレイモードの変更を通知します。
/// </summary>
[CreateAssetMenu(
    fileName = "GameplayModeChangedEvent",
    menuName = "Game/Event/Gameplay Mode Changed Event")]
public sealed class GameplayModeChangedEvent : ScriptableObject
{
    // モード変更イベント
    private event Action<GameplayModeChangedEventData> m_modeChanged;

    /// <summary>
    /// モード変更イベントを発行します。
    /// </summary>
    /// <param name="eventData">モード変更情報。</param>
    public void Raise(GameplayModeChangedEventData eventData)
    {
        m_modeChanged?.Invoke(eventData);
    }

    /// <summary>
    /// リスナーを登録します。
    /// </summary>
    /// <param name="listener">登録するリスナー。</param>
    public void RegisterListener(
        Action<GameplayModeChangedEventData> listener)
    {
        if (listener == null)
        {
            return;
        }

        m_modeChanged -= listener;
        m_modeChanged += listener;
    }

    /// <summary>
    /// リスナーを解除します。
    /// </summary>
    /// <param name="listener">解除するリスナー。</param>
    public void UnregisterListener(
        Action<GameplayModeChangedEventData> listener)
    {
        if (listener == null)
        {
            return;
        }

        m_modeChanged -= listener;
    }
}