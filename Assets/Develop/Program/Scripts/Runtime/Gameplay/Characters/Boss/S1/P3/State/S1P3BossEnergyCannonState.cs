using UnityEngine;

/// <summary>
/// ステージ1フェーズ3のエネルギー砲攻撃を実行します。
/// </summary>
public sealed class S1P3BossEnergyCannonState :
    StateBase<BossController>
{
    /// <summary>
    /// エネルギー砲攻撃の進行状態です。
    /// </summary>
    private enum EnergyCannonExecutionState
    {
        NONE,
        PREPARATION,
        STRAIGHT_FIRE,
        TRACKING,
        FINISHED
    }

    // フェーズ3全体の参照
    private S1P3BossReferences m_references;

    // エネルギー砲状態固有の参照
    private S1P3EnergyCannonStateReferences m_stateReferences;

    // エネルギー砲状態パラメータ
    private S1P3BossEnergyCannonStateParameters m_parameters;

    // 攻撃ID
    private AttackIdentifier m_attackIdentifier;

    // Animator Trigger ID
    private int m_animationTriggerId;

    // 現在の進行状態
    private EnergyCannonExecutionState m_executionState;

    // 現在状態の経過時間
    private float m_elapsedTime;
    private BossPhaseReferences m_commonReferences;

    /// <summary>
    /// エネルギー砲攻撃を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        m_elapsedTime = 0.0f;
        m_executionState =
            EnergyCannonExecutionState.NONE;

        if (!TryGetReferences())
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);

            return;
        }

        if (!ApplyAttackSetting())
        {
            Debug.LogError(
                "エネルギー砲攻撃の設定に失敗しました。");

            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);

            return;
        }

        // 攻撃中はボス本体を停止する
        Owner.Motor?.StopHorizontalMovement();

        // 開始時点では攻撃判定を無効化する
        Owner.AttackHitboxRegistry?.DisableHitboxes(
            m_attackIdentifier);

        m_executionState =
            EnergyCannonExecutionState.PREPARATION;

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);
    }

    /// <summary>
    /// エネルギー砲攻撃を更新します。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間。</param>
    protected override void OnUpdate(
        float deltaTime)
    {
        if (Owner.GetStateExecutionStatus() !=
            StateExecutionStatus.RUNNING)
        {
            return;
        }

        switch (m_executionState)
        {
            case EnergyCannonExecutionState.PREPARATION:
                UpdatePreparation(
                    deltaTime);
                break;

            case EnergyCannonExecutionState.STRAIGHT_FIRE:
                UpdateStraightFire(
                    deltaTime);
                break;

            case EnergyCannonExecutionState.TRACKING:
                UpdateTracking(
                    deltaTime);
                break;
        }
    }

    /// <summary>
    /// エネルギー砲攻撃を終了します。
    /// </summary>
    protected override void OnExitState()
    {
        if (m_attackIdentifier != null)
        {
            Owner.AttackHitboxRegistry?.DisableHitboxes(
                m_attackIdentifier);
        }

        if (Owner.GetStateExecutionStatus() ==
            StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }

        m_executionState =
            EnergyCannonExecutionState.NONE;

        m_elapsedTime = 0.0f;

        m_references = null;
        m_stateReferences = null;
        m_parameters = null;
        m_attackIdentifier = null;
    }

    /// <summary>
    /// 予備動作を更新します。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間。</param>
    private void UpdatePreparation(
        float deltaTime)
    {
        m_elapsedTime +=
            deltaTime;

        if (m_elapsedTime <
            m_parameters.PreparationDuration)
        {
            return;
        }

        StartStraightFire();
    }

    /// <summary>
    /// 直線発射を開始します。
    /// </summary>
    private void StartStraightFire()
    {
        m_elapsedTime = 0.0f;

        // 発射開始時点のPlayer方向へ向ける
        //AimAtPlayer(); // 最初は正面方向に撃つので一旦無効　にする

        // エネルギー砲の攻撃判定を有効化する
        Owner.EnableAttackHitboxes(
            m_attackIdentifier);

        m_executionState =
            EnergyCannonExecutionState.STRAIGHT_FIRE;
    }

    /// <summary>
    /// 直線発射状態を更新します。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間。</param>
    private void UpdateStraightFire(
        float deltaTime)
    {
        m_elapsedTime +=
            deltaTime;

        if (m_elapsedTime <
            m_parameters.StraightFireDuration)
        {
            return;
        }

        StartTracking();
    }

    /// <summary>
    /// Player追従を開始します。
    /// </summary>
    private void StartTracking()
    {
        m_elapsedTime = 0.0f;

        m_executionState =
            EnergyCannonExecutionState.TRACKING;
    }

    /// <summary>
    /// Player追従状態を更新します。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間。</param>
    private void UpdateTracking(
        float deltaTime)
    {
        TrackPlayer(
            deltaTime);

        m_elapsedTime +=
            deltaTime;

        if (m_elapsedTime <
            m_parameters.TrackingDuration)
        {
            return;
        }

        FinishAttack();
    }

    /// <summary>
    /// 発射開始時点のPlayer方向へエネルギー砲を向けます。
    /// </summary>
    private void AimAtPlayer()
    {
        //Transform cannonPivot =
        //    m_stateReferences.CannonPivot;
 
        //Transform playerTransform =
        //    m_commonReferences.PlayerTransform;

        //Vector3 direction =
        //    playerTransform.position -
        //    cannonPivot.position;

        //// 左右方向のみ追従する
        //direction.y = 0.0f;

        //if (direction.sqrMagnitude <=
        //    Mathf.Epsilon)
        //{
        //    return;
        //}

        //cannonPivot.rotation =
        //    Quaternion.LookRotation(
        //        direction.normalized,
        //        Vector3.up);
    }

    /// <summary>
    /// エネルギー砲をPlayer方向へ追従させます。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間。</param>
    private void TrackPlayer(
        float deltaTime)
    {
        Transform cannonPivot =
            m_stateReferences.CannonPivot;

        Transform playerTransform =
            m_commonReferences.PlayerTransform;

        Vector3 direction =
            playerTransform.position -
            cannonPivot.position;

        // 上下には追従しない
        direction.y = 0.0f;

        if (direction.sqrMagnitude <=
            Mathf.Epsilon)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction.normalized,
                Vector3.up);

        cannonPivot.rotation =
            Quaternion.RotateTowards(
                cannonPivot.rotation,
                targetRotation,
                m_parameters.TrackingSpeed *
                deltaTime);
    }

    /// <summary>
    /// エネルギー砲攻撃を正常終了します。
    /// </summary>
    private void FinishAttack()
    {
        if (m_executionState ==
            EnergyCannonExecutionState.FINISHED)
        {
            return;
        }

        m_executionState =
            EnergyCannonExecutionState.FINISHED;

        Owner.AttackHitboxRegistry?.DisableHitboxes(
            m_attackIdentifier);

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.SUCCEEDED);
    }

    /// <summary>
    /// 必要な参照を取得します。
    /// </summary>
    /// <returns>
    /// true：取得しました。
    /// false：取得できませんでした。
    /// </returns>
    private bool TryGetReferences()
    {
        if (Owner.PhaseController == null)
        {
            Debug.LogError(
                "PhaseControllerが設定されていません。");

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
        if (!Owner.PhaseController
                .TryGetCurrentPhaseComponent(
                    out m_commonReferences))
        {
            Debug.LogError(
                $"{nameof(BossPhaseReferences)}が" +
                "見つかりません。");

            return false;
        }

        m_stateReferences =
            m_references.EnergyCannonStateReferences;

        if (m_stateReferences == null)
        {
            Debug.LogError(
                $"{nameof(S1P3EnergyCannonStateReferences)}が" +
                "設定されていません。");

            return false;
        }

        m_parameters =
            m_references.StateParameterAsset.EnergyCannon;

        if (m_parameters == null)
        {
            Debug.LogError(
                $"{nameof(S1P3BossEnergyCannonStateParameters)}が" +
                "設定されていません。");

            return false;
        }

        if (m_stateReferences.CannonPivot == null)
        {
            Debug.LogError(
                "エネルギー砲のCannonPivotが" +
                "設定されていません。");

            return false;
        }

        if (m_commonReferences.PlayerTransform == null)
        {
            Debug.LogError(
                "PlayerTransformが設定されていません。");

            return false;
        }

        return true;
    }

    /// <summary>
    /// エネルギー砲の攻撃設定を取得して適用します。
    /// </summary>
    /// <returns>
    /// true：設定しました。
    /// false：設定できませんでした。
    /// </returns>
    private bool ApplyAttackSetting()
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
                S1P3BossAttackType.ENERGY_CANNON,
                out m_attackIdentifier,
                out string animationTriggerName))
        {
            Debug.LogError(
                $"{S1P3BossAttackType.ENERGY_CANNON}の" +
                "攻撃設定がありません。");

            return false;
        }

        if (m_attackIdentifier == null)
        {
            Debug.LogError(
                "エネルギー砲のAttack IDが" +
                "設定されていません。");

            return false;
        }

        if (string.IsNullOrEmpty(
                animationTriggerName))
        {
            Debug.LogError(
                "エネルギー砲のAnimator Triggerが" +
                "設定されていません。");

            return false;
        }

        if (Owner.AnimationController == null)
        {
            Debug.LogError(
                "AnimationControllerが設定されていません。");

            return false;
        }

        m_animationTriggerId =
            Animator.StringToHash(
                animationTriggerName);

        Owner.AnimationController.SetTrigger(
            m_animationTriggerId);

        return true;
    }
}