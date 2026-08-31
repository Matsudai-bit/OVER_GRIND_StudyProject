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
    /// 突進速度を取得します。
    /// </summary>
    public float ChargeSpeed { get; }

    /// <summary>
    /// 最大突進距離を取得します。
    /// </summary>
    public float MaxChargeDistance { get; }

    /// <summary>
    /// 障害物や行動範囲境界との停止余白を取得します。
    /// </summary>
    public float StopMargin { get; }

    /// <summary>
    /// 突進終了後の硬直時間を取得します。
    /// </summary>
    public float EndDuration { get; }

    /// <summary>
    /// 準備アニメーションのBool名を取得します。
    /// </summary>
    public string PreparationAnimationBoolName { get; }

    /// <summary>
    /// 突進アニメーションのBool名を取得します。
    /// </summary>
    public string ChargeAnimationBoolName { get; }

    /// <summary>
    /// 終了アニメーションのBool名を取得します。
    /// </summary>
    public string EndAnimationBoolName { get; }

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
        string preparationAnimationBoolName,
        string chargeAnimationBoolName,
        string endAnimationBoolName,
        AttackIdentifier attackIdentifier)
    {
        PreparationDuration = preparationDuration;
        RotationSpeed = rotationSpeed;
        ChargeSpeed = chargeSpeed;
        MaxChargeDistance = maxChargeDistance;
        StopMargin = stopMargin;
        EndDuration = endDuration;

        PreparationAnimationBoolName =
            preparationAnimationBoolName;

        ChargeAnimationBoolName =
            chargeAnimationBoolName;

        EndAnimationBoolName =
            endAnimationBoolName;

        AttackIdentifier = attackIdentifier;
    }
}