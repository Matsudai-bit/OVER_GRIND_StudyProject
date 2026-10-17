using System;
using UnityEngine;

/// <summary>
/// セーブデータの状態変更を通知します。
/// </summary>
[CreateAssetMenu(
    fileName = "SaveChangedEvent",
    menuName = "Game/Event/Save Changed Event")]
public sealed class SaveChangedEvent : ScriptableObject
{
    // セーブ状態変更イベント
    private event Action<SaveChangedEventData> m_changed;

    /// <summary>
    /// 状態変更を通知します。
    /// </summary>
    public void Raise(SaveChangedEventData eventData)
    {
        m_changed?.Invoke(eventData);
    }

    /// <summary>
    /// リスナーを登録します。
    /// </summary>
    public void RegisterListener(
        Action<SaveChangedEventData> listener)
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
    public void UnregisterListener(
        Action<SaveChangedEventData> listener)
    {
        if (listener == null)
        {
            return;
        }

        m_changed -= listener;
    }
}