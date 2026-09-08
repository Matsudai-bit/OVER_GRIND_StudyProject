using UnityEngine;

/// <summary>
/// プレイヤーの地上攻撃状態を管理します。
/// 攻撃ボタンを押している間、開始時点の体の向きを維持したまま
/// 直進し、一定周期で多段ヒット判定を行います。
/// 移動速度は攻撃継続時間として消費され、0になると攻撃は中断されます。
/// また、開始直後の猶予期間を過ぎてもヒット判定が空振りした場合は攻撃を中断します。
/// </summary>
public sealed class PlayerAttackingState
    : StateBase<PlayerStateMachineComponent>
{
    // 攻撃移動中の回転速度
    // 移動方向は開始時点の体の向きに固定するため、
    // ここでは回転させず0を指定する
    private const float ATTACK_ROTATION_SPEED = 0.0f;

    // 攻撃中に1秒あたり消費する移動速度
    // 開始時速度が攻撃の継続可能時間を決めるため、
    // この値が大きいほど短時間で速度を使い切り攻撃が終わる
    // TODO: 調整頻度が高い場合は既存のPlayerMovementParameterAsset等と
    //       同様にScriptableObjectへ切り出しInspector調整可能にする
    private const float SPEED_DECAY_PER_SECOND = 8.0f;

    // これ未満になったら速度を使い切ったとみなす閾値
    private const float SPEED_EPSILON = 0.01f;

    // 攻撃開始直後、空振り判定を猶予する時間（秒）
    // 物理のTrigger侵入判定がヒット周期に間に合わないケースを
    // 吸収するためのバッファ
    // TODO: 頻繁に調整するなら他パラメータ同様アセット化を検討
    private const float HIT_MISS_GRACE_DURATION = 0.7f;

    // 次のヒット判定までの経過時間
    private float m_hitTimer;

    // 攻撃状態に入ってからの経過時間
    private float m_elapsedSinceStart;

    // 攻撃中の現在の移動速度（開始時速度から消費され続ける）
    private float m_currentSlideSpeed;

    // 攻撃開始時に固定する移動方向（体が向いている方向）
    private Vector3 m_slideDirection;

    protected override void OnStartState()
    {
        // 攻撃開始時点の速度・向きを固定する
        // 速度は継続時間として、以後毎フレーム消費していく
        m_currentSlideSpeed = Owner.Motor.HorizontalSpeed;
        m_slideDirection = Owner.Motor.FacingDirection;

        m_hitTimer = 0.0f;
        m_elapsedSinceStart = 0.0f;

        Owner.AttackController.EnableContinuousAttackHitboxes();
        Owner.AnimationPresenter.PlayAttackAnimation();
    }

    protected override void OnUpdate(float deltaTime)
    {
        m_elapsedSinceStart += deltaTime;

        // 攻撃ボタンが離されたら攻撃を中断する
        if (!Owner.InputReader.IsAttackHeld)
        {
            Machine.ChangeState<PlayerIdlingState>();
            return;
        }

        // 速度を消費する。0になったら攻撃を強制中断する
        m_currentSlideSpeed -= SPEED_DECAY_PER_SECOND * deltaTime;

        if (m_currentSlideSpeed <= SPEED_EPSILON)
        {
            Machine.ChangeState<PlayerIdlingState>();
            return;
        }

        // 消費後の速度・開始時の向きを維持したまま移動し続ける
        // （スライディング攻撃のような、徐々に減速していく挙動）
        Owner.Motor.MoveAtFixedWorldDirection(
            m_slideDirection,
            m_currentSlideSpeed,
            ATTACK_ROTATION_SPEED,
            deltaTime);

        // ヒット判定を実行し、猶予期間を過ぎても空振りが確定した場合は攻撃を中断する
        if (!UpdateHitCycle(deltaTime))
        {
            Machine.ChangeState<PlayerIdlingState>();
            return;
        }

        // 接地中にジャンプ入力があれば攻撃を中断してジャンプへ遷移する
        if (Owner.Monitor.IsGrounded &&
            Owner.InputReader.HasJumpInput)
        {
            Machine.ChangeState<PlayerJumpingState>();
        }
    }

    protected override void OnExitState()
    {
        Owner.AnimationPresenter.StopAttackAnimation();
        Owner.AttackController.DisableAttackHitboxes();
    }

    /// <summary>
    /// 一定周期（基本<see cref="PlayerAttackController.BaseHitsPerSecond"/>回/秒）で
    /// 多段ヒット判定を実行します。
    /// 攻撃開始直後<see cref="HIT_MISS_GRACE_DURATION"/>の間は、
    /// 空振りしていても攻撃を継続扱いにします。
    /// </summary>
    /// <param name="deltaTime">経過時間。</param>
    /// <returns>
    /// true：命中が確認できている、または猶予期間中である（攻撃継続可）。
    /// false：猶予期間を過ぎたヒット判定が空振りだった（攻撃を中断すべき）。
    /// </returns>
    private bool UpdateHitCycle(float deltaTime)
    {
        float hitInterval =
            Owner.AttackController.BaseHitIntervalSeconds;

        // TODO: Vブースト中はここでhitIntervalに倍率（例:1/1.5）をかけて
        //       ヒットレートを引き上げる（今回未実装）

        m_hitTimer += deltaTime;

        bool hasEvaluatedThisFrame = false;
        bool wasLastEvaluationHit = false;

        // 1フレームで複数周期分経過した場合も取りこぼさないよう消化する
        while (m_hitTimer >= hitInterval)
        {
            m_hitTimer -= hitInterval;

            wasLastEvaluationHit =
                Owner.AttackController.ApplyContinuousHitTick();

            hasEvaluatedThisFrame = true;
        }

        // このフレームで判定が発生していなければ、
        // まだ空振りとは確定していないため継続扱いにする
        if (!hasEvaluatedThisFrame)
        {
            return true;
        }

        // 猶予期間中は、空振りしていても中断しない
        if (m_elapsedSinceStart < HIT_MISS_GRACE_DURATION)
        {
            return true;
        }

        return wasLastEvaluationHit;
    }
}