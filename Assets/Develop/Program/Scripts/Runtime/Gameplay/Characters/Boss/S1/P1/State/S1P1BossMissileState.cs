using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ステージ1フェーズ1のミサイル攻撃を実行します。
/// </summary>
public sealed class S1P1BossMissileState :
    StateBase<BossController>
{
    // ミサイル攻撃後の停止時間
    private const float IDLE_DURATION = 3.0f;

    // S1P1固有参照
    private S1P1BossReferences m_references;

    // ミサイル攻撃参照
    private S1P1BossMissileReferences m_missileReferences;

    // ミサイルの攻撃対象
    private Transform m_playerTransform;

    // 次に使用する発射地点のIndex
    private int m_nextLaunchSiteIndex;

    // 前回の発射からの経過時間
    private float m_launchElapsedTime;

    // 停止状態への変更を要求したか
    private bool m_isIdleRequested;

    private bool m_canLunch; // 発射可能かどうか

    // Animator Trigger ID
    private int m_animationTriggerID;

    private AnimationEventReceiver m_animationEventReceiver;


    /// <summary>
    /// ミサイル攻撃を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        m_nextLaunchSiteIndex = 0;
        m_launchElapsedTime = 0.0f;
        m_isIdleRequested = false;
        m_canLunch = false;

        // ミサイル攻撃中はその場で停止する
        Owner.Motor?.StopHorizontalMovement();

        if (!TryGetReferences())
        {
            Debug.LogError(
                "ミサイル攻撃に必要な参照を取得できませんでした。");

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

        // 1発のみの場合はそのまま攻撃終了
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
        if (!m_canLunch ||
            m_isIdleRequested ||
            HasFinishedLaunching())
        {
            return;
        }

        m_launchElapsedTime += deltaTime;

        if (m_launchElapsedTime <
            m_missileReferences.LaunchInterval)
        {
            return;
        }

        // 同一フレームに複数発射しないよう時間をリセットする
        m_launchElapsedTime = 0.0f;

        if (!TryLaunchNextMissile())
        {
            RequestFailedIdleState();
            return;
        }

        // 最後のミサイルを発射したら停止状態へ移行する
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

        Owner.SetStateExecutionStatus(
     StateExecutionStatus.SUCCEEDED);
        m_references = null;
        m_missileReferences = null;
        m_playerTransform = null;
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
                    launchSite.rotation, m_missileReferences.MissileParent);

            missile.Initialize(
                m_missileReferences.MissileParameter,
                m_playerTransform,
                m_missileReferences.MissileUpwardDuration);

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

        Machine.ChangeState<BossIdleState>(
            IDLE_DURATION);
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
    /// 現在フェーズからミサイル攻撃に必要な参照を取得します。
    /// </summary>
    /// <returns>
    /// true：必要な参照を取得できました。
    /// false：取得できませんでした。
    /// </returns>
    private bool TryGetReferences()
    {
        if (Owner.PhaseController == null)
        {
            return false;
        }

        if (!Owner.PhaseController
                .TryGetCurrentPhaseComponent(
                    out m_references))
        {
            return false;
        }


        if (!Owner.PhaseController
                .TryGetCurrentPhaseComponent(
                    out BossPhaseReferences commonReferences))
        {
            return false;
        }

        m_missileReferences = m_references.MissileReferences;
        if (!m_missileReferences
                .HasRequiredReferences())
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

        if (attackSettings == null)
        {
            Debug.LogError(
                $"{nameof(S1P1BossAttackSettings)}" +
                "が見つかりません。");

            return false;
        }

        if (!attackSettings.TryGetAttackSetting(
                S1P1BossAttackType.MISSILE,
                out _,
                out string animationTriggerName))
        {
            Debug.LogError(
                $"{S1P1BossAttackType.MISSILE}" +
                "の攻撃設定がありません。");

            return false;
        }

        m_animationEventReceiver = Owner.AnimationController.CurrentAnimationEventReceiver;
        if (string.IsNullOrEmpty(
                animationTriggerName) ||
            Owner.AnimationController == null)
        {
            return false;
        }

        m_animationTriggerID =
            Animator.StringToHash(
                animationTriggerName);

        Owner.AnimationController.SetTrigger(
            m_animationTriggerID);

        m_animationEventReceiver.AttackEventReceived += HandleAttackEvent;

        return true;
    }

    /// <summary>
    /// 攻撃AnimationEventを処理します。
    /// </summary>
    /// <param name="attackEventData">攻撃イベント情報。</param>
    private void HandleAttackEvent(AttackEventData attackEventData)
    {


        switch (attackEventData.AttackEventType)
        {
            case AttackEventType.HITBOX_ENABLE:
                // 最初の1発は攻撃開始時に発射する
                if (!TryLaunchNextMissile())
                {
                    RequestFailedIdleState();
                    return;
                }

                // 1発のみの場合はそのまま攻撃終了
                if (HasFinishedLaunching())
                {
                    CompleteMissileAttack();
                }
                m_canLunch = true;
                break;

            case AttackEventType.HITBOX_DISABLE:

                break;

            case AttackEventType.ANIMATION_END:

                break;
        }
    }
}