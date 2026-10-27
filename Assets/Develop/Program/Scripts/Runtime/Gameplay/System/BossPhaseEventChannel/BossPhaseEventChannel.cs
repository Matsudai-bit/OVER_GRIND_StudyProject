using System;
using UnityEngine;

/// <summary>
/// ボスのフェーズ情報を通知するイベントチャネルです。
/// </summary>
[CreateAssetMenu(
    fileName = "BossPhaseEventChannel",
    menuName = "Game/Event Channels/Boss Phase Event Channel")]
public sealed class BossPhaseEventChannel : ScriptableObject
{
    // 現在のフェーズ
    private BossPhaseID m_currentPhase;

    // 現在フェーズが設定されているか
    private bool m_hasCurrentPhase;

    /// <summary>
    /// フェーズが変更されたときに通知されます。
    /// </summary>
    public event Action<BossPhaseID, BossPhaseID> PhaseChanged;

    /// <summary>
    /// 現在フェーズが設定されているか取得します。
    /// </summary>
    public bool HasCurrentPhase =>
        m_hasCurrentPhase;

    /// <summary>
    /// 現在のフェーズを取得します。
    /// </summary>
    public BossPhaseID CurrentPhase =>
        m_currentPhase;

    /// <summary>
    /// 初期フェーズを通知します。
    /// </summary>
    /// <param name="phaseID">初期フェーズ。</param>
    public void RaiseInitialPhase(
        BossPhaseID phaseID)
    {
        m_currentPhase = phaseID;
        m_hasCurrentPhase = true;

        // 初期化時は変更前も変更後も同じフェーズとして通知
        PhaseChanged?.Invoke(
            phaseID,
            phaseID);
    }

    /// <summary>
    /// フェーズ変更を通知します。
    /// </summary>
    public void RaisePhaseChanged(
        BossPhaseID previousPhase,
        BossPhaseID currentPhase)
    {
        m_currentPhase = currentPhase;
        m_hasCurrentPhase = true;

        PhaseChanged?.Invoke(
            previousPhase,
            currentPhase);
    }
}