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

    // Animation設定を初期化済みか
    private bool m_isAnimationInitialized;

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
         * Start()内でPREPARATION通知が発行されるため、
         * 開始より先に購読します。
         */
        m_chargeExecutor.PhaseChanged +=
            HandleChargePhaseChanged;

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
            m_parameters
                .ChargeParameters[
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

        if (!m_chargeExecutor.Start(
                m_currentChargeParameters,
                targetPositionProvider))
        {
            SetFailed();
        }
    }

    /// <summary>
    /// 現在の突進完了を処理します。
    /// </summary>
    private void CompleteCurrentCharge()
    {
        m_currentChargeIndex++;

        if (m_currentChargeIndex >=
            CHARGE_COUNT)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.SUCCEEDED);

            return;
        }

        StartCurrentCharge();
    }

    /// <summary>
    /// 現在回数に応じた突進対象位置取得処理を生成します。
    /// </summary>
    /// <param name="targetPositionProvider">
    /// 対象位置を取得する処理。
    /// </param>
    /// <returns>
    /// true：生成できました。
    /// false：生成できませんでした。
    /// </returns>
    private bool TryCreateTargetPositionProvider(
        out Func<Vector3>
            targetPositionProvider)
    {
        targetPositionProvider =
            null;

        /*
         * 1、2回目は予備動作中も
         * Playerの現在位置を追尾します。
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
         * 目的地を選択して固定します。
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
    /// <returns>
    /// Playerの現在位置。
    /// </returns>
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
    /// <param name="destination">
    /// 取得した目的地。
    /// </param>
    /// <returns>
    /// true：目的地を取得しました。
    /// false：有効な目的地がありません。
    /// </returns>
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

        IReadOnlyList<Transform>
            destinationPoints =
                m_references
                    .ChargeDestinationPoints;

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
    /// <param name="phase">
    /// 変更後の行動フェーズ。
    /// </param>
    private void HandleChargePhaseChanged(
        BossActionPhase phase)
    {
        SetAnimationPhase(
            phase);
    }

    /// <summary>
    /// 現在の突進用Animation設定を適用します。
    /// </summary>
    /// <param name="parameters">
    /// 突進パラメータ。
    /// </param>
    /// <returns>
    /// true：設定できました。
    /// false：設定できませんでした。
    /// </returns>
    private bool ApplyAnimationParameters(
        S1BossChargeAttackParameters parameters)
    {
        if (Owner?.AnimationController == null ||
            parameters == null ||
            string.IsNullOrEmpty(
                parameters.AnimationActiveBoolName) ||
            string.IsNullOrEmpty(
                parameters.AnimationPhaseIntName))
        {
            return false;
        }

        int newActiveBoolID =
            Animator.StringToHash(
                parameters
                    .AnimationActiveBoolName);

        int newPhaseIntID =
            Animator.StringToHash(
                parameters
                    .AnimationPhaseIntName);

        /*
         * 突進ごとに別Animator Parameterを
         * 使用する設定にも対応します。
         */
        if (m_isAnimationInitialized &&
            m_animationActiveBoolID !=
                newActiveBoolID)
        {
            Owner.AnimationController.SetBool(
                m_animationActiveBoolID,
                false);
        }

        m_animationActiveBoolID =
            newActiveBoolID;

        m_animationPhaseIntID =
            newPhaseIntID;

        m_isAnimationInitialized =
            true;

        /*
         * Boolを有効化する前にPhaseを初期化し、
         * 前回値による誤遷移を防ぎます。
         */
        SetAnimationPhase(
            BossActionPhase.PREPARATION);

        Owner.AnimationController.SetBool(
            m_animationActiveBoolID,
            true);

        return true;
    }

    /// <summary>
    /// Animatorへ現在の行動フェーズを設定します。
    /// </summary>
    /// <param name="phase">
    /// 設定する行動フェーズ。
    /// </param>
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

        Owner.AnimationController.SetBool(
            m_animationActiveBoolID,
            false);

        m_isAnimationInitialized =
            false;
    }

    /// <summary>
    /// State開始に必要な参照を取得します。
    /// </summary>
    /// <returns>
    /// true：取得できました。
    /// false：取得できませんでした。
    /// </returns>
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




        if (m_commonReferences == null)
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