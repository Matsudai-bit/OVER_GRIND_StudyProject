using UnityEngine;

/// <summary>
/// プレイヤーの物理移動を管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerMotor : MonoBehaviour
{
    [SerializeField, Header("Parameter Asset")]
    private PlayerMotorComponentParameterAsset m_parameterAsset;

    // 時間パラメータの最小値
    private const float MIN_TIME = 0.01f;

    // 方向ベクトルの有効判定に使用する閾値
    private const float DIRECTION_SQR_THRESHOLD = 0.0001f;

    // 障害物検知を行う最低速度の閾値
    // これ未満の速度では障害物検知を行わない（静止判定のノイズ回避）
    private const float OBSTACLE_CHECK_SPEED_THRESHOLD = 0.01f;

    // 停止状態と判定する水平速度の閾値
    private const float STOP_SPEED_THRESHOLD = 0.01f;

    // 停止後に再加速を許可する、体と入力方向の角度差（度）
    private const float START_FACING_TOLERANCE = 1.0f;

    // 衝突計算の微小な上向き速度を無視するための閾値
    private const float UPWARD_VELOCITY_THRESHOLD = 0.01f;

    // 接地面として扱う接触法線のY成分
    private const float SUPPORT_NORMAL_Y_THRESHOLD = 0.5f;

    // 段差として扱う最小高さ
    private const float MIN_STEP_HEIGHT = 0.005f;

    // 段差正面の最下段Rayを配置する高さ
    private const float MIN_STEP_WALL_CHECK_HEIGHT = 0.005f;

    // 段差正面を複数高さで確認するRay数
    private const int STEP_WALL_CHECK_RAY_COUNT = 3;

    // 段差乗り越え後の空間確認で保持するCollider数
    private const int STEP_CLEARANCE_BUFFER_SIZE = 16;

    // 段差判定Raycastで保持するHit数
    private const int STEP_RAYCAST_BUFFER_SIZE = 16;

    // カメラ基準移動に使用するTransform
    [SerializeField, Header("移動基準")]
    private Transform m_movementReference;

    // 障害物として扱うレイヤー
    private LayerMask m_obstacleLayerMask;

    // 障害物への食い込みを防ぐための手前バッファ距離
    private float m_obstacleSkinWidth = 0.05f;

    // 障害物に接触している間、1秒あたり減速する速度
    private float m_obstacleDecelerationPerSecond = 20.0f;

    // 高速移動中の衝突で発生する意図しない上向き速度を抑制する最低速度
    [SerializeField, Header("高速衝突")]
    [Min(0.0f)]
    private float m_collisionLiftPreventionMinSpeed = 16.0f;

    // 自動で乗り越えられる最大段差高さ
    private float m_maxStepHeight = 0.3f;

    // Capsule前方から段差正面を探索する距離
    private float m_stepCheckDistance = 0.25f;

    // 足元から段差正面判定を行う高さ
    private float m_stepLowerCheckHeight = 0.05f;

    // 段差正面より少し奥で上面を探索する距離
    private float m_stepForwardOffset = 0.05f;

    // 乗り越え後のCollider接触を避けるための余白
    private float m_stepClearanceSkin = 0.02f;

    // プレイヤーの物理ボディ
    private Rigidbody m_playerRigidbody;

    // プレイヤーの移動用CapsuleCollider
    [SerializeField, Header("Collider")]
    private CapsuleCollider m_playerCapsuleCollider;
    private PhysicsMaterial m_groundPhysicsMaterial;
    private PhysicsMaterial m_airPhysicsMaterial;

    // 段差乗り越え後の空間確認用バッファ
    private readonly Collider[] m_stepClearanceBuffer =
        new Collider[STEP_CLEARANCE_BUFFER_SIZE];

    // 段差判定Raycast用バッファ
    private readonly RaycastHit[] m_stepRaycastBuffer =
        new RaycastHit[STEP_RAYCAST_BUFFER_SIZE];

    // プレイヤーの周辺環境監視機能
    private PlayerMonitor m_monitor;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // 直近のStep Up判定結果。段差判定の切り分けに使用します。
    [SerializeField, Header("Step Up Debug"), TextArea]
    private string m_stepDebugReason = "未判定";

    /// <summary>
    /// 直近のStep Up判定結果を取得します。
    /// </summary>
    public string StepDebugReason => m_stepDebugReason;
#endif

    // 現在使用している最大移動速度
    private float m_currentMaxMoveSpeed;

    // 初期化済みかどうか
    private bool m_isInitialized;

    // 誘導中だけ物理の押し戻し・重力から位置制御を分離し、離脱時に元の設定へ戻します。
    private bool m_isRailMotionActive;
    private bool m_previousIsKinematic;
    private CollisionDetectionMode m_previousCollisionDetection;
    private Vector3 m_railVelocity;
    private readonly PlayerRailCollisionGuard m_railCollisionGuard = new PlayerRailCollisionGuard();

    // 物理演算前の速度を保持し、衝突応答だけで上向き速度が発生したか判定します。
    private float m_verticalVelocityBeforePhysics;
    private float m_horizontalSpeedBeforePhysics;
    private float m_lastIntentionalUpwardVelocityFixedTime = float.NegativeInfinity;
    private bool m_wasMovingAlongGround;

    /// <summary>方向指定ジャンプの離陸元レールとの衝突を一時的に無視します。</summary>
    public void BeginRailCollisionIgnore(SplineRailInfo rail, float minimumDuration)
    {
        if (m_isInitialized) m_railCollisionGuard.Begin(m_playerRigidbody, rail, minimumDuration);
    }

    /// <summary>ジャンプ中断時などにレールとの衝突を復元します。</summary>
    public void RestoreRailCollisions() => m_railCollisionGuard.Restore();

    /// <summary>物理演算前の速度を記録し、レールから離れたことを確認します。</summary>
    private void FixedUpdate()
    {
        UpdateCapsuleFriction();
        if (m_isInitialized && !m_playerRigidbody.isKinematic)
        {
            Vector3 velocity = m_playerRigidbody.linearVelocity;
            m_verticalVelocityBeforePhysics = velocity.y;
            velocity.y = 0.0f;
            m_horizontalSpeedBeforePhysics = velocity.magnitude;
        }

        m_railCollisionGuard.Update();
    }

    /// <summary>衝突開始時に、物理衝突だけで発生した上向き速度を除去します。</summary>
    /// <param name="collision">衝突情報。</param>
    private void OnCollisionEnter(Collision collision)
    {
        PreventUnintentionalCollisionLift(collision);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        RecordRailJumpContact(collision);
#endif
    }

    /// <summary>衝突継続中に、物理衝突だけで発生した上向き速度を除去します。</summary>
    /// <param name="collision">衝突情報。</param>
    private void OnCollisionStay(Collision collision)
    {
        PreventUnintentionalCollisionLift(collision);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        RecordRailJumpContact(collision);
#endif
    }

    /// <summary>高速移動中の衝突が生成した、意図しない上向き速度を除去します。</summary>
    /// <param name="collision">衝突情報。</param>
    private void PreventUnintentionalCollisionLift(Collision collision)
    {
        if (!m_isInitialized ||
            m_playerRigidbody.isKinematic ||
            m_isRailMotionActive ||
            collision.contactCount <= 0 ||
            m_horizontalSpeedBeforePhysics < m_collisionLiftPreventionMinSpeed ||
            m_verticalVelocityBeforePhysics > UPWARD_VELOCITY_THRESHOLD ||
            WasIntentionalUpwardVelocitySetThisStep())
        {
            return;
        }

        Vector3 velocity = m_playerRigidbody.linearVelocity;
        if (velocity.y <= UPWARD_VELOCITY_THRESHOLD)
        {
            return;
        }

        // 水平速度は通常の衝突応答を維持し、勝手なジャンプ成分だけを除去します。
        // 足元の接触では0にして地面へ押し込まず、側面接触では元の落下速度を維持します。
        velocity.y = HasSupportingContact(collision)
            ? 0.0f
            : Mathf.Min(0.0f, m_verticalVelocityBeforePhysics);
        m_playerRigidbody.linearVelocity = velocity;
    }

    /// <summary>衝突情報にPlayerを下から支える接触面が含まれるか確認します。</summary>
    /// <param name="collision">確認する衝突情報。</param>
    /// <returns>true：下から支える接触面が含まれています。</returns>
    private bool HasSupportingContact(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y >= SUPPORT_NORMAL_Y_THRESHOLD)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>現在の物理更新でゲーム処理が上向き速度を設定したか確認します。</summary>
    /// <returns>true：ジャンプなどで上向き速度が設定されています。</returns>
    private bool WasIntentionalUpwardVelocitySetThisStep()
    {
        return Time.fixedTime - m_lastIntentionalUpwardVelocityFixedTime <=
            Time.fixedDeltaTime * 0.5f;
    }

    /// <summary>ジャンプや吹き飛ばしによる意図した上向き速度を記録します。</summary>
    /// <param name="verticalVelocity">設定する垂直速度。</param>
    private void RegisterIntentionalUpwardVelocity(float verticalVelocity)
    {
        if (verticalVelocity > UPWARD_VELOCITY_THRESHOLD)
        {
            m_lastIntentionalUpwardVelocityFixedTime = Time.fixedTime;
            m_wasMovingAlongGround = false;
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private string m_lastRailJumpContact = "none";

    /// <summary>発射直後の速度低下を調べるため、接触情報を初期化します。</summary>
    public void ResetRailJumpContact() => m_lastRailJumpContact = "none";

    /// <summary>ジャンプ診断用の物理設定と直近の接触相手を取得します。</summary>
    public string RailJumpPhysicsDetails => m_playerRigidbody == null ? "Rigidbody missing" :
        $"kinematic={m_playerRigidbody.isKinematic}, constraints={m_playerRigidbody.constraints}, contact={m_lastRailJumpContact}";

    /// <summary>衝突が初速を打ち消した場合に確認できる情報を保持します。</summary>
    private void RecordRailJumpContact(Collision collision)
    {
        m_lastRailJumpContact = $"{collision.collider.name}, normal=" +
            (collision.contactCount > 0 ? collision.GetContact(0).normal.ToString("F3") : "none");
    }
#endif

    /// <summary>レール誘導が設定した移動速度を取得します。</summary>
    public Vector3 RailVelocity => m_railVelocity;

    /// <summary>
    /// 現在使用している最大移動速度を取得します。
    /// </summary>
    public float MaxMoveSpeed => m_currentMaxMoveSpeed;

    /// <summary>
    /// 初期化済みかどうかを取得します。
    /// </summary>
    public bool IsInitialized => m_isInitialized;

    /// <summary>描画補間の影響を受けない物理位置を取得します。</summary>
    public Vector3 Position => m_isInitialized ? m_playerRigidbody.position : transform.position;

    /// <summary>ワールド空間の初速を設定します。</summary>
    /// <param name="velocity">水平・垂直成分を含む初速。</param>
    public void SetWorldVelocity(Vector3 velocity)
    {
        if (!m_isInitialized) return;
        RegisterIntentionalUpwardVelocity(velocity.y);
        m_playerRigidbody.linearVelocity = velocity;
    }

    /// <summary>速度を変更せず、指定した水平進行方向へ体の向きを即座に合わせます。</summary>
    /// <param name="worldDirection">ワールド空間の進行方向。垂直成分は使用しません。</param>
    public void AlignFacingToDirection(Vector3 worldDirection)
    {
        if (!m_isInitialized) return;
        worldDirection.y = 0.0f;
        if (worldDirection.sqrMagnitude <= DIRECTION_SQR_THRESHOLD) return;

        // 同じ物理更新内で攻撃へ移る場合も、新しい向きから移動を開始します。
        m_playerRigidbody.rotation = Quaternion.LookRotation(worldDirection.normalized, Vector3.up);
    }

    /// <summary>水平速度と上昇を維持し、空中攻撃中の落下速度を制限します。</summary>
    /// <param name="maxFallSpeed">許容する下向きの速度。</param>
    public void LimitAttackFallSpeed(float maxFallSpeed)
    {
        if (!m_isInitialized || m_playerRigidbody.isKinematic) return;
        Vector3 velocity = m_playerRigidbody.linearVelocity;
        float gravityStep = m_playerRigidbody.useGravity ? Physics.gravity.y * Time.fixedDeltaTime : 0.0f;
        // この後の物理計算で加わる重力も含めて制限します。
        velocity.y = Mathf.Max(velocity.y, -Mathf.Max(0.0f, maxFallSpeed) - gravityStep);
        m_playerRigidbody.linearVelocity = velocity;
    }

    /// <summary>物理衝突を維持しながら、空中攻撃の接触位置へ追従します。</summary>
    /// <param name="position">対象の移動と下降量を反映した目標位置。</param>
    /// <param name="deltaTime">物理更新の間隔。</param>
    public void MoveToAirAttackContact(Vector3 position, float deltaTime)
    {
        if (!m_isInitialized || m_playerRigidbody.isKinematic || deltaTime <= 0.0f) return;
        Vector3 velocity = (position - m_playerRigidbody.position) / deltaTime;
        // 停止・緩やかな下降を重力に上書きされないよう、次の物理更新分を補償します。
        if (m_playerRigidbody.useGravity) velocity -= Physics.gravity * deltaTime;
        RegisterIntentionalUpwardVelocity(velocity.y);
        m_playerRigidbody.linearVelocity = velocity;
    }

    /// <summary>固定更新によるレール誘導を開始し、物理演算との位置の取り合いを防ぎます。</summary>
    public void BeginRailMotion()
    {
        if (!m_isInitialized || m_isRailMotionActive) return;
        m_previousIsKinematic = m_playerRigidbody.isKinematic;
        m_previousCollisionDetection = m_playerRigidbody.collisionDetectionMode;
        m_railVelocity = m_playerRigidbody.linearVelocity;
        // ContinuousDynamicはキネマティックに対応しないため、一時的に切り替えます。
        m_playerRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        m_playerRigidbody.isKinematic = true;
        m_isRailMotionActive = true;
    }

    /// <summary>誘導前の物理設定を復元し、離脱速度を適用します。</summary>
    /// <param name="exitVelocity">離脱時の速度。</param>
    public void EndRailMotion(Vector3 exitVelocity)
    {
        if (!m_isInitialized || !m_isRailMotionActive) return;
        m_playerRigidbody.isKinematic = m_previousIsKinematic;
        m_playerRigidbody.collisionDetectionMode = m_previousCollisionDetection;
        m_isRailMotionActive = false;
        if (!m_playerRigidbody.isKinematic)
        {
            RegisterIntentionalUpwardVelocity(exitVelocity.y);
            m_playerRigidbody.linearVelocity = exitVelocity;
        }
    }

    /// <summary>無効化や破棄の際にも、一時的な物理設定を元に戻します。</summary>
    private void OnDisable()
    {
        RestoreCapsuleFriction();
        RestoreRailCollisions();
        EndRailMotion(m_railVelocity);
    }

    /// <summary>生成した空中用PhysicsMaterialを解放します。</summary>
    private void OnDestroy()
    {
        RestoreCapsuleFriction();
        if (m_airPhysicsMaterial != null) Destroy(m_airPhysicsMaterial);
    }

    /// <summary>空中では壁との摩擦をなくし、接地後は元の摩擦へ戻します。</summary>
    private void UpdateCapsuleFriction()
    {
        if (!m_isInitialized || m_playerCapsuleCollider == null ||
            m_airPhysicsMaterial == null || m_monitor == null) return;

        PhysicsMaterial desired = m_monitor.HasGroundSupport()
            ? m_groundPhysicsMaterial
            : m_airPhysicsMaterial;
        if (m_playerCapsuleCollider.sharedMaterial != desired)
            m_playerCapsuleCollider.sharedMaterial = desired;
    }

    /// <summary>PlayerのCapsuleColliderを元のPhysicsMaterialに戻します。</summary>
    private void RestoreCapsuleFriction()
    {
        if (m_playerCapsuleCollider != null && m_airPhysicsMaterial != null &&
            m_playerCapsuleCollider.sharedMaterial == m_airPhysicsMaterial)
            m_playerCapsuleCollider.sharedMaterial = m_groundPhysicsMaterial;
    }

    /// <summary>
    /// 滑走と無入力ジャンプを、障害物を確認しながら
    /// 同じ物理更新で移動させます。
    /// </summary>
    /// <param name="targetPosition">
    /// 次の物理ステップで到達する位置。
    /// </param>
    /// <param name="forward">進行方向。</param>
    /// <param name="deltaTime">物理更新時間。</param>
    /// <param name="obstacle">
    /// 経路上で最も近い障害物。
    /// </param>
    /// <returns>
    /// true：移動可能です。
    /// false：障害物があります、または誘導未開始です。
    /// </returns>
    public bool TryMoveAlongRail(
        Vector3 targetPosition,
        Vector3 forward,
        float deltaTime,
        bool isForcedExit,
        out RaycastHit obstacle)
    {
        obstacle = default;

        if (!m_isInitialized ||
            !m_isRailMotionActive ||
            deltaTime <= 0.0f)
        {
            return false;
        }

        Vector3 displacement =
            targetPosition -
            m_playerRigidbody.position;

        float distance =
            displacement.magnitude;

        float nearestDistance =
            float.PositiveInfinity;

        if (distance > Mathf.Epsilon)
        {
            Physics.SyncTransforms();

            RaycastHit[] hits =
                m_playerRigidbody.SweepTestAll(
                    displacement / distance,
                    distance,
                    QueryTriggerInteraction.Ignore);

            foreach (RaycastHit hit in hits)
            {
                Collider hitCollider =
                    hit.collider;

                if (hitCollider == null)
                {
                    continue;
                }

                // 自分自身は無視
                if (hitCollider.attachedRigidbody ==
                    m_playerRigidbody)
                {
                    continue;
                }

                // レール自身は無視
                if (IsRailCollider(hitCollider))
                {
                    continue;
                }
                if (!isForcedExit)
                {
                    continue;
                }

                // Obstacle Layer以外は無視
                if (!IsObstacleLayer(
                    hitCollider.gameObject.layer))
                {
                    continue;
                }

                // 最も近い障害物を採用
                if (hit.distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance =
                    hit.distance;

                obstacle = hit;
            }
        }

        // 障害物が存在する
        if (!float.IsPositiveInfinity(
            nearestDistance))
        {
            float safeDistance =
                Mathf.Max(
                    nearestDistance -
                    m_obstacleSkinWidth,
                    0.0f);

            Vector3 movement =
                displacement.normalized *
                safeDistance;

            m_playerRigidbody.position +=
                movement;

            m_railVelocity =
                movement / deltaTime;

            return false;
        }


        // 障害物がなければ目標位置へ移動
        m_railVelocity =
            displacement / deltaTime;

        m_playerRigidbody.MovePosition(
            targetPosition);

        if (forward.sqrMagnitude >
            DIRECTION_SQR_THRESHOLD)
        {
            m_playerRigidbody.MoveRotation(
                Quaternion.LookRotation(
                    forward,
                    Vector3.up));
        }

        return true;
    }

    /// <summary>レールの接触を、誘導を中断する障害物から除外します。</summary>
    /// <param name="collider">確認するコライダー。</param>
    /// <returns>true：レール。false：レール以外。</returns>
    private bool IsRailCollider(Collider collider)
    {
        int railLayer = LayerMask.NameToLayer("Rail");
        for (Transform target = collider.transform; target != null; target = target.parent)
        {
            if (target.gameObject.layer == railLayer || target.CompareTag("Rail") ||
                target.TryGetComponent<SplineRailInfo>(out _)) return true;
        }
        return false;
    }

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
    /// 既存コードとの互換用です。
    /// </summary>
    /// <param name="playerRigidbody">プレイヤーのRigidbody。</param>
    public void Initialize(
        Rigidbody playerRigidbody)
    {
        Initialize(
            playerRigidbody,
            GetComponent<PlayerMonitor>());
    }

    /// <summary>
    /// PlayerMotorを初期化します。
    /// </summary>
    /// <param name="playerRigidbody">プレイヤーのRigidbody。</param>
    /// <param name="monitor">プレイヤーの周辺環境監視機能。</param>
    public void Initialize(
        Rigidbody playerRigidbody,
        PlayerMonitor monitor)
    {
        ApplyParameterAsset();

        if (playerRigidbody == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerMotor)}] Rigidbodyが指定されていません。",
                this);

            m_isInitialized = false;
            return;
        }

        if (monitor == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerMotor)}] PlayerMonitorが指定されていません。",
                this);

            m_isInitialized = false;
            return;
        }

        m_playerRigidbody =
            playerRigidbody;

        m_monitor =
            monitor;

        if (m_playerCapsuleCollider == null)
        {
            m_playerCapsuleCollider =
                GetComponent<CapsuleCollider>();
        }

        if (m_playerCapsuleCollider == null)
        {
            Debug.LogWarning(
                $"[{nameof(PlayerMotor)}] " +
                "CapsuleColliderが見つからないため段差乗り越えを無効化します。",
                this);
        }
        else if (m_playerCapsuleCollider.direction != 1)
        {
            Debug.LogWarning(
                $"[{nameof(PlayerMotor)}] " +
                "段差乗り越えはY軸方向のCapsuleColliderを前提としています。",
                this);
        }

        if (m_playerCapsuleCollider != null && m_airPhysicsMaterial == null)
        {
            m_groundPhysicsMaterial = m_playerCapsuleCollider.sharedMaterial;
            m_airPhysicsMaterial = new PhysicsMaterial("Player Air Frictionless")
            {
                dynamicFriction = 0.0f,
                staticFriction = 0.0f,
                frictionCombine = PhysicsMaterialCombine.Minimum
            };
        }

        if (!m_playerRigidbody.isKinematic)
        {
            m_playerRigidbody.angularVelocity =
                Vector3.zero;
        }

        m_playerRigidbody.constraints |=
            RigidbodyConstraints.FreezeRotation;

        Vector3 initialVelocity = m_playerRigidbody.linearVelocity;
        m_verticalVelocityBeforePhysics = initialVelocity.y;
        initialVelocity.y = 0.0f;
        m_horizontalSpeedBeforePhysics = initialVelocity.magnitude;

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

        // 本体の向きを決めるための水平移動方向
        Vector3 horizontalMoveDirection =
            CalculateCameraRelativeDirection(
                normalizedInput);

        // 実際の物理移動では接地面に沿った方向を使用
        Vector3 moveDirection =
            CalculateGroundMoveDirection(
                horizontalMoveDirection);

        float inputMagnitude =
            normalizedInput.magnitude;

        float maxMoveSpeed =
            Mathf.Max(
                parameters.MaxMoveSpeed,
                0.0f);

        m_currentMaxMoveSpeed =
            maxMoveSpeed;

        Vector3 targetMoveVelocity =
            moveDirection *
            (maxMoveSpeed * inputMagnitude);

        Vector3 currentMoveVelocity =
            GetMoveVelocity();

        if (ShouldBrakeForReverseInput(
            currentMoveVelocity,
            moveDirection))
        {
            float brakeAcceleration =
                CalculateAcceleration(
                    maxMoveSpeed,
                    parameters.TimeToStop) *
                Mathf.Max(
                    1.0f,
                    parameters.ReverseBrakeMultiplier);

            Vector3 brakingVelocity =
                Vector3.MoveTowards(
                    currentMoveVelocity,
                    Vector3.zero,
                    brakeAcceleration *
                    deltaTime);

            ApplyHorizontalVelocity(
                brakingVelocity,
                deltaTime,
                applyObstacleAvoidance);

            return;
        }

        if (currentMoveVelocity.sqrMagnitude <=
                STOP_SPEED_THRESHOLD *
                STOP_SPEED_THRESHOLD &&
            horizontalMoveDirection.sqrMagnitude >
                DIRECTION_SQR_THRESHOLD)
        {
            Vector3 facingDirection =
                m_playerRigidbody.rotation *
                Vector3.forward;

            facingDirection.y = 0.0f;

            if (Vector3.Angle(
                    facingDirection,
                    horizontalMoveDirection) >
                START_FACING_TOLERANCE)
            {
                ApplyHorizontalVelocity(
                    Vector3.zero,
                    deltaTime,
                    applyObstacleAvoidance);

                RotateTowardsMoveDirection(
                    horizontalMoveDirection,
                    parameters.RotationSpeed,
                    deltaTime);

                return;
            }
        }

        float acceleration =
            CalculateAcceleration(
                maxMoveSpeed,
                parameters.TimeToMaxSpeed);

        Vector3 nextMoveVelocity =
            Vector3.MoveTowards(
                currentMoveVelocity,
                targetMoveVelocity,
                acceleration * deltaTime);

        ApplyHorizontalVelocity(
            nextMoveVelocity,
            deltaTime,
            applyObstacleAvoidance);

        // 本体の向きは水平面上だけで回転させる
        RotateTowardsMoveDirection(
            horizontalMoveDirection,
            parameters.RotationSpeed,
            deltaTime);
    }

    /// <summary>
    /// 現在の進行方向に対して入力が逆向きか判定します。
    /// </summary>
    /// <param name="horizontalVelocity">現在の水平速度。</param>
    /// <param name="inputDirection">カメラ基準で変換した入力方向。</param>
    /// <returns>true：移動中に90度を超える逆入力。false：停止中、無入力、または順方向。</returns>
    private bool ShouldBrakeForReverseInput(Vector3 horizontalVelocity, Vector3 inputDirection)
    {
        return horizontalVelocity.sqrMagnitude > STOP_SPEED_THRESHOLD * STOP_SPEED_THRESHOLD &&
            inputDirection.sqrMagnitude > DIRECTION_SQR_THRESHOLD &&
            Vector3.Dot(horizontalVelocity, inputDirection) < 0.0f;
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

        m_currentMaxMoveSpeed =
            maxMoveSpeed;

        Vector3 currentMoveVelocity =
            GetMoveVelocity();

        float deceleration =
            CalculateAcceleration(
                maxMoveSpeed,
                parameters.TimeToStop);

        Vector3 nextMoveVelocity =
            Vector3.MoveTowards(
                currentMoveVelocity,
                Vector3.zero,
                deceleration * deltaTime);

        ApplyHorizontalVelocity(
            nextMoveVelocity,
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

        RegisterIntentionalUpwardVelocity(nextVelocity.y);
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
    /// 水平移動方向を現在の接地面に沿った方向へ変換します。
    /// </summary>
    /// <param name="horizontalDirection">水平面上の移動方向。</param>
    /// <returns>接地面に沿った移動方向。</returns>
    private Vector3 CalculateGroundMoveDirection(
        Vector3 horizontalDirection)
    {
        if (!CanUseGroundSurface())
        {
            horizontalDirection.y = 0.0f;
            return horizontalDirection;
        }

        Vector3 groundDirection =
            Vector3.ProjectOnPlane(
                horizontalDirection,
                m_monitor.GroundNormal);

        if (groundDirection.sqrMagnitude <=
            DIRECTION_SQR_THRESHOLD)
        {
            return horizontalDirection;
        }

        return groundDirection.normalized;
    }

    /// <summary>
    /// 接地面を移動計算へ利用できるか確認します。
    /// </summary>
    private bool CanUseGroundSurface()
    {
        return m_monitor != null &&
               m_monitor.IsGrounded &&
               m_monitor.HasGroundSurface;
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
    /// 現在の移動面に沿った速度を取得します。
    /// </summary>
    /// <returns>
    /// 接地中は接地面の接線速度、非接地中は水平速度。
    /// </returns>
    private Vector3 GetMoveVelocity()
    {
        Vector3 velocity =
            m_playerRigidbody.linearVelocity;

        if (CanUseGroundSurface())
        {
            return Vector3.ProjectOnPlane(
                velocity,
                m_monitor.GroundNormal);
        }

        velocity.y = 0.0f;

        return velocity;
    }

    /// <summary>
    /// 移動速度をRigidbodyへ適用します。
    /// 接地中は速度を接地面へ沿わせ、
    /// 障害物検知を行う場合は進行方向の障害物を考慮します。
    /// </summary>
    /// <param name="horizontalVelocity">
    /// 適用したい移動速度。
    /// 呼び出し元が水平速度を渡した場合も接地中は斜面へ沿わせます。
    /// </param>
    /// <param name="deltaTime">この速度が適用される時間幅。</param>
    /// <param name="applyObstacleAvoidance">
    /// 障害物検知による減速を適用するかどうか。
    /// </param>
    private void ApplyHorizontalVelocity(
        Vector3 horizontalVelocity,
        float deltaTime,
        bool applyObstacleAvoidance = true)
    {
        Vector3 alignedMoveVelocity =
            AlignVelocityToGround(
                horizontalVelocity);

        bool didStepUp = false;

        if (applyObstacleAvoidance)
        {
            didStepUp =
                TryStepUp(
                    alignedMoveVelocity,
                    deltaTime);
        }

        Vector3 nextMoveVelocity =
            applyObstacleAvoidance &&
            !didStepUp
                ? ClampVelocityForObstacles(
                    alignedMoveVelocity,
                    deltaTime)
                : alignedMoveVelocity;

        Vector3 currentVelocity =
            m_playerRigidbody.linearVelocity;

        if (CanUseGroundSurface())
        {
            m_wasMovingAlongGround = true;
            Vector3 groundNormal =
                m_monitor.GroundNormal;

            // 重力や接触反力による法線方向速度は維持し、
            // 接線方向だけを移動制御する。
            Vector3 normalVelocity =
                Vector3.Project(
                    currentVelocity,
                    groundNormal);

            m_playerRigidbody.linearVelocity =
                nextMoveVelocity +
                normalVelocity;

            return;
        }

        // 坂に沿った移動で得た上向き速度を、接地を離れた後のジャンプへ持ち越さない。
        // 入力によるジャンプや吹き飛ばしで与えた上昇速度は維持します。
        nextMoveVelocity.y =
            m_wasMovingAlongGround &&
            currentVelocity.y > 0.0f &&
            !WasIntentionalUpwardVelocitySetThisStep()
                ? 0.0f
                : currentVelocity.y;
        m_wasMovingAlongGround = false;

        m_playerRigidbody.linearVelocity =
            nextMoveVelocity;
    }

    /// <summary>
    /// 指定速度を現在の接地面へ沿わせます。
    /// 速度の大きさは維持します。
    /// </summary>
    /// <param name="velocity">変換する速度。</param>
    /// <returns>接地面へ沿わせた速度。</returns>
    private Vector3 AlignVelocityToGround(
        Vector3 velocity)
    {
        if (!CanUseGroundSurface())
        {
            velocity.y = 0.0f;
            return velocity;
        }

        float speed =
            velocity.magnitude;

        if (speed <=
            DIRECTION_SQR_THRESHOLD)
        {
            return Vector3.zero;
        }

        Vector3 groundVelocity =
            Vector3.ProjectOnPlane(
                velocity,
                m_monitor.GroundNormal);

        if (groundVelocity.sqrMagnitude <=
            DIRECTION_SQR_THRESHOLD)
        {
            return Vector3.zero;
        }

        return groundVelocity.normalized *
               speed;
    }

    /// <summary>
    /// 段差判定に使用するLayerMaskを取得します。
    /// GroundとObstacleの両方を対象にします。
    /// </summary>
    private int GetStepDetectionLayerMask()
    {
        int groundLayerMask =
            m_monitor != null
                ? m_monitor.GroundLayerMask.value
                : 0;

        return m_obstacleLayerMask.value |
               groundLayerMask;
    }

    /// <summary>
    /// 進行方向に乗り越え可能な小段差がある場合、
    /// Rigidbodyを段差上面まで持ち上げます。
    /// </summary>
    /// <param name="moveVelocity">現在要求されている移動速度。</param>
    /// <returns>true：段差を乗り越えた。false：段差ではない、または乗り越え不可。</returns>
    private bool TryStepUp(
        Vector3 moveVelocity,
        float deltaTime)
    {
        // ParameterAssetはInitialize時だけではなく、
        // Play中の調整も即座に反映できるようStep判定直前にも同期します。
        ApplyStepParameterAsset();

        if (!CanUseGroundSurface())
        {
            return FailStep("接地面を使用できません。");
        }

        if (m_isRailMotionActive)
        {
            return FailStep("レール移動中です。");
        }

        if (m_playerCapsuleCollider == null)
        {
            return FailStep("CapsuleColliderがありません。");
        }

        if (m_playerCapsuleCollider.direction != 1)
        {
            return FailStep("CapsuleColliderがY軸方向ではありません。");
        }

        if (m_maxStepHeight <= MIN_STEP_HEIGHT)
        {
            return FailStep("Max Step Heightが小さすぎます。");
        }

        if (m_stepCheckDistance <= 0.0f)
        {
            return FailStep("Step Check Distanceが0以下です。");
        }

        if (deltaTime <= 0.0f)
        {
            return FailStep("deltaTimeが0以下です。");
        }

        Vector3 horizontalDirection =
            Vector3.ProjectOnPlane(
                moveVelocity,
                Vector3.up);

        float horizontalSpeed =
            horizontalDirection.magnitude;

        if (horizontalSpeed <=
            OBSTACLE_CHECK_SPEED_THRESHOLD)
        {
            return FailStep("移動速度が不足しています。");
        }

        horizontalDirection /=
            horizontalSpeed;

        if (!TryGetCapsuleWorldGeometry(
            out Vector3 topSphereCenter,
            out Vector3 bottomSphereCenter,
            out float capsuleRadius))
        {
            return FailStep("CapsuleCollider形状を取得できません。");
        }

        if (!TryGetCurrentStepSupportPoint(
            bottomSphereCenter,
            capsuleRadius,
            out Vector3 currentSupportPoint))
        {
            return FailStep("現在立っている支持面を取得できません。");
        }

        float currentGroundHeight =
            currentSupportPoint.y;

        Vector3 groundReferencePosition =
            new Vector3(
                bottomSphereCenter.x,
                currentGroundHeight,
                bottomSphereCenter.z);

        float lowerCheckHeight =
            Mathf.Clamp(
                m_stepLowerCheckHeight,
                MIN_STEP_HEIGHT,
                m_maxStepHeight);

        float travelDistance =
            horizontalSpeed *
            deltaTime;

        float lowerCheckDistance =
            capsuleRadius +
            Mathf.Min(
                m_stepCheckDistance,
                travelDistance +
                m_obstacleSkinWidth +
                m_stepClearanceSkin);

        if (lowerCheckDistance <=
            capsuleRadius)
        {
            return FailStep("下部判定距離がCapsule半径以下です。");
        }

        if (!TryGetNearestStepWallHit(
            groundReferencePosition,
            horizontalDirection,
            lowerCheckDistance,
            lowerCheckHeight,
            out RaycastHit lowerHit))
        {
            return FailStep("LOWER_HIT_FAILED: 段差正面を検出できません。");
        }

        float distanceFromCapsuleSurface =
            lowerHit.distance -
            capsuleRadius;

        if (distanceFromCapsuleSurface >
            m_stepCheckDistance +
            m_stepClearanceSkin)
        {
            return FailStep(
                $"LOWER_TOO_FAR: Capsule表面から{distanceFromCapsuleSurface:F3}m先です。");
        }

        Vector3 upperOrigin =
            groundReferencePosition +
            Vector3.up *
            (m_maxStepHeight +
             m_stepClearanceSkin);

        float upperCheckDistance =
            lowerHit.distance +
            m_stepForwardOffset +
            m_stepClearanceSkin;

        if (TryGetNearestStepHit(
            upperOrigin,
            horizontalDirection,
            upperCheckDistance,
            out RaycastHit upperHit))
        {
            return FailStep(
                $"UPPER_BLOCKED: {upperHit.collider.name} が上部を塞いでいます。");
        }

        if (!TryFindStepLandingSurface(
            lowerHit.point,
            horizontalDirection,
            currentGroundHeight,
            out RaycastHit landingHit,
            out _))
        {
            return FailStep("LANDING_FAILED: 登れる上面を検出できません。");
        }

        float stepHeight =
            landingHit.point.y -
            currentGroundHeight;

        if (stepHeight <= MIN_STEP_HEIGHT)
        {
            return FailStep(
                $"STEP_TOO_LOW: 段差高さ={stepHeight:F3}m");
        }

        if (stepHeight >
            m_maxStepHeight +
            m_stepClearanceSkin)
        {
            return FailStep(
                $"STEP_TOO_HIGH: 段差高さ={stepHeight:F3}m / 最大={m_maxStepHeight:F3}m");
        }

        // landingHit.pointはワールド座標です。
        // 壁面からのsampleDistanceだけではなく、現在位置からLanding地点までを使います。
        Vector3 horizontalToLanding =
            Vector3.ProjectOnPlane(
                landingHit.point -
                groundReferencePosition,
                Vector3.up);

        float forwardStepDistance =
            Mathf.Max(
                Vector3.Dot(
                    horizontalToLanding,
                    horizontalDirection) +
                m_stepClearanceSkin,
                0.0f);

        Vector3 stepOffset =
            Vector3.up *
            (stepHeight +
             m_stepClearanceSkin) +
            horizontalDirection *
            forwardStepDistance;

        if (!HasStepDestinationClearance(
            topSphereCenter,
            bottomSphereCenter,
            capsuleRadius,
            stepOffset))
        {
            return FailStep("CLEARANCE_FAILED: 移動先でCapsuleが他Colliderと重なります。");
        }

        m_playerRigidbody.position +=
            stepOffset;

        SetStepDebugReason(
            $"SUCCESS: height={stepHeight:F3}m, forward={forwardStepDistance:F3}m, " +
            $"landing={landingHit.collider.name}");

        return true;
    }

    /// <summary>
    /// 地面付近を複数高さで確認し、最も近い段差正面を取得します。
    /// </summary>
    /// <param name="groundReferencePosition">現在床の基準位置。</param>
    /// <param name="direction">確認する進行方向。</param>
    /// <param name="distance">確認距離。</param>
    /// <param name="maxCheckHeight">下部判定の最大高さ。</param>
    /// <param name="nearestHit">最も近い段差正面。</param>
    /// <returns>
    /// true：段差正面を検出しました。
    /// false：段差正面はありません。
    /// </returns>
    private bool TryGetNearestStepWallHit(
        Vector3 groundReferencePosition,
        Vector3 direction,
        float distance,
        float maxCheckHeight,
        out RaycastHit nearestHit)
    {
        nearestHit = default;

        if (distance <= 0.0f ||
            direction.sqrMagnitude <=
            DIRECTION_SQR_THRESHOLD)
        {
            return false;
        }

        direction.Normalize();

        float safeMaxCheckHeight =
            Mathf.Max(
                maxCheckHeight,
                MIN_STEP_WALL_CHECK_HEIGHT);

        float nearestDistance =
            float.PositiveInfinity;

        bool hasHit =
            false;

        for (int rayIndex = 0;
             rayIndex < STEP_WALL_CHECK_RAY_COUNT;
             rayIndex++)
        {
            float heightRate =
                STEP_WALL_CHECK_RAY_COUNT <= 1
                    ? 0.0f
                    : (float)rayIndex /
                      (STEP_WALL_CHECK_RAY_COUNT - 1);

            float checkHeight =
                Mathf.Lerp(
                    MIN_STEP_WALL_CHECK_HEIGHT,
                    safeMaxCheckHeight,
                    heightRate);

            Vector3 origin =
                groundReferencePosition +
                Vector3.up * checkHeight;

            int hitCount =
                Physics.RaycastNonAlloc(
                    origin,
                    direction,
                    m_stepRaycastBuffer,
                    distance,
                    GetStepDetectionLayerMask(),
                    QueryTriggerInteraction.Ignore);

            for (int hitIndex = 0;
                 hitIndex < hitCount;
                 hitIndex++)
            {
                RaycastHit hit =
                    m_stepRaycastBuffer[hitIndex];

                Collider hitCollider =
                    hit.collider;

                if (hitCollider == null)
                {
                    continue;
                }

                // 自分自身に属するColliderは無視する。
                if (hitCollider.attachedRigidbody ==
                    m_playerRigidbody)
                {
                    continue;
                }

                // 通常移動で登れる面は坂処理へ任せる。
                // 登れない急斜面は、垂直壁と同様にStep Up候補として扱う。
                if (m_monitor != null &&
                    m_monitor.IsClimbableGroundSurface(
                        hit.normal))
                {
                    continue;
                }

                if (hit.distance >=
                    nearestDistance)
                {
                    continue;
                }

                nearestDistance =
                    hit.distance;

                nearestHit =
                    hit;

                hasHit =
                    true;
            }
        }

        return hasHit;
    }

    /// <summary>
    /// 段差正面から前方へ複数地点を調べ、最初に見つかった登坂可能な上面を取得します。
    /// </summary>
    /// <param name="stepWallPoint">段差正面のHit位置。</param>
    /// <param name="direction">進行方向。</param>
    /// <param name="currentGroundHeight">現在立っている支持面の高さ。</param>
    /// <param name="landingHit">採用した段差上面。</param>
    /// <param name="forwardStepDistance">段差を越えるために前進させる距離。</param>
    /// <returns>true：登坂可能な上面を検出した。false：有効な上面がない。</returns>
    private bool TryFindStepLandingSurface(
        Vector3 stepWallPoint,
        Vector3 direction,
        float currentGroundHeight,
        out RaycastHit landingHit,
        out float forwardStepDistance)
    {
        landingHit = default;
        forwardStepDistance = 0.0f;

        if (m_monitor == null ||
            direction.sqrMagnitude <=
            DIRECTION_SQR_THRESHOLD)
        {
            return false;
        }

        direction.Normalize();

        float sampleInterval =
            Mathf.Max(
                m_stepForwardOffset,
                m_stepClearanceSkin,
                MIN_STEP_HEIGHT);

        float maxSearchDistance =
            Mathf.Max(
                m_stepCheckDistance,
                sampleInterval);

        float landingProbeHeight =
            currentGroundHeight +
            m_maxStepHeight +
            m_stepClearanceSkin;

        float landingProbeDistance =
            m_maxStepHeight +
            m_stepClearanceSkin * 2.0f;

        int sampleCount =
            Mathf.Max(
                Mathf.CeilToInt(
                    maxSearchDistance /
                    sampleInterval),
                1);

        for (int sampleIndex = 1;
             sampleIndex <= sampleCount;
             sampleIndex++)
        {
            float sampleDistance =
                Mathf.Min(
                    sampleInterval * sampleIndex,
                    maxSearchDistance);

            Vector3 horizontalPosition =
                stepWallPoint +
                direction * sampleDistance;

            Vector3 probeOrigin =
                new Vector3(
                    horizontalPosition.x,
                    landingProbeHeight,
                    horizontalPosition.z);

            if (!TryGetNearestStepHit(
                probeOrigin,
                Vector3.down,
                landingProbeDistance,
                out RaycastHit candidateHit))
            {
                continue;
            }

            // 急斜面を拾った場合は、さらに奥の上面を探索する。
            if (!m_monitor.IsClimbableGroundSurface(
                candidateHit.normal))
            {
                continue;
            }

            float stepHeight =
                candidateHit.point.y -
                currentGroundHeight;

            if (stepHeight <= MIN_STEP_HEIGHT ||
                stepHeight >
                m_maxStepHeight +
                m_stepClearanceSkin)
            {
                continue;
            }

            landingHit =
                candidateHit;

            // 呼び出し側でワールド座標から正確な前進量を再計算します。
            forwardStepDistance =
                sampleDistance;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Step Upの基準に使用する、現在のプレイヤー直下の支持面を取得します。
    /// Ground Probeが前方の段差上面を先に検出しても、現在床の高さを維持できます。
    /// </summary>
    /// <param name="bottomSphereCenter">CapsuleCollider下側球の中心。</param>
    /// <param name="capsuleRadius">CapsuleColliderのワールド半径。</param>
    /// <param name="supportPoint">現在の支持面上の点。</param>
    /// <returns>true：支持面を取得できた。false：取得できなかった。</returns>
    private bool TryGetCurrentStepSupportPoint(
        Vector3 bottomSphereCenter,
        float capsuleRadius,
        out Vector3 supportPoint)
    {
        supportPoint = Vector3.zero;

        if (m_monitor == null)
        {
            return false;
        }

        // プレイヤー中心直下だけを見ることで、
        // 前方にある段差上面を現在床として誤採用しない。
        Vector3 origin =
            bottomSphereCenter;

        float probeDistance =
            Mathf.Max(
                capsuleRadius +
                m_stepClearanceSkin +
                m_stepCheckDistance,
                0.5f);

        int hitCount =
            Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                m_stepRaycastBuffer,
                probeDistance,
                m_monitor.GroundLayerMask,
                QueryTriggerInteraction.Ignore);

        float nearestDistance =
            float.PositiveInfinity;

        bool hasHit =
            false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit =
                m_stepRaycastBuffer[i];

            Collider hitCollider =
                hit.collider;

            if (hitCollider == null)
            {
                continue;
            }

            if (hitCollider.attachedRigidbody ==
                m_playerRigidbody)
            {
                continue;
            }

            if (!m_monitor.IsClimbableGroundSurface(
                hit.normal))
            {
                continue;
            }

            if (hit.distance >=
                nearestDistance)
            {
                continue;
            }

            nearestDistance =
                hit.distance;

            supportPoint =
                hit.point;

            hasHit =
                true;
        }

        return hasHit;
    }

    /// <summary>
    /// 段差判定用Raycastから、自身のColliderを除外して最も近いHitを取得します。
    /// </summary>
    private bool TryGetNearestStepHit(
        Vector3 origin,
        Vector3 direction,
        float distance,
        out RaycastHit nearestHit)
    {
        nearestHit = default;

        if (distance <= 0.0f ||
            direction.sqrMagnitude <=
            DIRECTION_SQR_THRESHOLD)
        {
            return false;
        }

        direction.Normalize();

        int hitCount =
            Physics.RaycastNonAlloc(
                origin,
                direction,
                m_stepRaycastBuffer,
                distance,
                GetStepDetectionLayerMask(),
                QueryTriggerInteraction.Ignore);

        float nearestDistance =
            float.PositiveInfinity;

        bool hasHit =
            false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit =
                m_stepRaycastBuffer[i];

            Collider hitCollider =
                hit.collider;

            if (hitCollider == null)
            {
                continue;
            }

            if (hitCollider.attachedRigidbody ==
                m_playerRigidbody)
            {
                continue;
            }

            if (hit.distance >=
                nearestDistance)
            {
                continue;
            }

            nearestDistance =
                hit.distance;

            nearestHit =
                hit;

            hasHit =
                true;
        }

        return hasHit;
    }

    /// <summary>
    /// CapsuleColliderのワールド空間形状を取得します。
    /// </summary>
    /// <param name="topSphereCenter">上側球の中心。</param>
    /// <param name="bottomSphereCenter">下側球の中心。</param>
    /// <param name="radius">ワールド空間の半径。</param>
    /// <returns>true：取得成功。false：Y軸CapsuleColliderではない。</returns>
    private bool TryGetCapsuleWorldGeometry(
        out Vector3 topSphereCenter,
        out Vector3 bottomSphereCenter,
        out float radius)
    {
        topSphereCenter = Vector3.zero;
        bottomSphereCenter = Vector3.zero;
        radius = 0.0f;

        if (m_playerCapsuleCollider == null ||
            m_playerCapsuleCollider.direction != 1)
        {
            return false;
        }

        Transform capsuleTransform =
            m_playerCapsuleCollider.transform;

        Vector3 lossyScale =
            capsuleTransform.lossyScale;

        float horizontalScale =
            Mathf.Max(
                Mathf.Abs(lossyScale.x),
                Mathf.Abs(lossyScale.z));

        float verticalScale =
            Mathf.Abs(lossyScale.y);

        radius =
            m_playerCapsuleCollider.radius *
            horizontalScale;

        float height =
            Mathf.Max(
                m_playerCapsuleCollider.height *
                verticalScale,
                radius * 2.0f);

        float sphereCenterOffset =
            Mathf.Max(
                height * 0.5f - radius,
                0.0f);

        Vector3 worldCenter =
            capsuleTransform.TransformPoint(
                m_playerCapsuleCollider.center);

        topSphereCenter =
            worldCenter +
            Vector3.up * sphereCenterOffset;

        bottomSphereCenter =
            worldCenter -
            Vector3.up * sphereCenterOffset;

        return true;
    }

    /// <summary>
    /// 段差上へ移動したCapsuleColliderが他Colliderへ重ならないか確認します。
    /// </summary>
    /// <param name="topSphereCenter">現在の上側球中心。</param>
    /// <param name="bottomSphereCenter">現在の下側球中心。</param>
    /// <param name="capsuleRadius">Capsule半径。</param>
    /// <param name="stepOffset">段差上へ持ち上げる量。</param>
    /// <returns>true：移動先に十分な空間がある。false：他Colliderと重なる。</returns>
    private bool HasStepDestinationClearance(
        Vector3 topSphereCenter,
        Vector3 bottomSphereCenter,
        float capsuleRadius,
        Vector3 stepOffset)
    {
        float clearanceRadius =
            Mathf.Max(
                capsuleRadius -
                m_stepClearanceSkin,
                0.01f);

        int hitCount =
            Physics.OverlapCapsuleNonAlloc(
                bottomSphereCenter + stepOffset,
                topSphereCenter + stepOffset,
                clearanceRadius,
                m_stepClearanceBuffer,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider =
                m_stepClearanceBuffer[i];

            if (hitCollider == null)
            {
                continue;
            }

            if (hitCollider ==
                    m_playerCapsuleCollider ||
                hitCollider.attachedRigidbody ==
                    m_playerRigidbody)
            {
                continue;
            }

            return false;
        }

        return true;
    }

    /// <summary>
    /// 進行方向に障害物がある場合、
    /// 実際の現在速度を基準に緩やかに減速させます。
    /// </summary>
    /// <param name="horizontalVelocity">
    /// 要求されている目標水平速度。
    /// </param>
    /// <param name="deltaTime">
    /// この速度が適用される時間幅。
    /// </param>
    /// <returns>
    /// 障害物を考慮した水平速度。
    /// </returns>
    private Vector3 ClampVelocityForObstacles(
        Vector3 horizontalVelocity,
        float deltaTime)
    {
        float requestedSpeed =
            horizontalVelocity.magnitude;

        if (requestedSpeed <=
                OBSTACLE_CHECK_SPEED_THRESHOLD ||
            deltaTime <= 0.0f)
        {
            return horizontalVelocity;
        }

        Vector3 direction =
            horizontalVelocity / requestedSpeed;

        // この物理ステップで進もうとしている距離
        float travelDistance =
            requestedSpeed * deltaTime;

        // 進行方向上に存在するColliderを取得
        RaycastHit[] hits =
            m_playerRigidbody.SweepTestAll(
                direction,
                travelDistance,
                QueryTriggerInteraction.Ignore);

        float nearestObstacleDistance =
            float.PositiveInfinity;

        foreach (RaycastHit hit in hits)
        {
            Collider hitCollider =
                hit.collider;

            if (hitCollider == null)
            {
                continue;
            }

            // 自分自身に属するColliderは無視
            if (hitCollider.attachedRigidbody ==
                m_playerRigidbody)
            {
                continue;
            }

            // Obstacle Layer以外は無視
            if (!IsObstacleLayer(
                hitCollider.gameObject.layer))
            {
                continue;
            }

            // 最も近い障害物だけを使用
            if (hit.distance >=
                nearestObstacleDistance)
            {
                continue;
            }

            nearestObstacleDistance =
                hit.distance;
        }

        // 障害物が存在しない
        if (float.IsPositiveInfinity(
            nearestObstacleDistance))
        {
            return horizontalVelocity;
        }

        // 障害物の少し手前まで進行可能
        float allowedDistance =
            Mathf.Max(
                nearestObstacleDistance -
                m_obstacleSkinWidth,
                0.0f);

        float allowedSpeedThisStep =
            allowedDistance / deltaTime;

        // 現在の実際の水平速度
        float currentActualSpeed =
            GetMoveVelocity().magnitude;

        float decelerationThisStep =
            m_obstacleDecelerationPerSecond *
            deltaTime;

        // 障害物へ接近した場合は徐々に減速
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
    /// プレイヤーの水平移動が停止しているか取得します。
    /// </summary>
    public bool IsHorizontallyStopped =>
        HorizontalSpeed <= STOP_SPEED_THRESHOLD;

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
    bool applyObstacleAvoidance = true,
    bool rotatePlayer = true)
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

        if (rotatePlayer)
        {
            RotateTowardsMoveDirection(
                facingDirection,
                facingRotationSpeed,
                deltaTime);
        }

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
    /// <summary>攻撃中心から離れる水平速度と上向き速度を適用します。</summary>
    public void ApplyKnockback(Vector3 attackCenter, float speed, float liftSpeed)
    {
        if (!m_isInitialized) return;

        Vector3 direction = Vector3.ProjectOnPlane(m_playerRigidbody.position - attackCenter, Vector3.up);
        if (direction.sqrMagnitude <= DIRECTION_SQR_THRESHOLD)
        {
            direction = Vector3.ProjectOnPlane(-transform.forward, Vector3.up);
        }
        if (direction.sqrMagnitude <= DIRECTION_SQR_THRESHOLD)
        {
            direction = Vector3.back;
        }
        Vector3 knockbackVelocity = direction.normalized * Mathf.Max(0.0f, speed)
            + Vector3.up * Mathf.Max(0.0f, liftSpeed);
        RegisterIntentionalUpwardVelocity(knockbackVelocity.y);
        m_playerRigidbody.linearVelocity = knockbackVelocity;
    }

    /// <summary>吹き飛び速度を解除します。以降の落下には通常の重力を使用します。</summary>
    public void StopKnockback()
    {
        if (!m_isInitialized) return;
        m_playerRigidbody.linearVelocity = Vector3.zero;
    }

    /// <summary>
    /// 指定したLayerが障害物として設定されているか確認します。
    /// </summary>
    /// <param name="layer">確認するLayer。</param>
    /// <returns>
    /// true：障害物Layerです。
    /// false：障害物Layerではありません。
    /// </returns>
    private bool IsObstacleLayer(int layer)
    {
        int layerMask = 1 << layer;

        return (m_obstacleLayerMask.value & layerMask) != 0;
    }
    /// <summary>
    /// Step Upで使用するParameterAssetの値を実行時キャッシュへ同期します。
    /// Play中のInspector調整も次のStep判定から反映されます。
    /// </summary>
    private void ApplyStepParameterAsset()
    {
        if (m_parameterAsset == null)
        {
            return;
        }

        m_maxStepHeight =
            m_parameterAsset.MaxStepHeight;

        m_stepCheckDistance =
            m_parameterAsset.StepCheckDistance;

        m_stepLowerCheckHeight =
            m_parameterAsset.StepLowerCheckHeight;

        m_stepForwardOffset =
            m_parameterAsset.StepForwardOffset;

        m_stepClearanceSkin =
            m_parameterAsset.StepClearanceSkin;
    }

    /// <summary>
    /// Step Up失敗理由を記録してfalseを返します。
    /// </summary>
    private bool FailStep(string reason)
    {
        SetStepDebugReason(reason);
        return false;
    }

    /// <summary>
    /// Step Up診断情報を更新します。
    /// </summary>
    private void SetStepDebugReason(string reason)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        m_stepDebugReason =
            reason;
#endif
    }

    /// <summary>Applies the assigned parameter asset.</summary>
    private void ApplyParameterAsset()
    {
        if (m_parameterAsset == null) return;
        m_obstacleLayerMask = m_parameterAsset.ObstacleLayerMask;
        m_obstacleSkinWidth = m_parameterAsset.ObstacleSkinWidth;
        m_obstacleDecelerationPerSecond = m_parameterAsset.ObstacleDecelerationPerSecond;
        m_collisionLiftPreventionMinSpeed = m_parameterAsset.CollisionLiftPreventionMinSpeed;
        m_maxStepHeight = m_parameterAsset.MaxStepHeight;
        m_stepCheckDistance = m_parameterAsset.StepCheckDistance;
        m_stepLowerCheckHeight = m_parameterAsset.StepLowerCheckHeight;
        m_stepForwardOffset = m_parameterAsset.StepForwardOffset;
        m_stepClearanceSkin = m_parameterAsset.StepClearanceSkin;
    }

}
