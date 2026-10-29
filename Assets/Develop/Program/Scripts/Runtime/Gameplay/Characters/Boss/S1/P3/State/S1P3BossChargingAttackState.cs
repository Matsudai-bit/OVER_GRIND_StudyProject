using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ステージ1フェーズ3の連続突進攻撃を実行します。
/// </summary>
public sealed class S1P3BossChargingAttackState :
    StateBase<BossController>
{
    // フェーズ3で行う突進回数
    private const int CHARGE_COUNT = 3;

    // プレイヤー
    private Transform m_playerTransform;

    // フェーズ3固有参照
    private S1P3BossReferences m_references;

    // フェーズ共通参照
    private BossPhaseReferences m_commonReferences;

    // 連続突進パラメータ
    private S1P3BossSequentialChargeAttackParameters
        m_parameters;

    // 現在実行中の突進パラメータ
    private S1BossChargeAttackParameters
        m_currentChargeParameters;

    // 直線突進実行機構
    private StraightChargeExecutor
        m_chargeExecutor;

    // 現在の突進回数
    private int m_currentChargeIndex;

    // 突進攻撃中Bool ID
    private int m_animationActiveBoolID;

    // 突進攻撃フェーズInt ID
    private int m_animationPhaseIntID;

    // 現在Animatorへ反映している旋回方向
    private BossChargeTurnDirection?
        m_currentAnimationTurnDirection;

    // Animation設定を初期化済みか
    private bool m_isAnimationInitialized;
    // 突進攻撃の旋回方向Int ID
    private int m_animationTurnDirectionIntID;

    /// <summary>
    /// 連続突進を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        if (!TryInitializeReferences())
        {
            SetFailed();
            return;
        }

        m_parameters =
            Owner.PhaseParameters?
                .SequentialChargeAttack;

        if (m_parameters == null ||
            m_parameters.ChargeParameters == null ||
            m_parameters.ChargeParameters.Count <
                CHARGE_COUNT)
        {
            Debug.LogError(
                $"[{nameof(S1P3BossChargingAttackState)}] " +
                "連続突進パラメータが不足しています。");

            SetFailed();
            return;
        }

        m_currentChargeIndex =
            0;

        m_chargeExecutor =
            new StraightChargeExecutor(
                Owner);

        /*
         * Start()内でフェーズ・旋回方向通知が発生するため、
         * 開始より先に購読します。
         */
        m_chargeExecutor.PhaseChanged +=
            HandleChargePhaseChanged;

        m_chargeExecutor.TurnDirectionChanged +=
            HandleTurnDirectionChanged;

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);

        StartCurrentCharge();
    }

    /// <summary>
    /// 連続突進を物理更新します。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        if (Owner == null ||
            Owner.GetStateExecutionStatus() !=
                StateExecutionStatus.RUNNING)
        {
            return;
        }

        if (m_chargeExecutor == null)
        {
            SetFailed();
            return;
        }

        m_chargeExecutor.FixedUpdate(
            Time.fixedDeltaTime);

        if (m_chargeExecutor.HasFailed)
        {
            SetFailed();
            return;
        }

        if (!m_chargeExecutor.IsCompleted)
        {
            return;
        }

        CompleteCurrentCharge();
    }

    /// <summary>
    /// 現在回数の突進を開始します。
    /// </summary>
    private void StartCurrentCharge()
    {
        if (m_parameters == null ||
            m_parameters.ChargeParameters == null ||
            m_currentChargeIndex < 0 ||
            m_currentChargeIndex >=
                m_parameters.ChargeParameters.Count)
        {
            SetFailed();
            return;
        }

        m_currentChargeParameters =
            m_parameters.ChargeParameters[
                m_currentChargeIndex];

        if (m_currentChargeParameters == null)
        {
            SetFailed();
            return;
        }

        if (!ApplyAnimationParameters(
                m_currentChargeParameters))
        {
            Debug.LogError(
                $"[{nameof(S1P3BossChargingAttackState)}] " +
                $"{m_currentChargeIndex + 1}回目の" +
                "Animation設定を取得できませんでした。");

            SetFailed();
            return;
        }

        if (!TryCreateTargetPositionProvider(
                out Func<Vector3>
                    targetPositionProvider))
        {
            SetFailed();
            return;
        }

        m_currentAnimationTurnDirection =
            null;

        if (!m_chargeExecutor.Start(
                m_currentChargeParameters,
                targetPositionProvider))
        {
            SetFailed();
            return;
        }

        /*
         * Event発行タイミングに依存せず、
         * 初回方向を確実にAnimatorへ反映します。
         */
        SetAnimationTurnDirection(
            m_chargeExecutor.TurnDirection);

        Owner.AnimationController.SetBool(
            m_animationActiveBoolID,
            true);
    }

    /// <summary>
    /// 現在の突進完了を処理します。
    /// </summary>
    private void CompleteCurrentCharge()
    {

        m_currentAnimationTurnDirection =
            null;

        m_currentChargeIndex++;

        if (m_currentChargeIndex >=
            CHARGE_COUNT)
        {
            Owner.AnimationController?.SetBool(
                m_animationActiveBoolID,
                false);

            Owner.SetStateExecutionStatus(
                StateExecutionStatus.SUCCEEDED);

            return;
        }

        StartCurrentCharge();
    }

    /// <summary>
    /// 現在回数に応じた突進対象位置取得処理を生成します。
    /// </summary>
    private bool TryCreateTargetPositionProvider(
        out Func<Vector3>
            targetPositionProvider)
    {
        targetPositionProvider =
            null;

        /*
         * 1、2回目はPlayerの現在位置を追尾します。
         */
        if (m_currentChargeIndex < 2)
        {
            if (m_playerTransform == null)
            {
                return false;
            }

            targetPositionProvider =
                GetPlayerPosition;

            return true;
        }

        /*
         * 3回目はPlayerから最も遠い
         * 目的地を固定目標として使用します。
         */
        if (!TryGetThirdChargeDestination(
                out Transform destination))
        {
            return false;
        }

        Vector3 fixedDestination =
            destination.position;

        targetPositionProvider =
            () => fixedDestination;

        return true;
    }

    /// <summary>
    /// Playerの現在位置を取得します。
    /// </summary>
    private Vector3 GetPlayerPosition()
    {
        if (m_playerTransform == null)
        {
            return Owner.transform.position;
        }

        return m_playerTransform.position;
    }

    /// <summary>
    /// Playerから最も遠い3回目の突進地点を取得します。
    /// </summary>
    private bool TryGetThirdChargeDestination(
        out Transform destination)
    {
        destination =
            null;

        if (m_references == null ||
            m_playerTransform == null ||
            m_references.ChargeDestinationPoints ==
                null)
        {
            return false;
        }

        IReadOnlyList<Transform> destinationPoints =
            m_references.ChargeDestinationPoints;

        float farthestSqrDistance =
            -1.0f;

        foreach (Transform destinationPoint
                 in destinationPoints)
        {
            if (destinationPoint == null)
            {
                continue;
            }

            Vector3 difference =
                destinationPoint.position -
                m_playerTransform.position;

            difference.y =
                0.0f;

            float sqrDistance =
                difference.sqrMagnitude;

            if (sqrDistance <=
                farthestSqrDistance)
            {
                continue;
            }

            farthestSqrDistance =
                sqrDistance;

            destination =
                destinationPoint;
        }

        return destination != null;
    }

    /// <summary>
    /// Executorの行動フェーズ変更を処理します。
    /// </summary>
    private void HandleChargePhaseChanged(
        BossActionPhase phase)
    {
        SetAnimationPhase(
            phase);
    }

    /// <summary>
    /// Executorの旋回方向変更を処理します。
    /// </summary>
    private void HandleTurnDirectionChanged(
        BossChargeTurnDirection direction)
    {
        SetAnimationTurnDirection(
            direction);
    }

    /// <summary>
    /// 現在の突進用Animation設定を適用します。
    /// </summary>
    private bool ApplyAnimationParameters(
        S1BossChargeAttackParameters parameters)
    {
        if (Owner?.AnimationController == null ||
            parameters == null ||
            string.IsNullOrEmpty(
                parameters.AnimationActiveBoolName) ||
            string.IsNullOrEmpty(
                parameters.AnimationPhaseIntName) ||
            string.IsNullOrEmpty(
                parameters.AnimationTurnDirectionIntName))
        {
            return false;
        }

        int newActiveBoolID =
            Animator.StringToHash(
                parameters.AnimationActiveBoolName);

        int newPhaseIntID =
            Animator.StringToHash(
                parameters.AnimationPhaseIntName);

        int newTurnDirectionIntID =
            Animator.StringToHash(
                parameters.AnimationTurnDirectionIntName);

        /*
         * 前回の突進で別Parameterを使用していた場合、
         * 旧Animationパラメータを解除します。
         */
        if (m_isAnimationInitialized)
        {
            if (m_animationActiveBoolID !=
                newActiveBoolID)
            {
                Owner.AnimationController.SetBool(
                    m_animationActiveBoolID,
                    false);
            }
        }

        m_animationActiveBoolID =
            newActiveBoolID;

        m_animationPhaseIntID =
            newPhaseIntID;

        m_animationTurnDirectionIntID =
            newTurnDirectionIntID;

        m_isAnimationInitialized =
            true;

        SetAnimationPhase(
            BossActionPhase.PREPARATION);

        m_currentAnimationTurnDirection =
            null;

        return true;
    }

    /// <summary>
    /// Animatorへ現在の行動フェーズを設定します。
    /// </summary>
    private void SetAnimationPhase(
        BossActionPhase phase)
    {
        if (!m_isAnimationInitialized ||
            Owner?.AnimationController == null)
        {
            return;
        }

        Owner.AnimationController.SetInteger(
            m_animationPhaseIntID,
            (int)phase);
    }

    /// <summary>
    /// Animatorへ現在の旋回方向を設定します。
    /// </summary>
    private void SetAnimationTurnDirection(
        BossChargeTurnDirection direction)
    {
        if (!m_isAnimationInitialized ||
            Owner?.AnimationController == null)
        {
            return;
        }

        if (m_currentAnimationTurnDirection ==
            direction)
        {
            return;
        }

        m_currentAnimationTurnDirection =
            direction;

        Owner.AnimationController.SetInteger(
            m_animationTurnDirectionIntID,
            (int)direction);
    }


    /// <summary>
    /// 突進Animationを初期状態へ戻します。
    /// </summary>
    private void ResetAnimationParameters()
    {
        if (!m_isAnimationInitialized ||
            Owner?.AnimationController == null)
        {
            return;
        }

        Owner.AnimationController.SetInteger(
            m_animationPhaseIntID,
            (int)BossActionPhase.PREPARATION);

        Owner.AnimationController.SetInteger(
            m_animationTurnDirectionIntID,
            0);

        Owner.AnimationController.SetBool(
            m_animationActiveBoolID,
            false);

        m_currentAnimationTurnDirection =
            null;

        m_isAnimationInitialized =
            false;
    }

    /// <summary>
    /// State開始に必要な参照を取得します。
    /// </summary>
    private bool TryInitializeReferences()
    {
        if (Owner == null ||
            Owner.PhaseController == null)
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
                    out m_commonReferences))
        {
            return false;
        }

        if (m_commonReferences == null ||
            m_commonReferences.PlayerTransform ==
                null)
        {
            return false;
        }

        m_playerTransform =
            m_commonReferences.PlayerTransform;

        return true;
    }

    /// <summary>
    /// 連続突進を終了します。
    /// </summary>
    protected override void OnExitState()
    {
        StartCoolTimeIfSucceeded();

        ReleaseChargeExecutor();

        ResetAnimationParameters();

        m_currentChargeParameters =
            null;

        m_parameters =
            null;

        m_playerTransform =
            null;

        m_commonReferences =
            null;

        m_references =
            null;

        m_currentChargeIndex =
            0;

        if (Owner != null &&
            Owner.GetStateExecutionStatus() ==
                StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }
    }

    /// <summary>
    /// Executorとの接続を解除します。
    /// </summary>
    private void ReleaseChargeExecutor()
    {
        if (m_chargeExecutor == null)
        {
            return;
        }

        m_chargeExecutor.PhaseChanged -=
            HandleChargePhaseChanged;

        m_chargeExecutor.TurnDirectionChanged -=
            HandleTurnDirectionChanged;

        m_chargeExecutor.Cancel();

        m_chargeExecutor =
            null;
    }

    /// <summary>
    /// 正常終了した行動のクールタイムを開始します。
    /// </summary>
    private void StartCoolTimeIfSucceeded()
    {
        if (Owner == null ||
            Owner.GetStateExecutionStatus() !=
                StateExecutionStatus.SUCCEEDED ||
            m_references == null ||
            m_references.DecisionParameterAsset ==
                null)
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
                S1P3BossChargingAttackState>(
                m_references
                    .DecisionParameterAsset
                    .Charge
                    .CoolTime);
    }

    /// <summary>
    /// Stateを失敗状態にします。
    /// </summary>
    private void SetFailed()
    {
        ReleaseChargeExecutor();

        ResetAnimationParameters();

        if (Owner == null)
        {
            return;
        }

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.FAILED);
    }
}