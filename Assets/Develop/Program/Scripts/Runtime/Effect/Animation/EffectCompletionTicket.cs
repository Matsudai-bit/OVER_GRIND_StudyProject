using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 1回の再生要求に対する終了通知を保持します。
/// </summary>
internal sealed class EffectCompletionTicket
{
    // 再生終了の購読者
    private Action<EffectCompletionReason> m_completedCallbacks;

    // 再生終了時の状態
    private bool m_isCompleted;
    private EffectCompletionReason m_completionReason;

    // 複数再生をまとめる場合の未終了件数
    private int m_remainingCount;
    private EffectCompletionReason m_aggregateReason = EffectCompletionReason.COMPLETED;

    /// <summary>
    /// 再生が終了済みか取得します。
    /// </summary>
    public bool IsCompleted => m_isCompleted;

    /// <summary>
    /// 終了済みの場合に終了理由を取得します。
    /// </summary>
    public EffectCompletionReason? CompletionReason =>
        m_isCompleted ? m_completionReason : (EffectCompletionReason?)null;

    /// <summary>
    /// 終了イベントに登録します。
    /// 終了済みなら登録時に1回通知します。
    /// </summary>
    /// <param name="callback">再生終了時に実行する処理。</param>
    /// <returns>true：登録または通知しました。false：引数が無効です。</returns>
    public bool Register(Action<EffectCompletionReason> callback)
    {
        if (callback == null)
        {
            return false;
        }

        if (m_isCompleted)
        {
            InvokeSafely(callback, m_completionReason);
            return true;
        }

        m_completedCallbacks += callback;
        return true;
    }

    /// <summary>
    /// 終了イベントの登録を解除します。
    /// </summary>
    public void Unregister(Action<EffectCompletionReason> callback)
    {
        if (callback != null)
        {
            m_completedCallbacks -= callback;
        }
    }

    /// <summary>
    /// 再生終了を1度だけ通知します。
    /// </summary>
    public void Complete(EffectCompletionReason reason)
    {
        if (m_isCompleted)
        {
            return;
        }

        m_isCompleted = true;
        m_completionReason = reason;

        // 通知前に購読リストを開放し、コールバック経由の再登録にも対応します。
        Action<EffectCompletionReason> callbacks = m_completedCallbacks;
        m_completedCallbacks = null;
        if (callbacks == null)
        {
            return;
        }

        // ある購読者が例外を投げても、残りの購読者へ通知します。
        foreach (Delegate subscriber in callbacks.GetInvocationList())
        {
            InvokeSafely((Action<EffectCompletionReason>)subscriber, reason);
        }
    }

    /// <summary>
    /// 子の再生がすべて終了したときだけ通知するチケットを作成します。
    /// </summary>
    /// <param name="children">個々の再生チケット。</param>
    /// <returns>一括再生用の終了チケット。</returns>
    public static EffectCompletionTicket Combine(IReadOnlyList<EffectCompletionTicket> children)
    {
        if (children == null || children.Count == 0)
        {
            return null;
        }

        if (children.Count == 1)
        {
            return children[0];
        }

        EffectCompletionTicket group = new EffectCompletionTicket();
        group.m_remainingCount = children.Count;

        // 終了済みの子についてもRegister()が即時通知するため、取りこぼしません。
        foreach (EffectCompletionTicket child in children)
        {
            if (child == null)
            {
                group.OnChildCompleted(EffectCompletionReason.CANCELLED);
                continue;
            }

            child.Register(group.OnChildCompleted);
        }
        return group;
    }

    /// <summary>
    /// 子の終了を集計し、すべて終了したら一度だけ通知します。
    /// </summary>
    private void OnChildCompleted(EffectCompletionReason reason)
    {
        if (m_isCompleted)
        {
            return;
        }

        // 複合結果の優先順位：CANCELLED > STOPPED > COMPLETED。
        if (reason == EffectCompletionReason.CANCELLED ||
            (reason == EffectCompletionReason.STOPPED &&
             m_aggregateReason == EffectCompletionReason.COMPLETED))
        {
            m_aggregateReason = reason;
        }

        m_remainingCount--;
        if (m_remainingCount == 0)
        {
            Complete(m_aggregateReason);
        }
    }

    /// <summary>
    /// 例外による通知の中断を防ぎます。
    /// </summary>
    private static void InvokeSafely(
        Action<EffectCompletionReason> callback, EffectCompletionReason reason)
    {
        try
        {
            callback(reason);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }
}
