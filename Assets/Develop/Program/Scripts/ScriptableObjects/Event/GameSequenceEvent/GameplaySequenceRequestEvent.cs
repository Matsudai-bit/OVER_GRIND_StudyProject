using System;
using UnityEngine;

/// <summary>
/// ゲームプレイシーケンスの開始要求を通知します。
/// </summary>
[CreateAssetMenu(
    fileName = "GameplaySequenceRequestEvent",
    menuName = "Game/Event/Gameplay Sequence Request Event")]
public sealed class GameplaySequenceRequestEvent : ScriptableObject
{
    // シーケンス開始要求イベント
    private event Action<GameplaySequenceRequestEventData> m_requested;

    /// <summary>
    /// シーケンス開始要求を発行します。
    /// </summary>
    /// <param name="sequence">開始するシーケンス。</param>
    public void Raise(GameplaySequenceAsset sequence)
    {
        if (sequence == null)
        {
            Debug.LogWarning(
                "開始要求するGameplaySequenceAssetが指定されていません。",
                this);

            return;
        }

        GameplaySequenceRequestEventData eventData =
            new GameplaySequenceRequestEventData(sequence);

        m_requested?.Invoke(eventData);
    }

    /// <summary>
    /// リスナーを登録します。
    /// </summary>
    /// <param name="listener">登録するリスナー。</param>
    public void RegisterListener(
        Action<GameplaySequenceRequestEventData> listener)
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
    /// <param name="listener">解除するリスナー。</param>
    public void UnregisterListener(
        Action<GameplaySequenceRequestEventData> listener)
    {
        if (listener == null)
        {
            return;
        }

        m_requested -= listener;
    }
}