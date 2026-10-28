using System;
using UnityEngine;

/// <summary>
/// プレイヤー各コンポーネントの情報を集約し、
/// Animation・Effect・Sound・Camera演出向けに読み取り専用の現在状況を提供します。
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerStatusProvider : MonoBehaviour
{
    // 移動中として扱う最低水平速度
    private const float MOVING_SPEED_THRESHOLD = 0.05f;

    // 上昇・下降として扱う最低垂直速度
    private const float VERTICAL_SPEED_THRESHOLD = 0.05f;

    // プレイヤー監視機能
    private PlayerMonitor m_monitor;

    // プレイヤー移動機能
    private PlayerMotor m_motor;

    // プレイヤーステートマシン
    private PlayerStateMachineComponent m_stateMachine;

    // 現在状況
    private PlayerStatusSnapshot m_currentStatus;

    // 直前状況
    private PlayerStatusSnapshot m_previousStatus;

    // 有効なスナップショットを保持しているか
    private bool m_hasStatus;

    // Stateから明示的にON/OFFされる状態
    private PlayerConditionFlags m_stateConditions;

    // 初期化済みか
    private bool m_isInitialized;

    /// <summary>
    /// プレイヤー状況を更新した際に通知します。
    /// </summary>
    public event Action<PlayerStatusSnapshot> StatusUpdated;

    /// <summary>
    /// 状態フラグが変化した際に通知します。
    /// </summary>
    public event Action<PlayerConditionFlags, bool> ConditionChanged;

    /// <summary>
    /// プレイヤーが着地した瞬間に通知します。
    /// </summary>
    public event Action<PlayerStatusSnapshot> Landed;

    /// <summary>
    /// プレイヤーが地面から離れた瞬間に通知します。
    /// </summary>
    public event Action<PlayerStatusSnapshot> LeftGround;

    /// <summary>
    /// 現在のプレイヤー状況を取得します。
    /// </summary>
    public PlayerStatusSnapshot CurrentStatus =>
        m_currentStatus;

    /// <summary>
    /// 1回前に取得したプレイヤー状況を取得します。
    /// 着地直前の落下速度などを参照する用途に使用できます。
    /// </summary>
    public PlayerStatusSnapshot PreviousStatus =>
        m_previousStatus;

    /// <summary>
    /// 初期化済みかどうかを取得します。
    /// </summary>
    public bool IsInitialized =>
        m_isInitialized;

    /// <summary>
    /// プレイヤー状況提供機能を初期化します。
    /// </summary>
    /// <param name="monitor">プレイヤー監視機能。</param>
    /// <param name="motor">プレイヤー移動機能。</param>
    /// <param name="stateMachine">プレイヤーステートマシン。</param>
    public void Initialize(
        PlayerMonitor monitor,
        PlayerMotor motor,
        PlayerStateMachineComponent stateMachine)
    {
        if (monitor == null ||
            motor == null ||
            stateMachine == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerStatusProvider)}] " +
                "初期化に必要な参照が不足しています。",
                this);

            m_isInitialized = false;
            return;
        }

        m_monitor = monitor;
        m_motor = motor;
        m_stateMachine = stateMachine;
        m_stateConditions = PlayerConditionFlags.NONE;

        m_isInitialized = true;

        // 初期化時は変化イベントを発生させず、
        // 現在値と直前値を同じ状態で初期化する。
        m_currentStatus =
            CreateSnapshot();

        m_previousStatus =
            m_currentStatus;

        m_hasStatus = true;
    }

    /// <summary>
    /// 描画・演出が参照する直前のタイミングで状況を更新します。
    /// </summary>
    private void LateUpdate()
    {
        RefreshStatus();
    }

    /// <summary>
    /// 現在状況を再取得し、変化通知を発行します。
    /// </summary>
    public void RefreshStatus()
    {
        if (!m_isInitialized)
        {
            return;
        }

        PlayerStatusSnapshot nextStatus =
            CreateSnapshot();

        if (!m_hasStatus)
        {
            m_currentStatus = nextStatus;
            m_previousStatus = nextStatus;
            m_hasStatus = true;

            StatusUpdated?.Invoke(
                m_currentStatus);

            return;
        }

        m_previousStatus =
            m_currentStatus;

        m_currentStatus =
            nextStatus;

        NotifyConditionChanges(
            m_previousStatus,
            m_currentStatus);

        StatusUpdated?.Invoke(
            m_currentStatus);
    }

    /// <summary>
    /// 現在の各コンポーネントからプレイヤー状況を生成します。
    /// </summary>
    /// <returns>現在のプレイヤー状況。</returns>
    private PlayerStatusSnapshot CreateSnapshot()
    {
        Vector3 velocity =
            m_monitor.CurrentVelocity;

        Vector3 horizontalVelocity =
            m_monitor.HorizontalVelocity;

        bool isGrounded =
            m_monitor.IsGrounded &&
            m_monitor.HasGroundContact;

        PlayerConditionFlags conditions =
            BuildConditionFlags(
                isGrounded,
                horizontalVelocity.magnitude,
                velocity.y);

        Vector3 groundNormal =
            m_monitor.HasGroundSurface
                ? m_monitor.GroundNormal
                : Vector3.up;

        return new PlayerStatusSnapshot(
            conditions,
            m_motor.Position,
            velocity,
            horizontalVelocity,
            m_motor.MaxMoveSpeed,
            groundNormal,
            m_monitor.GroundSlopeAngle,
            m_monitor.GroundCollider);
    }

    /// <summary>
    /// 物理状態とStateMachineから現在の状態フラグを構築します。
    /// </summary>
    private PlayerConditionFlags BuildConditionFlags(
        bool isGrounded,
        float horizontalSpeed,
        float verticalSpeed)
    {
        PlayerConditionFlags conditions =
            PlayerConditionFlags.NONE;

        if (isGrounded)
        {
            conditions |=
                PlayerConditionFlags.GROUNDED;
        }
        else
        {
            conditions |=
                PlayerConditionFlags.AIRBORNE;

            if (verticalSpeed >
                VERTICAL_SPEED_THRESHOLD)
            {
                conditions |=
                    PlayerConditionFlags.RISING;
            }
            else if (verticalSpeed <
                     -VERTICAL_SPEED_THRESHOLD)
            {
                conditions |=
                    PlayerConditionFlags.FALLING;
            }
        }

        if (horizontalSpeed >
            MOVING_SPEED_THRESHOLD)
        {
            conditions |=
                PlayerConditionFlags.MOVING;
        }

        AddStateConditionFlags(
            ref conditions);

        // Stateが明示的にON/OFFした状態を合成する。
        conditions |=
            m_stateConditions;

        return conditions;
    }

    /// <summary>
    /// 現在のStateMachine状態をフラグへ追加します。
    /// </summary>
    private void AddStateConditionFlags(
        ref PlayerConditionFlags conditions)
    {
        if (m_stateMachine == null ||
            !m_stateMachine.IsInitialized)
        {
            return;
        }

        if (m_stateMachine.IsCurrentState<
                PlayerIdlingState>())
        {
            conditions |=
                PlayerConditionFlags.IDLING;
        }

        if (m_stateMachine.IsCurrentState<
                PlayerWalkingState>())
        {
            conditions |=
                PlayerConditionFlags.WALKING;
        }

        if (m_stateMachine.IsCurrentState<
                PlayerJumpingState>())
        {
            conditions |=
                PlayerConditionFlags.JUMPING;
        }

        if (m_stateMachine.IsCurrentState<
                PlayerAttackingState>())
        {
            conditions |=
                PlayerConditionFlags.ATTACKING;
        }

        if (m_stateMachine.IsCurrentState<
                PlayerBoostChargingState>())
        {
            conditions |=
                PlayerConditionFlags.BOOST_CHARGING;
        }

        if (m_stateMachine.IsCurrentState<
                PlayerVRunningState>())
        {
            conditions |=
                PlayerConditionFlags.BOOSTING;
        }

        if (m_stateMachine.IsCurrentState<
                PlayerGrindingState>())
        {
            conditions |=
                PlayerConditionFlags.GRINDING;
        }

        if (m_stateMachine.IsCurrentState<
                PlayerRailJumpingState>())
        {
            conditions |=
                PlayerConditionFlags.RAIL_JUMPING;
        }

        if (m_stateMachine.IsCurrentState<
                PlayerRailExitingState>())
        {
            conditions |=
                PlayerConditionFlags.RAIL_EXITING;
        }

        if (m_stateMachine.IsCurrentState<
                PlayerHitState>())
        {
            conditions |=
                PlayerConditionFlags.HIT_REACTION;
        }
    }

    /// <summary>
    /// Stateが管理する状態フラグをON/OFFします。
    /// StateのOnStartStateでtrue、OnExitStateでfalseを設定してください。
    /// </summary>
    /// <param name="condition">変更する状態フラグ。</param>
    /// <param name="isEnabled">true：ON。false：OFF。</param>
    public void SetStateCondition(
        PlayerConditionFlags condition,
        bool isEnabled)
    {
        if (condition == PlayerConditionFlags.NONE)
        {
            return;
        }

        if (isEnabled)
        {
            m_stateConditions |=
                condition;
        }
        else
        {
            m_stateConditions &=
                ~condition;
        }

        // State切り替え直後からAnimation・Effect側が
        // 最新状態を取得できるよう、その場でSnapshotへ反映する。
        RefreshStatus();
    }

    /// <summary>
    /// Stateが明示設定した状態フラグをすべて解除します。
    /// </summary>
    public void ClearStateConditions()
    {
        if (m_stateConditions ==
            PlayerConditionFlags.NONE)
        {
            return;
        }

        m_stateConditions =
            PlayerConditionFlags.NONE;

        RefreshStatus();
    }

    /// <summary>
    /// 前回から変化した状態フラグを通知します。
    /// </summary>
    private void NotifyConditionChanges(
        PlayerStatusSnapshot previousStatus,
        PlayerStatusSnapshot currentStatus)
    {
        PlayerConditionFlags changedConditions =
            previousStatus.Conditions ^
            currentStatus.Conditions;

        if (changedConditions ==
            PlayerConditionFlags.NONE)
        {
            return;
        }

        for (int bitIndex = 0;
             bitIndex < 31;
             bitIndex++)
        {
            PlayerConditionFlags condition =
                (PlayerConditionFlags)
                (1 << bitIndex);

            if ((changedConditions & condition) == 0)
            {
                continue;
            }

            bool isEnabled =
                currentStatus.HasCondition(
                    condition);

            ConditionChanged?.Invoke(
                condition,
                isEnabled);
        }

        bool wasGrounded =
            previousStatus.HasCondition(
                PlayerConditionFlags.GROUNDED);

        bool isGrounded =
            currentStatus.HasCondition(
                PlayerConditionFlags.GROUNDED);

        if (!wasGrounded &&
            isGrounded)
        {
            Landed?.Invoke(
                currentStatus);
        }
        else if (wasGrounded &&
                 !isGrounded)
        {
            LeftGround?.Invoke(
                currentStatus);
        }
    }
}
