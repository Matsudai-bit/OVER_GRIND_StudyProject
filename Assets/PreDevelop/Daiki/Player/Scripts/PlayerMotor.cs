using UnityEngine;

/// <summary>
/// プレイヤーの物理移動を実行します。
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerMotor : MonoBehaviour
{
    // 時間パラメータの最小値
    private const float MIN_TIME = 0.01f;

    // ジャンプ力の最小値
    private const float MIN_JUMP_POWER = 1.0f;

    // 方向ベクトルの有効判定に使用する閾値
    private const float DIRECTION_SQR_THRESHOLD = 0.0001f;

    // カメラ基準移動に使用するTransform
    [SerializeField, Header("移動基準")]
    private Transform m_movementReference;

    // プレイヤーの物理ボディ
    private Rigidbody m_playerRigidbody;

    // 物理移動パラメータ
    private PlayerMotorParameters m_parameters;

    // 初期化されているか
    private bool m_isInitialized;

    /// <summary>
    /// 通常移動の最高速度を取得します。
    /// </summary>
    public float MaxMoveSpeed => m_parameters.MaxMoveSpeed;

    /// <summary>
    /// 初期化されているかを取得します。
    /// </summary>
    /// <returns>
    /// true：初期化されています。
    /// false：初期化されていません。
    /// </returns>
    public bool IsInitialized => m_isInitialized;

    /// <summary>
    /// PlayerMotorを初期化します。
    /// </summary>
    /// <param name="playerRigidbody">プレイヤーの物理ボディ。</param>
    /// <param name="parameters">物理移動パラメータ。</param>
    public void Initialize(
        Rigidbody playerRigidbody,
        PlayerMotorParameters parameters)
    {
        if (playerRigidbody == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerMotor)}] Rigidbodyが設定されていません。",
                this);

            m_isInitialized = false;
            return;
        }

        if (!ValidateParameters(parameters))
        {
            m_isInitialized = false;
            return;
        }

        m_playerRigidbody = playerRigidbody;
        m_parameters = parameters;
        m_isInitialized = true;

        if (m_playerRigidbody.isKinematic)
        {
            Debug.LogWarning(
                $"[{nameof(PlayerMotor)}] " +
                "RigidbodyがIs Kinematicのため、速度を変更できません。",
                this);
        }
    }

    /// <summary>
    /// プレイヤーを上方向へ移動させます。
    /// </summary>
    /// <param name="deltaTime">物理更新の経過時間。</param>
    public void Jump(float deltaTime)
    {
        if (!m_isInitialized)
        {
            return;
        }

        float acceleration = m_parameters.JumpPower;

        Vector3 velocity =
            m_playerRigidbody.transform.up *
            acceleration *
            deltaTime;

        Vector3 nextVelocity = velocity;

        // 水平方向の速度は維持
        nextVelocity.x = m_playerRigidbody.linearVelocity.x;
        nextVelocity.z = m_playerRigidbody.linearVelocity.z;

        m_playerRigidbody.linearVelocity = nextVelocity;
    }

    /// <summary>
    /// 入力方向へプレイヤーを加速させます。
    /// </summary>
    /// <param name="moveInput">移動入力。</param>
    /// <param name="deltaTime">物理更新の経過時間。</param>
    public void Move(Vector2 moveInput, float deltaTime)
    {
        if (!m_isInitialized)
        {
            return;
        }

        Vector2 normalizedInput =
            Vector2.ClampMagnitude(moveInput, 1.0f);

        // 入力をカメラ基準のワールド方向へ変換
        Vector3 moveDirection =
            CalculateCameraRelativeDirection(normalizedInput);

        float inputMagnitude = normalizedInput.magnitude;

        // アナログ入力の大きさを最高速度へ反映
        Vector3 targetHorizontalVelocity =
            moveDirection *
            (m_parameters.MaxMoveSpeed * inputMagnitude);

        Vector3 currentHorizontalVelocity =
            GetHorizontalVelocity();

        float acceleration = CalculateAcceleration(
            m_parameters.MaxMoveSpeed,
            m_parameters.TimeToMaxSpeed);

        Vector3 nextHorizontalVelocity = Vector3.MoveTowards(
            currentHorizontalVelocity,
            targetHorizontalVelocity,
            acceleration * deltaTime);

        ApplyHorizontalVelocity(nextHorizontalVelocity);

        // 移動方向へ徐々に回転
        RotateTowardsMoveDirection(
            moveDirection,
            deltaTime);
    }

    /// <summary>
    /// プレイヤーの水平速度を減速させます。
    /// </summary>
    /// <param name="deltaTime">物理更新の経過時間。</param>
    public void Decelerate(float deltaTime)
    {
        if (!m_isInitialized)
        {
            return;
        }

        Vector3 currentHorizontalVelocity =
            GetHorizontalVelocity();

        float deceleration = CalculateAcceleration(
            m_parameters.MaxMoveSpeed,
            m_parameters.TimeToStop);

        Vector3 nextHorizontalVelocity = Vector3.MoveTowards(
            currentHorizontalVelocity,
            Vector3.zero,
            deceleration * deltaTime);

        ApplyHorizontalVelocity(nextHorizontalVelocity);
    }

    /// <summary>
    /// プレイヤーの水平速度を即座に停止します。
    /// </summary>
    public void StopImmediately()
    {
        if (!m_isInitialized)
        {
            return;
        }

        ApplyHorizontalVelocity(Vector3.zero);
    }

    /// <summary>
    /// 移動入力をカメラ基準のワールド方向へ変換します。
    /// </summary>
    /// <param name="moveInput">移動入力。</param>
    /// <returns>ワールド空間の移動方向。</returns>
    private Vector3 CalculateCameraRelativeDirection(
        Vector2 moveInput)
    {
        Vector3 referenceForward = m_movementReference != null
            ? m_movementReference.forward
            : Vector3.forward;

        // 上下方向を除外して水平面へ投影
        Vector3 forward = Vector3.ProjectOnPlane(
            referenceForward,
            Vector3.up);

        if (forward.sqrMagnitude <= DIRECTION_SQR_THRESHOLD)
        {
            forward = Vector3.forward;
        }

        forward.Normalize();

        // 水平面上の右方向を生成
        Vector3 right = Vector3.Cross(
            Vector3.up,
            forward);

        right.Normalize();

        Vector3 moveDirection =
            (right * moveInput.x) +
            (forward * moveInput.y);

        if (moveDirection.sqrMagnitude > 1.0f)
        {
            moveDirection.Normalize();
        }

        return moveDirection;
    }

    /// <summary>
    /// 移動方向へプレイヤーを徐々に回転させます。
    /// </summary>
    /// <param name="moveDirection">移動方向。</param>
    /// <param name="deltaTime">物理更新の経過時間。</param>
    private void RotateTowardsMoveDirection(
        Vector3 moveDirection,
        float deltaTime)
    {
        if (moveDirection.sqrMagnitude <=
            DIRECTION_SQR_THRESHOLD)
        {
            return;
        }

        // 垂直方向の回転を除外
        moveDirection.y = 0.0f;
        moveDirection.Normalize();

        Quaternion targetRotation = Quaternion.LookRotation(
            moveDirection,
            Vector3.up);

        Quaternion nextRotation = Quaternion.RotateTowards(
            m_playerRigidbody.rotation,
            targetRotation,
            m_parameters.RotationSpeed * deltaTime);

        m_playerRigidbody.MoveRotation(nextRotation);
    }

    /// <summary>
    /// 現在の水平速度を取得します。
    /// </summary>
    /// <returns>現在の水平速度。</returns>
    private Vector3 GetHorizontalVelocity()
    {
        Vector3 horizontalVelocity =
            m_playerRigidbody.linearVelocity;

        horizontalVelocity.y = 0.0f;
        return horizontalVelocity;
    }

    /// <summary>
    /// 水平速度をRigidbodyへ適用します。
    /// </summary>
    /// <param name="horizontalVelocity">適用する水平速度。</param>
    private void ApplyHorizontalVelocity(
        Vector3 horizontalVelocity)
    {
        Vector3 nextVelocity = horizontalVelocity;

        // 重力やジャンプによる垂直速度は維持
        nextVelocity.y = m_playerRigidbody.linearVelocity.y;

        m_playerRigidbody.linearVelocity = nextVelocity;
    }

    /// <summary>
    /// 指定時間で目標速度へ到達する加速度を計算します。
    /// </summary>
    /// <param name="targetSpeed">目標速度。</param>
    /// <param name="requiredTime">到達までの時間。</param>
    /// <returns>加速度。</returns>
    private float CalculateAcceleration(
        float targetSpeed,
        float requiredTime)
    {
        float safeTime = Mathf.Max(
            requiredTime,
            MIN_TIME);

        return targetSpeed / safeTime;
    }

    /// <summary>
    /// 物理移動パラメータが有効か確認します。
    /// </summary>
    /// <param name="parameters">確認するパラメータ。</param>
    /// <returns>
    /// true：有効なパラメータです。
    /// false：無効なパラメータです。
    /// </returns>
    private bool ValidateParameters(
        PlayerMotorParameters parameters)
    {
        if (parameters.MaxMoveSpeed < 0.0f)
        {
            Debug.LogError(
                $"[{nameof(PlayerMotor)}] " +
                "最高移動速度は0以上に設定してください。",
                this);

            return false;
        }

        if (parameters.JumpPower < MIN_JUMP_POWER)
        {
            Debug.LogError(
                $"[{nameof(PlayerMotor)}] " +
                $"ジャンプ力は{MIN_JUMP_POWER}以上に設定してください。",
                this);

            return false;
        }

        if (parameters.TimeToMaxSpeed < MIN_TIME ||
            parameters.TimeToStop < MIN_TIME)
        {
            Debug.LogError(
                $"[{nameof(PlayerMotor)}] " +
                $"時間パラメータは{MIN_TIME}以上に設定してください。",
                this);

            return false;
        }

        if (parameters.RotationSpeed < 0.0f)
        {
            Debug.LogError(
                $"[{nameof(PlayerMotor)}] " +
                "回転速度は0以上に設定してください。",
                this);

            return false;
        }

        return true;
    }
}