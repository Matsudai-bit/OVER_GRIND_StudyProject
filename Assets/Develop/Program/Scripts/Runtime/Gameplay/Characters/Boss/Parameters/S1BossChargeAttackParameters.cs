using UnityEngine;

/// <summary>
/// ボスの突進攻撃で使用するパラメータを保持します。
/// </summary>
public sealed class S1BossChargeAttackParameters
{
    /// <summary>
    /// 突進前の準備時間を取得します。
    /// </summary>
    public float PreparationDuration { get; }

    /// <summary>
    /// 準備中の回転速度を取得します。
    /// </summary>
    public float RotationSpeed { get; }

    /// <summary>
    /// 突進の最高速度を取得します。
    /// </summary>
    public float ChargeSpeed { get; }

    /// <summary>
    /// 最大突進距離を取得します。
    /// </summary>
    public float MaxChargeDistance { get; }

    /// <summary>
    /// NavMesh境界との停止余白を取得します。
    /// </summary>
    public float StopMargin { get; }

    /// <summary>
    /// 突進終了後の硬直時間を取得します。
    /// </summary>
    public float EndDuration { get; }

    /// <summary>
    /// 最高速度へ到達するまでの時間を取得します。
    /// </summary>
    public float AccelerationDuration { get; }

    /// <summary>
    /// 突進加速時のイージングを取得します。
    /// </summary>
    public AnimationCurve AccelerationCurve { get; }

    /// <summary>
    /// 停止地点の何m手前から減速を開始するか取得します。
    /// </summary>
    public float DecelerationDistance { get; }

    /// <summary>
    /// 突進減速時のイージングを取得します。
    /// </summary>
    public AnimationCurve DecelerationCurve { get; }

    /// <summary>
    /// 対象位置を通過してから進む距離を取得します。
    /// </summary>
    public float TargetOverrunDistance { get; }

    /// <summary>
    /// 突進攻撃中を示すAnimator Bool名を取得します。
    /// </summary>
    public string AnimationActiveBoolName { get; }

    /// <summary>
    /// 突進攻撃の段階を示すAnimator Int名を取得します。
    /// </summary>
    public string AnimationPhaseIntName { get; }

    /// <summary>
    /// 突進攻撃の識別子を取得します。
    /// </summary>
    public AttackIdentifier AttackIdentifier { get; }

    /// <summary>
    /// 突進攻撃パラメータを生成します。
    /// </summary>
    public S1BossChargeAttackParameters(
        float preparationDuration,
        float rotationSpeed,
        float chargeSpeed,
        float maxChargeDistance,
        float stopMargin,
        float endDuration,
        float accelerationDuration,
        AnimationCurve accelerationCurve,
        float decelerationDistance,
        AnimationCurve decelerationCurve,
        float targetOverrunDistance,
        string animationActiveBoolName,
        string animationPhaseIntName,
        AttackIdentifier attackIdentifier)
    {
        PreparationDuration =
            preparationDuration;

        RotationSpeed =
            rotationSpeed;

        ChargeSpeed =
            chargeSpeed;

        MaxChargeDistance =
            maxChargeDistance;

        StopMargin =
            stopMargin;

        EndDuration =
            endDuration;

        AccelerationDuration =
            accelerationDuration;

        AccelerationCurve =
            accelerationCurve;

        DecelerationDistance =
            decelerationDistance;

        DecelerationCurve =
            decelerationCurve;

        TargetOverrunDistance =
            targetOverrunDistance;

        AnimationActiveBoolName =
            animationActiveBoolName;

        AnimationPhaseIntName =
            animationPhaseIntName;

        AttackIdentifier =
            attackIdentifier;
    }
}