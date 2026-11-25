// PlayerDiedEvent.cs
using System;
using UnityEngine;

/// <summary>
/// プレイヤーの死亡をゲーム全体へ通知します。
/// </summary>
[CreateAssetMenu(
    fileName = "PlayerDiedEvent",
    menuName = "Game/Event/Player Died Event")]
public sealed class PlayerDiedEvent : ScriptableObject
{
    // プレイヤー死亡イベント
    private event Action m_playerDied;

    /// <summary>
    /// プレイヤー死亡イベントを発行します。
    /// </summary>
    public void Raise()
    {
        m_playerDied?.Invoke();
    }

    /// <summary>
    /// リスナーを登録します。
    /// </summary>
    /// <param name="listener">登録するリスナー。</param>
    public void RegisterListener(
        Action listener)
    {
        if (listener == null)
        {
            return;
        }

        m_playerDied -= listener;
        m_playerDied += listener;
    }

    /// <summary>
    /// リスナーを解除します。
    /// </summary>
    /// <param name="listener">解除するリスナー。</param>
    public void UnregisterListener(
        Action listener)
    {
        if (listener == null)
        {
            return;
        }

        m_playerDied -= listener;
    }
}