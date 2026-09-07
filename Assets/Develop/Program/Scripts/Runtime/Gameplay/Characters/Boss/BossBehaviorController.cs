using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// フェーズごとのBehavior Graph実行オブジェクトを管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class BossBehaviorController : MonoBehaviour
{
    /// <summary>
    /// フェーズごとのBehavior Graph参照です。
    /// </summary>
    [Serializable]
    private sealed class PhaseBehaviorEntry
    {
        // 対象フェーズ
        [SerializeField]
        private BossPhaseID m_phaseID;

        // Behavior Graphを実行するGameObject
        [SerializeField]
        private GameObject m_behaviorObject;

        /// <summary>
        /// 対象フェーズを取得します。
        /// </summary>
        public BossPhaseID PhaseID => m_phaseID;

        /// <summary>
        /// Behavior Graph実行オブジェクトを取得します。
        /// </summary>
        public GameObject BehaviorObject => m_behaviorObject;
    }

    // フェーズごとのBehavior Graph実行オブジェクト
    [SerializeField, Header("フェーズ別Behavior")]
    private List<PhaseBehaviorEntry> m_phaseBehaviors = new();

    // 現在使用しているBehavior Graph実行オブジェクト
    private GameObject m_currentBehaviorObject;

    /// <summary>
    /// 使用するフェーズのBehavior Graphへ切り替えます。
    /// </summary>
    /// <param name="phaseID">使用するフェーズ。</param>
    public void SetPhase(BossPhaseID phaseID)
    {
        m_currentBehaviorObject = null;

        foreach (PhaseBehaviorEntry entry in m_phaseBehaviors)
        {
            if (entry == null || entry.BehaviorObject == null)
            {
                continue;
            }

            bool isTargetPhase = entry.PhaseID == phaseID;
            entry.BehaviorObject.SetActive(isTargetPhase);

            if (isTargetPhase)
            {
                m_currentBehaviorObject = entry.BehaviorObject;
            }
        }
    }

    /// <summary>
    /// 現在のBehavior Graphを停止します。
    /// </summary>
    public void StopBehavior()
    {
        if (m_currentBehaviorObject == null)
        {
            return;
        }

        m_currentBehaviorObject.SetActive(false);
        m_currentBehaviorObject = null;
    }
}
