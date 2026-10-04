using System;
using UnityEngine;

/// <summary>
/// ゲームプレイシーケンスを実行する基底クラスです。
/// </summary>
public abstract class GameplaySequenceRunner : MonoBehaviour
{
    // 実行するシーケンス
    [SerializeField, Header("シーケンス設定")]
    private GameplaySequenceAsset m_sequence;

    // 実行中か
    private bool m_isRunning = false;

    /// <summary>
    /// シーケンス完了時に通知されます。
    /// </summary>
    public event Action<GameplaySequenceRunner> Completed;

    /// <summary>
    /// 実行対象のシーケンスを取得します。
    /// </summary>
    public GameplaySequenceAsset Sequence => m_sequence;

    /// <summary>
    /// シーケンス実行中か取得します。
    /// </summary>
    public bool IsRunning => m_isRunning;

    /// <summary>
    /// シーケンスを開始します。
    /// </summary>
    /// <returns>
    /// true：開始しました。
    /// false：開始できませんでした。
    /// </returns>
    public bool StartSequence()
    {
        if (m_isRunning)
        {
            return false;
        }

        if (m_sequence == null)
        {
            Debug.LogWarning(
                $"{nameof(GameplaySequenceAsset)} が設定されていません。",
                this);

            return false;
        }

        m_isRunning = true;

        if (OnStartSequence())
        {
            return true;
        }

        m_isRunning = false;

        return false;
    }

    /// <summary>
    /// シーケンスを中断します。
    /// </summary>
    /// <returns>
    /// true：中断しました。
    /// false：中断できませんでした。
    /// </returns>
    public bool StopSequence()
    {
        if (!m_isRunning)
        {
            return false;
        }

        // 停止処理中の完了通知を防ぐ
        m_isRunning = false;

        OnStopSequence();

        return true;
    }

    /// <summary>
    /// シーケンス開始時の処理を実行します。
    /// </summary>
    /// <returns>
    /// true：開始処理に成功しました。
    /// false：開始処理に失敗しました。
    /// </returns>
    protected abstract bool OnStartSequence();

    /// <summary>
    /// シーケンス停止時の処理を実行します。
    /// </summary>
    protected abstract void OnStopSequence();

    /// <summary>
    /// シーケンス完了を通知します。
    /// </summary>
    protected void CompleteSequence()
    {
        if (!m_isRunning)
        {
            return;
        }

        m_isRunning = false;

        Completed?.Invoke(this);
    }
}