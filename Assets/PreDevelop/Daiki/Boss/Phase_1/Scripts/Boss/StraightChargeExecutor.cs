using System;
using UnityEngine;

/// <summary>
/// 1回分の直線突進を実行します。
/// </summary>
public sealed class StraightChargeExecutor
{
    /// <summary>
    /// 直線突進の実行状態です。
    /// </summary>
    private enum ExecutionPhase
    {
        IDLE,
        PREPARING,
        CHARGING,
        ENDING,
        COMPLETED,
        FAILED
    }

    // 方向ベクトルの有効判定に使用する閾値
    private const float DIRECTION_SQR_THRESHOLD = 0.0001f;

    // 到達判定に使用する距離
    private const float REACH_DISTANCE = 0.05f;

    // 所有するボス
    private readonly BossController m_owner;

    // 実行設定
    private S1BossChargeAttackParameters m_parameters;

    // 目標位置を取得する処理
    private Func<Vector3> m_targetPositionProvider;

    // 現在の実行状態
    private ExecutionPhase m_executionPhase;

    // 現在フェーズの経過時間
    private float m_elapsedTime;

    // 突進方向
    private Vector3 m_chargeDirection;

    // 突進終了位置
    private Vector3 m_chargeEndPosition;

    /// <summary>
    /// 突進が完了したか取得します。
    /// </summary>
    public bool IsCompleted =>
        m_executionPhase == ExecutionPhase.COMPLETED;

    /// <summary>
    /// 突進が失敗したか取得します。
    /// </summary>
    public bool HasFailed =>
        m_executionPhase == ExecutionPhase.FAILED;

    /// <summary>
    /// 直線突進実行機構を生成します。
    /// </summary>
    /// <param name="owner">実行対象のボス。</param>
    public StraightChargeExecutor(BossController owner)
    {
        m_owner = owner;
        m_executionPhase = ExecutionPhase.IDLE;
    }

    /// <summary>
    /// 直線突進を開始します。
    /// </summary>
    /// <param name="parameters">突進設定。</param>
    /// <param name="targetPositionProvider">目標位置を取得する処理。</param>
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
            m_owner.AnimationController == null ||
            parameters == null ||
            targetPositionProvider == null)
        {
            m_executionPhase = ExecutionPhase.FAILED;
            return false;
        }

        if (parameters.ChargeSpeed <= 0.0f)
        {
            m_executionPhase = ExecutionPhase.FAILED;
            return false;
        }

        m_parameters = parameters;
        m_targetPositionProvider = targetPositionProvider;

        m_elapsedTime = 0.0f;
        m_chargeDirection = Vector3.zero;
        m_chargeEndPosition = Vector3.zero;

        m_owner.Motor.StopHorizontalMovement();

        // 予備動作アニメーションを開始します。
        SetAnimationBool(
            m_parameters.PreparationAnimationBoolName,
            true);

        m_executionPhase = ExecutionPhase.PREPARING;

        return true;
    }

    /// <summary>
    /// 直線突進を物理更新します。
    /// </summary>
    /// <param name="deltaTime">物理フレームの経過時間。</param>
    public void FixedUpdate(float deltaTime)
    {
        switch (m_executionPhase)
        {
            case ExecutionPhase.PREPARING:
                UpdatePreparation(deltaTime);
                break;

            case ExecutionPhase.CHARGING:
                UpdateCharge(deltaTime);
                break;

            case ExecutionPhase.ENDING:
                UpdateEnding(deltaTime);
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
            m_owner.Motor?.StopHorizontalMovement();
            DisableHitbox();

            ResetAnimationBools();
        }

        m_parameters = null;
        m_targetPositionProvider = null;

        m_elapsedTime = 0.0f;
        m_chargeDirection = Vector3.zero;
        m_chargeEndPosition = Vector3.zero;

        m_executionPhase = ExecutionPhase.IDLE;
    }

    /// <summary>
    /// 予備動作を更新します。
    /// </summary>
    /// <param name="deltaTime">物理フレームの経過時間。</param>
    private void UpdatePreparation(float deltaTime)
    {
        if (!TryGetTargetDirection(out Vector3 direction))
        {
            SetFailed();
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction,
                Vector3.up);

        m_owner.Motor.RotateTowards(
            targetRotation,
            m_parameters.RotationSpeed,
            deltaTime);

        m_elapsedTime += deltaTime;

        if (m_elapsedTime <
            m_parameters.PreparationDuration)
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
        m_chargeDirection =
            m_owner.transform.forward;

        m_chargeDirection.y = 0.0f;

        if (m_chargeDirection.sqrMagnitude <=
            DIRECTION_SQR_THRESHOLD)
        {
            SetFailed();
            return;
        }

        m_chargeDirection.Normalize();

        float chargeDistance =
            m_owner.Navigation.GetMaxStraightMoveDistance(
                m_chargeDirection,
                m_parameters.MaxChargeDistance,
                m_parameters.StopMargin);

        // 予備動作を終了します。
        SetAnimationBool(
            m_parameters.PreparationAnimationBoolName,
            false);

        if (chargeDistance <= 0.0f)
        {
            StartEnding();
            return;
        }

        m_chargeEndPosition =
            m_owner.transform.position +
            m_chargeDirection * chargeDistance;

        m_elapsedTime = 0.0f;

        // 突進アニメーションを開始します。
        SetAnimationBool(
            m_parameters.ChargeAnimationBoolName,
            true);

        EnableHitbox();

        m_executionPhase =
            ExecutionPhase.CHARGING;
    }

    /// <summary>
    /// 突進移動を更新します。
    /// </summary>
    /// <param name="deltaTime">物理フレームの経過時間。</param>
    private void UpdateCharge(float deltaTime)
    {
        Vector3 currentPosition =
            m_owner.transform.position;

        Vector3 nextPosition =
            Vector3.MoveTowards(
                currentPosition,
                m_chargeEndPosition,
                m_parameters.ChargeSpeed * deltaTime);

        m_owner.Motor.MovePosition(nextPosition);

        Vector3 difference =
            m_chargeEndPosition - nextPosition;

        difference.y = 0.0f;

        if (difference.sqrMagnitude >
            REACH_DISTANCE * REACH_DISTANCE)
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

        // 突進アニメーションを終了します。
        SetAnimationBool(
            m_parameters.ChargeAnimationBoolName,
            false);

        // 終了アニメーションを開始します。
        SetAnimationBool(
            m_parameters.EndAnimationBoolName,
            true);

        m_elapsedTime = 0.0f;

        m_executionPhase =
            ExecutionPhase.ENDING;

        if (m_parameters.EndDuration <= 0.0f)
        {
            SetCompleted();
        }
    }

    /// <summary>
    /// 終了状態を更新します。
    /// </summary>
    /// <param name="deltaTime">物理フレームの経過時間。</param>
    private void UpdateEnding(float deltaTime)
    {
        m_elapsedTime += deltaTime;

        if (m_elapsedTime <
            m_parameters.EndDuration)
        {
            return;
        }

        SetCompleted();
    }

    /// <summary>
    /// 現在の目標方向を取得します。
    /// </summary>
    /// <param name="direction">目標方向。</param>
    /// <returns>
    /// true：有効な方向を取得しました。
    /// false：方向を取得できませんでした。
    /// </returns>
    private bool TryGetTargetDirection(
        out Vector3 direction)
    {
        direction =
            m_targetPositionProvider.Invoke() -
            m_owner.transform.position;

        direction.y = 0.0f;

        if (direction.sqrMagnitude <=
            DIRECTION_SQR_THRESHOLD)
        {
            direction = Vector3.zero;
            return false;
        }

        direction.Normalize();

        return true;
    }

    /// <summary>
    /// AnimatorのBool値を設定します。
    /// </summary>
    /// <param name="parameterName">Boolパラメータ名。</param>
    /// <param name="value">設定する値。</param>
    private void SetAnimationBool(
        string parameterName,
        bool value)
    {
        if (string.IsNullOrEmpty(parameterName) ||
            m_owner?.AnimationController == null)
        {
            return;
        }

        int parameterID =
            Animator.StringToHash(parameterName);

        m_owner.AnimationController.SetBool(
            parameterID,
            value);
    }

    /// <summary>
    /// 突進用のAnimator Boolをすべて解除します。
    /// </summary>
    private void ResetAnimationBools()
    {
        if (m_parameters == null)
        {
            return;
        }

        SetAnimationBool(
            m_parameters.PreparationAnimationBoolName,
            false);

        SetAnimationBool(
            m_parameters.ChargeAnimationBoolName,
            false);

        SetAnimationBool(
            m_parameters.EndAnimationBoolName,
            false);
    }

    /// <summary>
    /// 突進Hitboxを有効にします。
    /// </summary>
    private void EnableHitbox()
    {
        if (m_parameters.AttackIdentifier == null)
        {
            return;
        }

        m_owner.AttackHitboxRegistry?.EnableHitbox(
            m_parameters.AttackIdentifier);
    }

    /// <summary>
    /// 突進Hitboxを無効にします。
    /// </summary>
    private void DisableHitbox()
    {
        if (m_parameters?.AttackIdentifier == null)
        {
            return;
        }

        m_owner.AttackHitboxRegistry?.DisableHitbox(
            m_parameters.AttackIdentifier);
    }

    /// <summary>
    /// 突進を完了状態にします。
    /// </summary>
    private void SetCompleted()
    {
        // 終了アニメーションも解除します。
        SetAnimationBool(
            m_parameters.EndAnimationBoolName,
            false);

        m_executionPhase =
            ExecutionPhase.COMPLETED;
    }

    /// <summary>
    /// 突進を失敗状態にします。
    /// </summary>
    private void SetFailed()
    {
        m_owner?.Motor?.StopHorizontalMovement();

        DisableHitbox();
        ResetAnimationBools();

        m_executionPhase =
            ExecutionPhase.FAILED;
    }
}