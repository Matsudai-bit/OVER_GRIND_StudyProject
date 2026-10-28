using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤー状況をAnimatorへ反映します。
/// 常時同期する基本状態と、Stateから明示的に設定する状態を仲介します。
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerAnimationPresenter : MonoBehaviour
{
    [SerializeField, Header("Parameter Asset")]
    private PlayerAnimationPresenterParameterAsset m_parameterAsset;

    [SerializeField, Header("Animator")]
    private Animator m_animator;

    [SerializeField, Min(0.0f)]
    private float m_moveSpeedDampTime = 0.1f;

    // 0除算を防ぐための最低移動速度
    private const float MIN_MAX_MOVE_SPEED = 0.0001f;

    // 移動チャージ方向
    private const int MOVE_CHARGE_DIRECTION_LEFT = -1;
    private const int MOVE_CHARGE_DIRECTION_NEUTRAL = 0;
    private const int MOVE_CHARGE_DIRECTION_RIGHT = 1;

    /// <summary>
    /// Animatorパラメータの定義です。
    /// </summary>
    private readonly struct AnimatorParameterDefinition
    {
        /// <summary>
        /// Animatorパラメータ定義を生成します。
        /// </summary>
        public AnimatorParameterDefinition(
            string name,
            AnimatorControllerParameterType type)
        {
            Name = name;
            Hash = Animator.StringToHash(name);
            Type = type;
        }

        /// <summary>
        /// Animator上のパラメータ名を取得します。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// AnimatorパラメータのHashを取得します。
        /// </summary>
        public int Hash { get; }

        /// <summary>
        /// Animatorパラメータの型を取得します。
        /// </summary>
        public AnimatorControllerParameterType Type { get; }
    }

    /// <summary>
    /// PlayerAnimationPresenterが使用するAnimatorパラメータをまとめます。
    /// 新しいパラメータを追加する場合は、ここへ定義を追加してください。
    /// </summary>
    private static class AnimatorParameters
    {
        // ------------------------------------------------------------
        // 常時同期パラメータ
        // ------------------------------------------------------------

        public static readonly AnimatorParameterDefinition MoveSpeed =
            new(
                "MoveSpeed",
                AnimatorControllerParameterType.Float);

        public static readonly AnimatorParameterDefinition IsGrounded =
            new(
                "IsGrounded",
                AnimatorControllerParameterType.Bool);

        // ------------------------------------------------------------
        // Stateから明示的に設定するパラメータ
        // ------------------------------------------------------------

        public static readonly AnimatorParameterDefinition JumpStart =
            new(
                "JumpStart",
                AnimatorControllerParameterType.Trigger);

        public static readonly AnimatorParameterDefinition IsJumping =
            new(
                "IsJumping",
                AnimatorControllerParameterType.Bool);

        public static readonly AnimatorParameterDefinition JumpType =
            new(
                "JumpType",
                AnimatorControllerParameterType.Int);

        public static readonly AnimatorParameterDefinition AttackStart =
            new(
                "AttackStart",
                AnimatorControllerParameterType.Trigger);

        public static readonly AnimatorParameterDefinition IsAttacking =
            new(
                "IsAttacking",
                AnimatorControllerParameterType.Bool);

        public static readonly AnimatorParameterDefinition WalkStart =
            new(
                "WalkStart",
                AnimatorControllerParameterType.Trigger);

        public static readonly AnimatorParameterDefinition IsWalking =
            new(
                "IsWalking",
                AnimatorControllerParameterType.Bool);

        public static readonly AnimatorParameterDefinition Hit =
            new(
                "Hit",
                AnimatorControllerParameterType.Trigger);

        public static readonly AnimatorParameterDefinition VRunning =
            new(
                "VRunning",
                AnimatorControllerParameterType.Bool);

        public static readonly AnimatorParameterDefinition StationaryChargeStart =
            new(
                "StationaryChargeStart",
                AnimatorControllerParameterType.Trigger);

        public static readonly AnimatorParameterDefinition IsStationaryCharging =
            new(
                "IsStationaryCharging",
                AnimatorControllerParameterType.Bool);

        public static readonly AnimatorParameterDefinition RailGrind =
            new(
                "RailGrind",
                AnimatorControllerParameterType.Bool);

        public static readonly AnimatorParameterDefinition MoveChargeStart =
            new(
                "MoveChargeStart",
                AnimatorControllerParameterType.Trigger);

        public static readonly AnimatorParameterDefinition IsMoveCharging =
            new(
                "IsMoveCharging",
                AnimatorControllerParameterType.Bool);

        public static readonly AnimatorParameterDefinition IsChargeAttacking=
            new(
                "IsChargeAttacking",
                AnimatorControllerParameterType.Bool);
        public static readonly AnimatorParameterDefinition ChargeAttackStart =
          new(
              "ChargeAttackStart",
              AnimatorControllerParameterType.Trigger);

        public static readonly AnimatorParameterDefinition MoveChargeDirection =
            new(
                "MoveChargeDirection",
                AnimatorControllerParameterType.Int);

        /// <summary>
        /// Presenterが使用する全Animatorパラメータです。
        /// </summary>
        public static readonly AnimatorParameterDefinition[] All =
        {
            MoveSpeed,
            IsGrounded,
            JumpStart,
            IsJumping,
            JumpType,
            AttackStart,
            IsAttacking,
            WalkStart,
            IsWalking,
            Hit,
            VRunning,
            StationaryChargeStart,
            IsStationaryCharging,
            RailGrind,
            MoveChargeStart,
            IsMoveCharging,
            IsChargeAttacking,
            ChargeAttackStart,
            MoveChargeDirection
        };
    }

    // Animatorに存在するパラメータ型をHash単位で保持する
    private readonly Dictionary<int, AnimatorControllerParameterType>
        m_animatorParameterTypes = new();

    // プレイヤー状況提供機能
    private PlayerStatusProvider m_statusProvider;

    // 初期化済みか
    private bool m_isInitialized;

    /// <summary>
    /// 初期化済みかどうかを取得します。
    /// </summary>
    public bool IsInitialized =>
        m_isInitialized;

    /// <summary>
    /// アニメーション表示機能を初期化します。
    /// </summary>
    /// <param name="statusProvider">プレイヤー状況提供機能。</param>
    public void Initialize(
        PlayerStatusProvider statusProvider)
    {
        if (m_parameterAsset != null)
        {
            m_moveSpeedDampTime =
                m_parameterAsset.MoveSpeedDampTime;
        }

        ResolveAnimatorReference();

        if (!ValidateReferences(
                statusProvider))
        {
            m_isInitialized = false;
            return;
        }

        UnsubscribeStatusEvents();

        m_statusProvider =
            statusProvider;

        CacheAnimatorParameters();

        m_isInitialized = true;

        SubscribeStatusEvents();

        RefreshAutomaticParameters(
            m_statusProvider.CurrentStatus,
            true);
    }

    // ============================================================
    // State制御パラメータ
    // ============================================================

  

    /// <summary>
    /// 指定したジャンプ種別でジャンプアニメーションを開始します。
    /// </summary>
    /// <param name="jumpType">ジャンプアニメーション種別。</param>
    public void PlayJumpAnimation(
        PlayerJumpAnimationType jumpType)
    {
        // Any State遷移後に継続状態を正しく参照できるよう、
        // Boolと種別を先に設定してからTriggerを発行する。
        SetBool(
            AnimatorParameters.IsJumping,
            true);

        SetInt(
            AnimatorParameters.JumpType,
            (int)jumpType);

        ResetTrigger(
            AnimatorParameters.JumpStart);

        SetTrigger(
            AnimatorParameters.JumpStart);
    }

    /// <summary>
    /// ジャンプアニメーションを停止します。
    /// </summary>
    public void StopJumpAnimation()
    {
        SetBool(
            AnimatorParameters.IsJumping,
            false);

        // State終了直前の未消費Triggerが次の状態で発火しないよう解除する。
        ResetTrigger(
            AnimatorParameters.JumpStart);
    }

    /// <summary>
    /// 攻撃アニメーションを開始します。
    /// </summary>
    public void PlayAttackAnimation()
    {
        // 継続状態を先に有効化してから開始要求を発行する。
        SetBool(
            AnimatorParameters.IsAttacking,
            true);

        ResetTrigger(
            AnimatorParameters.AttackStart);

        SetTrigger(
            AnimatorParameters.AttackStart);
    }

    /// <summary>
    /// 攻撃アニメーションを停止します。
    /// </summary>
    public void StopAttackAnimation()
    {
        SetBool(
            AnimatorParameters.IsAttacking,
            false);

        // 未消費の開始要求が残ったまま次Stateへ遷移することを防ぐ。
        ResetTrigger(
            AnimatorParameters.AttackStart);
    }

    /// <summary>
    /// 被弾アニメーションを要求します。
    /// </summary>
    public void PlayHitAnimation()
    {
        SetTrigger(
            AnimatorParameters.Hit);
    }

    /// <summary>
    /// 歩行アニメーションを開始します。
    /// </summary>
    public void PlayWalkAnimation()
    {
        // 継続状態を先に有効化してから開始要求を発行する。
        SetBool(
            AnimatorParameters.IsWalking,
            true);

        ResetTrigger(
            AnimatorParameters.WalkStart);

        SetTrigger(
            AnimatorParameters.WalkStart);
    }

    /// <summary>
    /// 歩行アニメーションを停止します。
    /// </summary>
    public void StopWalkAnimation()
    {
        SetBool(
            AnimatorParameters.IsWalking,
            false);

        // Any Stateからの開始要求が残らないように解除する。
        ResetTrigger(
            AnimatorParameters.WalkStart);
    }

    /// <summary>
    /// レールグラインドアニメーションを開始します。
    /// </summary>
    public void PlayRailGrindAnimation()
    {
        SetBool(
            AnimatorParameters.RailGrind,
            true);
    }

    /// <summary>
    /// レールグラインドアニメーションを停止します。
    /// </summary>
    public void StopRailGrindAnimation()
    {
        SetBool(
            AnimatorParameters.RailGrind,
            false);
    }

    /// <summary>
    /// Vブースト走行アニメーションを開始します。
    /// </summary>
    public void PlayVBoostRunningAnimation()
    {
        SetBool(
            AnimatorParameters.VRunning,
            true);
    }

    /// <summary>
    /// Vブースト走行アニメーションを停止します。
    /// </summary>
    public void StopVBoostRunningAnimation()
    {
        SetBool(
            AnimatorParameters.VRunning,
            false);
    }

    /// <summary>
    /// 停止Vブーストチャージアニメーションを開始します。
    /// </summary>
    public void PlayStationaryBoostChargingAnimation()
    {
        // 継続状態を先に有効化してから開始要求を発行する。
        SetBool(
            AnimatorParameters.IsStationaryCharging,
            true);

        ResetTrigger(
            AnimatorParameters.StationaryChargeStart);

        SetTrigger(
            AnimatorParameters.StationaryChargeStart);
    }

    /// <summary>
    /// 停止Vブーストチャージアニメーションを停止します。
    /// </summary>
    public void StopStationaryBoostChargingAnimation()
    {
        SetBool(
            AnimatorParameters.IsStationaryCharging,
            false);

        ResetTrigger(
            AnimatorParameters.StationaryChargeStart);
    }

    /// <summary>
    /// 移動チャージアニメーション状態を開始します。
    /// </summary>
    public void PlayChargeMovingAnimation()
    {
        if (m_statusProvider != null)
        {
            m_statusProvider.SetStateCondition(
                PlayerConditionFlags.CHARGE_MOVING,
                true);
        }

        // 継続状態を先に有効化してから開始要求を発行する。
        SetBool(
            AnimatorParameters.IsMoveCharging,
            true);

        ResetTrigger(
            AnimatorParameters.MoveChargeStart);

        SetTrigger(
            AnimatorParameters.MoveChargeStart);
    }

    /// <summary>
    /// 移動チャージアニメーション状態を停止します。
    /// </summary>
    public void StopChargeMovingAnimation()
    {
        if (m_statusProvider != null)
        {
            m_statusProvider.SetStateCondition(
                PlayerConditionFlags.CHARGE_MOVING,
                false);
        }

        SetBool(
            AnimatorParameters.IsMoveCharging,
            false);

        ResetTrigger(
            AnimatorParameters.MoveChargeStart);

        ResetMoveChargeDirection();
    }

    /// <summary>
    /// 移動チャージ中の左右方向をAnimatorへ設定します。
    /// -1：左、0：正面・入力なし、1：右。
    /// </summary>
    /// <param name="direction">移動チャージ方向。</param>
    public void SetMoveChargeDirection(
        int direction)
    {
        int clampedDirection =
            Mathf.Clamp(
                direction,
                MOVE_CHARGE_DIRECTION_LEFT,
                MOVE_CHARGE_DIRECTION_RIGHT);

        SetInt(
            AnimatorParameters.MoveChargeDirection,
            clampedDirection);
    }

    /// <summary>
    /// 移動チャージ方向をニュートラルへ戻します。
    /// </summary>
    public void ResetMoveChargeDirection()
    {
        SetMoveChargeDirection(
            MOVE_CHARGE_DIRECTION_NEUTRAL);
    }

    /// <summary>
    /// チャージ攻撃アニメーション状態を開始します。
    /// </summary>
    public void PlayChargeAttackingAnimation()
    {
        if (m_statusProvider != null)
        {
            m_statusProvider.SetStateCondition(
                PlayerConditionFlags.CHARGE_ATTACKING,
                true);
        }

        SetBool(
            AnimatorParameters.IsChargeAttacking,
            true);
        SetTrigger(
       AnimatorParameters.ChargeAttackStart);
    }

    /// <summary>
    /// チャージ攻撃アニメーション状態を停止します。
    /// </summary>
    public void StopChargeAttackingAnimation()
    {
        if (m_statusProvider != null)
        {
            m_statusProvider.SetStateCondition(
                PlayerConditionFlags.CHARGE_ATTACKING,
                false);
        }

        SetBool(
            AnimatorParameters.IsChargeAttacking,
            false);
    }

    /// <summary>
    /// アクション用Triggerをリセットします。
    /// </summary>
    public void ResetActionTriggers()
    {
        ResetTrigger(
            AnimatorParameters.Hit);

        ResetTrigger(
            AnimatorParameters.JumpStart);

        ResetTrigger(
            AnimatorParameters.AttackStart);

        ResetTrigger(
            AnimatorParameters.WalkStart);

        ResetTrigger(
            AnimatorParameters.StationaryChargeStart);

        ResetTrigger(
            AnimatorParameters.MoveChargeStart);
    }

    // ============================================================
    // 常時同期パラメータ
    // ============================================================

    /// <summary>
    /// PlayerStatusProvider更新時に基本Animatorパラメータを同期します。
    /// </summary>
    /// <param name="status">最新のプレイヤー状況。</param>
    private void HandleStatusUpdated(
        PlayerStatusSnapshot status)
    {
        RefreshAutomaticParameters(
            status,
            false);
    }

    /// <summary>
    /// Stateに依存せず常に同期するAnimatorパラメータを反映します。
    /// </summary>
    /// <param name="status">反映するプレイヤー状況。</param>
    /// <param name="immediate">補間を使用せず即時反映する場合はtrue。</param>
    private void RefreshAutomaticParameters(
        PlayerStatusSnapshot status,
        bool immediate)
    {
        if (!CanControlAnimator())
        {
            return;
        }

        float maxMoveSpeed =
            Mathf.Max(
                status.MaxMoveSpeed,
                MIN_MAX_MOVE_SPEED);

        float normalizedMoveSpeed =
            Mathf.Clamp01(
                status.HorizontalSpeed /
                maxMoveSpeed);

        if (immediate)
        {
            SetFloat(
                AnimatorParameters.MoveSpeed,
                normalizedMoveSpeed);
        }
        else
        {
            SetFloat(
                AnimatorParameters.MoveSpeed,
                normalizedMoveSpeed,
                m_moveSpeedDampTime,
                Time.deltaTime);
        }

        SetBool(
            AnimatorParameters.IsGrounded,
            status.HasCondition(
                PlayerConditionFlags.GROUNDED));
    }

    // ============================================================
    // Animatorパラメータ共通操作
    // ============================================================

    /// <summary>
    /// Animatorパラメータ情報をキャッシュします。
    /// </summary>
    private void CacheAnimatorParameters()
    {
        m_animatorParameterTypes.Clear();

        foreach (AnimatorControllerParameter parameter
                 in m_animator.parameters)
        {
            m_animatorParameterTypes[
                parameter.nameHash] =
                parameter.type;
        }

        LogMissingParameters();
    }

    /// <summary>
    /// 指定したAnimatorパラメータが正しい型で存在するか確認します。
    /// </summary>
    private bool HasAnimatorParameter(
        AnimatorParameterDefinition definition)
    {
        return m_animatorParameterTypes.TryGetValue(
                   definition.Hash,
                   out AnimatorControllerParameterType type) &&
               type == definition.Type;
    }

    /// <summary>
    /// Boolパラメータを安全に設定します。
    /// </summary>
    private void SetBool(
        AnimatorParameterDefinition definition,
        bool value)
    {
        if (!CanControlAnimator() ||
            !HasAnimatorParameter(definition))
        {
            return;
        }

        m_animator.SetBool(
            definition.Hash,
            value);
    }

    /// <summary>
    /// Floatパラメータを補間なしで安全に設定します。
    /// </summary>
    private void SetFloat(
        AnimatorParameterDefinition definition,
        float value)
    {
        if (!CanControlAnimator() ||
            !HasAnimatorParameter(definition))
        {
            return;
        }

        m_animator.SetFloat(
            definition.Hash,
            value);
    }

    /// <summary>
    /// Floatパラメータを補間付きで安全に設定します。
    /// </summary>
    private void SetFloat(
        AnimatorParameterDefinition definition,
        float value,
        float dampTime,
        float deltaTime)
    {
        if (!CanControlAnimator() ||
            !HasAnimatorParameter(definition))
        {
            return;
        }

        m_animator.SetFloat(
            definition.Hash,
            value,
            Mathf.Max(
                dampTime,
                0.0f),
            Mathf.Max(
                deltaTime,
                0.0f));
    }

    /// <summary>
    /// Intパラメータを安全に設定します。
    /// </summary>
    private void SetInt(
        AnimatorParameterDefinition definition,
        int value)
    {
        if (!CanControlAnimator() ||
            !HasAnimatorParameter(definition))
        {
            return;
        }

        m_animator.SetInteger(
            definition.Hash,
            value);
    }

    /// <summary>
    /// Triggerパラメータを安全に設定します。
    /// </summary>
    private void SetTrigger(
        AnimatorParameterDefinition definition)
    {
        if (!CanControlAnimator() ||
            !HasAnimatorParameter(definition))
        {
            return;
        }

        m_animator.SetTrigger(
            definition.Hash);
    }

    /// <summary>
    /// Triggerパラメータを安全にリセットします。
    /// </summary>
    private void ResetTrigger(
        AnimatorParameterDefinition definition)
    {
        if (!CanControlAnimator() ||
            !HasAnimatorParameter(definition))
        {
            return;
        }

        m_animator.ResetTrigger(
            definition.Hash);
    }

    /// <summary>
    /// Presenterが使用するAnimatorパラメータの不足を警告します。
    /// </summary>
    private void LogMissingParameters()
    {
        foreach (AnimatorParameterDefinition definition
                 in AnimatorParameters.All)
        {
            if (HasAnimatorParameter(
                    definition))
            {
                continue;
            }

            Debug.LogWarning(
                $"[{nameof(PlayerAnimationPresenter)}] " +
                $"Animatorに{definition.Type}型の" +
                $"「{definition.Name}」パラメータがありません。",
                this);
        }
    }

    // ============================================================
    // PlayerStatusProvider購読
    // ============================================================

    /// <summary>
    /// プレイヤー状況更新イベントを購読します。
    /// </summary>
    private void SubscribeStatusEvents()
    {
        if (m_statusProvider == null)
        {
            return;
        }

        m_statusProvider.StatusUpdated -=
            HandleStatusUpdated;

        m_statusProvider.StatusUpdated +=
            HandleStatusUpdated;
    }

    /// <summary>
    /// プレイヤー状況更新イベントの購読を解除します。
    /// </summary>
    private void UnsubscribeStatusEvents()
    {
        if (m_statusProvider == null)
        {
            return;
        }

        m_statusProvider.StatusUpdated -=
            HandleStatusUpdated;
    }

    /// <summary>
    /// 再有効化時にプレイヤー状況更新イベントを購読します。
    /// </summary>
    private void OnEnable()
    {
        if (!m_isInitialized)
        {
            return;
        }

        SubscribeStatusEvents();
    }

    /// <summary>
    /// 無効化時にプレイヤー状況更新イベントの購読を解除します。
    /// </summary>
    private void OnDisable()
    {
        UnsubscribeStatusEvents();
    }

    // ============================================================
    // 初期化
    // ============================================================

    /// <summary>
    /// Animator参照を自動取得します。
    /// </summary>
    private void ResolveAnimatorReference()
    {
        if (m_animator != null)
        {
            return;
        }

        m_animator =
            GetComponentInChildren<Animator>();
    }

    /// <summary>
    /// 初期化に必要な参照を確認します。
    /// </summary>
    private bool ValidateReferences(
        PlayerStatusProvider statusProvider)
    {
        bool isValid = true;

        if (m_animator == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerAnimationPresenter)}] " +
                "Animatorが見つかりません。",
                this);

            isValid = false;
        }
        else if (m_animator.runtimeAnimatorController == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerAnimationPresenter)}] " +
                "Animator Controllerが設定されていません。",
                this);

            isValid = false;
        }

        if (statusProvider == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerAnimationPresenter)}] " +
                $"{nameof(PlayerStatusProvider)}が指定されていません。",
                this);

            isValid = false;
        }
        else if (!statusProvider.IsInitialized)
        {
            Debug.LogError(
                $"[{nameof(PlayerAnimationPresenter)}] " +
                $"{nameof(PlayerStatusProvider)}が初期化されていません。",
                this);

            isValid = false;
        }

        return isValid;
    }

    /// <summary>
    /// Animatorを操作可能か確認します。
    /// </summary>
    private bool CanControlAnimator()
    {
        return m_isInitialized &&
               m_animator != null &&
               m_animator.runtimeAnimatorController != null;
    }

    /// <summary>
    /// Inspector設定時にAnimator参照を補完します。
    /// </summary>
    private void Reset()
    {
        ResolveAnimatorReference();
    }

    /// <summary>
    /// Inspector編集時にAnimator参照を補完します。
    /// </summary>
    private void OnValidate()
    {
        ResolveAnimatorReference();
    }
}
