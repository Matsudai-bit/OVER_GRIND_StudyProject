using System;
using UnityEngine;

/// <summary>
/// ゲームプレイCueの状態変更を通知します。
/// </summary>
[CreateAssetMenu(
    fileName = "GameplayCueChangedEvent",
    menuName = "Game/Event/Gameplay Cue Changed Event")]
public sealed class GameplayCueChangedEvent : ScriptableObject
{
    // Cue変更イベント
    private event Action<GameplayCueChangedEventData> m_cueChanged;

    /// <summary>
    /// Cue変更イベントを発行します。
    /// </summary>
    /// <param name="eventData">Cue変更情報。</param>
    public void Raise(GameplayCueChangedEventData eventData)
    {
        m_cueChanged?.Invoke(eventData);
    }

    /// <summary>
    /// リスナーを登録します。
    /// </summary>
    /// <param name="listener">登録するリスナー。</param>
    public void RegisterListener(
        Action<GameplayCueChangedEventData> listener)
    {
        if (listener == null)
        {
            return;
        }

        m_cueChanged -= listener;
        m_cueChanged += listener;
    }

    /// <summary>
    /// リスナーを解除します。
    /// </summary>
    /// <param name="listener">解除するリスナー。</param>
    public void UnregisterListener(
        Action<GameplayCueChangedEventData> listener)
    {
        if (listener == null)
        {
            return;
        }

        m_cueChanged -= listener;
    }
}