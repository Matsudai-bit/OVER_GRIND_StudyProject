using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ボスのフェーズとフェーズ遷移要求を管理します。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BossController))]
[RequireComponent(typeof(BossBehaviorController))]
[RequireComponent(typeof(BossAnimationController))]
[RequireComponent(typeof(BossNavigation))]
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

        // フェーズ共通参照
        [SerializeField]
        private BossPhaseReferences m_phaseReferences;

        // フェーズパラメータ供給元
        [SerializeField]
        private BossPhaseParameterProvider m_parameterProvider;

        // フェーズごとの攻撃ダメージ設定
        [SerializeField]
        private BossPhaseAttackDamageProvider m_attackDamageProvider;

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

        /// <summary>
        /// フェーズ共通参照を取得します。
        /// </summary>
        public BossPhaseReferences PhaseReferences =>
            m_phaseReferences;

        /// <summary>
        /// フェーズパラメータ供給元を取得します。
        /// </summary>
        public BossPhaseParameterProvider ParameterProvider =>
            m_parameterProvider;

        /// <summary>
        /// 攻撃ダメージ設定を取得します。
        /// </summary>
        public BossPhaseAttackDamageProvider AttackDamageProvider =>
            m_attackDamageProvider;
    }

    // 初期フェーズ
    [SerializeField, Header("フェーズ設定")]
    private BossPhaseID m_initialPhase =
        BossPhaseID.PHASE_1;

    // フェーズ定義
    [SerializeField]
    private List<PhaseDefinition> m_phaseDefinitions = new();

    // ボス制御
    [SerializeField, Header("参照")]
    private BossController m_bossController;

    // Behavior Graph管理
    [SerializeField]
    private BossBehaviorController m_behaviorController;

    // アニメーション管理
    [SerializeField]
    private BossAnimationController m_animationController;

    // Navigation管理
    [SerializeField]
    private BossNavigation m_navigation;

    // 現在のフェーズ位置
    private int m_currentPhaseIndex = -1;

    // フェーズ遷移中か
    private bool m_isTransitioning;

    // 現在適用されている攻撃ダメージ設定
    private BossPhaseAttackDamageProvider m_currentAttackDamageProvider;

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

            return m_phaseDefinitions[
                m_currentPhaseIndex].PhaseID;
        }
    }

    /// <summary>
    /// フェーズ遷移中か取得します。
    /// </summary>
    public bool IsTransitioning =>
        m_isTransitioning;

    /// <summary>
    /// 初期化します。
    /// </summary>
    private void Awake()
    {
        ResolveReferences();

        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        SubscribeObjectives();

        if (!TryFindPhaseIndex(
                m_initialPhase,
                out int initialPhaseIndex))
        {
            Debug.LogError(
                $"[{nameof(BossPhaseController)}] " +
                $"初期フェーズ {m_initialPhase} の設定がありません。",
                this);

            enabled = false;
            return;
        }

        m_currentPhaseIndex =
            initialPhaseIndex;

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
        int nextPhaseIndex =
            m_currentPhaseIndex + 1;

        if (!IsValidPhaseIndex(nextPhaseIndex))
        {
            return false;
        }

        m_currentPhaseIndex =
            nextPhaseIndex;

        m_isTransitioning = false;

        ApplyCurrentPhase();

        PhaseChanged?.Invoke(
            CurrentPhase);

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
    public bool SetPhase(
        BossPhaseID phaseID)
    {
        if (!TryFindPhaseIndex(
                phaseID,
                out int phaseIndex))
        {
            return false;
        }

        m_currentPhaseIndex =
            phaseIndex;

        m_isTransitioning = false;

        ApplyCurrentPhase();

        PhaseChanged?.Invoke(
            CurrentPhase);

        return true;
    }

    /// <summary>
    /// 現在のフェーズから指定コンポーネントを取得します。
    /// </summary>
    /// <typeparam name="T">取得するコンポーネント型。</typeparam>
    /// <param name="component">取得したコンポーネント。</param>
    /// <returns>
    /// true：取得できました。
    /// false：取得できませんでした。
    /// </returns>
    public bool TryGetCurrentPhaseComponent<T>(
        out T component)
        where T : Component
    {
        component = null;

        if (!IsValidPhaseIndex(
                m_currentPhaseIndex))
        {
            return false;
        }

        GameObject phaseRoot =
            m_phaseDefinitions[
                m_currentPhaseIndex].PhaseRoot;

        if (phaseRoot == null)
        {
            return false;
        }

        component =
            phaseRoot.GetComponentInChildren<T>(
                true);

        return component != null;
    }

    /// <summary>
    /// フェーズ終了通知を処理します。
    /// </summary>
    /// <param name="objective">
    /// 達成された終了条件。
    /// </param>
    private void HandleObjectiveCompleted(
        BossPhaseObjective objective)
    {
        if (m_isTransitioning ||
            !IsValidPhaseIndex(
                m_currentPhaseIndex))
        {
            return;
        }

        PhaseDefinition currentPhase =
            m_phaseDefinitions[
                m_currentPhaseIndex];

        if (currentPhase.Objective !=
            objective)
        {
            return;
        }

        m_isTransitioning = true;

        m_behaviorController.StopBehavior();

        PhaseCompletionRequested?.Invoke(
            currentPhase.PhaseID);
    }

    /// <summary>
    /// 現在フェーズの設定を適用します。
    /// </summary>
    private void ApplyCurrentPhase()
    {
        if (!IsValidPhaseIndex(
                m_currentPhaseIndex))
        {
            return;
        }

        PhaseDefinition currentPhase =
            m_phaseDefinitions[
                m_currentPhaseIndex];

        // 前フェーズの攻撃ダメージ設定を先に解除します。
        ReleaseAttackDamageSettings(
            currentPhase.AttackDamageProvider);

        SetActivePhaseRoot();

        // フェーズ固有のHitbox対応とダメージパラメータを適用します。
        ApplyAttackDamageSettings(
            currentPhase);

        // フェーズ固有パラメータを先に適用
        // Behaviorが開始された際にStateから取得できるようにします。
        ApplyPhaseParameters(
            currentPhase);

        ApplyNavigationSettings(
            currentPhase);

        // フェーズに対応するアニメーションを設定
        m_animationController.SetPhase(
            currentPhase.PhaseID);

        // 最後にBehaviorを切り替える
        m_behaviorController.SetPhase(
            currentPhase.PhaseID);
    }

    /// <summary>
    /// 現在フェーズのルートのみ有効化します。
    /// </summary>
    private void SetActivePhaseRoot()
    {
        for (int i = 0;
             i < m_phaseDefinitions.Count;
             i++)
        {
            PhaseDefinition phaseDefinition =
                m_phaseDefinitions[i];

            if (phaseDefinition?.PhaseRoot == null)
            {
                continue;
            }

            bool isCurrentPhase =
                i == m_currentPhaseIndex;

            phaseDefinition.PhaseRoot.SetActive(
                isCurrentPhase);
        }
    }

    /// <summary>
    /// 現在フェーズの攻撃ダメージ設定を適用します。
    /// </summary>
    /// <param name="phaseDefinition">
    /// 適用するフェーズ定義。
    /// </param>
    private void ApplyAttackDamageSettings(
        PhaseDefinition phaseDefinition)
    {
        BossPhaseAttackDamageProvider attackDamageProvider =
            phaseDefinition.AttackDamageProvider;

        if (attackDamageProvider == null)
        {
            Debug.LogWarning(
                $"[{nameof(BossPhaseController)}] " +
                $"{phaseDefinition.PhaseID} の" +
                $"{nameof(BossPhaseAttackDamageProvider)}が設定されていません。",
                this);

            m_currentAttackDamageProvider = null;
            return;
        }

        if (!attackDamageProvider.ApplyDamageSettings())
        {
            Debug.LogError(
                $"[{nameof(BossPhaseController)}] " +
                $"{phaseDefinition.PhaseID} の" +
                "攻撃ダメージ設定の適用に失敗しました。",
                attackDamageProvider);

            m_currentAttackDamageProvider = null;
            return;
        }

        m_currentAttackDamageProvider =
            attackDamageProvider;
    }

    /// <summary>
    /// 前フェーズの攻撃ダメージ設定を解除します。
    /// </summary>
    /// <param name="nextProvider">
    /// 次フェーズで使用する設定。
    /// </param>
    private void ReleaseAttackDamageSettings(
        BossPhaseAttackDamageProvider nextProvider)
    {
        if (m_currentAttackDamageProvider == null ||
            m_currentAttackDamageProvider == nextProvider)
        {
            return;
        }

        m_currentAttackDamageProvider.ClearDamageSettings();
        m_currentAttackDamageProvider = null;
    }

    /// <summary>
    /// 現在フェーズのパラメータをボスへ適用します。
    /// </summary>
    /// <param name="phaseDefinition">
    /// 適用するフェーズ定義。
    /// </param>
    private void ApplyPhaseParameters(
        PhaseDefinition phaseDefinition)
    {
        BossPhaseParameterProvider parameterProvider =
            phaseDefinition.ParameterProvider;

        if (parameterProvider == null)
        {
            m_bossController.SetPhaseParameters(
                BossPhaseParameters.Empty);

            return;
        }

        BossPhaseParameters phaseParameters =
            parameterProvider.CreatePhaseParameters();

        if (phaseParameters == null)
        {
            Debug.LogError(
                $"[{nameof(BossPhaseController)}] " +
                $"{phaseDefinition.PhaseID} の" +
                "フェーズパラメータ生成に失敗しました。",
                parameterProvider);

            m_bossController.SetPhaseParameters(
                BossPhaseParameters.Empty);

            return;
        }

        m_bossController.SetPhaseParameters(
            phaseParameters);
    }

    /// <summary>
    /// 現在フェーズのNavigation設定を適用します。
    /// </summary>
    /// <param name="phaseDefinition">
    /// 適用するフェーズ定義。
    /// </param>
    private void ApplyNavigationSettings(
        PhaseDefinition phaseDefinition)
    {
        BossPhaseReferences phaseReferences =
            phaseDefinition.PhaseReferences;

        if (phaseReferences == null)
        {
            Debug.LogWarning(
                $"[{nameof(BossPhaseController)}] " +
                $"{phaseDefinition.PhaseID} の" +
                $"{nameof(BossPhaseReferences)}が設定されていません。",
                this);

            return;
        }

        if (phaseReferences.NavMeshSurface != null)
        {
            m_navigation.SetNavMeshSurface(
                phaseReferences.NavMeshSurface);
        }

        if (phaseReferences.GroundCollider != null)
        {
            m_navigation.SetNavigationOrigin(
                phaseReferences
                    .GroundCollider
                    .transform);
        }
    }

    /// <summary>
    /// 終了条件のイベントを購読します。
    /// </summary>
    private void SubscribeObjectives()
    {
        foreach (PhaseDefinition phaseDefinition
                 in m_phaseDefinitions)
        {
            if (phaseDefinition?.Objective == null)
            {
                continue;
            }

            phaseDefinition.Objective.Completed +=
                HandleObjectiveCompleted;
        }
    }

    /// <summary>
    /// 終了条件のイベント購読を解除します。
    /// </summary>
    private void UnsubscribeObjectives()
    {
        foreach (PhaseDefinition phaseDefinition
                 in m_phaseDefinitions)
        {
            if (phaseDefinition?.Objective == null)
            {
                continue;
            }

            phaseDefinition.Objective.Completed -=
                HandleObjectiveCompleted;
        }
    }

    /// <summary>
    /// 必要なコンポーネントを取得します。
    /// </summary>
    private void ResolveReferences()
    {
        if (m_bossController == null)
        {
            m_bossController =
                GetComponent<BossController>();
        }

        if (m_behaviorController == null)
        {
            m_behaviorController =
                GetComponent<BossBehaviorController>();
        }

        if (m_animationController == null)
        {
            m_animationController =
                GetComponent<BossAnimationController>();
        }

        if (m_navigation == null)
        {
            m_navigation =
                GetComponent<BossNavigation>();
        }
    }

    /// <summary>
    /// 必要な参照が設定されているか確認します。
    /// </summary>
    /// <returns>
    /// true：必要な参照が設定されています。
    /// false：参照が不足しています。
    /// </returns>
    private bool ValidateReferences()
    {
        bool isValid = true;

        if (m_bossController == null)
        {
            Debug.LogError(
                $"[{nameof(BossPhaseController)}] " +
                $"{nameof(BossController)}が見つかりません。",
                this);

            isValid = false;
        }

        if (m_behaviorController == null)
        {
            Debug.LogError(
                $"[{nameof(BossPhaseController)}] " +
                $"{nameof(BossBehaviorController)}が見つかりません。",
                this);

            isValid = false;
        }

        if (m_animationController == null)
        {
            Debug.LogError(
                $"[{nameof(BossPhaseController)}] " +
                $"{nameof(BossAnimationController)}が見つかりません。",
                this);

            isValid = false;
        }

        if (m_navigation == null)
        {
            Debug.LogError(
                $"[{nameof(BossPhaseController)}] " +
                $"{nameof(BossNavigation)}が見つかりません。",
                this);

            isValid = false;
        }

        return isValid;
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
        for (int i = 0;
             i < m_phaseDefinitions.Count;
             i++)
        {
            PhaseDefinition phaseDefinition =
                m_phaseDefinitions[i];

            if (phaseDefinition != null &&
                phaseDefinition.PhaseID ==
                phaseID)
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
    /// <param name="phaseIndex">
    /// 確認する位置。
    /// </param>
    /// <returns>
    /// true：有効です。
    /// false：無効です。
    /// </returns>
    private bool IsValidPhaseIndex(
        int phaseIndex)
    {
        return phaseIndex >= 0 &&
               phaseIndex <
               m_phaseDefinitions.Count;
    }
}
