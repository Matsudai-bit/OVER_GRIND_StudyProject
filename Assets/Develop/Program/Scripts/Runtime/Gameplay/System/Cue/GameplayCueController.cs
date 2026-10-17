using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ゲームプレイ中のCueを管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class GameplayCueController : MonoBehaviour
{
    // Cue要求イベント
    [SerializeField, Header("Cue要求イベント")]
    private GameplayCueRequestEvent m_cueRequestEvent;

    // Cue変更イベント
    [SerializeField, Header("Cue変更イベント")]
    private GameplayCueChangedEvent m_cueChangedEvent;

    // 実行中Cueと自動終了Coroutine
    private readonly Dictionary<GameplayCueAsset, Coroutine> m_activeCues = new();

    /// <summary>
    /// 実行中のCue数を取得します。
    /// </summary>
    public int ActiveCueCount => m_activeCues.Count;

    private void OnEnable()
    {
        if (m_cueRequestEvent == null)
        {
            Debug.LogWarning(
                $"{nameof(GameplayCueRequestEvent)} が設定されていません。",
                this);

            return;
        }

        m_cueRequestEvent.RegisterListener(
            OnCueRequested);
    }

    private void OnDisable()
    {
        if (m_cueRequestEvent != null)
        {
            m_cueRequestEvent.UnregisterListener(
                OnCueRequested);
        }

        StopAllCoroutines();
        m_activeCues.Clear();
    }

    /// <summary>
    /// Cueを開始します。
    /// </summary>
    /// <param name="cue">開始するCue。</param>
    /// <returns>
    /// true：開始しました。
    /// false：開始できませんでした。
    /// </returns>
    public bool StartCue(GameplayCueAsset cue)
    {
        if (cue == null)
        {
            return false;
        }

        // 同じCueの重複実行を防止
        if (m_activeCues.ContainsKey(cue))
        {
            return false;
        }

        // 通知中に終了要求が来ても処理できるよう先に登録
        m_activeCues.Add(cue, null);

        RaiseCueChangedEvent(
            cue,
            GameplayCueEventType.STARTED);

        // 通知中に終了している場合は自動終了処理を開始しない
        if (!m_activeCues.ContainsKey(cue))
        {
            return true;
        }

        if (cue.HasDuration)
        {
            Coroutine coroutine = StartCoroutine(
                WaitForCueDuration(cue));

            m_activeCues[cue] = coroutine;
        }

        return true;
    }

    /// <summary>
    /// Cueを終了します。
    /// </summary>
    /// <param name="cue">終了するCue。</param>
    /// <returns>
    /// true：終了しました。
    /// false：終了できませんでした。
    /// </returns>
    public bool FinishCue(GameplayCueAsset cue)
    {
        if (cue == null)
        {
            return false;
        }

        if (!m_activeCues.TryGetValue(
            cue,
            out Coroutine coroutine))
        {
            return false;
        }

        if (coroutine != null)
        {
            StopCoroutine(coroutine);
        }

        m_activeCues.Remove(cue);

        RaiseCueChangedEvent(
            cue,
            GameplayCueEventType.FINISHED);

        return true;
    }

    /// <summary>
    /// 実行中のCueをすべて終了します。
    /// </summary>
    public void FinishAllCues()
    {
        if (m_activeCues.Count <= 0)
        {
            return;
        }

        // 終了処理でDictionaryが変更されるためコピーして処理
        List<GameplayCueAsset> activeCues =
            new List<GameplayCueAsset>(m_activeCues.Keys);

        foreach (GameplayCueAsset cue in activeCues)
        {
            FinishCue(cue);
        }
    }

    /// <summary>
    /// 指定したCueが実行中か確認します。
    /// </summary>
    /// <param name="cue">確認するCue。</param>
    /// <returns>
    /// true：実行中です。
    /// false：実行中ではありません。
    /// </returns>
    public bool IsCueActive(GameplayCueAsset cue)
    {
        if (cue == null)
        {
            return false;
        }

        return m_activeCues.ContainsKey(cue);
    }

    /// <summary>
    /// Cue要求を処理します。
    /// </summary>
    /// <param name="eventData">Cue要求情報。</param>
    private void OnCueRequested(
        GameplayCueRequestEventData eventData)
    {
        switch (eventData.RequestType)
        {
            case GameplayCueRequestType.START:
                StartCue(eventData.Cue);
                break;

            case GameplayCueRequestType.FINISH:
                FinishCue(eventData.Cue);
                break;
        }
    }

    /// <summary>
    /// 指定時間後にCueを終了します。
    /// </summary>
    /// <param name="cue">終了するCue。</param>
    private IEnumerator WaitForCueDuration(
        GameplayCueAsset cue)
    {
        yield return new WaitForSeconds(cue.Duration);

        FinishCue(cue);
    }

    /// <summary>
    /// Cue状態変更を通知します。
    /// </summary>
    /// <param name="cue">対象のCue。</param>
    /// <param name="eventType">変更種別。</param>
    private void RaiseCueChangedEvent(
        GameplayCueAsset cue,
        GameplayCueEventType eventType)
    {
        if (m_cueChangedEvent == null)
        {
            Debug.LogWarning(
                $"{nameof(GameplayCueChangedEvent)} が設定されていません。",
                this);

            return;
        }

        GameplayCueChangedEventData eventData =
            new GameplayCueChangedEventData(
                cue,
                eventType);

        m_cueChangedEvent.Raise(eventData);
    }
}