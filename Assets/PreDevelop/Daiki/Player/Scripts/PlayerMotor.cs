using UnityEngine;

/// <summary>
/// プレイヤーの物理移動を管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerMotor : MonoBehaviour
{
    // 時間パラメータの最小値
    private const float MIN_TIME = 0.01f;

    // 方向ベクトルの有効判定に使用する閾値
    private const float DIRECTION_SQR_THRESHOLD = 0.0001f;

    // カメラ基準移動に使用するTransform
    [SerializeField, Header("移動基準")]
    private Transform m_movementReference;

    // プレイヤーの物理ボディ
    private Rigidbody m_playerRigidbody;

    // 現在使用している最大移動速度
    private float m_currentMaxMoveSpeed;

    // 初期化済みかどうか
    private bool m_isInitialized;

    /// <summary>
    /// 現在使用している最大移動速度を取得します。
    /// </summary>
    public float MaxMoveSpeed => m_currentMaxMoveSpeed;

    /// <summary>
    /// 初期化済みかどうかを取得します。
    /// </summary>
    public bool IsInitialized => m_isInitialized;

    /// <summary>
    /// 現在の水平方向の移動方向を取得します。
    /// 速度がほぼ0の場合は、現在のプレイヤーの正面方向を返します。
    /// </summary>
    public Vector3 HorizontalDirection
    {
        get
        {
            if (!m_isInitialized ||
                m_playerRigidbody == null)
            {
                return Vector3.forward;
            }

            Vector3 velocity =
                m_playerRigidbody.linearVelocity;

            velocity.y = 0.0f;

            if (velocity.sqrMagnitude <=
                DIRECTION_SQR_THRESHOLD)
            {
                return m_playerRigidbody.transform.forward;
            }

            return velocity.normalized;
        }
    }

    /// <summary>
    /// 現在の垂直方向の速度を取得します。
    /// </summary>
    public float VerticalVelocity =>
        m_isInitialized
            ? m_playerRigidbody.linearVelocity.y
            : 0.0f;

    /// <summary>
    /// PlayerMotorを初期化します。
    /// </summary>
    /// <param name="playerRigidbody">プレイヤーのRigidbody。</param>
    public void Initialize(Rigidbody playerRigidbody)
    {
        if (playerRigidbody == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerMotor)}] Rigidbodyが指定されていません。",
                this);

            m_isInitialized = false;
            return;
        }

        m_playerRigidbody = playerRigidbody;
        m_isInitialized = true;

        if (m_playerRigidbody.isKinematic)
        {
            Debug.LogWarning(
                $"[{nameof(PlayerMotor)}] " +
                "RigidbodyがIs Kinematicです。",
                this);
        }
    }

    /// <summary>
    /// 指定したパラメータでプレイヤーを移動させます。
    /// </summary>
    /// <param name="moveInput">移動入力。</param>
    /// <param name="parameters">移動用パラメータ。</param>
    /// <param name="deltaTime">物理更新の経過時間。</param>
    public void Move(
        Vector2 moveInput,
        PlayerMoveParameters parameters,
        float deltaTime)
    {
        if (!m_isInitialized)
        {
            return;
        }

        Vector2 normalizedInput =
            Vector2.ClampMagnitude(
                moveInput,
                1.0f);

        // 入力をカメラ基準のワールド方向へ変換
        Vector3 moveDirection =
            CalculateCameraRelativeDirection(
                normalizedInput);

        float inputMagnitude =
            normalizedInput.magnitude;

        float maxMoveSpeed =
            Mathf.Max(
                parameters.MaxMoveSpeed,
                0.0f);

        m_currentMaxMoveSpeed = maxMoveSpeed;

        // 目標水平速度を計算
        Vector3 targetHorizontalVelocity =
            moveDirection *
            (maxMoveSpeed * inputMagnitude);

        Vector3 currentHorizontalVelocity =
            GetHorizontalVelocity();

        float acceleration =
            CalculateAcceleration(
                maxMoveSpeed,
                parameters.TimeToMaxSpeed);

        Vector3 nextHorizontalVelocity =
            Vector3.MoveTowards(
                currentHorizontalVelocity,
                targetHorizontalVelocity,
                acceleration * deltaTime);

        ApplyHorizontalVelocity(
            nextHorizontalVelocity);

        // 移動方向へ徐々に回転
        RotateTowardsMoveDirection(
            moveDirection,
            parameters.RotationSpeed,
            deltaTime);
    }

    /// <summary>
    /// 指定したパラメータで現在の移動速度を減速させます。
    /// </summary>
    /// <param name="parameters">移動用パラメータ。</param>
    /// <param name="deltaTime">物理更新の経過時間。</param>
    public void Decelerate(
        PlayerMoveParameters parameters,
        float deltaTime)
    {
        if (!m_isInitialized)
        {
            return;
        }

        float maxMoveSpeed =
            Mathf.Max(
                parameters.MaxMoveSpeed,
                0.0f);

        m_currentMaxMoveSpeed = maxMoveSpeed;

        Vector3 currentHorizontalVelocity =
            GetHorizontalVelocity();

        float deceleration =
            CalculateAcceleration(
                maxMoveSpeed,
                parameters.TimeToStop);

        Vector3 nextHorizontalVelocity =
            Vector3.MoveTowards(
                currentHorizontalVelocity,
                Vector3.zero,
                deceleration * deltaTime);

        ApplyHorizontalVelocity(
            nextHorizontalVelocity);
    }

    /// <summary>
    /// プレイヤーにジャンプの初速を与えます。
    /// </summary>
    /// <param name="jumpPower">ジャンプ力。</param>
    public void Jump(float jumpPower)
    {
        if (!m_isInitialized)
        {
            return;
        }

        Vector3 nextVelocity =
            m_playerRigidbody.linearVelocity;

        nextVelocity.y = jumpPower;

        m_playerRigidbody.linearVelocity =
            nextVelocity;
    }

    /// <summary>
    /// 落下速度の増加とジャンプの高さ調整のための
    /// 追加重力を適用します。
    /// </summary>
    /// <param name="parameterAsset">
    /// 移動用パラメータアセット。
    /// </param>
    /// <param name="isJumpHeld">
    /// ジャンプ入力を押し続けているかどうか。
    /// </param>
    /// <param name="deltaTime">
    /// 物理更新の経過時間。
    /// </param>
    public void ApplyExtraGravity(
        PlayerMovementParameterAsset parameterAsset,
        bool isJumpHeld,
        float deltaTime)
    {
        if (!m_isInitialized)
        {
            return;
        }

        Vector3 velocity =
            m_playerRigidbody.linearVelocity;

        if (velocity.y < 0.0f)
        {
            // 落下中は追加の重力を加算
            // Use Gravityによる1倍分はエンジン側で処理済み
            velocity.y +=
                Physics.gravity.y *
                (parameterAsset.FallGravityMultiplier - 1.0f) *
                deltaTime;
        }
        else if (
            velocity.y > 0.0f &&
            !isJumpHeld)
        {
            // 上昇中に入力を離した場合は
            // 追加の重力を加算してジャンプ高度を下げる
            velocity.y +=
                Physics.gravity.y *
                (parameterAsset.LowJumpMultiplier - 1.0f) *
                deltaTime;
        }

        // velocity.y > 0.0f && isJumpHeld の場合は
        // エンジンの標準重力のみが作用する

        float maxFallSpeed =
            Mathf.Max(
                parameterAsset.MaxFallSpeed,
                0.0f);

        if (velocity.y < -maxFallSpeed)
        {
            velocity.y = -maxFallSpeed;
        }

        m_playerRigidbody.linearVelocity =
            velocity;
    }

    /// <summary>
    /// プレイヤーの水平移動速度を即座に停止させます。
    /// </summary>
    public void StopImmediately()
    {
        if (!m_isInitialized)
        {
            return;
        }

        ApplyHorizontalVelocity(
            Vector3.zero);
    }

    /// <summary>
    /// 移動入力をカメラ基準のワールド方向へ変換します。
    /// ドリフト処理など、速度計算を外部で行いたい場合にも使用できます。
    /// </summary>
    /// <param name="moveInput">移動入力。</param>
    /// <returns>ワールド空間の移動方向。</returns>
    public Vector3 CalculateCameraRelativeDirection(
        Vector2 moveInput)
    {
        Vector3 referenceForward =
            m_movementReference != null
                ? m_movementReference.forward
                : Vector3.forward;

        Vector3 forward =
            Vector3.ProjectOnPlane(
                referenceForward,
                Vector3.up);

        if (forward.sqrMagnitude <=
            DIRECTION_SQR_THRESHOLD)
        {
            forward = Vector3.forward;
        }

        forward.Normalize();

        Vector3 right =
            Vector3.Cross(
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
    /// <param name="rotationSpeed">1秒間の最大回転角度。</param>
    /// <param name="deltaTime">物理更新の経過時間。</param>
    private void RotateTowardsMoveDirection(
        Vector3 moveDirection,
        float rotationSpeed,
        float deltaTime)
    {
        if (moveDirection.sqrMagnitude <=
            DIRECTION_SQR_THRESHOLD)
        {
            return;
        }

        moveDirection.y = 0.0f;
        moveDirection.Normalize();

        Quaternion targetRotation =
            Quaternion.LookRotation(
                moveDirection,
                Vector3.up);

        Quaternion nextRotation =
            Quaternion.RotateTowards(
                m_playerRigidbody.rotation,
                targetRotation,
                Mathf.Max(
                    rotationSpeed,
                    0.0f) *
                deltaTime);

        m_playerRigidbody.MoveRotation(
            nextRotation);
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
    /// <param name="horizontalVelocity">
    /// 適用する水平速度。
    /// </param>
    private void ApplyHorizontalVelocity(
        Vector3 horizontalVelocity)
    {
        Vector3 nextVelocity =
            horizontalVelocity;

        nextVelocity.y =
            m_playerRigidbody.linearVelocity.y;

        m_playerRigidbody.linearVelocity =
            nextVelocity;
    }

    /// <summary>
    /// 指定時間で目標速度へ到達するための
    /// 加速度を計算します。
    /// </summary>
    /// <param name="targetSpeed">目標速度。</param>
    /// <param name="requiredTime">到達時間。</param>
    /// <returns>加速度。</returns>
    private float CalculateAcceleration(
        float targetSpeed,
        float requiredTime)
    {
        float safeTime =
            Mathf.Max(
                requiredTime,
                MIN_TIME);

        return targetSpeed / safeTime;
    }

    /// <summary>
    /// プレイヤーの現在の水平移動速度を取得します。
    /// </summary>
    public float HorizontalSpeed
    {
        get
        {
            if (!m_isInitialized ||
                m_playerRigidbody == null)
            {
                return 0.0f;
            }

            Vector3 velocity =
                m_playerRigidbody.linearVelocity;

            velocity.y = 0.0f;

            return velocity.magnitude;
        }
    }

    /// <summary>
    /// 指定した速度で移動します。
    /// 移動方向と回転方向は入力方向に従います。
    /// </summary>
    /// <param name="moveInput">移動入力。</param>
    /// <param name="speed">移動速度。</param>
    /// <param name="rotationSpeed">回転速度。</param>
    /// <param name="deltaTime">物理更新時間。</param>
    public void MoveAtFixedSpeed(
        Vector2 moveInput,
        float speed,
        float rotationSpeed,
        float deltaTime)
    {
        if (!m_isInitialized)
        {
            return;
        }

        Vector2 normalizedInput =
            Vector2.ClampMagnitude(
                moveInput,
                1.0f);

        Vector3 moveDirection =
            CalculateCameraRelativeDirection(
                normalizedInput);

        float inputMagnitude =
            normalizedInput.magnitude;

        Vector3 targetHorizontalVelocity =
            moveDirection *
            (Mathf.Max(
                speed,
                0.0f) *
             inputMagnitude);

        ApplyHorizontalVelocity(
            targetHorizontalVelocity);

        RotateTowardsMoveDirection(
            moveDirection,
            rotationSpeed,
            deltaTime);

        m_currentMaxMoveSpeed =
            Mathf.Max(
                speed,
                0.0f);
    }

    /// <summary>
    /// 指定したワールド方向へ、指定速度で移動します。
    /// スティック入力やカメラ方向の影響は受けません。
    ///
    /// ブーストダッシュなど、
    /// 開始時に決定した方向へ固定して移動する場合に使用します。
    /// </summary>
    /// <param name="worldDirection">
    /// ワールド空間での移動方向。
    /// </param>
    /// <param name="speed">移動速度。</param>
    /// <param name="rotationSpeed">
    /// プレイヤーの回転速度。
    /// </param>
    /// <param name="deltaTime">
    /// 物理更新時間。
    /// </param>
    public void MoveAtFixedWorldDirection(
        Vector3 worldDirection,
        float speed,
        float rotationSpeed,
        float deltaTime)
    {
        if (!m_isInitialized)
        {
            return;
        }

        // 水平方向のみを使用
        worldDirection.y = 0.0f;

        if (worldDirection.sqrMagnitude <=
            DIRECTION_SQR_THRESHOLD)
        {
            return;
        }

        worldDirection.Normalize();

        Vector3 targetHorizontalVelocity =
            worldDirection *
            Mathf.Max(
                speed,
                0.0f);

        // 移動方向はworldDirectionに完全固定
        ApplyHorizontalVelocity(
            targetHorizontalVelocity);

        // 向きだけは指定した回転速度で徐々に合わせる
        RotateTowardsMoveDirection(
            worldDirection,
            rotationSpeed,
            deltaTime);

        m_currentMaxMoveSpeed =
            Mathf.Max(
                speed,
                0.0f);
    }

    /// <summary>
    /// ドリフトのように、実際の進行方向と
    /// キャラクターの向きを別々に制御しながら
    /// 指定速度で移動します。
    /// </summary>
    /// <param name="velocityDirection">
    /// 実際に進む方向。
    /// </param>
    /// <param name="speed">移動速度。</param>
    /// <param name="facingDirection">
    /// キャラクターが向く目標方向。
    /// </param>
    /// <param name="facingRotationSpeed">
    /// キャラクターの向きを変更する速度。
    /// </param>
    /// <param name="deltaTime">
    /// 物理更新時間。
    /// </param>
    public void MoveWithDriftAtFixedSpeed(
        Vector3 velocityDirection,
        float speed,
        Vector3 facingDirection,
        float facingRotationSpeed,
        float deltaTime)
    {
        if (!m_isInitialized)
        {
            return;
        }

        Vector3 normalizedVelocityDirection =
            velocityDirection.sqrMagnitude >
                DIRECTION_SQR_THRESHOLD
                ? velocityDirection.normalized
                : Vector3.forward;

        Vector3 targetHorizontalVelocity =
            normalizedVelocityDirection *
            Mathf.Max(
                speed,
                0.0f);

        ApplyHorizontalVelocity(
            targetHorizontalVelocity);

        // キャラクターの向きは実際の進行方向とは独立して制御
        RotateTowardsMoveDirection(
            facingDirection,
            facingRotationSpeed,
            deltaTime);

        m_currentMaxMoveSpeed =
            Mathf.Max(
                speed,
                0.0f);
    }
}