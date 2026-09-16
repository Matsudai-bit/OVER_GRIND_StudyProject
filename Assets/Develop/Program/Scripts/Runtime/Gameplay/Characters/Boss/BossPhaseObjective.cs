using System;
using UnityEngine;

/// <summary>
/// ボスフェーズの終了条件を提供します。
/// </summary>
public abstract class BossPhaseObjective : MonoBehaviour
{
    /// <summary>
    /// フェーズ終了条件を満たしたときに通知されます。
    /// </summary>
    public event Action<BossPhaseObjective> Completed;

    // フェーズ終了条件を満たしているか
    private bool m_isCompleted;

    /// <summary>
    /// フェーズ終了条件を満たしているか取得します。
    /// </summary>
    public bool IsCompleted => m_isCompleted;

    /// <summary>
    /// フェーズ終了条件の達成を通知します。
    /// </summary>
    protected void CompleteObjective()
    {
        if (m_isCompleted)
        {
            return;
        }

        m_isCompleted = true;
        Completed?.Invoke(this);
    }

    /// <summary>
    /// フェーズ終了条件を未達成状態へ戻します。
    /// </summary>
    public void ResetObjective()
    {
        m_isCompleted = false;
        OnResetObjective();
    }

    /// <summary>
    /// 派生クラス固有のリセット処理を行います。
    /// </summary>
    protected virtual void OnResetObjective()
    {
    }
}
