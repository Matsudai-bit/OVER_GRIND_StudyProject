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

    // 障害物検知を行う最低速度の閾値
    // これ未満の速度では障害物検知を行わない（静止判定のノイズ回避）
    private const float OBSTACLE_CHECK_SPEED_THRESHOLD = 0.01f;

    // カメラ基準移動に使用するTransform
    [SerializeField, Header("移動基準")]
    private Transform m_movementReference;

    // 障害物への食い込みを防ぐための手前バッファ距離
    // 大きいほど障害物の手前で早めに減速するが、
    // 隙間の狭い通路で引っかかりやすくなる
    [SerializeField, Header("障害物検知")]
    [Min(0.0f)]
    private float m_obstacleSkinWidth = 0.05f;

    // 障害物に接触している間、1秒あたり減速する速度
    // 値が大きいほど、障害物へ接触した際に速く止まる
    [SerializeField]
    [Min(0.0f)]
    private float m_obstacleDecelerationPerSecond = 20.0f;

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
    /// プレイヤーの体が現在向いている水平方向を取得します。
    /// 移動速度やカメラの向きに関係なく、
    /// Transformの正面方向を基準とします（攻撃の繰り出し方向などに使用）。
    /// </summary>
    public Vector3 FacingDirection
    {
        get
        {
            if (!m_isInitialized ||
                m_playerRigidbody == null)
            {
                return Vector3.forward;
            }

            Vector3 forward =
                m_playerRigidbody.transform.forward;

            forward.y = 0.0f;

            if (forward.sqrMagnitude <=
                DIRECTION_SQR_THRESHOLD)
            {
                return Vector3.forward;
            }

            return forward.normalized;
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
    /// <param name="applyObstacleAvoidance">
    /// 障害物への接触時に緩やかな減速を適用するかどうか。
    /// 通常はtrueのまま使用してください。
    /// </param>
    public void Move(
        Vector2 moveInput,
        PlayerMoveParameters parameters,
        float deltaTime,
        bool applyObstacleAvoidance = true)
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
            nextHorizontalVelocity,
            deltaTime,
            applyObstacleAvoidance);

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
            nextHorizontalVelocity,
            deltaTime);
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

        // 目標速度が0のため障害物検知は不要
        Vector3 nextVelocity =
            Vector3.zero;

        nextVelocity.y =
            m_playerRigidbody.linearVelocity.y;

        m_playerRigidbody.linearVelocity =
            nextVelocity;
    }

    /// <summary>
    /// 指定した方向・距離ぶん、Rigidbodyの位置を即座に補正します（水平成分のみ）。
    /// アニメーション駆動の部位（脚など）が物理演算を介さずに
    /// 対象へ食い込んでしまった場合に、その貫通量を打ち消す用途を想定しています。
    /// </summary>
    /// <param name="direction">補正する方向。</param>
    /// <param name="distance">補正する距離。</param>
    public void ResolvePenetration(
        Vector3 direction,
        float distance)
    {
        if (!m_isInitialized || distance <= 0.0f)
        {
            return;
        }

        Vector3 horizontalDirection = direction;
        horizontalDirection.y = 0.0f;

        if (horizontalDirection.sqrMagnitude <=
            DIRECTION_SQR_THRESHOLD)
        {
            return;
        }

        horizontalDirection.Normalize();

        Vector3 offset =
            horizontalDirection * distance;

        m_playerRigidbody.MovePosition(
            m_playerRigidbody.position + offset);
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
    /// 障害物検知を行う場合、進行方向に障害物がないかを確認し、
    /// あれば緩やかに減速させます（<see cref="ClampVelocityForObstacles"/>）。
    /// </summary>
    /// <param name="horizontalVelocity">
    /// 適用したい水平速度（障害物検知前の目標値）。
    /// </param>
    /// <param name="deltaTime">
    /// この速度が適用される時間幅。障害物までの距離判定に使用する。
    /// </param>
    /// <param name="applyObstacleAvoidance">
    /// 障害物検知による減速を適用するかどうか。
    /// falseの場合、目標速度をそのまま適用します
    /// （攻撃状態など、独自の速度制御を優先したい場合に使用）。
    /// </param>
    private void ApplyHorizontalVelocity(
        Vector3 horizontalVelocity,
        float deltaTime,
        bool applyObstacleAvoidance = true)
    {
        Vector3 nextHorizontalVelocity =
            applyObstacleAvoidance
                ? ClampVelocityForObstacles(
                    horizontalVelocity,
                    deltaTime)
                : horizontalVelocity;

        Vector3 nextVelocity =
            nextHorizontalVelocity;

        nextVelocity.y =
            m_playerRigidbody.linearVelocity.y;

        m_playerRigidbody.linearVelocity =
            nextVelocity;
    }

    /// <summary>
    /// 進行方向に障害物がある場合、
    /// 実際の現在速度を基準に緩やかに減速させます。
    /// 障害物の手前で瞬時に速度を切り詰めるのではなく、
    /// <see cref="m_obstacleDecelerationPerSecond"/>の減速度で
    /// 徐々に速度を落とすことで、不自然な急停止を避けます。
    /// </summary>
    /// <param name="horizontalVelocity">要求されている目標水平速度。</param>
    /// <param name="deltaTime">この速度が適用される時間幅。</param>
    /// <returns>障害物を考慮して減速した水平速度。</returns>
    private Vector3 ClampVelocityForObstacles(
        Vector3 horizontalVelocity,
        float deltaTime)
    {
        float requestedSpeed = horizontalVelocity.magnitude;

        if (requestedSpeed <= OBSTACLE_CHECK_SPEED_THRESHOLD ||
            deltaTime <= 0.0f)
        {
            return horizontalVelocity;
        }

        Vector3 direction =
            horizontalVelocity / requestedSpeed;

        // このステップで実際に進もうとしている距離
        float travelDistance =
            requestedSpeed * deltaTime;

        // Rigidbodyに付いている非Trigger Colliderを基準に、
        // 進行方向へのスイープ判定を行う
        // （AttackHitbox等のTrigger Colliderは自動的に除外される）
        bool isBlocked =
            m_playerRigidbody.SweepTest(
                direction,
                out RaycastHit hitInfo,
                travelDistance,
                QueryTriggerInteraction.Ignore);

        if (!isBlocked)
        {
            return horizontalVelocity;
        }

        // 障害物の手前、バッファ分だけ余裕を持たせた距離までなら
        // 進んでよい速度（このステップの上限）
        float allowedDistance =
            Mathf.Max(
                hitInfo.distance - m_obstacleSkinWidth,
                0.0f);

        float allowedSpeedThisStep =
            allowedDistance / deltaTime;

        // 現在の実速度を基準に、上限速度へ向けて
        // 緩やかに減速させる（瞬時の切り詰めを避ける）
        float currentActualSpeed =
            GetHorizontalVelocity().magnitude;

        float decelerationThisStep =
            m_obstacleDecelerationPerSecond * deltaTime;

        float nextSpeed =
            Mathf.MoveTowards(
                currentActualSpeed,
                allowedSpeedThisStep,
                decelerationThisStep);

        nextSpeed =
            Mathf.Clamp(
                nextSpeed,
                0.0f,
                requestedSpeed);

        return direction * nextSpeed;
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
    /// <param name="applyObstacleAvoidance">
    /// 障害物への接触時に緩やかな減速を適用するかどうか。
    /// </param>
    public void MoveAtFixedSpeed(
        Vector2 moveInput,
        float speed,
        float rotationSpeed,
        float deltaTime,
        bool applyObstacleAvoidance = true)
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
            targetHorizontalVelocity,
            deltaTime,
            applyObstacleAvoidance);

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
    /// <param name="applyObstacleAvoidance">
    /// 障害物への接触時に緩やかな減速を適用するかどうか。
    /// 攻撃状態のように、独自の速度消費ロジックを優先したい場合はfalseを指定してください。
    /// </param>
    public void MoveAtFixedWorldDirection(
        Vector3 worldDirection,
        float speed,
        float rotationSpeed,
        float deltaTime,
        bool applyObstacleAvoidance = true)
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
            targetHorizontalVelocity,
            deltaTime,
            applyObstacleAvoidance);

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
    /// <param name="applyObstacleAvoidance">
    /// 障害物への接触時に緩やかな減速を適用するかどうか。
    /// </param>
    public void MoveWithDriftAtFixedSpeed(
        Vector3 velocityDirection,
        float speed,
        Vector3 facingDirection,
        float facingRotationSpeed,
        float deltaTime,
        bool applyObstacleAvoidance = true)
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
            targetHorizontalVelocity,
            deltaTime,
            applyObstacleAvoidance);

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
    /// <summary>
    /// 空中移動時の水平方向の空気抵抗を適用します。
    /// </summary>
    /// <param name="airResistance">空気抵抗の強さ。</param>
    /// <param name="deltaTime">経過時間。</param>
    public void ApplyAirResistance(
        float airResistance,
        float deltaTime)
    {
        if (!m_isInitialized ||
            airResistance <= 0.0f)
        {
            return;
        }

        Vector3 velocity =
            m_playerRigidbody.linearVelocity;

        Vector3 horizontalVelocity =
            new Vector3(
                velocity.x,
                0.0f,
                velocity.z);

        float horizontalSpeed =
            horizontalVelocity.magnitude;

        if (horizontalSpeed <= 0.0f)
        {
            return;
        }

        float deceleration =
            horizontalSpeed * airResistance;

        Vector3 nextHorizontalVelocity =
            Vector3.MoveTowards(
                horizontalVelocity,
                Vector3.zero,
                deceleration * deltaTime);

        // 垂直方向の速度は変更しない
        m_playerRigidbody.linearVelocity =
            new Vector3(
                nextHorizontalVelocity.x,
                velocity.y,
                nextHorizontalVelocity.z);
    }
}