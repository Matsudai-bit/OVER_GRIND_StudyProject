/// <summary>
/// ボスの突進攻撃で使用するパラメータを保持します。
/// </summary>
public sealed class S1BossChargeAttackParameters
{
    /// <summary>
    /// 狙い確定状態の継続時間を取得します。
    /// </summary>
    public float TargetLockDuration { get; }

    /// <summary>
    /// 目標方向への回転速度を取得します。
    /// </summary>
    public float RotationSpeed { get; }

    /// <summary>
    /// 突進速度を取得します。
    /// </summary>
    public float ChargeSpeed { get; }

    /// <summary>
    /// 最大突進距離を取得します。
    /// </summary>
    public float MaxChargeDistance { get; }

    /// <summary>
    /// 障害物や移動範囲との停止余白を取得します。
    /// </summary>
    public float StopMargin { get; }

    /// <summary>
    /// 突進終了後の硬直時間を取得します。
    /// </summary>
    public float EndDuration { get; }

    /// <summary>
    /// 突進攻撃中を示すAnimator Bool名を取得します。
    /// </summary>
    public string AnimationActiveBoolName { get; }

    /// <summary>
    /// 突進攻撃フェーズを示すAnimator Int名を取得します。
    /// </summary>
    public string AnimationPhaseIntName { get; }

    /// <summary>
    /// 左右旋回方向を示すAnimator Int名を取得します。
    /// </summary>
    public string AnimationTurnDirectionIntName { get; }

    /// <summary>
    /// 突進攻撃の識別子を取得します。
    /// </summary>
    public AttackIdentifier AttackIdentifier { get; }

    /// <summary>
    /// 突進攻撃パラメータを生成します。
    /// </summary>
    public S1BossChargeAttackParameters(
        float targetLockDuration,
        float rotationSpeed,
        float chargeSpeed,
        float maxChargeDistance,
        float stopMargin,
        float endDuration,
        string animationActiveBoolName,
        string animationPhaseIntName,
        string animationTurnDirectionIntName,
        AttackIdentifier attackIdentifier)
    {
        TargetLockDuration =
            targetLockDuration;

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

        AnimationActiveBoolName =
            animationActiveBoolName;

        AnimationPhaseIntName =
            animationPhaseIntName;

        AnimationTurnDirectionIntName =
            animationTurnDirectionIntName;

        AttackIdentifier =
            attackIdentifier;
    }
}