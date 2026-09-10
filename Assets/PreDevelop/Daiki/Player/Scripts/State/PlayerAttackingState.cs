using UnityEngine;

/// <summary>
/// プレイヤーの地上攻撃状態を管理します。
/// 攻撃ボタンを押している間、開始時点の体の向きを維持したまま
/// 移動しながら攻撃を続け、一定周期で多段ヒット判定を行います。
/// 移動速度は攻撃継続リソースとして消費され、0になると攻撃は中断されます。
/// 攻撃対象に命中している場合は通常速度で消費し、
/// 空振りしている場合は緩やかに消費します。
/// 移動・判定処理は物理タイミングに同期させるためFixedUpdateで行います。
/// </summary>
public sealed class PlayerAttackingState
    : StateBase<PlayerStateMachineComponent>
{
    // 攻撃移動中の回転速度
    // 移動方向は開始時点の体の向きに固定するため、
    // ここでは回転させず0を指定する
    private const float ATTACK_ROTATION_SPEED = 0.0f;

    // 攻撃対象に命中しているときに1秒あたり消費する速度
    private const float HIT_SPEED_DECAY_PER_SECOND = 5.0f;

    // 攻撃対象に命中していないときに1秒あたり消費する速度
    // 空振り中は攻撃を長く継続できるよう、命中時より緩やかに消費する
    private const float MISS_SPEED_DECAY_PER_SECOND = 1.0f;

    // これ未満になったら速度を使い切ったとみなす閾値
    private const float SPEED_EPSILON = 0.01f;

    // 次のヒット判定までの経過時間
    private float m_hitTimer;

    // 攻撃中の現在の移動速度（攻撃継続リソース）
    private float m_currentSlideSpeed;

    // 攻撃開始時に固定する移動方向（体が向いている方向）
    private Vector3 m_slideDirection;

    /// <summary>
    /// 攻撃状態開始時の初期化を行います。
    /// </summary>
    protected override void OnStartState()
    {
        // 攻撃開始時点の速度・向きを固定する
        // 速度は攻撃継続リソースとして、以後消費していく
        m_currentSlideSpeed = Owner.Motor.HorizontalSpeed;
        m_slideDirection = Owner.Motor.FacingDirection;

        m_hitTimer = 0.0f;

        Owner.AttackController.EnableContinuousAttackHitboxes();
        Owner.AnimationPresenter.PlayAttackAnimation();

        // UI上は、実際の物理速度ではなく
        // 攻撃継続リソースとしての速度を表示させる
        Owner.SetSpeedDisplayOverride(m_currentSlideSpeed);
    }

    /// <summary>
    /// 毎フレームの入力処理を行います。
    /// </summary>
    /// <param name="deltaTime">フレーム間の経過時間。</param>
    protected override void OnUpdate(float deltaTime)
    {
        // 攻撃ボタンを離した場合は攻撃を終了する
        if (!Owner.InputReader.IsAttackHeld)
        {
            Machine.ChangeState<PlayerIdlingState>();
        }
    }

    /// <summary>
    /// 一定間隔の物理更新処理を行います。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        float fixedDeltaTime = Time.fixedDeltaTime;

        // 攻撃ボタンが離されている場合は、
        // OnUpdate側で状態遷移するためここでは何もしない
        if (!Owner.InputReader.IsAttackHeld)
        {
            return;
        }

        // 一定周期で攻撃のヒット判定を実行する
        bool isHittingTarget = UpdateHitCycle(fixedDeltaTime);

        // 命中中と空振り中で速度の消費量を変更する
        float speedDecayPerSecond =
            isHittingTarget
                ? HIT_SPEED_DECAY_PER_SECOND
                : MISS_SPEED_DECAY_PER_SECOND;

        m_currentSlideSpeed -=
            speedDecayPerSecond * fixedDeltaTime;

        // 速度を使い切ったら攻撃を終了する
        if (m_currentSlideSpeed <= SPEED_EPSILON)
        {
            Machine.ChangeState<PlayerIdlingState>();
            return;
        }

        // 開始時の向きを維持したまま攻撃移動する
        // 障害物減速は無効化し、攻撃側で速度を管理する
        Owner.Motor.MoveAtFixedWorldDirection(
            m_slideDirection,
            m_currentSlideSpeed,
            ATTACK_ROTATION_SPEED,
            fixedDeltaTime,
            applyObstacleAvoidance: false);

        // UI上は攻撃継続リソースを表示する
        Owner.SetSpeedDisplayOverride(m_currentSlideSpeed);

        // 脚（AttackHitbox）が対象に食い込んでいる場合、
        // その貫通量ぶんPlayer本体を後方へ補正する
        if (Owner.AttackController.TryGetMaxPenetration(
                out Vector3 penetrationDirection,
                out float penetrationDistance))
        {
            Owner.Motor.ResolvePenetration(
                penetrationDirection,
                penetrationDistance);
        }

        // 接地中にジャンプ入力があれば攻撃を中断してジャンプへ遷移する
        if (Owner.Monitor.IsGrounded &&
            Owner.InputReader.HasJumpInput)
        {
            Machine.ChangeState<PlayerJumpingState>();
        }
    }

    /// <summary>
    /// 攻撃状態終了時の後処理を行います。
    /// </summary>
    protected override void OnExitState()
    {
        Owner.AnimationPresenter.StopAttackAnimation();
        Owner.AttackController.DisableAttackHitboxes();

        // 攻撃終了後は通常通り実際の物理速度を表示させる
        Owner.ClearSpeedDisplayOverride();
    }

    /// <summary>
    /// 一定周期で多段ヒット判定を実行します。
    /// </summary>
    /// <param name="fixedDeltaTime">物理更新の経過時間。</param>
    /// <returns>
    /// true：攻撃対象に命中している。
    /// false：攻撃対象に命中していない。
    /// </returns>
    private bool UpdateHitCycle(float fixedDeltaTime)
    {
        float hitInterval =
            Owner.AttackController.BaseHitIntervalSeconds;

        // TODO: Vブースト中はここでhitIntervalに倍率（例:1/1.5）をかけて
        //       ヒットレートを引き上げる（今回未実装）

        m_hitTimer += fixedDeltaTime;

        bool hasEvaluatedThisFrame = false;
        bool wasLastEvaluationHit = false;

        // 1周期で複数タイミング分経過した場合も取りこぼさないよう消化する
        while (m_hitTimer >= hitInterval)
        {
            m_hitTimer -= hitInterval;

            wasLastEvaluationHit =
                Owner.AttackController.ApplyContinuousHitTick();

            hasEvaluatedThisFrame = true;
        }

        // このフレームでヒット判定が発生していない場合は、
        // 直前の判定結果を使用する。
        if (!hasEvaluatedThisFrame)
        {
            return wasLastEvaluationHit;
        }

        return wasLastEvaluationHit;
    }
}
