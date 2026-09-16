using System;
using UnityEngine;

/// <summary>
/// ゲームプレイCueへの要求を通知します。
/// </summary>
[CreateAssetMenu(
    fileName = "GameplayCueRequestEvent",
    menuName = "Game/Event/Gameplay Cue Request Event")]
public sealed class GameplayCueRequestEvent : ScriptableObject
{
    // Cue要求イベント
    private event Action<GameplayCueRequestEventData> m_requested;

    /// <summary>
    /// Cue開始要求を発行します。
    /// </summary>
    /// <param name="cue">開始するCue。</param>
    public void RaiseStart(GameplayCueAsset cue)
    {
        Raise(cue, GameplayCueRequestType.START);
    }

    /// <summary>
    /// Cue終了要求を発行します。
    /// </summary>
    /// <param name="cue">終了するCue。</param>
    public void RaiseFinish(GameplayCueAsset cue)
    {
        Raise(cue, GameplayCueRequestType.FINISH);
    }

    /// <summary>
    /// リスナーを登録します。
    /// </summary>
    /// <param name="listener">登録するリスナー。</param>
    public void RegisterListener(
        Action<GameplayCueRequestEventData> listener)
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
        Action<GameplayCueRequestEventData> listener)
    {
        if (listener == null)
        {
            return;
        }

        m_requested -= listener;
    }

    /// <summary>
    /// Cue要求を通知します。
    /// </summary>
    /// <param name="cue">対象のCue。</param>
    /// <param name="requestType">要求種別。</param>
    private void Raise(
        GameplayCueAsset cue,
        GameplayCueRequestType requestType)
    {
        if (cue == null)
        {
            Debug.LogWarning(
                $"{nameof(GameplayCueAsset)} が指定されていません。",
                this);

            return;
        }

        GameplayCueRequestEventData eventData =
            new GameplayCueRequestEventData(
                cue,
                requestType);

        m_requested?.Invoke(eventData);
    }
}