using UnityEngine;

/// <summary>停止中に開始するブーストチャージを管理します。</summary>
public sealed class PlayerStaticChargeState : PlayerBoostChargingState
{
    /// <summary>停止開始のチャージであることを示します。</summary>
    protected override bool IsStaticCharge => true;

    /// <summary>停止チャージの向き計算を初期化します。</summary>
    protected override void InitializeChargeMode() { }

    /// <summary>停止チャージのアニメーションを開始します。</summary>
    protected override void PlayChargeAnimation()
    {
        Owner.AnimationPresenter.StopChargeMovingAnimation();
        Owner.AnimationPresenter.PlayStationaryBoostChargingAnimation();
    }

    /// <summary>停止チャージのアニメーションを終了します。</summary>
    protected override void StopChargeAnimation()
    {
        Owner.AnimationPresenter.StopStationaryBoostChargingAnimation();
    }

    /// <summary>停止中は移動チャージ方向の表示を解除します。</summary>
    /// <param name="input">移動入力。</param>
    protected override void UpdateChargeDirectionAnimation(Vector2 input)
    {
        Owner.AnimationPresenter.ResetMoveChargeDirection();
    }

    /// <summary>左右入力でその場回転し、対象があれば視線を補正します。</summary>
    /// <param name="input">移動入力。</param>
    /// <param name="turnSpeed">現在の旋回速度。</param>
    /// <param name="target">現在のターゲット。</param>
    protected override void UpdateChargeMode(Vector2 input, float turnSpeed, Transform target)
    {
        UpdateStationaryChargeRotation(input);
        if (target != null)
            UpdateTargetFacingDirection(target);

        m_currentModelFacingDirection = m_currentFacingDirection;
    }

    /// <summary>停止チャージでは本体の向きを変更しません。</summary>
    protected override void AlignFacingOnExit() { }

    /// <summary>
    /// 停止中チャージ時の回転処理を行います。
    ///
    /// 最初に確定した左右入力に応じてプレイヤーをその場で回転させます。
    /// 回転速度はチャージ率によって変化せず、常に一定です。
    /// </summary>
    /// <param name="input">正規化された移動入力。</param>
    private void UpdateStationaryChargeRotation(
        Vector2 input)
    {
        // 停止中はドリフト用の旋回方向固定を使わず、左右入力をそのまま反映する。
        // これにより、チャージ中でも任意のタイミングで左右へ切り返せる。
        float steeringInput =
            m_parameterAsset.GetSmoothedSteeringInput(input);

        if (Mathf.Approximately(steeringInput, 0.0f))
        {
            return;
        }

        float rotationAmount =
            steeringInput *
            m_parameterAsset.StationaryChargeRotationSpeed *
            Time.fixedDeltaTime;

        Owner.transform.Rotate(
            0.0f,
            rotationAmount,
            0.0f,
            Space.World);

        m_currentFacingDirection =
            Owner.transform.forward;

        m_currentFacingDirection.y = 0.0f;

        if (m_currentFacingDirection.sqrMagnitude >
            0.0001f)
        {
            m_currentFacingDirection.Normalize();
        }

        m_currentVelocityDirection =
            m_currentFacingDirection;

        // 停止中開始ではPlayer本体を回転させる既存仕様を維持するため、
        // モデルもPlayer本体の正面へ合わせます。
        m_currentModelFacingDirection =
            m_currentFacingDirection;

    }
}
