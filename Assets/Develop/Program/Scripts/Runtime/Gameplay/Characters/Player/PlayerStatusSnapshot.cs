using UnityEngine;

/// <summary>
/// ある時点におけるプレイヤー状況の読み取り専用スナップショットです。
/// Animation・Effect・Sound・Camera演出などの参照元として使用します。
/// </summary>
public readonly struct PlayerStatusSnapshot
{
    /// <summary>
    /// プレイヤー状況のスナップショットを生成します。
    /// </summary>
    public PlayerStatusSnapshot(
        PlayerConditionFlags conditions,
        Vector3 position,
        Vector3 velocity,
        Vector3 horizontalVelocity,
        float maxMoveSpeed,
        Vector3 groundNormal,
        float groundSlopeAngle,
        Collider groundCollider)
    {
        Conditions = conditions;
        Position = position;
        Velocity = velocity;
        HorizontalVelocity = horizontalVelocity;
        HorizontalSpeed = horizontalVelocity.magnitude;
        VerticalSpeed = velocity.y;
        MaxMoveSpeed = Mathf.Max(maxMoveSpeed, 0.0f);
        GroundNormal = groundNormal;
        GroundSlopeAngle = Mathf.Max(groundSlopeAngle, 0.0f);
        GroundCollider = groundCollider;
    }

    /// <summary>
    /// 現在成立している状態フラグを取得します。
    /// </summary>
    public PlayerConditionFlags Conditions { get; }

    /// <summary>
    /// プレイヤーのワールド座標を取得します。
    /// </summary>
    public Vector3 Position { get; }

    /// <summary>
    /// プレイヤーの現在速度を取得します。
    /// </summary>
    public Vector3 Velocity { get; }

    /// <summary>
    /// プレイヤーの水平速度ベクトルを取得します。
    /// </summary>
    public Vector3 HorizontalVelocity { get; }

    /// <summary>
    /// プレイヤーの水平速度を取得します。
    /// </summary>
    public float HorizontalSpeed { get; }

    /// <summary>
    /// プレイヤーの垂直速度を取得します。
    /// </summary>
    public float VerticalSpeed { get; }

    /// <summary>
    /// 現在の最大移動速度を取得します。
    /// </summary>
    public float MaxMoveSpeed { get; }

    /// <summary>
    /// 現在の接地面法線を取得します。
    /// </summary>
    public Vector3 GroundNormal { get; }

    /// <summary>
    /// 現在の接地面角度を取得します。
    /// </summary>
    public float GroundSlopeAngle { get; }

    /// <summary>
    /// 現在プレイヤーを支えている地面Colliderを取得します。
    /// </summary>
    public Collider GroundCollider { get; }

    /// <summary>
    /// 移動しながらVブーストをチャージしているかを取得します。
    /// </summary>
    public bool IsChargeMoving =>
        HasCondition(
            PlayerConditionFlags.CHARGE_MOVING);

    /// <summary>
    /// チャージ攻撃中かを取得します。
    /// </summary>
    public bool IsChargeAttacking =>
        HasCondition(
            PlayerConditionFlags.CHARGE_ATTACKING);

    /// <summary>
    /// 指定した状態が成立しているか確認します。
    /// </summary>
    /// <param name="condition">確認する状態。</param>
    /// <returns>true：状態が成立しています。</returns>
    public bool HasCondition(
        PlayerConditionFlags condition)
    {
        return (Conditions & condition) != 0;
    }

    /// <summary>
    /// 指定したすべての状態が成立しているか確認します。
    /// </summary>
    /// <param name="conditions">確認する状態群。</param>
    /// <returns>true：すべての状態が成立しています。</returns>
    public bool HasAllConditions(
        PlayerConditionFlags conditions)
    {
        return (Conditions & conditions) == conditions;
    }

    /// <summary>
    /// 指定した状態のいずれかが成立しているか確認します。
    /// </summary>
    /// <param name="conditions">確認する状態群。</param>
    /// <returns>true：いずれかの状態が成立しています。</returns>
    public bool HasAnyCondition(
        PlayerConditionFlags conditions)
    {
        return (Conditions & conditions) != 0;
    }
}
