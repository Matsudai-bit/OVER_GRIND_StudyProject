using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ステージ1フェーズ1のミサイル攻撃を実行します。
/// </summary>
public sealed class S1P1BossMissileState :
    StateBase<BossController>
{
    // S1P1固有参照
    private S1P1BossReferences m_references;

    // ミサイル攻撃参照
    private S1P1BossMissileReferences m_missileReferences;

    // ミサイル状態のパラメータ
    private S1P1BossMissileStateParameters m_parameters;

    // ミサイルの攻撃対象
    private Transform m_playerTransform;

    // 次に使用する発射地点のIndex
    private int m_nextLaunchSiteIndex;

    // 前回の発射からの経過時間
    private float m_launchElapsedTime;

    // 停止状態への変更を要求したか
    private bool m_isIdleRequested;

    // ミサイルを発射可能か
    private bool m_canLaunch;

    // Animator Trigger ID
    private int m_animationTriggerID;

    // AnimationEvent受信
    private AnimationEventReceiver m_animationEventReceiver;

    /// <summary>
    /// ミサイル攻撃を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        m_nextLaunchSiteIndex = 0;
        m_launchElapsedTime = 0.0f;
        m_isIdleRequested = false;
        m_canLaunch = false;

        Owner.Motor?.StopHorizontalMovement();

        if (!TryGetReferencesAndParameters())
        {
            Debug.LogError(
                "ミサイル攻撃に必要な参照またはパラメータを取得できませんでした。");

            RequestFailedIdleState();
            return;
        }

        if (!ApplyAttackSetting())
        {
            Debug.LogError(
                "ミサイル攻撃のAnimation設定を取得できませんでした。");

            RequestFailedIdleState();
            return;
        }

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);

        if (HasFinishedLaunching())
        {
            CompleteMissileAttack();
        }
    }

    /// <summary>
    /// ミサイルの連続発射を更新します。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間。</param>
    protected override void OnUpdate(float deltaTime)
    {
        if (!m_canLaunch ||
            m_isIdleRequested ||
            m_parameters == null ||
            HasFinishedLaunching())
        {
            return;
        }

        m_launchElapsedTime += deltaTime;

        if (m_launchElapsedTime <
            m_parameters.LaunchInterval)
        {
            return;
        }

        m_launchElapsedTime = 0.0f;

        if (!TryLaunchNextMissile())
        {
            RequestFailedIdleState();
            return;
        }

        if (HasFinishedLaunching())
        {
            CompleteMissileAttack();
        }
    }

    /// <summary>
    /// ミサイル攻撃を終了します。
    /// </summary>
    protected override void OnExitState()
    {
        StartCoolTimeIfSucceeded();

        if (m_animationEventReceiver != null)
        {
            m_animationEventReceiver.AttackEventReceived -=
                HandleAttackEvent;
        }

        Owner.Motor?.StopHorizontalMovement();

        if (Owner.GetStateExecutionStatus() ==
            StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }

        m_references = null;
        m_missileReferences = null;
        m_parameters = null;
        m_playerTransform = null;
        m_animationEventReceiver = null;

        Owner.SetStateExecutionStatus(
      StateExecutionStatus.SUCCEEDED);
    }

    /// <summary>
    /// 正常終了したミサイル攻撃のクールタイムを開始します。
    /// </summary>
    private void StartCoolTimeIfSucceeded()
    {
        if (Owner.GetStateExecutionStatus() !=
            StateExecutionStatus.SUCCEEDED ||
            m_references == null ||
            m_references.DecisionParameterAsset == null)
        {
            return;
        }

        BossStateCoolTimeManager coolTimeManager =
            Owner.GetComponent<BossStateCoolTimeManager>();

        if (coolTimeManager == null)
        {
            return;
        }

        coolTimeManager.StartCoolTime<S1P1BossMissileState>(
            m_references.DecisionParameterAsset.Missile.CoolTime);
    }

    /// <summary>
    /// 次の発射地点からミサイルを1発発射します。
    /// </summary>
    /// <returns>
    /// true：ミサイルを発射しました。
    /// false：発射できませんでした。
    /// </returns>
    private bool TryLaunchNextMissile()
    {
        IReadOnlyList<Transform> launchSites =
            m_missileReferences.LaunchSites;

        while (m_nextLaunchSiteIndex <
               launchSites.Count)
        {
            Transform launchSite =
                launchSites[m_nextLaunchSiteIndex];

            m_nextLaunchSiteIndex++;

            if (launchSite == null)
            {
                Debug.LogWarning(
                    $"ミサイル発射地点 " +
                    $"{m_nextLaunchSiteIndex - 1} がnullです。");

                continue;
            }

            S1P1MissileController missile =
                UnityEngine.Object.Instantiate(
                    m_missileReferences.MissilePrefab,
                    launchSite.position,
                    launchSite.rotation,
                    m_missileReferences.MissileParent);

            // StateParameterAssetから取得したミサイル本体設定を渡す
            missile.Initialize(
                m_parameters.MissileParameterAsset,
                m_playerTransform,
                m_parameters.MissileUpwardDuration);

            missile.Launch();

            return true;
        }

        return false;
    }

    /// <summary>
    /// すべてのミサイルを発射したか確認します。
    /// </summary>
    /// <returns>
    /// true：すべて発射しました。
    /// false：未発射のミサイルがあります。
    /// </returns>
    private bool HasFinishedLaunching()
    {
        if (m_missileReferences == null ||
            m_missileReferences.LaunchSites == null)
        {
            return true;
        }

        return m_nextLaunchSiteIndex >=
               m_missileReferences.LaunchSites.Count;
    }

    /// <summary>
    /// ミサイル攻撃を正常終了します。
    /// </summary>
    private void CompleteMissileAttack()
    {
  

        RequestIdleState();
    }

    /// <summary>
    /// 停止状態へ移行します。
    /// </summary>
    private void RequestIdleState()
    {
        if (m_isIdleRequested)
        {
            return;
        }

        m_isIdleRequested = true;

        Owner.Motor?.StopHorizontalMovement();

        float idleDuration =
            m_parameters?.IdleDuration ?? 0.0f;

        Machine.ChangeState<BossIdleState>(
            idleDuration);
    }

    /// <summary>
    /// 攻撃失敗として停止状態へ移行します。
    /// </summary>
    private void RequestFailedIdleState()
    {
        Owner.SetStateExecutionStatus(
            StateExecutionStatus.FAILED);

        RequestIdleState();
    }

    /// <summary>
    /// 現在フェーズからミサイル攻撃に必要な参照とパラメータを取得します。
    /// </summary>
    /// <returns>
    /// true：必要な情報を取得できました。
    /// false：取得できませんでした。
    /// </returns>
    private bool TryGetReferencesAndParameters()
    {
        if (Owner.PhaseController == null)
        {
            return false;
        }

        if (!Owner.PhaseController.TryGetCurrentPhaseComponent(
                out m_references))
        {
            return false;
        }

        if (!Owner.PhaseController.TryGetCurrentPhaseComponent(
                out BossPhaseReferences commonReferences))
        {
            return false;
        }

        m_missileReferences =
            m_references.MissileReferences;

        if (m_missileReferences == null ||
            !m_missileReferences.HasRequiredReferences())
        {
            return false;
        }

        if (m_references.StateParameterAsset == null ||
            !m_references.StateParameterAsset.HasRequiredParameters())
        {
            return false;
        }

        m_parameters =
            m_references.StateParameterAsset.Missile;

        if (m_parameters == null ||
            !m_parameters.HasRequiredParameters())
        {
            return false;
        }

        if (commonReferences.PlayerTransform == null)
        {
            return false;
        }

        m_playerTransform =
            commonReferences.PlayerTransform;

        return true;
    }

    /// <summary>
    /// ミサイル攻撃のAnimation設定を適用します。
    /// </summary>
    /// <returns>
    /// true：設定できました。
    /// false：設定できませんでした。
    /// </returns>
    private bool ApplyAttackSetting()
    {
        S1P1BossAttackSettings attackSettings =
            Owner.GetComponentInChildren<
                S1P1BossAttackSettings>(true);

        if (attackSettings == null ||
            Owner.AnimationController == null)
        {
            return false;
        }

        if (!attackSettings.TryGetAttackSetting(
                S1P1BossAttackType.MISSILE,
                out _,
                out string animationTriggerName))
        {
            return false;
        }

        m_animationEventReceiver =
            Owner.AnimationController.CurrentAnimationEventReceiver;

        if (m_animationEventReceiver == null ||
            string.IsNullOrEmpty(
                animationTriggerName))
        {
            return false;
        }

        m_animationTriggerID =
            Animator.StringToHash(
                animationTriggerName);

        Owner.AnimationController.SetTrigger(
            m_animationTriggerID);

        m_animationEventReceiver.AttackEventReceived +=
            HandleAttackEvent;

        return true;
    }

    /// <summary>
    /// 攻撃AnimationEventを処理します。
    /// </summary>
    /// <param name="attackEventData">攻撃イベント情報。</param>
    private void HandleAttackEvent(
        AttackEventData attackEventData)
    {
        switch (attackEventData.AttackEventType)
        {
            case AttackEventType.HITBOX_ENABLE:
                if (!TryLaunchNextMissile())
                {
                    RequestFailedIdleState();
                    return;
                }

                if (HasFinishedLaunching())
                {
                    CompleteMissileAttack();
                    return;
                }

                m_canLaunch = true;
                break;

            case AttackEventType.HITBOX_DISABLE:
                break;

            case AttackEventType.ANIMATION_END:
                break;
        }
    }
}
