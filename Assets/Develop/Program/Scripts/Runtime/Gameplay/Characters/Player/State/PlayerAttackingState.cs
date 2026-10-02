using UnityEngine;

/// <summary>
/// プレイヤーの地上攻撃状態を管理します。
/// 攻撃ボタンを押している間、開始時点の体の向きを維持したまま
/// 移動しながら攻撃を続け、一定周期で多段ヒット判定を行います。
/// 移動速度は攻撃継続時間として消費されます。
/// </summary>
public sealed class PlayerAttackingState
    : StateBase<PlayerStateMachineComponent>
{
    // 攻撃中は開始時の方向へ固定するため回転させない
    private const float ATTACK_ROTATION_SPEED = 0.0f;

    // 攻撃中に1秒あたり消費する速度
    private const float SPEED_DECAY_PER_SECOND = 8.0f;

    // これ未満になったら速度を使い切ったとみなす
    private const float SPEED_EPSILON = 0.01f;

    // 攻撃開始時の速度
    private float m_initialSlideSpeed;

    // 攻撃中の残り速度
    private float m_currentSlideSpeed;

    // 次のヒット判定までの経過時間
    private float m_hitTimer;

    // 攻撃開始時に固定する移動方向
    private Vector3 m_slideDirection;

    /// <summary>
    /// 攻撃状態を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        m_currentSlideSpeed =
            Owner.Motor.HorizontalSpeed;

        m_initialSlideSpeed =
            Mathf.Max(
                m_currentSlideSpeed,
                SPEED_EPSILON);

        m_slideDirection =
            Owner.Motor.FacingDirection;

        m_hitTimer = 0.0f;

        Owner.AttackController
            .EnableContinuousAttackHitboxes();

        Owner.AnimationPresenter
            .PlayAttackAnimation();

        Owner.SetSpeedDisplayOverride(
            m_currentSlideSpeed);
    }

    /// <summary>
    /// 攻撃状態を更新します。
    /// </summary>
    /// <param name="deltaTime">経過時間。</param>
    protected override void OnUpdate(float deltaTime)
    {
        // 攻撃ボタンを離したら通常移動へ戻る
        if (!Owner.InputReader.IsAttackHeld)
        {
            Machine.ChangeState<PlayerIdlingState>();
        }
    }

    /// <summary>
    /// 攻撃状態の物理更新を行います。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        float fixedDeltaTime =
            Time.fixedDeltaTime;

        if (!Owner.InputReader.IsAttackHeld)
        {
            return;
        }

        // 攻撃継続用の速度を消費する。
        // 実際の移動速度を遅くしても、この値自体は変更しない。
        m_currentSlideSpeed -=
            SPEED_DECAY_PER_SECOND *
            fixedDeltaTime;

        if (m_currentSlideSpeed <= SPEED_EPSILON)
        {
            Machine.ChangeState<PlayerIdlingState>();
            return;
        }

        UpdateMovement(fixedDeltaTime);

        // UIには攻撃継続用の残り速度を表示する
        Owner.SetSpeedDisplayOverride(
            m_currentSlideSpeed);

        // 速度に応じてヒット間隔を変化させる
        UpdateHitCycle(fixedDeltaTime);

        // ヒットボックスが対象へ食い込んでいる場合は位置を補正する
        if (Owner.AttackController.TryGetMaxPenetration(
                out Vector3 penetrationDirection,
                out float penetrationDistance))
        {
            Owner.Motor.ResolvePenetration(
                penetrationDirection,
                penetrationDistance);
        }

        // 攻撃中でもジャンプ入力を受け付ける
        if (Owner.Monitor.IsGrounded &&
            Owner.InputReader.HasJumpInput)
        {
            Machine.ChangeState<PlayerJumpingState>();
        }
    }

    /// <summary>
    /// 攻撃中の移動を更新します。
    /// </summary>
    /// <param name="fixedDeltaTime">物理更新時間。</param>
    private void UpdateMovement(float fixedDeltaTime)
    {
        // 移動入力がない場合は、通常の減速処理を
        // 攻撃用の倍率で緩やかにして使用する。
        if (!Owner.InputReader.HasMoveInput)
        {
            PlayerMoveParameters normalParameters =
                Owner.MovementParameterAsset
                    .CreateMoveParameters();

            float attackDecelerationMultiplier =
                Mathf.Max(
                    Owner.MovementParameterAsset
                        .AttackDecelerationMultiplier,
                    1.0f);

            PlayerMoveParameters attackParameters =
                new PlayerMoveParameters(
                    normalParameters.MaxMoveSpeed,
                    normalParameters.TimeToMaxSpeed,
                    normalParameters.TimeToStop *
                        attackDecelerationMultiplier,
                    normalParameters.RotationSpeed);

            Owner.Motor.Decelerate(
                attackParameters,
                fixedDeltaTime);

            return;
        }

        // ヒット中だけ実際の移動速度を遅くする。
        // m_currentSlideSpeed自体は変更しない。
        float movementSpeed =
            m_currentSlideSpeed;

        if (Owner.AttackController.IsHittingAnyTarget())
        {
            movementSpeed *=
                Owner.MovementParameterAsset
                    .AttackHitMovementSpeedMultiplier;
        }

        Owner.Motor.MoveAtFixedWorldDirection(
            m_slideDirection,
            movementSpeed,
            ATTACK_ROTATION_SPEED,
            fixedDeltaTime,
            applyObstacleAvoidance: false);
    }

    /// <summary>
    /// 攻撃速度に応じた多段ヒット判定を実行します。
    /// 攻撃開始時を基準として、速度が低下するほど
    /// 1秒あたりのヒット回数も減少します。
    /// </summary>
    /// <param name="fixedDeltaTime">物理更新時間。</param>
    private void UpdateHitCycle(float fixedDeltaTime)
    {
        float speedRate =
            Mathf.Clamp01(
                m_currentSlideSpeed /
                m_initialSlideSpeed);

        float currentHitsPerSecond =
            Owner.AttackController.BaseHitsPerSecond *
            speedRate;

        float hitInterval =
            1.0f /
            Mathf.Max(
                currentHitsPerSecond,
                0.0001f);

        m_hitTimer += fixedDeltaTime;

        while (m_hitTimer >= hitInterval)
        {
            m_hitTimer -= hitInterval;

            // 空振りしても攻撃状態は終了しない。
            // 押している間は継続して判定する。
            Owner.AttackController
                .ApplyContinuousHitTick();
        }
    }

    /// <summary>
    /// 攻撃状態を終了します。
    /// </summary>
    protected override void OnExitState()
    {
        Owner.AnimationPresenter
            .StopAttackAnimation();

        Owner.AttackController
            .DisableAttackHitboxes();

        Owner.ClearSpeedDisplayOverride();
    }
}