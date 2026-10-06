using UnityEngine;

/// <summary>
/// プレイヤーのステートマシンを管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerStateMachineComponent : MonoBehaviour
{
    [SerializeField, Header("レールジャンプ")]
    private PlayerRailJumpParameterAsset m_railJumpParameterAsset;

    // 既存のシーンで保存された設定を、アセット未設定時の互換値として保持します。
    [SerializeField, HideInInspector]
    private PlayerRailJumpParameters m_railJumpParameters = new PlayerRailJumpParameters();

    private System.Action m_pendingRailStateChange;

    /// <summary>レールジャンプ専用の調整値を取得します。</summary>
    public PlayerRailJumpParameters RailJumpParameters =>
        m_railJumpParameterAsset != null ? m_railJumpParameterAsset.Parameters :
            m_railJumpParameters ?? (m_railJumpParameters = new PlayerRailJumpParameters());

    /// <summary>レール関連の状態変更を、次の物理更新の先頭で実行します。</summary>
    /// <typeparam name="TState">切り替え先の状態。</typeparam>
    /// <param name="args">状態の生成に渡す引数。</param>
    public void RequestRailStateChange<TState>(params object[] args)
        where TState : StateBase<PlayerStateMachineComponent>
    {
        if (!m_isInitialized || m_pendingRailStateChange != null) return;
        StateBase<PlayerStateMachineComponent> sourceState = m_stateMachine.GetNowState();
        m_pendingRailStateChange = () =>
        {
            // 被弾などで状態が変わっていた場合、古いジャンプや着地の予約を破棄します。
            if (m_stateMachine.GetNowState() != sourceState) return;
            // 入力予約後の衝突で滑走が終わっていたら、追加のジャンプ初速を与えません。
            if (typeof(TState) == typeof(PlayerRailJumpingState) && !m_splineGrindController.IsGrinding) return;
            m_stateMachine.ChangeState<TState>(args);
            m_stateMachine.Update(0.0f);
        };
    }

    // ============================================================
    // 参照
    // ============================================================

    // プレイヤー入力
    private PlayerInputReader m_inputReader;

    // プレイヤー監視機能
    private PlayerMonitor m_monitor;

    // プレイヤー移動機能
    private PlayerMotor m_motor;

    // プレイヤーアニメーション表示機能
    private PlayerAnimationPresenter m_animationPresenter;

    // プレイヤー攻撃機能
    private PlayerAttackController m_attackController;

    // スプライングラインド機能
    private SplineGrindController m_splineGrindController;

    // Vゲージ表示機能
    private VGaugeUI m_vGaugeUI;

    // 速度表示機能
    private VSpeedUI m_vSpeedUI;

    // ParameterModel上の共有データ。UIがなくても更新します。
    private VGaugePlaceModel m_vGaugePlaceModel;
    private SpeedPlaceModel m_speedPlaceModel;

    /// <summary>Vゲージの値を管理するモデルを取得します。</summary>
    public VGaugePlaceModel VGaugePlaceModel => m_vGaugePlaceModel;

    /// <summary>毎フレーム更新する速度モデルを取得します。</summary>
    public SpeedPlaceModel SpeedPlaceModel => m_speedPlaceModel;

    // プレイヤーカメラ機能
    private PlayerCamera m_playerCamera;

    // 見た目のモデルオブジェクト
    // （トップ階層とは独立して見た目の向きだけを制御したい場合に使用）
    private Transform m_modelTransform;

    // 通常移動パラメータ
    private PlayerMovementParameterAsset
        m_movementParameterAsset;

    // Vブースト移動パラメータ
    private PlayerVBoostMovementParameterAsset
        m_vBoostMovementParameterAsset;

    // ブーストチャージパラメータ
    private PlayerBoostChargingParameterAsset
        m_boostChargingParameterAsset;

    // プレイヤー用ステートマシン
    private StateMachine<PlayerStateMachineComponent>
        m_stateMachine;

    // 初期化されているか
    private bool m_isInitialized;

    /// <summary>
    /// プレイヤー入力を取得します。
    /// </summary>
    public PlayerInputReader InputReader =>
        m_inputReader;

    /// <summary>
    /// プレイヤー監視機能を取得します。
    /// </summary>
    public PlayerMonitor Monitor =>
        m_monitor;

    /// <summary>
    /// プレイヤー移動機能を取得します。
    /// </summary>
    public PlayerMotor Motor =>
        m_motor;

    /// <summary>
    /// プレイヤー攻撃機能を取得します。
    /// </summary>
    public PlayerAttackController AttackController =>
        m_attackController;

    /// <summary>
    /// プレイヤーアニメーション表示機能を取得します。
    /// </summary>
    public PlayerAnimationPresenter AnimationPresenter =>
        m_animationPresenter;

    /// <summary>
    /// スプライングラインド機能を取得します。
    /// </summary>
    public SplineGrindController GrindController =>
        m_splineGrindController;

    /// <summary>
    /// Vゲージ表示機能を取得します。
    /// シーンに配置されていない場合はnullを返すことがあります。
    /// </summary>
    public VGaugeUI VGaugeUI =>
        m_vGaugeUI;

    /// <summary>
    /// 速度表示機能を取得します。
    /// シーンに配置されていない場合はnullを返すことがあります。
    /// </summary>
    public VSpeedUI VSpeedUI =>
        m_vSpeedUI;

    /// <summary>
    /// プレイヤーカメラ機能を取得します。
    /// </summary>
    public PlayerCamera PlayerCamera =>
        m_playerCamera;

    /// <summary>
    /// 見た目のモデルオブジェクトを取得します。
    /// 未設定の場合はnullを返すことがあります。
    /// </summary>
    public Transform ModelTransform =>
        m_modelTransform;

    /// <summary>
    /// 通常移動パラメータを取得します。
    /// </summary>
    public PlayerMovementParameterAsset MovementParameterAsset =>
        m_movementParameterAsset;

    /// <summary>
    /// Vブースト移動パラメータを取得します。
    /// </summary>
    public PlayerVBoostMovementParameterAsset
        VBoostMovementParameterAsset =>
            m_vBoostMovementParameterAsset;

    /// <summary>
    /// ブーストチャージパラメータを取得します。
    /// </summary>
    public PlayerBoostChargingParameterAsset
        BoostChargingParameterAsset =>
            m_boostChargingParameterAsset;

    /// <summary>
    /// 初期化されているかを取得します。
    /// </summary>
    public bool IsInitialized =>
        m_isInitialized;


    // ============================================================
    // 速度表示オーバーライド
    // ============================================================

    // 速度UIへ表示する値を、実際の物理速度の代わりに上書きする値
    // （nullの場合は通常通りMotor.HorizontalSpeedを表示する）
    // 例：攻撃中に物理速度が実際の状況と異なる場合でも、
    // 　　攻撃継続リソースとしての速度を見た目上は減衰表示させたい場合に使用
    // 表示上書きの値はSpeedPlaceModelで保持します。

    /// <summary>
    /// 速度UIへ表示する値を上書き設定します。
    /// PlayerAttackingStateなど、実際の物理速度とは別の値を
    /// 見た目上の速度として表示したいStateが使用します。
    /// </summary>
    /// <param name="displaySpeed">表示する速度値。</param>
    public void SetSpeedDisplayOverride(float displaySpeed)
    {
        m_speedPlaceModel.SetSpeedDisplayOverride(displaySpeed);
    }

    /// <summary>
    /// 速度UIの表示オーバーライドを解除し、
    /// 通常通り実際の物理速度を表示する状態に戻します。
    /// </summary>
    public void ClearSpeedDisplayOverride()
    {
        m_speedPlaceModel.ClearSpeedDisplayOverride();
    }


    // ============================================================
    // Vブースト関連
    // ============================================================

    // チャージ解除時点でのゲージ量（0～1）
    /// <summary>チャージ終了時の割合をモデル経由で取得・設定します。</summary>
    public float CarriedBoostGaugeRate
    {
        get => m_vGaugePlaceModel.CarriedBoostGaugeRate;
        set => m_vGaugePlaceModel.CarriedBoostGaugeRate = value;
    }

    // ------------------------------------------------------------
    // チャージ終了時のダッシュ方向
    // ------------------------------------------------------------

    // チャージ終了時にプレイヤーが向いていた方向。
    // VRunningStateのBOOST_DASH中はこの方向に固定して移動します。
    private Vector3 m_boostDashDirection;

    /// <summary>
    /// チャージ終了時に確定したVブーストダッシュ方向を取得・設定します。
    /// </summary>
    public Vector3 BoostDashDirection
    {
        get => m_boostDashDirection;
        set
        {
            Vector3 direction = value;
            direction.y = 0.0f;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            m_boostDashDirection =
                direction.normalized;
        }
    }

    // ------------------------------------------------------------
    // 中断・ゲージ関連
    // ------------------------------------------------------------

    // Vブースト中断時点／進行中の残りゲージ量（0～1）
    // PlayerVRunningStateが開始・参照・更新し、
    // 中断中（Idling/Jumping中）もこの本体側で消費し続けます
    /// <summary>中断中も維持するブースト残量をモデル経由で取得・設定します。</summary>
    public float SuspendedBoostGaugeRate
    {
        get => m_vGaugePlaceModel.SuspendedBoostGaugeRate;
        set => m_vGaugePlaceModel.SuspendedBoostGaugeRate = value;
    }

    // Vブーストが中断中か
    // （trueの間、移動入力・接地などの条件が揃えばVRunningへ復帰する）
    private bool m_isBoostSuspended;

    public bool IsBoostSuspended
    {
        get => m_isBoostSuspended;
        set => m_isBoostSuspended = value;
    }

    // ゲージを1秒あたりどれだけ消費するか
    // （VRunningState開始時に設定される）
    // 消費レートと残量の計算はVGaugePlaceModelが管理します。

    /// <summary>
    /// ゲージの1秒あたりの消費レートを設定します。
    /// PlayerVRunningStateがブースト開始時に呼び出します。
    /// </summary>
    public void SetBoostGaugeDepletionRate(float ratePerSecond)
    {
        m_vGaugePlaceModel.SetBoostGaugeDepletionRate(ratePerSecond);
    }


    // ============================================================
    // 初期化
    // ============================================================

    /// <summary>
    /// プレイヤー用ステートマシンを初期化します。
    /// </summary>
    public void Initialize(
        PlayerInputReader inputReader,
        PlayerMonitor monitor,
        PlayerMotor motor,
        PlayerAnimationPresenter animationPresenter,
        PlayerAttackController attackController,
        SplineGrindController splineGrindController,
        PlayerMovementParameterAsset movementParameterAsset,
        PlayerVBoostMovementParameterAsset
            vBoostMovementParameterAsset,
        PlayerBoostChargingParameterAsset
            boostChargingParameterAsset,
        VGaugeUI vGaugeUI,
        VSpeedUI vSpeedUI,
        PlayerCamera playerCamera,
        Transform modelTransform,
        VGaugePlaceModel vGaugePlaceModel,
        SpeedPlaceModel speedPlaceModel)
    {
        if (inputReader == null ||
            monitor == null ||
            motor == null ||
            animationPresenter == null ||
            attackController == null ||
            splineGrindController == null ||
            movementParameterAsset == null ||
            vBoostMovementParameterAsset == null ||
            boostChargingParameterAsset == null ||
            vGaugePlaceModel == null ||
            speedPlaceModel == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerStateMachineComponent)}] " +
                "初期化に必要な参照が不足しています。",
                this);

            m_isInitialized = false;
            return;
        }

        // VGaugeUI・VSpeedUI・PlayerCamera・ModelTransformはUI/演出側の用途のため、
        // 未設定でも初期化失敗とはしない
        if (vGaugeUI == null)
        {
            Debug.LogWarning(
                $"[{nameof(PlayerStateMachineComponent)}] " +
                $"{nameof(VGaugeUI)}が設定されていません。" +
                "ゲージ演出は行われません。",
                this);
        }

        if (vSpeedUI == null)
        {
            Debug.LogWarning(
                $"[{nameof(PlayerStateMachineComponent)}] " +
                $"{nameof(VSpeedUI)}が設定されていません。" +
                "速度表示は行われません。",
                this);
        }

        if (playerCamera == null)
        {
            Debug.LogWarning(
                $"[{nameof(PlayerStateMachineComponent)}] " +
                $"{nameof(PlayerCamera)}が設定されていません。" +
                "チャージ中のカメラ演出は行われません。",
                this);
        }

        if (modelTransform == null)
        {
            Debug.LogWarning(
                $"[{nameof(PlayerStateMachineComponent)}] " +
                $"{nameof(ModelTransform)}が設定されていません。" +
                "モデルの向きを個別制御する演出は行われません。",
                this);
        }

        m_stateMachine?.Dispose();

        m_inputReader = inputReader;
        m_monitor = monitor;
        m_motor = motor;
        m_animationPresenter =
            animationPresenter;

        m_attackController =
            attackController;

        m_splineGrindController =
            splineGrindController;

        m_movementParameterAsset =
            movementParameterAsset;

        m_vBoostMovementParameterAsset =
            vBoostMovementParameterAsset;

        m_boostChargingParameterAsset =
            boostChargingParameterAsset;

        m_vGaugeUI = vGaugeUI;
        m_vSpeedUI = vSpeedUI;
        m_playerCamera = playerCamera;
        m_modelTransform = modelTransform;
        m_vGaugePlaceModel = vGaugePlaceModel;
        m_speedPlaceModel = speedPlaceModel;
        m_vGaugePlaceModel.ResetGauge();
        m_speedPlaceModel.ResetSpeed();
        RefreshParameterDisplays();

        m_stateMachine =
            new StateMachine<PlayerStateMachineComponent>(
                this);

        m_isInitialized = true;

        m_stateMachine.ChangeState<PlayerIdlingState>();
    }


    // ============================================================
    // State
    // ============================================================

    /// <summary>
    /// 現在のステートが指定された型か確認します。
    /// </summary>
    public bool IsCurrentState<TState>()
        where TState :
            StateBase<PlayerStateMachineComponent>
    {
        if (!m_isInitialized)
        {
            return false;
        }

        return m_stateMachine.IsCurrentState<TState>();
    }


    // ============================================================
    // 更新
    // ============================================================

    /// <summary>
    /// ステートマシンを更新します。
    /// </summary>
    private void Update()
    {
        if (!m_isInitialized)
        {
            return;
        }

        m_stateMachine.Update(Time.deltaTime);
    }

    /// <summary>
    /// 物理状態とステートを更新します。
    /// </summary>
    private void FixedUpdate()
    {
        if (!m_isInitialized)
        {
            return;
        }

        m_monitor.Refresh();

        // 滑走を進める前に押下を消費し、この物理更新内で発射します。
        // Updateでの予約待ち中に終端へ到達して入力が失われることを防ぎます。
        if (m_pendingRailStateChange == null &&
            m_stateMachine.IsCurrentState<PlayerGrindingState>() &&
            m_splineGrindController.IsGrinding && m_inputReader.ConsumeJumpPress())
        {
            RequestRailStateChange<PlayerRailJumpingState>(m_inputReader.MoveInput);
        }

        System.Action railStateChange = m_pendingRailStateChange;
        m_pendingRailStateChange = null;
        railStateChange?.Invoke();
        m_stateMachine.FixedUpdate();

        // Vブースト中（中断中を含む）は、
        // 現在のStateに関係なく常にゲージを消費する
        UpdateSuspendableBoostGauge();
    }

    /// <summary>
    /// 物理更新とState更新の後に、毎フレーム速度をモデルへ保存して表示します。
    /// </summary>
    private void LateUpdate()
    {
        if (!m_isInitialized)
        {
            return;
        }

        m_speedPlaceModel.UpdateSpeed(m_motor.HorizontalSpeed);
        RefreshParameterDisplays();
    }

    /// <summary>
    /// モデルの値をUIへ渡します。将来の表示接続の変更箇所をここへまとめています。
    /// </summary>
    private void RefreshParameterDisplays()
    {
        if (m_vGaugeUI != null)
        {
            m_vGaugeUI.SetGaugeRate(m_vGaugePlaceModel.GetGaugeRate());
        }

        if (m_vSpeedUI != null)
        {
            m_vSpeedUI.SetSpeed(m_speedPlaceModel.Speed);
        }
    }

    /// <summary>
    /// Vブースト中（VRunningState中・中断中の両方）の
    /// モデルのゲージ消費をStateに関係なく行います。表示はLateUpdateで反映します。
    /// </summary>
    private void UpdateSuspendableBoostGauge()
    {
        bool isConsumingGauge =
            IsCurrentState<PlayerVRunningState>() ||
            m_isBoostSuspended;

        if (!isConsumingGauge)
        {
            return;
        }

        m_vGaugePlaceModel.ConsumeBoostGauge(Time.fixedDeltaTime);

        // 中断中にゲージを使い切った場合も、
        // 復帰しようがないため中断状態を解除しておく
        if (SuspendedBoostGaugeRate <= 0.0f &&
            m_isBoostSuspended)
        {
            m_isBoostSuspended = false;

            if (m_vGaugeUI != null)
            {
                m_vGaugeUI.SetCharging(false);
            }
        }
    }


    // ============================================================
    // 破棄
    // ============================================================

    /// <summary>
    /// ステートマシンを破棄します。
    /// </summary>
    private void OnDestroy()
    {
        m_stateMachine?.Dispose();

        m_pendingRailStateChange = null;
        m_stateMachine = null;
        m_isInitialized = false;
    }
    [SerializeField, Header("被弾ノックバック"), Min(0.0f)]
    private float m_knockbackSpeed = 12.0f;
    [SerializeField, Min(0.0f), Tooltip("全攻撃共通のノックバック倍率。攻撃固有の倍率と乗算します。")]
    private float m_knockbackRate = 1.0f;
    [SerializeField, Tooltip("攻撃別設定が渡されなかった場合の設定。未設定なら従来の速度・時間を使用します。")]
    private PlayerKnockbackProfile m_defaultKnockbackProfile;
    [SerializeField, Min(0.0f)]
    private float m_knockbackLiftSpeed = 5.0f;
    [SerializeField, Min(0.01f)]
    private float m_knockbackDuration = 0.8f;
    [SerializeField, Min(0.0f)]
    private float m_hitRecoveryDuration = 0.2f;
    [SerializeField, Tooltip("吹き飛びを終了させる地形・壁のレイヤー")]
    private LayerMask m_hitEnvironmentLayerMask = ~0;

    /// <summary>被弾中の無敵状態を取得します。</summary>
    public bool IsHitReacting { get; private set; }
    /// <summary>被弾中に地形へ衝突したかを取得します。</summary>
    public bool HasHitEnvironment { get; private set; }
    /// <summary>吹き飛びの水平速度を取得します。</summary>
    public float KnockbackSpeed => Mathf.Max(0.0f, m_knockbackSpeed);
    /// <summary>吹き飛び開始時の上向き速度を取得します。</summary>
    public float KnockbackLiftSpeed => Mathf.Max(0.0f, m_knockbackLiftSpeed);
    /// <summary>吹き飛びの最大時間を取得します。</summary>
    public float KnockbackDuration => Mathf.Max(0.01f, m_knockbackDuration);
    /// <summary>吹き飛び後の復帰待機時間を取得します。</summary>
    public float HitRecoveryDuration => Mathf.Max(0.0f, m_hitRecoveryDuration);

    /// <summary>既存の遷移予約より優先して被弾状態を開始します。</summary>
    public bool TryStartHitReaction(Vector3 attackCenter)
    {
        return TryStartHitReaction(attackCenter, null);
    }

    /// <summary>攻撃別設定を使って被弾を開始します。未指定時は標準設定を使用します。</summary>
    public bool TryStartHitReaction(Vector3 attackCenter, PlayerKnockbackProfile profile, AttackIdentifier attackIdentifier = null)
    {
        if (!m_isInitialized || !isActiveAndEnabled || IsHitReacting) return false;

        m_pendingRailStateChange = null;
        IsHitReacting = true;
        HasHitEnvironment = false;
        PlayerKnockbackProfile selectedProfile = profile != null ? profile : m_defaultKnockbackProfile;
        float rate = Mathf.Max(0.0f, m_knockbackRate);
        float horizontalSpeed = KnockbackSpeed;
        float liftSpeed = KnockbackLiftSpeed;
        float duration = KnockbackDuration;
        float recoveryDuration = HitRecoveryDuration;
        if (selectedProfile != null)
        {
            // 攻撃IDが未設定・未登録なら、既存アセットの標準設定を使用します。
            bool hasAttackSettings = selectedProfile.TryGetSettings(attackIdentifier, out PlayerAttackKnockbackSettings settings);
            rate *= hasAttackSettings ? settings.KnockbackRate : selectedProfile.KnockbackRate;
            horizontalSpeed = hasAttackSettings ? settings.HorizontalSpeed : selectedProfile.HorizontalSpeed;
            liftSpeed = hasAttackSettings ? settings.LiftSpeed : selectedProfile.LiftSpeed;
            duration = hasAttackSettings ? settings.Duration : selectedProfile.Duration;
            recoveryDuration = hasAttackSettings ? settings.RecoveryDuration : selectedProfile.RecoveryDuration;
        }

        // 被弾開始時に値を確定し、共有アセットの変更で飛行途中の設定が変わることを防ぎます。
        m_stateMachine.ChangeState<PlayerHitState>(attackCenter,
            horizontalSpeed * rate, liftSpeed * rate, duration, recoveryDuration);
        // 通常の遷移はUpdateまで保留されるため、被弾時は直ちに適用します。
        // これにより次のFixedUpdateで古い移動や攻撃が継続することを防ぎます。
        m_stateMachine.Update(0.0f);
        return true;
    }

    /// <summary>被弾状態の終了時に無敵と衝突記録を解除します。</summary>
    public void EndHitReaction()
    {
        IsHitReacting = false;
        HasHitEnvironment = false;
    }

    /// <summary>地形との新たな接触を被弾終了判定へ渡します。</summary>
    private void OnCollisionEnter(Collision collision)
    {
        CheckHitEnvironment(collision);
    }

    /// <summary>既に壁に触れている場合も被弾終了を検知します。</summary>
    private void OnCollisionStay(Collision collision)
    {
        CheckHitEnvironment(collision);
    }

    /// <summary>発射直後の床接触を除外し、地形・壁への衝突を記録します。</summary>
    private void CheckHitEnvironment(Collision collision)
    {
        if (!IsHitReacting || (m_hitEnvironmentLayerMask.value & (1 << collision.gameObject.layer)) == 0)
            return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            if (normal.y > 0.5f && m_motor.VerticalVelocity > 0.0f) continue;
            HasHitEnvironment = true;
            return;
        }
    }
}