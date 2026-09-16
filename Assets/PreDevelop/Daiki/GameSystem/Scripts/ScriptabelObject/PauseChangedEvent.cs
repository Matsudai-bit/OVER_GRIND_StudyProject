using System;
using UnityEngine;

/// <summary>
/// ポーズ状態の変更を通知します。
/// </summary>
[CreateAssetMenu(
    fileName = "PauseChangedEvent",
    menuName = "Game/Event/Pause Changed Event")]
public sealed class PauseChangedEvent : ScriptableObject
{
    // ポーズ状態変更イベント
    private event Action<PauseChangedEventData> m_pauseChanged;

    /// <summary>
    /// ポーズ状態変更イベントを発行します。
    /// </summary>
    /// <param name="eventData">ポーズ状態変更情報。</param>
    public void Raise(PauseChangedEventData eventData)
    {
        m_pauseChanged?.Invoke(eventData);
    }

    /// <summary>
    /// リスナーを登録します。
    /// </summary>
    /// <param name="listener">登録するリスナー。</param>
    public void RegisterListener(
        Action<PauseChangedEventData> listener)
    {
        if (listener == null)
        {
            return;
        }

        m_pauseChanged -= listener;
        m_pauseChanged += listener;
    }

    /// <summary>
    /// リスナーを解除します。
    /// </summary>
    /// <param name="listener">解除するリスナー。</param>
    public void UnregisterListener(
        Action<PauseChangedEventData> listener)
    {
        if (listener == null)
        {
            return;
        }

        m_pauseChanged -= listener;
    }
}