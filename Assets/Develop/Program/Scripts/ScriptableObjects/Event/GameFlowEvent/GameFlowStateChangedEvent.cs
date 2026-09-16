using System;
using UnityEngine;

/// <summary>
/// ゲーム進行状態の変更を通知します。
/// </summary>
[CreateAssetMenu(
    fileName = "GameFlowStateChangedEvent",
    menuName = "Game/Event/Game Flow State Changed Event")]
public sealed class GameFlowStateChangedEvent : ScriptableObject
{
    // 状態変更イベント
    private event Action<GameFlowStateChangedEventData> m_stateChanged;

    /// <summary>
    /// 状態変更イベントを発行します。
    /// </summary>
    /// <param name="eventData">状態変更情報。</param>
    public void Raise(GameFlowStateChangedEventData eventData)
    {
        m_stateChanged?.Invoke(eventData);
    }

    /// <summary>
    /// リスナーを登録します。
    /// </summary>
    /// <param name="listener">登録するリスナー。</param>
    public void RegisterListener(
        Action<GameFlowStateChangedEventData> listener)
    {
        if (listener == null)
        {
            return;
        }

        m_stateChanged -= listener;
        m_stateChanged += listener;
    }

    /// <summary>
    /// リスナーを解除します。
    /// </summary>
    /// <param name="listener">解除するリスナー。</param>
    public void UnregisterListener(
        Action<GameFlowStateChangedEventData> listener)
    {
        if (listener == null)
        {
            return;
        }

        m_stateChanged -= listener;
    }
}