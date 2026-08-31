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

    // フェーズ3突進設定
    private S1P3BossChargeSettings m_chargeSettings;

    // 直線突進実行機構
    private StraightChargeExecutor m_chargeExecutor;

    // 現在の突進回数
    private int m_currentChargeIndex;

    /// <summary>
    /// 連続突進を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        if (Owner == null ||
            Owner.PhaseController == null)
        {
            SetFailed();
            return;
        }

        if (!Owner.PhaseController.TryGetCurrentPhaseComponent(
                out m_references) ||
            !Owner.PhaseController.TryGetCurrentPhaseComponent(
                out m_chargeSettings))
        {
            SetFailed();
            return;
        }

        GameObject player =
            GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            SetFailed();
            return;
        }

        m_playerTransform = player.transform;
        m_chargeExecutor = new StraightChargeExecutor(Owner);
        m_currentChargeIndex = 0;

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);

        StartCurrentCharge();
    }

    /// <summary>
    /// 連続突進を物理更新します。
    /// </summary>
    protected override void OnFixedUpdate()
    {
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

        m_currentChargeIndex++;

        if (m_currentChargeIndex >= CHARGE_COUNT)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.SUCCEEDED);
            return;
        }

        StartCurrentCharge();
    }

    /// <summary>
    /// 現在回数の突進を開始します。
    /// </summary>
    private void StartCurrentCharge()
    {
        if (!m_chargeSettings.TryGetChargeSettings(
                m_currentChargeIndex,
                out StraightChargeSettings settings))
        {
            SetFailed();
            return;
        }

        Func<Vector3> targetPositionProvider;

        // 1、2回目は予備動作中もPlayerを追尾します。
        if (m_currentChargeIndex < 2)
        {
            targetPositionProvider =
                () => m_playerTransform.position;
        }
        else
        {
            if (!TryGetThirdChargeDestination(
                    out Transform destination))
            {
                SetFailed();
                return;
            }

            // 3回目は選択した地点を固定目標にします。
            targetPositionProvider =
                () => destination.position;
        }

        //if (!m_chargeExecutor.Start(
        //        settings,
        //        targetPositionProvider))
        //{
        //    SetFailed();
        //}
    }

    /// <summary>
    /// Playerから最も遠い3回目の突進地点を取得します。
    /// </summary>
    /// <param name="destination">取得した目的地。</param>
    /// <returns>
    /// true：目的地を取得しました。
    /// false：有効な目的地がありません。
    /// </returns>
    private bool TryGetThirdChargeDestination(
        out Transform destination)
    {
        destination = null;

        if (m_references == null ||
            m_playerTransform == null)
        {
            return false;
        }

        IReadOnlyList<Transform> destinationPoints =
            m_references.ChargeDestinationPoints;

        float farthestSqrDistance = -1.0f;

        foreach (Transform destinationPoint in destinationPoints)
        {
            if (destinationPoint == null)
            {
                continue;
            }

            Vector3 difference =
                destinationPoint.position -
                m_playerTransform.position;

            difference.y = 0.0f;

            float sqrDistance =
                difference.sqrMagnitude;

            if (sqrDistance <= farthestSqrDistance)
            {
                continue;
            }

            farthestSqrDistance = sqrDistance;
            destination = destinationPoint;
        }

        return destination != null;
    }

    /// <summary>
    /// 連続突進を終了します。
    /// </summary>
    protected override void OnExitState()
    {
        m_chargeExecutor?.Cancel();

        m_chargeExecutor = null;
        m_playerTransform = null;
        m_references = null;
        m_chargeSettings = null;
        m_currentChargeIndex = 0;

        if (Owner != null &&
            Owner.GetStateExecutionStatus() ==
            StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }
    }

    /// <summary>
    /// Stateを失敗状態にします。
    /// </summary>
    private void SetFailed()
    {
        if (Owner == null)
        {
            return;
        }

        m_chargeExecutor?.Cancel();

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.FAILED);
    }
}
