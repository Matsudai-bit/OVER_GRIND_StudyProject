using System;
using UnityEngine;

/// <summary>
/// 1回分の直線突進を実行します。
/// </summary>
public sealed class StraightChargeExecutor
{
    /// <summary>
    /// 直線突進の内部実行状態です。
    /// </summary>
    private enum ExecutionPhase
    {
        IDLE,
        TURNING,
        TARGET_LOCKING,
        CHARGING,
        ENDING,
        COMPLETED,
        FAILED
    }

    // 方向ベクトルの有効判定に使用する閾値
    private const float DIRECTION_SQR_THRESHOLD =
        0.0001f;

    // 到達判定に使用する距離
    private const float REACH_DISTANCE =
        0.05f;

    // 所有するボス
    private readonly BossController m_owner;

    // 突進攻撃パラメータ
    private S1BossChargeAttackParameters m_parameters;

    // 目標位置を取得する処理
    private Func<Vector3> m_targetPositionProvider;

    // 現在の実行状態
    private ExecutionPhase m_executionPhase;

    // 現在フェーズの経過時間
    private float m_elapsedTime;

    // 突進方向
    private Vector3 m_chargeDirection;

    // 狙い確定後の固定回転
    private Quaternion m_lockedRotation;

    // 突進終了位置
    private Vector3 m_chargeEndPosition;

    // 旋回方向
    private BossChargeTurnDirection m_turnDirection;
    /// <summary>
    /// 旋回方向が変更されたときに通知されます。
    /// </summary>
    public event Action<BossChargeTurnDirection>
        TurnDirectionChanged;
    /// <summary>
    /// 行動フェーズが変更されたときに通知されます。
    /// </summary>
    public event Action<BossActionPhase>
        PhaseChanged;

    /// <summary>
    /// 突進が完了したか取得します。
    /// </summary>
    public bool IsCompleted =>
        m_executionPhase ==
        ExecutionPhase.COMPLETED;

    /// <summary>
    /// 突進が失敗したか取得します。
    /// </summary>
    public bool HasFailed =>
        m_executionPhase ==
        ExecutionPhase.FAILED;

    /// <summary>
    /// 開始時に決定した旋回方向を取得します。
    /// </summary>
    public BossChargeTurnDirection TurnDirection =>
        m_turnDirection;

    /// <summary>
    /// 直線突進実行機構を生成します。
    /// </summary>
    /// <param name="owner">
    /// 実行対象のボス。
    /// </param>
    public StraightChargeExecutor(
        BossController owner)
    {
        m_owner =
            owner;

        m_executionPhase =
            ExecutionPhase.IDLE;
    }

    /// <summary>
    /// 直線突進を開始します。
    /// </summary>
    /// <param name="parameters">
    /// 突進攻撃パラメータ。
    /// </param>
    /// <param name="targetPositionProvider">
    /// 目標位置を取得する処理。
    /// </param>
    /// <returns>
    /// true：開始できました。
    /// false：開始できませんでした。
    /// </returns>
    public bool Start(
        S1BossChargeAttackParameters parameters,
        Func<Vector3> targetPositionProvider)
    {
        Cancel();

        if (m_owner == null ||
            m_owner.Motor == null ||
            m_owner.Navigation == null ||
            parameters == null ||
            targetPositionProvider == null)
        {
            m_executionPhase =
                ExecutionPhase.FAILED;

            return false;
        }

        if (parameters.ChargeSpeed <= 0.0f ||
            parameters.MaxChargeDistance <= 0.0f ||
            parameters.TargetLockDuration < 0.0f ||
            parameters.RotationSpeed < 0.0f ||
            parameters.StopMargin < 0.0f ||
            parameters.EndDuration < 0.0f)
        {
            m_executionPhase =
                ExecutionPhase.FAILED;

            return false;
        }

        m_parameters =
            parameters;

        m_targetPositionProvider =
            targetPositionProvider;

        m_elapsedTime =
            0.0f;

        m_chargeDirection =
            Vector3.zero;

        m_lockedRotation =
            m_owner.transform.rotation;

        m_chargeEndPosition =
            Vector3.zero;

        if (!DetermineTurnDirection())
        {
            SetFailed();

            return false;
        }

        m_owner.Motor.StopHorizontalMovement();

        m_executionPhase =
            ExecutionPhase.TURNING;

        NotifyPhaseChanged(
            BossActionPhase.PREPARATION);

        return true;
    }

    /// <summary>
    /// 直線突進を物理更新します。
    /// </summary>
    /// <param name="deltaTime">
    /// 物理フレームの経過時間。
    /// </param>
    public void FixedUpdate(
        float deltaTime)
    {
        switch (m_executionPhase)
        {
            case ExecutionPhase.TURNING:
                UpdateTurn(
                    deltaTime);
                break;

            case ExecutionPhase.TARGET_LOCKING:
                UpdateTargetLock(
                    deltaTime);
                break;

            case ExecutionPhase.CHARGING:
                UpdateCharge(
                    deltaTime);
                break;

            case ExecutionPhase.ENDING:
                UpdateEnding(
                    deltaTime);
                break;
        }
    }

    /// <summary>
    /// 実行中の突進を中止します。
    /// </summary>
    public void Cancel()
    {
        if (m_owner != null)
        {
            m_owner.Motor?
                .StopHorizontalMovement();

            DisableHitbox();
        }

        m_parameters =
            null;

        m_targetPositionProvider =
            null;

        m_elapsedTime =
            0.0f;

        m_chargeDirection =
            Vector3.zero;

        m_lockedRotation =
            Quaternion.identity;

        m_chargeEndPosition =
            Vector3.zero;

        m_executionPhase =
            ExecutionPhase.IDLE;
    }

    /// <summary>
    /// 目標方向への旋回を更新します。
    /// </summary>
    private void UpdateTurn(
    float deltaTime)
    {
        if (!TryGetTargetDirection(
                out Vector3 direction))
        {
            SetFailed();
            return;
        }

        UpdateTurnDirection(
            direction);

        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction,
                Vector3.up);

        bool hasReachedTarget =
            m_owner.Motor.RotateTowards(
                targetRotation,
                m_parameters.RotationSpeed,
                deltaTime);

        if (!hasReachedTarget)
        {
            return;
        }

        StartTargetLock();
    }
    /// <summary>
    /// 現在の目標方向から必要な旋回方向を更新します。
    /// </summary>
    private void UpdateTurnDirection(
        Vector3 targetDirection)
    {
        Vector3 currentForward =
            m_owner.transform.forward;

        currentForward.y =
            0.0f;

        if (currentForward.sqrMagnitude <=
            DIRECTION_SQR_THRESHOLD)
        {
            return;
        }

        currentForward.Normalize();

        float signedAngle =
            Vector3.SignedAngle(
                currentForward,
                targetDirection,
                Vector3.up);

        BossChargeTurnDirection direction =
            signedAngle >= 0.0f
                ? BossChargeTurnDirection.RIGHT
                : BossChargeTurnDirection.LEFT;

        SetTurnDirection(
            direction);
    }
    /// <summary>
    /// 狙い確定状態を開始します。
    /// </summary>
    private void StartTargetLock()
    {
        /*
         * この瞬間の向きを固定する。
         * 以降Playerが移動しても追尾しない。
         */
        m_chargeDirection =
            m_owner.transform.forward;

        m_chargeDirection.y =
            0.0f;

        if (m_chargeDirection.sqrMagnitude <=
            DIRECTION_SQR_THRESHOLD)
        {
            SetFailed();

            return;
        }

        m_chargeDirection.Normalize();

        m_lockedRotation =
            Quaternion.LookRotation(
                m_chargeDirection,
                Vector3.up);

        m_elapsedTime =
            0.0f;

        m_owner.Motor.StopHorizontalMovement();

        m_executionPhase =
            ExecutionPhase.TARGET_LOCKING;

        NotifyPhaseChanged(
            BossActionPhase.TARGET_LOCK);

        if (m_parameters.TargetLockDuration <=
            0.0f)
        {
            StartCharge();
        }
    }

    /// <summary>
    /// 狙い確定状態を更新します。
    /// </summary>
    private void UpdateTargetLock(
        float deltaTime)
    {
        m_owner.Motor.StopHorizontalMovement();

        /*
         * 外力などで向きがずれても、
         * ロックした方向へ戻す。
         */
        m_owner.Motor.RotateTowards(
            m_lockedRotation,
            m_parameters.RotationSpeed,
            deltaTime);

        m_elapsedTime +=
            deltaTime;

        if (m_elapsedTime <
            m_parameters.TargetLockDuration)
        {
            return;
        }

        StartCharge();
    }

    /// <summary>
    /// 突進移動を開始します。
    /// </summary>
    private void StartCharge()
    {
        if (m_chargeDirection.sqrMagnitude <=
            DIRECTION_SQR_THRESHOLD)
        {
            SetFailed();

            return;
        }

        float chargeDistance =
            m_owner.Navigation
                .GetMaxStraightMoveDistance(
                    m_chargeDirection,
                    m_parameters.MaxChargeDistance,
                    m_parameters.StopMargin);

        /*
         * 移動可能距離が無い場合は、
         * 突進せず終了動作へ移行する。
         */
        if (chargeDistance <= 0.0f)
        {
            StartEnding();

            return;
        }

        m_chargeEndPosition =
            m_owner.transform.position +
            m_chargeDirection *
            chargeDistance;

        m_elapsedTime =
            0.0f;

        EnableHitbox();

        m_executionPhase =
            ExecutionPhase.CHARGING;

        NotifyPhaseChanged(
            BossActionPhase.EXECUTION);
    }

    /// <summary>
    /// 突進移動を更新します。
    /// </summary>
    private void UpdateCharge(
        float deltaTime)
    {
        Vector3 currentPosition =
            m_owner.transform.position;

        Vector3 nextPosition =
            Vector3.MoveTowards(
                currentPosition,
                m_chargeEndPosition,
                m_parameters.ChargeSpeed *
                deltaTime);

        m_owner.Motor.MovePosition(
            nextPosition);

        Vector3 difference =
            m_chargeEndPosition -
            nextPosition;

        difference.y =
            0.0f;

        if (difference.sqrMagnitude >
            REACH_DISTANCE *
            REACH_DISTANCE)
        {
            return;
        }

        m_owner.Motor.MovePosition(
            m_chargeEndPosition);

        StartEnding();
    }

    /// <summary>
    /// 終了処理を開始します。
    /// </summary>
    private void StartEnding()
    {
        m_owner.Motor.StopHorizontalMovement();

        DisableHitbox();

        m_elapsedTime =
            0.0f;

        m_executionPhase =
            ExecutionPhase.ENDING;

        NotifyPhaseChanged(
            BossActionPhase.RECOVERY);

        if (m_parameters.EndDuration <=
            0.0f)
        {
            SetCompleted();
        }
    }

    /// <summary>
    /// 終了状態を更新します。
    /// </summary>
    private void UpdateEnding(
        float deltaTime)
    {
        m_elapsedTime +=
            deltaTime;

        if (m_elapsedTime <
            m_parameters.EndDuration)
        {
            return;
        }

        SetCompleted();
    }

    /// <summary>
    /// 最初に再生する左右旋回方向を決定します。
    /// </summary>
    private bool DetermineTurnDirection()
    {
        if (!TryGetTargetDirection(
                out Vector3 targetDirection))
        {
            return false;
        }

        Vector3 currentForward =
            m_owner.transform.forward;

        currentForward.y =
            0.0f;

        if (currentForward.sqrMagnitude <=
            DIRECTION_SQR_THRESHOLD)
        {
            return false;
        }

        currentForward.Normalize();

        float signedAngle =
            Vector3.SignedAngle(
                currentForward,
                targetDirection,
                Vector3.up);

        m_turnDirection =
            signedAngle >= 0.0f
                ? BossChargeTurnDirection.RIGHT
                : BossChargeTurnDirection.LEFT;

        return true;
    }

    /// <summary>
    /// 現在の目標方向を取得します。
    /// </summary>
    private bool TryGetTargetDirection(
        out Vector3 direction)
    {
        direction =
            Vector3.zero;

        if (m_targetPositionProvider == null ||
            m_owner == null)
        {
            return false;
        }

        direction =
            m_targetPositionProvider.Invoke() -
            m_owner.transform.position;

        direction.y =
            0.0f;

        if (direction.sqrMagnitude <=
            DIRECTION_SQR_THRESHOLD)
        {
            direction =
                Vector3.zero;

            return false;
        }

        direction.Normalize();

        return true;
    }

    /// <summary>
    /// 突進Hitboxを有効にします。
    /// </summary>
    private void EnableHitbox()
    {
        if (m_parameters?.AttackIdentifier ==
            null)
        {
            return;
        }

        m_owner.EnableAttackHitboxes(
            m_parameters.AttackIdentifier);
    }

    /// <summary>
    /// 突進Hitboxを無効にします。
    /// </summary>
    private void DisableHitbox()
    {
        if (m_parameters?.AttackIdentifier ==
            null)
        {
            return;
        }

        m_owner.AttackHitboxRegistry?
            .DisableHitboxes(
                m_parameters.AttackIdentifier);
    }

    /// <summary>
    /// 行動フェーズの変更を通知します。
    /// </summary>
    private void NotifyPhaseChanged(
        BossActionPhase phase)
    {
        PhaseChanged?.Invoke(
            phase);
    }

    /// <summary>
    /// 突進を完了状態にします。
    /// </summary>
    private void SetCompleted()
    {
        m_owner?.Motor?
            .StopHorizontalMovement();

        DisableHitbox();

        m_executionPhase =
            ExecutionPhase.COMPLETED;
    }

    /// <summary>
    /// 突進を失敗状態にします。
    /// </summary>
    private void SetFailed()
    {
        m_owner?.Motor?
            .StopHorizontalMovement();

        DisableHitbox();

        m_executionPhase =
            ExecutionPhase.FAILED;
    }

    private void SetTurnDirection(
    BossChargeTurnDirection direction)
    {
        if (m_turnDirection == direction)
        {
            return;
        }

        m_turnDirection =
            direction;

        TurnDirectionChanged?.Invoke(
            direction);
    }
}