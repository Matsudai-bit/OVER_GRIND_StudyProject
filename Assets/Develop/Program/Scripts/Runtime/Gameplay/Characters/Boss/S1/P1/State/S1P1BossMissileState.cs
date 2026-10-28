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
    private S1BossMissileReferences m_missileReferences;

    // ミサイル状態のパラメータ
    private S1P1BossMissileStateParameters m_parameters;

    // ミサイルの攻撃対象
    private Transform m_playerTransform;

    // 次に使用する発射地点のIndex
    private int m_nextLaunchSiteIndex;

    // 発射済みミサイル数
    private int m_firedMissileCount;

    // 攻撃終了を要求しているか
    private bool m_isFinishRequested;

    // 停止状態への変更を要求したか
    private bool m_isIdleRequested;

    // Animator Bool ID
    private int m_animationBoolID;

    // 爆発Hitboxへ渡す攻撃ID
    private AttackIdentifier m_attackIdentifier;

    // AnimationEvent受信
    private AnimationEventReceiver m_animationEventReceiver;

    /// <summary>
    /// ミサイル攻撃を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        m_nextLaunchSiteIndex = 0;
        m_firedMissileCount = 0;
        m_isFinishRequested = false;
        m_isIdleRequested = false;

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

        Owner.AnimationController?.SetBool(
            m_animationBoolID,
            false);

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
        m_attackIdentifier = null;
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

        if (launchSites == null ||
            launchSites.Count == 0)
        {
            return false;
        }

        // 全発射地点がnullだった場合の無限ループを防ぐ
        int searchCount = 0;

        while (searchCount < launchSites.Count)
        {
            // 発射地点を順番に使用し、
            // 最後まで到達したら先頭へ戻る
            int launchSiteIndex =
                m_nextLaunchSiteIndex %
                launchSites.Count;

            m_nextLaunchSiteIndex =
                (launchSiteIndex + 1) %
                launchSites.Count;

            searchCount++;

            Transform launchSite =
                launchSites[launchSiteIndex];

            if (launchSite == null)
            {
                Debug.LogWarning(
                    $"ミサイル発射地点 {launchSiteIndex} がnullです。");

                continue;
            }

            S1MissileController missile =
                UnityEngine.Object.Instantiate(
                    m_missileReferences.MissilePrefab,
                    launchSite.position,
                    launchSite.rotation,
                    m_missileReferences.MissileParent);

            missile.Initialize(
                m_parameters.MissileParameterAsset,
                m_playerTransform,
                m_parameters.MissileUpwardDuration,
                m_attackIdentifier);

            missile.Launch();

            return true;
        }

        return false;
    }

    /// <summary>
    /// 指定された数のミサイルを発射したか確認します。
    /// </summary>
    private bool HasFinishedLaunching()
    {
        if (m_parameters == null)
        {
            return true;
        }

        return m_firedMissileCount >=
               m_parameters.LaunchCount;
    }

    /// <summary>
    /// ミサイル攻撃の終了を要求します。
    /// </summary>
    private void RequestFinishAttack()
    {
        if (m_isFinishRequested)
        {
            return;
        }

        m_isFinishRequested = true;

        // BoolをOFFにしてAnimationループを終了させる。
        // この時点ではまだStateをSUCCEEDEDにしない。
        Owner.AnimationController?.SetBool(
            m_animationBoolID,
            false);
    }

    /// <summary>
    /// ミサイル攻撃を正常終了します。
    /// </summary>
    private void CompleteMissileAttack()
    {
        if (m_isIdleRequested)
        {
            return;
        }

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.SUCCEEDED);

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
                out m_attackIdentifier,
                out string animationParameterName))
        {
            return false;
        }

        m_animationEventReceiver =
            Owner.AnimationController
                .CurrentAnimationEventReceiver;

        if (m_animationEventReceiver == null ||
            string.IsNullOrEmpty(
                animationParameterName))
        {
            return false;
        }

        m_animationBoolID =
            Animator.StringToHash(
                animationParameterName);

        m_animationEventReceiver.AttackEventReceived +=
            HandleAttackEvent;

        Owner.AnimationController.SetBool(
            m_animationBoolID,
            true);

        return true;
    }

    /// <summary>
    /// 攻撃AnimationEventを処理します。
    /// </summary>
    /// <param name="attackEventData">
    /// 攻撃イベント情報。
    /// </param>
    private void HandleAttackEvent(
        AttackEventData attackEventData)
    {
        if (attackEventData == null)
        {
            return;
        }

        switch (attackEventData.AttackEventType)
        {
            case AttackEventType.HITBOX_ENABLE:
                HandleEnableAttack();
                break;

            case AttackEventType.HITBOX_DISABLE:
                break;

            case AttackEventType.ANIMATION_END:
                HandleAnimationEnd();
                break;
        }
    }

    /// <summary>
    /// EnableAttackごとにミサイルを1発発射します。
    /// </summary>
    private void HandleEnableAttack()
    {
        if (m_isFinishRequested)
        {
            return;
        }

        if (!TryLaunchNextMissile())
        {
            RequestFailedIdleState();
            return;
        }

        m_firedMissileCount++;

        if (!HasFinishedLaunching())
        {
            return;
        }

        RequestFinishAttack();
    }

    /// <summary>
    /// Animation終了時に攻撃終了を確定します。
    /// </summary>
    private void HandleAnimationEnd()
    {
        // 途中のAnimationループ終了では
        // Stateを終了させない
        if (!m_isFinishRequested)
        {
            return;
        }

        CompleteMissileAttack();
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
            m_references.DecisionParameterAsset
                .Missile
                .CoolTime);
    }
}