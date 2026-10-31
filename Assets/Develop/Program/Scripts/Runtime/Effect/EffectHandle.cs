using System;
using System.Collections.Generic;

/// <summary>
/// 個別または一括再生の停止と終了通知を扱うハンドルです。
/// </summary>
public readonly struct EffectHandle
{
    // 個別の再生対象と世代番号
    private readonly EffectInstance m_instance;
    private readonly int m_generation;

    // 同じIDなどで一括再生した子ハンドル
    private readonly EffectHandle[] m_children;

    // 再生世代ごとに独立した終了通知チケット
    private readonly EffectCompletionTicket m_completionTicket;

    /// <summary>
    /// 単体再生のハンドルを作成します。
    /// </summary>
    public EffectHandle(EffectInstance instance, int generation)
    {
        m_instance = instance;
        m_generation = generation;
        m_children = null;
        m_completionTicket = instance != null && instance.Generation == generation
            ? instance.CompletionTicket : null;
    }

    /// <summary>
    /// 複数再生のハンドルを作成します。
    /// </summary>
    private EffectHandle(EffectHandle[] children)
    {
        m_instance = null;
        m_generation = 0;
        m_children = children;

        List<EffectCompletionTicket> tickets =
            new List<EffectCompletionTicket>(children.Length);
        foreach (EffectHandle child in children)
        {
            tickets.Add(child.m_completionTicket);
        }
        m_completionTicket = EffectCompletionTicket.Combine(tickets);
    }

    /// <summary>
    /// 再生中または自然停止待ちのインスタンスが存在するか取得します。
    /// </summary>
    public bool IsValid => ActiveCount > 0;

    /// <summary>
    /// 再生が完全終了したか取得します。
    /// </summary>
    public bool IsCompleted => m_completionTicket != null && m_completionTicket.IsCompleted;

    /// <summary>
    /// 終了した場合の理由を取得します。未終了・無効ハンドルはnullです。
    /// </summary>
    public EffectCompletionReason? CompletionReason => m_completionTicket?.CompletionReason;

    /// <summary>
    /// 現在有効なインスタンス数を取得します。
    /// </summary>
    public int ActiveCount
    {
        get
        {
            if (m_children == null)
            {
                return m_instance != null && m_instance.IsGenerationActive(m_generation) ? 1 : 0;
            }

            int count = 0;
            foreach (EffectHandle child in m_children)
            {
                count += child.ActiveCount;
            }
            return count;
        }
    }

    /// <summary>
    /// 再生終了時に実行する処理を登録します。
    /// 終了済みの場合は登録時に同期的に1回通知します。
    /// </summary>
    /// <param name="callback">終了理由を受け取る処理。</param>
    /// <returns>true：登録または通知しました。false：無効なハンドルか引数です。</returns>
    public bool RegisterCompletedCallback(Action<EffectCompletionReason> callback)
    {
        return m_completionTicket != null && m_completionTicket.Register(callback);
    }

    /// <summary>
    /// 再生終了時の処理を登録解除します。
    /// </summary>
    public void UnregisterCompletedCallback(Action<EffectCompletionReason> callback)
    {
        m_completionTicket?.Unregister(callback);
    }

    /// <summary>
    /// 再生中のすべてのインスタンスを停止します。
    /// </summary>
    /// <param name="stopMode">停止方法。</param>
    /// <returns>true：1件以上に停止要求できました。false：対象がありません。</returns>
    public bool Stop(EffectStopMode stopMode = EffectStopMode.GRACEFUL)
    {
        if (m_children == null)
        {
            return m_instance != null &&
                m_instance.StopIfGenerationMatches(m_generation, stopMode);
        }

        bool hasStopped = false;
        foreach (EffectHandle child in m_children)
        {
            // 全件停止するため短絡評価しません。
            if (child.Stop(stopMode))
            {
                hasStopped = true;
            }
        }
        return hasStopped;
    }

    /// <summary>
    /// 複数のハンドルを1つにまとめます。
    /// </summary>
    internal static EffectHandle Combine(IReadOnlyList<EffectHandle> handles)
    {
        if (handles == null || handles.Count == 0)
        {
            return default;
        }

        if (handles.Count == 1)
        {
            return handles[0];
        }

        // 一括再生リストの変更から切り離すためコピーします。
        EffectHandle[] children = new EffectHandle[handles.Count];
        for (int i = 0; i < handles.Count; i++)
        {
            children[i] = handles[i];
        }
        return new EffectHandle(children);
    }
}
