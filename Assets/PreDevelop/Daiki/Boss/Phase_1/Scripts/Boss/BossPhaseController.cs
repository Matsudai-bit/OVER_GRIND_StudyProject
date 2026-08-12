using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ボスのフェーズとフェーズ遷移要求を管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class BossPhaseController : MonoBehaviour
{
    /// <summary>
    /// フェーズ設定です。
    /// </summary>
    [Serializable]
    private sealed class PhaseDefinition
    {
        // フェーズID
        [SerializeField]
        private BossPhaseID m_phaseID;

        // フェーズ中に使用するルートオブジェクト
        [SerializeField]
        private GameObject m_phaseRoot;

        // フェーズ終了条件
        [SerializeField]
        private BossPhaseObjective m_objective;

        /// <summary>
        /// フェーズIDを取得します。
        /// </summary>
        public BossPhaseID PhaseID => m_phaseID;

        /// <summary>
        /// フェーズルートを取得します。
        /// </summary>
        public GameObject PhaseRoot => m_phaseRoot;

        /// <summary>
        /// フェーズ終了条件を取得します。
        /// </summary>
        public BossPhaseObjective Objective => m_objective;
    }

    // 初期フェーズ
    [SerializeField, Header("フェーズ設定")]
    private BossPhaseID m_initialPhase = BossPhaseID.PHASE_1;

    // フェーズ定義
    [SerializeField]
    private List<PhaseDefinition> m_phaseDefinitions = new();

    // Behavior Graph管理
    [SerializeField, Header("参照")]
    private BossBehaviorController m_behaviorController;

    // アニメーション管理
    [SerializeField]
    private BossAnimationController m_animationController;

    // 現在のフェーズ位置
    private int m_currentPhaseIndex = -1;

    // フェーズ遷移中か
    private bool m_isTransitioning;

    /// <summary>
    /// フェーズ終了条件を満たしたときに通知されます。
    /// </summary>
    public event Action<BossPhaseID> PhaseCompletionRequested;

    /// <summary>
    /// フェーズが変更されたときに通知されます。
    /// </summary>
    public event Action<BossPhaseID> PhaseChanged;

    /// <summary>
    /// 現在のフェーズを取得します。
    /// </summary>
    public BossPhaseID CurrentPhase
    {
        get
        {
            if (!IsValidPhaseIndex(m_currentPhaseIndex))
            {
                return m_initialPhase;
            }

            return m_phaseDefinitions[m_currentPhaseIndex].PhaseID;
        }
    }

    /// <summary>
    /// フェーズ遷移中か取得します。
    /// </summary>
    public bool IsTransitioning => m_isTransitioning;

    /// <summary>
    /// 初期化します。
    /// </summary>
    private void Awake()
    {
        CacheComponents();
        SubscribeObjectives();

        if (!TryFindPhaseIndex(m_initialPhase, out int initialPhaseIndex))
        {
            Debug.LogError(
                $"初期フェーズ{m_initialPhase}の設定がありません。",
                this);

            enabled = false;
            return;
        }

        m_currentPhaseIndex = initialPhaseIndex;
        ApplyCurrentPhase();
    }

    /// <summary>
    /// イベント購読を解除します。
    /// </summary>
    private void OnDestroy()
    {
        UnsubscribeObjectives();
    }

    /// <summary>
    /// 次のフェーズへ進めます。
    /// </summary>
    /// <returns>
    /// true：次のフェーズへ進みました。
    /// false：次のフェーズがありません。
    /// </returns>
    public bool AdvancePhase()
    {
        int nextPhaseIndex = m_currentPhaseIndex + 1;

        if (!IsValidPhaseIndex(nextPhaseIndex))
        {
            return false;
        }

        m_currentPhaseIndex = nextPhaseIndex;
        m_isTransitioning = false;

        ApplyCurrentPhase();
        PhaseChanged?.Invoke(CurrentPhase);
        return true;
    }

    /// <summary>
    /// 指定フェーズへ切り替えます。
    /// </summary>
    /// <param name="phaseID">切り替えるフェーズ。</param>
    /// <returns>
    /// true：切り替えました。
    /// false：対象フェーズがありません。
    /// </returns>
    public bool SetPhase(BossPhaseID phaseID)
    {
        if (!TryFindPhaseIndex(phaseID, out int phaseIndex))
        {
            return false;
        }

        m_currentPhaseIndex = phaseIndex;
        m_isTransitioning = false;

        ApplyCurrentPhase();
        PhaseChanged?.Invoke(CurrentPhase);
        return true;
    }

    /// <summary>
    /// フェーズ終了通知を処理します。
    /// </summary>
    /// <param name="objective">達成された終了条件。</param>
    private void HandleObjectiveCompleted(BossPhaseObjective objective)
    {
        if (m_isTransitioning ||
            !IsValidPhaseIndex(m_currentPhaseIndex))
        {
            return;
        }

        PhaseDefinition currentPhase =
            m_phaseDefinitions[m_currentPhaseIndex];

        if (currentPhase.Objective != objective)
        {
            return;
        }

        m_isTransitioning = true;
        m_behaviorController?.StopBehavior();

        PhaseCompletionRequested?.Invoke(currentPhase.PhaseID);
    }

    /// <summary>
    /// 現在フェーズの設定を適用します。
    /// </summary>
    private void ApplyCurrentPhase()
    {
        if (!IsValidPhaseIndex(m_currentPhaseIndex))
        {
            return;
        }

        for (int i = 0; i < m_phaseDefinitions.Count; i++)
        {
            PhaseDefinition phaseDefinition = m_phaseDefinitions[i];

            if (phaseDefinition?.PhaseRoot == null)
            {
                continue;
            }

            phaseDefinition.PhaseRoot.SetActive(i == m_currentPhaseIndex);
        }

        BossPhaseID currentPhase = CurrentPhase;
        m_animationController?.SetPhase(currentPhase);
        m_behaviorController?.SetPhase(currentPhase);
    }

    /// <summary>
    /// 終了条件のイベントを購読します。
    /// </summary>
    private void SubscribeObjectives()
    {
        foreach (PhaseDefinition phaseDefinition in m_phaseDefinitions)
        {
            if (phaseDefinition?.Objective == null)
            {
                continue;
            }

            phaseDefinition.Objective.Completed += HandleObjectiveCompleted;
        }
    }

    /// <summary>
    /// 終了条件のイベント購読を解除します。
    /// </summary>
    private void UnsubscribeObjectives()
    {
        foreach (PhaseDefinition phaseDefinition in m_phaseDefinitions)
        {
            if (phaseDefinition?.Objective == null)
            {
                continue;
            }

            phaseDefinition.Objective.Completed -= HandleObjectiveCompleted;
        }
    }

    /// <summary>
    /// 必要なコンポーネントを取得します。
    /// </summary>
    private void CacheComponents()
    {
        if (m_behaviorController == null)
        {
            m_behaviorController = GetComponent<BossBehaviorController>();
        }

        if (m_animationController == null)
        {
            m_animationController = GetComponent<BossAnimationController>();
        }
    }

    /// <summary>
    /// 指定フェーズの位置を取得します。
    /// </summary>
    /// <param name="phaseID">検索するフェーズ。</param>
    /// <param name="phaseIndex">取得した位置。</param>
    /// <returns>
    /// true：見つかりました。
    /// false：見つかりませんでした。
    /// </returns>
    private bool TryFindPhaseIndex(
        BossPhaseID phaseID,
        out int phaseIndex)
    {
        for (int i = 0; i < m_phaseDefinitions.Count; i++)
        {
            PhaseDefinition phaseDefinition = m_phaseDefinitions[i];

            if (phaseDefinition != null &&
                phaseDefinition.PhaseID == phaseID)
            {
                phaseIndex = i;
                return true;
            }
        }

        phaseIndex = -1;
        return false;
    }

    /// <summary>
    /// フェーズ位置が有効か確認します。
    /// </summary>
    /// <param name="phaseIndex">確認する位置。</param>
    /// <returns>
    /// true：有効です。
    /// false：無効です。
    /// </returns>
    private bool IsValidPhaseIndex(int phaseIndex)
    {
        return phaseIndex >= 0 &&
               phaseIndex < m_phaseDefinitions.Count;
    }
}
