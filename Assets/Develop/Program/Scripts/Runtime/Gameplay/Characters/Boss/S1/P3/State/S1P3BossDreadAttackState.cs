using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ステージ1フェーズ3のドレット攻撃を実行します。
/// </summary>
public sealed class S1P3BossDreadAttackState :
    StateBase<BossController>
{
    // 仕様上のオイル発射数
    private const int DREAD_PROJECTILE_COUNT = 12;

    // フェーズ3参照
    private BossPhaseReferences m_commonReferences;
    private S1P3BossReferences m_references;

    // ドレット攻撃固有参照
    private DreadAttackStateReferences m_stateReferences;

    // ドレット攻撃パラメータ
    private S1P3BossDreadAttackStateParameters m_parameters;

    // 攻撃ID
    private AttackIdentifier m_attackIdentifier;

    // Animator Trigger ID
    private int m_animationTriggerId;

    // オイルに設定するダメージ
    private int m_projectileDamage;

    // 生成したオイル
    private readonly List<S1P3OilController> m_spawnedOils =
        new();

    // 発射したオイル数
    private int m_launchedOilCount;

    // 着弾したオイル数
    private int m_landedOilCount;

    // 発射済みか
    private bool m_hasLaunched;

    // 全弾着弾後の待機中か
    private bool m_isWaitingFinish;

    // 状態終了待機時間
    private float m_finishElapsedTime;

    // AnimationEvent購読中か
    private bool m_isAnimationEventSubscribed;

    /// <summary>
    /// ドレット攻撃を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        ResetRuntimeState();

        if (!TryGetReferences())
        {
            FailState();

            return;
        }

        if (!TryGetAttackSetting())
        {
            FailState();

            return;
        }

        if (!TryGetProjectileDamage(
                out m_projectileDamage))
        {
            FailState();

            return;
        }

        Owner.Motor?.StopHorizontalMovement();

        SubscribeAnimationEvent();

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);

        Owner.AnimationController.SetTrigger(
            m_animationTriggerId);
    }

    /// <summary>
    /// 全弾着弾後の終了待機を更新します。
    /// </summary>
    /// <param name="deltaTime">経過時間。</param>
    protected override void OnUpdate(
        float deltaTime)
    {
        if (!m_isWaitingFinish)
        {
            return;
        }

        m_finishElapsedTime +=
            deltaTime;

        if (m_finishElapsedTime <
            m_parameters.FinishDelay)
        {
            return;
        }

        FinishAttack();
    }

    /// <summary>
    /// ドレット攻撃を終了します。
    /// </summary>
    protected override void OnExitState()
    {
        UnsubscribeAnimationEvent();
        UnsubscribeOilEvents();

        // 途中終了した場合は飛行中のオイルを停止する
        if (Owner.GetStateExecutionStatus() ==
            StateExecutionStatus.RUNNING)
        {
            CancelFlyingOils();

            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }

        m_spawnedOils.Clear();

        m_references = null;
        m_stateReferences = null;
        m_parameters = null;
        m_attackIdentifier = null;
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
                LaunchOils();
                break;

            case AttackEventType.ANIMATION_END:
                // ドレット攻撃はAnimation終了では完了しない
                break;
        }
    }

    /// <summary>
    /// 全発射地点からオイルを発射します。
    /// </summary>
    private void LaunchOils()
    {
        if (m_hasLaunched)
        {
            return;
        }

        m_hasLaunched = true;

        IReadOnlyList<Transform> launchPoints =
            m_stateReferences.LaunchPoints;

        foreach (Transform launchPoint
                 in launchPoints)
        {
            if (launchPoint == null)
            {
                continue;
            }

            S1P3OilController oilController =
                Object.Instantiate(
                    m_stateReferences.OilPrefab,
                    launchPoint.position,
                    launchPoint.rotation);

            if (oilController == null)
            {
                continue;
            }

            if (!oilController.Initialize(
                 m_commonReferences.PlayerTransform,
                 m_parameters,
                 m_stateReferences.GroundLayerMask,
                 m_projectileDamage,
                 m_stateReferences.TargetDecalPrefab,
                 m_stateReferences.OilDecalPrefab))
            {
                Object.Destroy(
                    oilController.gameObject);

                continue;
            }

            oilController.Landed +=
                HandleOilLanded;

            oilController.FlightFailed +=
                HandleOilFlightFailed;

            m_spawnedOils.Add(
                oilController);

            m_launchedOilCount++;

            oilController.Launch();
        }

        if (m_launchedOilCount <= 0)
        {
            Debug.LogError(
                "ドレット攻撃のオイルを発射できませんでした。");

            FailState();
        }
    }

    /// <summary>
    /// オイル着弾を処理します。
    /// </summary>
    /// <param name="oilController">着弾したオイル。</param>
    private void HandleOilLanded(
        S1P3OilController oilController)
    {
        if (oilController == null)
        {
            return;
        }

        oilController.Landed -=
            HandleOilLanded;

        oilController.FlightFailed -=
            HandleOilFlightFailed;

        m_landedOilCount++;

        if (m_landedOilCount <
            m_launchedOilCount)
        {
            return;
        }

        // 全弾着弾後から2秒間待機する
        m_isWaitingFinish = true;
        m_finishElapsedTime = 0.0f;
    }

    /// <summary>
    /// オイル飛行失敗を処理します。
    /// </summary>
    /// <param name="oilController">失敗したオイル。</param>
    private void HandleOilFlightFailed(
        S1P3OilController oilController)
    {
        Debug.LogError(
            "ドレット攻撃のオイル飛行に失敗しました。");

        CancelFlyingOils();

        FailState();
    }

    /// <summary>
    /// ドレット攻撃を正常終了します。
    /// </summary>
    private void FinishAttack()
    {
        m_isWaitingFinish = false;

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.SUCCEEDED);
    }

    /// <summary>
    /// 必要な参照を取得します。
    /// </summary>
    /// <returns>
    /// true：取得しました。
    /// false：必要な参照がありません。
    /// </returns>
    private bool TryGetReferences()
    {
        if (Owner.PhaseController == null)
        {
            return false;
        }

        if (!Owner.PhaseController
               .TryGetCurrentPhaseComponent(
                   out m_commonReferences))
        {
            Debug.LogError(
                $"{nameof(BossPhaseReferences)}が見つかりません。");

            return false;
        }
        if (m_commonReferences == null)
        {
            Debug.LogError(
                $"{nameof(BossPhaseReferences)}が" +
                "設定されていません。");

            return false;
        }    

        if (!Owner.PhaseController
                .TryGetCurrentPhaseComponent(
                    out m_references))
        {
            Debug.LogError(
                $"{nameof(S1P3BossReferences)}が見つかりません。");

            return false;
        }

        m_stateReferences =
            m_references.DreadAttackStateReferences;

        if (m_stateReferences == null)
        {
            Debug.LogError(
                $"{nameof(DreadAttackStateReferences)}が" +
                "設定されていません。");

            return false;
        }

        m_parameters = m_references.StateParameterAsset.DreadAttack;

        if (m_parameters == null)
        {
            Debug.LogError(
                $"{nameof(S1P3BossDreadAttackStateParameters)}が" +
                "設定されていません。");

            return false;
        }

        if (m_commonReferences.PlayerTransform == null)
        {
            Debug.LogError(
                "PlayerTransformが設定されていません。");

            return false;
        }

        if (m_references.AttackSettingsProvider == null)
        {
            Debug.LogError(
                $"{nameof(S1BossPhaseAttackSettingsProvider)}が" +
                "設定されていません。");

            return false;
        }

        if (m_stateReferences.OilPrefab == null)
        {
            Debug.LogError(
                "オイルPrefabが設定されていません。");

            return false;
        }

        //if (m_stateReferences.LaunchPoints == null ||
        //    m_stateReferences.LaunchPoints.Count !=
        //    DREAD_PROJECTILE_COUNT)
        //{
        //    Debug.LogError(
        //        $"ドレット攻撃の発射地点を" +
        //        $"{DREAD_PROJECTILE_COUNT}個設定してください。");

        //    return false;
        //}

        foreach (Transform launchPoint
                 in m_stateReferences.LaunchPoints)
        {
            if (launchPoint != null)
            {
                continue;
            }

            Debug.LogError(
                "ドレット攻撃の発射地点に未設定があります。");

            return false;
        }

        return true;
    }

    /// <summary>
    /// 攻撃IDとAnimator Triggerを取得します。
    /// </summary>
    /// <returns>
    /// true：取得しました。
    /// false：取得できませんでした。
    /// </returns>
    private bool TryGetAttackSetting()
    {
        S1P3BossAttackSettings attackSettings =
            Owner.GetComponentInChildren<
                S1P3BossAttackSettings>(true);

        if (attackSettings == null)
        {
            Debug.LogError(
                $"{nameof(S1P3BossAttackSettings)}が" +
                "見つかりません。");

            return false;
        }

        if (!attackSettings.TryGetAttackSetting(
                S1P3BossAttackType.DREAD_ATTACK,
                out m_attackIdentifier,
                out string animationTriggerName))
        {
            Debug.LogError(
                $"{S1P3BossAttackType.DREAD_ATTACK}の" +
                "攻撃設定がありません。");

            return false;
        }

        if (m_attackIdentifier == null ||
            string.IsNullOrEmpty(
                animationTriggerName) ||
            Owner.AnimationController == null ||
            Owner.AnimationController
                .CurrentAnimationEventReceiver == null)
        {
            return false;
        }

        m_animationTriggerId =
            Animator.StringToHash(
                animationTriggerName);

        return true;
    }

    /// <summary>
    /// ドレット飛翔物のダメージを取得します。
    /// </summary>
    /// <param name="damage">取得したダメージ。</param>
    /// <returns>
    /// true：取得しました。
    /// false：取得できませんでした。
    /// </returns>
    private bool TryGetProjectileDamage(
        out int damage)
    {
        damage = 0;

        S1BossAttackDamageParameterAsset parameterAsset =
            m_references.AttackSettingsProvider.ParameterAsset;

        if (parameterAsset == null)
        {
            Debug.LogError(
                "攻撃ダメージパラメータが設定されていません。");

            return false;
        }

        if (!parameterAsset.TryGetAttackParameter(
                m_attackIdentifier,
                out AttackDamageParameter<S1BossHitboxId>
                    attackParameter))
        {
            Debug.LogError(
                $"{m_attackIdentifier.name}の" +
                "ダメージパラメータがありません。");

            return false;
        }

        IReadOnlyList<HitboxDamageParameter<S1BossHitboxId>>
            hitboxParameters =
                attackParameter.HitboxDamageParameters;

        if (hitboxParameters == null)
        {
            return false;
        }

        foreach (HitboxDamageParameter<S1BossHitboxId>
                 hitboxParameter
                 in hitboxParameters)
        {
            if (hitboxParameter == null)
            {
                continue;
            }

            if (hitboxParameter.HitboxId !=
                S1BossHitboxId.DREAD_PROJECTILE)
            {
                continue;
            }

            damage =
                hitboxParameter.Damage;

            return true;
        }

        Debug.LogError(
            $"{S1BossHitboxId.DREAD_PROJECTILE}の" +
            "ダメージ設定がありません。");

        return false;
    }

    /// <summary>
    /// AnimationEventを購読します。
    /// </summary>
    private void SubscribeAnimationEvent()
    {
        if (m_isAnimationEventSubscribed)
        {
            return;
        }

        Owner.AnimationController
            .CurrentAnimationEventReceiver
            .AttackEventReceived +=
            HandleAttackEvent;

        m_isAnimationEventSubscribed = true;
    }

    /// <summary>
    /// AnimationEventの購読を解除します。
    /// </summary>
    private void UnsubscribeAnimationEvent()
    {
        if (!m_isAnimationEventSubscribed ||
            Owner.AnimationController == null ||
            Owner.AnimationController
                .CurrentAnimationEventReceiver == null)
        {
            return;
        }

        Owner.AnimationController
            .CurrentAnimationEventReceiver
            .AttackEventReceived -=
            HandleAttackEvent;

        m_isAnimationEventSubscribed = false;
    }

    /// <summary>
    /// オイルイベントの購読を解除します。
    /// </summary>
    private void UnsubscribeOilEvents()
    {
        foreach (S1P3OilController oilController
                 in m_spawnedOils)
        {
            if (oilController == null)
            {
                continue;
            }

            oilController.Landed -=
                HandleOilLanded;

            oilController.FlightFailed -=
                HandleOilFlightFailed;
        }
    }

    /// <summary>
    /// 飛行中のオイルを停止します。
    /// </summary>
    private void CancelFlyingOils()
    {
        foreach (S1P3OilController oilController
                 in m_spawnedOils)
        {
            oilController?.Cancel();
        }
    }

    /// <summary>
    /// 状態を失敗終了します。
    /// </summary>
    private void FailState()
    {
        Owner.SetStateExecutionStatus(
            StateExecutionStatus.FAILED);
    }

    /// <summary>
    /// 実行時情報を初期化します。
    /// </summary>
    private void ResetRuntimeState()
    {
        m_spawnedOils.Clear();

        m_launchedOilCount = 0;
        m_landedOilCount = 0;

        m_hasLaunched = false;
        m_isWaitingFinish = false;
        m_finishElapsedTime = 0.0f;

        m_isAnimationEventSubscribed = false;
    }
}