using UnityEngine;

/// <summary>
/// プレイヤーの状態をAnimatorへ反映します。
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerAnimationPresenter : MonoBehaviour
{
    // ゼロ除算を防ぐための最小速度
    private const float MIN_MAX_MOVE_SPEED = 0.0001f;

    // Animatorパラメーター名
    private const string MOVE_SPEED_PARAMETER_NAME = "MoveSpeed";
    private const string IS_GROUNDED_PARAMETER_NAME = "IsGrounded";
    private const string JUMP_PARAMETER_NAME = "Jump";
    private const string ATTACK_PARAMETER_NAME = "Attack";
    private const string WALK_PARAMETER_NAME = "Walk";
    private const string HIT_PARAMETER_NAME = "Hit";

    // Animatorパラメーターのハッシュ値
    private static readonly int MOVE_SPEED_HASH =
        Animator.StringToHash(MOVE_SPEED_PARAMETER_NAME);

    private static readonly int IS_GROUNDED_HASH =
        Animator.StringToHash(IS_GROUNDED_PARAMETER_NAME);

    private static readonly int JUMP_HASH =
        Animator.StringToHash(JUMP_PARAMETER_NAME);

    private static readonly int ATTACK_HASH =
        Animator.StringToHash(ATTACK_PARAMETER_NAME);

    private static readonly int HIT_HASH =
        Animator.StringToHash(HIT_PARAMETER_NAME);    

    private static readonly int WALK_HASH =
        Animator.StringToHash(WALK_PARAMETER_NAME);

    // プレイヤーのAnimator
    [SerializeField]
    private Animator m_animator;

    // 移動速度パラメーターの補間時間
    [SerializeField, Min(0.0f)]
    private float m_moveSpeedDampTime = 0.1f;

    // プレイヤー監視機能
    private PlayerMonitor m_monitor;

    // プレイヤー移動機能
    private PlayerMotor m_motor;

    // Animatorパラメーターの存在状態
    private bool m_hasMoveSpeedParameter;
    private bool m_hasGroundedParameter;
    private bool m_hasJumpParameter;
    private bool m_hasAttackParameter;
    private bool m_hasHitParameter;
    private bool m_hasWalkParameter;

    // 初期化されているか
    private bool m_isInitialized;

    /// <summary>
    /// 初期化されているかを取得します。
    /// </summary>
    /// <returns>
    /// true：初期化されています。
    /// false：初期化されていません。
    /// </returns>
    public bool IsInitialized => m_isInitialized;

    /// <summary>
    /// アニメーション表示機能を初期化します。
    /// </summary>
    /// <param name="monitor">プレイヤー監視機能。</param>
    /// <param name="motor">プレイヤー移動機能。</param>
    public void Initialize(
        PlayerMonitor monitor,
        PlayerMotor motor)
    {
        ResolveAnimatorReference();

        if (!ValidateReferences(monitor, motor))
        {
            m_isInitialized = false;
            return;
        }

        m_monitor = monitor;
        m_motor = motor;

        CacheAnimatorParameters();

        m_isInitialized = true;

        // 初期状態を即座にAnimatorへ反映
        RefreshAnimationImmediately();
    }

    /// <summary>
    /// ジャンプアニメーションを要求します。
    /// </summary>
    public void PlayJumpAnimation()
    {
        if (!CanControlAnimator() || !m_hasJumpParameter)
        {
            return;
        }

        m_animator.SetBool(JUMP_HASH, true);
    }
    /// <summary>
    /// ジャンプアニメーションを要求します。
    /// </summary>
    public void StopJumpAnimation()
    {
        if (!CanControlAnimator() || !m_hasJumpParameter)
        {
            return;
        }

        m_animator.SetBool(JUMP_HASH, false);
    }

    /// <summary>
    /// 攻撃アニメーションを要求します。
    /// </summary>
    public void PlayAttackAnimation()
    {
        if (!CanControlAnimator() || !m_hasAttackParameter)
        {
            return;
        }

        m_animator.SetBool(ATTACK_HASH,true);
    }
    /// <summary>
    /// 攻撃アニメーション停止を要求します。
    /// </summary>
    public void StopAttackAnimation()
    {
        if (!CanControlAnimator() || !m_hasAttackParameter)
        {
            return;
        }

        m_animator.SetBool(ATTACK_HASH, false);
    }

    /// <summary>
    /// 被弾アニメーションを要求します。
    /// </summary>
    public void PlayHitAnimation()
    {
        if (!CanControlAnimator() || !m_hasHitParameter)
        {
            return;
        }

        m_animator.SetTrigger(HIT_HASH);
    }

    /// <summary>
    /// 歩くアニメーションを要求します。
    /// </summary>
    public void PlayWalkAnimation()
    {
        if (!CanControlAnimator() || !m_hasWalkParameter)
        {
            return;
        }

        m_animator.SetBool(WALK_HASH, true);
    }

    /// <summary>
    /// 歩くアニメーション停止を要求します。
    /// </summary>
    public void StopWalkAnimation()
    {
        if (!CanControlAnimator() || !m_hasWalkParameter)
        {
            return;
        }

        m_animator.SetBool(WALK_HASH, false);
    }


    /// <summary>
    /// アクション用Triggerをリセットします。
    /// </summary>
    public void ResetActionTriggers()
    {
        if (!CanControlAnimator())
        {
            return;
        }

        if (m_hasAttackParameter)
        {
            m_animator.ResetTrigger(ATTACK_HASH);
        }

        if (m_hasHitParameter)
        {
            m_animator.ResetTrigger(HIT_HASH);
        }
    }

    /// <summary>
    /// 移動状態をAnimatorへ反映します。
    /// </summary>
    private void LateUpdate()
    {
        if (!CanControlAnimator())
        {
            return;
        }

        UpdateLocomotionParameters();
    }

    /// <summary>
    /// 移動と接地状態をAnimatorへ反映します。
    /// </summary>
    private void UpdateLocomotionParameters()
    {
        if (m_hasMoveSpeedParameter)
        {
            float normalizedMoveSpeed =
                CalculateNormalizedMoveSpeed();

            m_animator.SetFloat(
                MOVE_SPEED_HASH,
                normalizedMoveSpeed,
                m_moveSpeedDampTime,
                Time.deltaTime);
        }

        if (m_hasGroundedParameter)
        {
            m_animator.SetBool(
                IS_GROUNDED_HASH,
                m_monitor.IsGrounded);
        }
    }

    /// <summary>
    /// 現在状態を補間なしでAnimatorへ反映します。
    /// </summary>
    private void RefreshAnimationImmediately()
    {
        if (!CanControlAnimator())
        {
            return;
        }

        if (m_hasMoveSpeedParameter)
        {
            m_animator.SetFloat(
                MOVE_SPEED_HASH,
                CalculateNormalizedMoveSpeed());
        }

        if (m_hasGroundedParameter)
        {
            m_animator.SetBool(
                IS_GROUNDED_HASH,
                m_monitor.IsGrounded);
        }
    }

    /// <summary>
    /// 現在の移動速度を0から1へ正規化します。
    /// </summary>
    /// <returns>正規化された移動速度。</returns>
    private float CalculateNormalizedMoveSpeed()
    {
        float maxMoveSpeed = Mathf.Max(
            m_motor.MaxMoveSpeed,
            MIN_MAX_MOVE_SPEED);

        return Mathf.Clamp01(
            m_monitor.HorizontalSpeed / maxMoveSpeed);
    }

    /// <summary>
    /// Animatorパラメーターの存在状態を保存します。
    /// </summary>
    private void CacheAnimatorParameters()
    {
        m_hasMoveSpeedParameter = HasAnimatorParameter(
            MOVE_SPEED_HASH,
            AnimatorControllerParameterType.Float);

        m_hasGroundedParameter = HasAnimatorParameter(
            IS_GROUNDED_HASH,
            AnimatorControllerParameterType.Bool);

        m_hasJumpParameter = HasAnimatorParameter(
            JUMP_HASH,
            AnimatorControllerParameterType.Bool);

        m_hasAttackParameter = HasAnimatorParameter(
            ATTACK_HASH,
            AnimatorControllerParameterType.Bool);

        m_hasHitParameter = HasAnimatorParameter(
            HIT_HASH,
            AnimatorControllerParameterType.Trigger);

        m_hasWalkParameter = HasAnimatorParameter(
            WALK_HASH,
            AnimatorControllerParameterType.Bool);

        LogMissingParameters();
    }

    /// <summary>
    /// 指定したAnimatorパラメーターが存在するか確認します。
    /// </summary>
    /// <param name="parameterHash">パラメーターのハッシュ値。</param>
    /// <param name="parameterType">パラメーターの種類。</param>
    /// <returns>
    /// true：指定したパラメーターが存在します。
    /// false：指定したパラメーターが存在しません。
    /// </returns>
    private bool HasAnimatorParameter(
        int parameterHash,
        AnimatorControllerParameterType parameterType)
    {
        foreach (AnimatorControllerParameter parameter
                 in m_animator.parameters)
        {
            if (parameter.nameHash == parameterHash &&
                parameter.type == parameterType)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 未設定のAnimatorパラメーターを警告します。
    /// </summary>
    private void LogMissingParameters()
    {
        LogMissingParameter(
            m_hasMoveSpeedParameter,
            MOVE_SPEED_PARAMETER_NAME,
            AnimatorControllerParameterType.Float);

        //LogMissingParameter(
        //    m_hasGroundedParameter,
        //    IS_GROUNDED_PARAMETER_NAME,
        //    AnimatorControllerParameterType.Bool);

        LogMissingParameter(
            m_hasJumpParameter,
            JUMP_PARAMETER_NAME,
            AnimatorControllerParameterType.Bool);

        LogMissingParameter(
            m_hasAttackParameter,
            ATTACK_PARAMETER_NAME,
            AnimatorControllerParameterType.Trigger);

        //LogMissingParameter(
        //    m_hasHitParameter,
        //    HIT_PARAMETER_NAME,
        //    AnimatorControllerParameterType.Trigger);
    }

    /// <summary>
    /// Animatorパラメーター不足時に警告を出力します。
    /// </summary>
    /// <param name="hasParameter">パラメーターが存在するか。</param>
    /// <param name="parameterName">パラメーター名。</param>
    /// <param name="parameterType">パラメーターの種類。</param>
    private void LogMissingParameter(
        bool hasParameter,
        string parameterName,
        AnimatorControllerParameterType parameterType)
    {
        if (hasParameter)
        {
            return;
        }

        Debug.LogWarning(
            $"[{nameof(PlayerAnimationPresenter)}] " +
            $"Animatorに{parameterType}型の" +
            $"「{parameterName}」パラメーターがありません。",
            this);
    }

    /// <summary>
    /// Animator参照を取得します。
    /// </summary>
    private void ResolveAnimatorReference()
    {
        if (m_animator != null)
        {
            return;
        }

        m_animator = GetComponentInChildren<Animator>();
    }

    /// <summary>
    /// 初期化に必要な参照を確認します。
    /// </summary>
    /// <param name="monitor">プレイヤー監視機能。</param>
    /// <param name="motor">プレイヤー移動機能。</param>
    /// <returns>
    /// true：必要な参照があります。
    /// false：必要な参照が不足しています。
    /// </returns>
    private bool ValidateReferences(
        PlayerMonitor monitor,
        PlayerMotor motor)
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

        if (monitor == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerAnimationPresenter)}] " +
                "PlayerMonitorが指定されていません。",
                this);

            isValid = false;
        }

        if (motor == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerAnimationPresenter)}] " +
                "PlayerMotorが指定されていません。",
                this);

            isValid = false;
        }

        return isValid;
    }

    /// <summary>
    /// Animatorを操作可能か確認します。
    /// </summary>
    /// <returns>
    /// true：Animatorを操作できます。
    /// false：Animatorを操作できません。
    /// </returns>
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