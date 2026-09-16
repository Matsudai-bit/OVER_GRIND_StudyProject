using System;
using UnityEngine;

/// <summary>
/// 現在のセーブデータの保存要求を通知します。
/// </summary>
[CreateAssetMenu(
    fileName = "SaveRequestEvent",
    menuName = "Game/Event/Save Request Event")]
public sealed class SaveRequestEvent : ScriptableObject
{
    // セーブ要求
    private event Action m_requested;

    /// <summary>
    /// セーブ要求を発行します。
    /// </summary>
    public void Raise()
    {
        m_requested?.Invoke();
    }

    /// <summary>
    /// リスナーを登録します。
    /// </summary>
    public void RegisterListener(Action listener)
    {
        if (listener == null)
        {
            return;
        }

        m_requested -= listener;
        m_requested += listener;
    }

    /// <summary>
    /// リスナーを解除します。
    /// </summary>
    public void UnregisterListener(Action listener)
    {
        if (listener == null)
        {
            return;
        }

        m_requested -= listener;
    }
}