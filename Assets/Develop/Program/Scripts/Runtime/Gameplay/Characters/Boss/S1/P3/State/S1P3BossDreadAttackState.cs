using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ステージ1フェーズ3のドレット攻撃を実行します。
/// AnimationEventのHITBOX_ENABLEごとに
/// オイルを1発ずつ発射します。
/// </summary>
public sealed class S1P3BossDreadAttackState :
    StateBase<BossController>
{
    // フェーズ3参照
    private BossPhaseReferences m_commonReferences;
    private S1P3BossReferences m_references;

    // ドレット攻撃固有参照
    private DreadAttackStateReferences m_stateReferences;

    // ドレット攻撃パラメータ
    private S1P3BossDreadAttackStateParameters m_parameters;

    // 攻撃ID
    private AttackIdentifier m_attackIdentifier;

    // Animator Bool ID
    private int m_animationBoolID;

    // オイルに設定するダメージ
    private int m_projectileDamage;

    // 生成したオイル
    private readonly List<S1P3OilController> m_spawnedOils =
        new();

    // 次に使用する発射地点Index
    private int m_nextLaunchPointIndex;

    // 発射したオイル数
    private int m_launchedOilCount;

    // 着弾したオイル数
    private int m_landedOilCount;

    // 全弾発射済みか
    private bool m_hasFinishedLaunching;

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

        Owner.Motor?
            .StopHorizontalMovement();

        SubscribeAnimationEvent();

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);

        /*
         * TriggerではなくBoolで
         * ドレット攻撃Animationを開始します。
         */
        Owner.AnimationController.SetBool(
            m_animationBoolID,
            true);
    }

    /// <summary>
    /// 全弾着弾後の終了待機を更新します。
    /// </summary>
    /// <param name="deltaTime">
    /// 経過時間。
    /// </param>
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

        if (Owner?.AnimationController != null)
        {
            Owner.AnimationController.SetBool(
                m_animationBoolID,
                false);
        }

        /*
         * 途中終了した場合は
         * 飛行中のオイルを停止します。
         */
        if (Owner != null &&
            Owner.GetStateExecutionStatus() ==
                StateExecutionStatus.RUNNING)
        {
            CancelFlyingOils();

            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }

        m_spawnedOils.Clear();

        m_commonReferences =
            null;

        m_references =
            null;

        m_stateReferences =
            null;

        m_parameters =
            null;

        m_attackIdentifier =
            null;
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
        if (attackEventData == null ||
            Owner == null ||
            Owner.GetStateExecutionStatus() !=
                StateExecutionStatus.RUNNING)
        {
            return;
        }

        switch (attackEventData.AttackEventType)
        {
            case AttackEventType.HITBOX_ENABLE:
                LaunchNextOil();
                break;

            case AttackEventType.ANIMATION_END:
                /*
                 * ドレット攻撃はAnimation終了では
                 * Stateを終了しません。
                 * 全オイル着弾を待ちます。
                 */
                break;
        }
    }

    /// <summary>
    /// 次の発射地点からオイルを1発発射します。
    /// HITBOX_ENABLE 1回につき1発だけ発射します。
    /// </summary>
    private void LaunchNextOil()
    {
        if (m_hasFinishedLaunching ||
            m_parameters == null)
        {
            return;
        }

        if (m_launchedOilCount >=
            m_parameters.LaunchCount)
        {
            FinishLaunching();

            return;
        }

        IReadOnlyList<Transform> launchPoints =
            m_stateReferences.LaunchPoints;

        if (launchPoints == null ||
            launchPoints.Count == 0)
        {
            Debug.LogError(
                "ドレット攻撃の発射地点が設定されていません。");

            FailState();

            return;
        }

        /*
         * 発射数が発射地点数より多い場合にも対応するため、
         * 発射地点を循環して使用します。
         */
        int searchCount =
            0;

        while (searchCount <
               launchPoints.Count)
        {
            int launchPointIndex =
                m_nextLaunchPointIndex %
                launchPoints.Count;

            m_nextLaunchPointIndex =
                (launchPointIndex + 1) %
                launchPoints.Count;

            searchCount++;

            Transform launchPoint =
                launchPoints[launchPointIndex];

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
                Debug.LogError(
                    "ドレット攻撃のオイルを生成できませんでした。");

                FailState();

                return;
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

                Debug.LogError(
                    "ドレット攻撃のオイルを初期化できませんでした。");

                FailState();

                return;
            }

            oilController.Landed +=
                HandleOilLanded;

            oilController.FlightFailed +=
                HandleOilFlightFailed;

            m_spawnedOils.Add(
                oilController);

            m_launchedOilCount++;

            oilController.Launch();

            /*
             * 指定数を撃ち終えたら
             * 発射AnimationのBoolをOFFにします。
             */
            if (m_launchedOilCount >=
                m_parameters.LaunchCount)
            {
                FinishLaunching();
            }

            return;
        }

        Debug.LogError(
            "有効なドレット攻撃の発射地点がありません。");

        FailState();
    }

    /// <summary>
    /// 全オイルの発射完了を処理します。
    /// </summary>
    private void FinishLaunching()
    {
        if (m_hasFinishedLaunching)
        {
            return;
        }

        m_hasFinishedLaunching =
            true;

        if (Owner?.AnimationController == null)
        {
            return;
        }

        /*
         * 発射回数分のAnimationEventを受け取ったため、
         * BoolをOFFにして発射Animationを終了させます。
         */
        Owner.AnimationController.SetBool(
            m_animationBoolID,
            false);
    }

    /// <summary>
    /// オイル着弾を処理します。
    /// </summary>
    /// <param name="oilController">
    /// 着弾したオイル。
    /// </param>
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

        /*
         * 全弾の発射が終わっており、
         * かつ発射した全オイルが着弾した場合のみ
         * 終了待機へ移行します。
         */
        if (!m_hasFinishedLaunching ||
            m_landedOilCount <
                m_launchedOilCount)
        {
            return;
        }

        m_isWaitingFinish =
            true;

        m_finishElapsedTime =
            0.0f;
    }

    /// <summary>
    /// オイル飛行失敗を処理します。
    /// </summary>
    /// <param name="oilController">
    /// 失敗したオイル。
    /// </param>
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
        if (!m_isWaitingFinish)
        {
            return;
        }

        m_isWaitingFinish =
            false;

        /*
         * SUCCEEDEDより先に
         * クールタイムを開始します。
         */
        StartCoolTime();

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
        if (Owner == null ||
            Owner.PhaseController == null)
        {
            return false;
        }

        if (!Owner.PhaseController
                .TryGetCurrentPhaseComponent(
                    out m_commonReferences))
        {
            Debug.LogError(
                $"{nameof(BossPhaseReferences)}が" +
                "見つかりません。");

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
                $"{nameof(S1P3BossReferences)}が" +
                "見つかりません。");

            return false;
        }

        m_stateReferences =
            m_references
                .DreadAttackStateReferences;

        if (m_stateReferences == null)
        {
            Debug.LogError(
                $"{nameof(DreadAttackStateReferences)}が" +
                "設定されていません。");

            return false;
        }

        if (m_references.StateParameterAsset == null)
        {
            return false;
        }

        m_parameters =
            m_references
                .StateParameterAsset
                .DreadAttack;

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

        IReadOnlyList<Transform> launchPoints =
    m_stateReferences.LaunchPoints;

        if (launchPoints == null ||
            launchPoints.Count == 0)
        {
            Debug.LogError(
                "ドレット攻撃の発射地点が設定されていません。");

            return false;
        }

        bool hasValidLaunchPoint =
            false;

        foreach (Transform launchPoint in
                 launchPoints)
        {
            if (launchPoint == null)
            {
                continue;
            }

            hasValidLaunchPoint =
                true;

            break;
        }

        if (!hasValidLaunchPoint)
        {
            Debug.LogError(
                "有効なドレット攻撃の発射地点がありません。");

            return false;
        }

        return true;
    }

    /// <summary>
    /// 攻撃IDとAnimator Boolを取得します。
    /// </summary>
    /// <returns>
    /// true：取得しました。
    /// false：取得できませんでした。
    /// </returns>
    private bool TryGetAttackSetting()
    {
        S1P3BossAttackSettings attackSettings =
            Owner.GetComponentInChildren<
                S1P3BossAttackSettings>(
                    true);

        if (attackSettings == null)
        {
            Debug.LogError(
                $"{nameof(S1P3BossAttackSettings)}が" +
                "見つかりません。");

            return false;
        }

        /*
         * AttackSettings側の文字列は
         * Trigger名ではなくBool名として扱います。
         */
        if (!attackSettings.TryGetAttackSetting(
                S1P3BossAttackType.DREAD_ATTACK,
                out m_attackIdentifier,
                out string animationBoolName))
        {
            Debug.LogError(
                $"{S1P3BossAttackType.DREAD_ATTACK}の" +
                "攻撃設定がありません。");

            return false;
        }

        if (m_attackIdentifier == null ||
            string.IsNullOrEmpty(
                animationBoolName) ||
            Owner.AnimationController == null ||
            Owner.AnimationController
                .CurrentAnimationEventReceiver == null)
        {
            return false;
        }

        m_animationBoolID =
            Animator.StringToHash(
                animationBoolName);

        return true;
    }

    /// <summary>
    /// ドレット飛翔物のダメージを取得します。
    /// </summary>
    /// <param name="damage">
    /// 取得したダメージ。
    /// </param>
    /// <returns>
    /// true：取得しました。
    /// false：取得できませんでした。
    /// </returns>
    private bool TryGetProjectileDamage(
        out int damage)
    {
        damage =
            0;

        S1BossAttackDamageParameterAsset parameterAsset =
            m_references
                .AttackSettingsProvider
                .ParameterAsset;

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

        IReadOnlyList<
            HitboxDamageParameter<S1BossHitboxId>>
            hitboxParameters =
                attackParameter
                    .HitboxDamageParameters;

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

        m_isAnimationEventSubscribed =
            true;
    }

    /// <summary>
    /// AnimationEventの購読を解除します。
    /// </summary>
    private void UnsubscribeAnimationEvent()
    {
        if (!m_isAnimationEventSubscribed ||
            Owner?.AnimationController == null ||
            Owner.AnimationController
                .CurrentAnimationEventReceiver == null)
        {
            return;
        }

        Owner.AnimationController
            .CurrentAnimationEventReceiver
            .AttackEventReceived -=
            HandleAttackEvent;

        m_isAnimationEventSubscribed =
            false;
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
            oilController?
                .Cancel();
        }
    }

    /// <summary>
    /// ドレット攻撃のクールタイムを開始します。
    /// </summary>
    private void StartCoolTime()
    {
        if (Owner == null ||
            m_references == null ||
            m_references.DecisionParameterAsset == null)
        {
            return;
        }

        BossStateCoolTimeManager coolTimeManager =
            Owner.GetComponent<
                BossStateCoolTimeManager>();

        if (coolTimeManager == null)
        {
            return;
        }

        coolTimeManager
            .StartCoolTime<
                S1P3BossDreadAttackState>(
                m_references
                    .DecisionParameterAsset
                    .DreadAttack
                    .CoolTime);
    }

    /// <summary>
    /// 状態を失敗終了します。
    /// </summary>
    private void FailState()
    {
        if (Owner?.AnimationController != null)
        {
            Owner.AnimationController.SetBool(
                m_animationBoolID,
                false);
        }

        Owner?.SetStateExecutionStatus(
            StateExecutionStatus.FAILED);
    }

    /// <summary>
    /// 実行時情報を初期化します。
    /// </summary>
    private void ResetRuntimeState()
    {
        m_spawnedOils.Clear();

        m_nextLaunchPointIndex =
            0;

        m_launchedOilCount =
            0;

        m_landedOilCount =
            0;

        m_hasFinishedLaunching =
            false;

        m_isWaitingFinish =
            false;

        m_finishElapsedTime =
            0.0f;

        m_isAnimationEventSubscribed =
            false;
    }
}