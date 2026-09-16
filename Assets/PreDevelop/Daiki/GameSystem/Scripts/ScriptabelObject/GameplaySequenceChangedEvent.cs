using System;
using UnityEngine;

/// <summary>
/// ゲームプレイシーケンスの変更を通知します。
/// </summary>
[CreateAssetMenu(
    fileName = "GameplaySequenceChangedEvent",
    menuName = "Game/Event/Gameplay Sequence Changed Event")]
public sealed class GameplaySequenceChangedEvent : ScriptableObject
{
    // シーケンス変更イベント
    private event Action<GameplaySequenceChangedEventData> m_sequenceChanged;

    /// <summary>
    /// シーケンス変更イベントを発行します。
    /// </summary>
    /// <param name="eventData">シーケンス変更情報。</param>
    public void Raise(GameplaySequenceChangedEventData eventData)
    {
        m_sequenceChanged?.Invoke(eventData);
    }

    /// <summary>
    /// リスナーを登録します。
    /// </summary>
    /// <param name="listener">登録するリスナー。</param>
    public void RegisterListener(
        Action<GameplaySequenceChangedEventData> listener)
    {
        if (listener == null)
        {
            return;
        }

        m_sequenceChanged -= listener;
        m_sequenceChanged += listener;
    }

    /// <summary>
    /// リスナーを解除します。
    /// </summary>
    /// <param name="listener">解除するリスナー。</param>
    public void UnregisterListener(
        Action<GameplaySequenceChangedEventData> listener)
    {
        if (listener == null)
        {
            return;
        }

        m_sequenceChanged -= listener;
    }
}