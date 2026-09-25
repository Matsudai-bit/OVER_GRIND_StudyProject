using System;
using UnityEngine;

/// <summary>
/// ポーズ状態の変更要求を通知します。
/// </summary>
[CreateAssetMenu(
    fileName = "PauseRequestEvent",
    menuName = "Game/Event/Pause Request Event")]
public sealed class PauseRequestEvent : ScriptableObject
{
    // ポーズ要求イベント
    private event Action<PauseRequestEventData> m_requested;

    /// <summary>
    /// ポーズ要求を発行します。
    /// </summary>
    public void RaisePause()
    {
        Raise(PauseRequestType.PAUSE);
    }

    /// <summary>
    /// ポーズ解除要求を発行します。
    /// </summary>
    public void RaiseResume()
    {
        Raise(PauseRequestType.RESUME);
    }

    /// <summary>
    /// ポーズ状態切り替え要求を発行します。
    /// </summary>
    public void RaiseToggle()
    {
        Raise(PauseRequestType.TOGGLE);
    }

    /// <summary>
    /// リスナーを登録します。
    /// </summary>
    /// <param name="listener">登録するリスナー。</param>
    public void RegisterListener(
        Action<PauseRequestEventData> listener)
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
        Action<PauseRequestEventData> listener)
    {
        if (listener == null)
        {
            return;
        }

        m_requested -= listener;
    }

    /// <summary>
    /// ポーズ要求を通知します。
    /// </summary>
    /// <param name="requestType">要求種別。</param>
    private void Raise(PauseRequestType requestType)
    {
        PauseRequestEventData eventData =
            new PauseRequestEventData(requestType);

        m_requested?.Invoke(eventData);
    }
}