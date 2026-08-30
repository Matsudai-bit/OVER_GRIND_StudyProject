using UnityEngine;

/// <summary>
/// プレイヤーを構成するコンポーネントを初期化します。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInputReader))]
[RequireComponent(typeof(PlayerMonitor))]
[RequireComponent(typeof(PlayerMotor))]
[RequireComponent(typeof(PlayerAnimationPresenter))]
[RequireComponent(typeof(PlayerStateMachineComponent))]
[RequireComponent(typeof(PlayerAttackController))]
[RequireComponent(typeof(SplineGrindController))]
public sealed class PlayerRootController : MonoBehaviour
{
    // 通常移動パラメータ
    [SerializeField, Header("パラメータ")]
    private PlayerMovementParameterAsset
        m_movementParameterAsset;

    // Vブースト移動パラメータ
    [SerializeField]
    private PlayerVBoostMovementParameterAsset
        m_vBoostMovementParameterAsset;

    // プレイヤーの物理ボディ
    [SerializeField, Header("コンポーネント")]
    private Rigidbody m_playerRigidbody;

    // プレイヤー入力
    [SerializeField]
    private PlayerInputReader m_inputReader;

    // プレイヤー監視機能
    [SerializeField]
    private PlayerMonitor m_monitor;

    // プレイヤー移動機能
    [SerializeField]
    private PlayerMotor m_motor;

    // プレイヤーアニメーション表示機能
    [SerializeField]
    private PlayerAnimationPresenter m_animationPresenter;

    // プレイヤーステートマシン
    [SerializeField]
    private PlayerStateMachineComponent
        m_stateMachineComponent;

    // プレイヤー攻撃機能
    [SerializeField]
    private PlayerAttackController m_attackController;

    // スプライングラインド機能
    [SerializeField]
    private SplineGrindController m_splineGrindController;

    // Vゲージ表示機能
    [SerializeField]
    private VGaugeUI m_vGaugeUI;

    // 速度表示機能
    [SerializeField]
    private VSpeedUI m_vSpeedUI;

    // 初期化されているか
    private bool m_isInitialized;

    /// <summary>
    /// プレイヤーを初期化します。
    /// </summary>
    private void Awake()
    {
        ResolveReferences();

        if (!ValidateReferences())
        {
            m_isInitialized = false;
            enabled = false;
            return;
        }

        m_monitor.Initialize(
            m_playerRigidbody);

        m_motor.Initialize(
            m_playerRigidbody);

        if (!m_motor.IsInitialized)
        {
            m_isInitialized = false;
            enabled = false;
            return;
        }

        m_animationPresenter.Initialize(
            m_monitor,
            m_motor);

        m_stateMachineComponent.Initialize(
            m_inputReader,
            m_monitor,
            m_motor,
            m_animationPresenter,
            m_attackController,
            m_splineGrindController,
            m_movementParameterAsset,
            m_vBoostMovementParameterAsset,
            m_vGaugeUI,
            m_vSpeedUI);

        m_isInitialized =
            m_motor.IsInitialized &&
            m_stateMachineComponent.IsInitialized &&
            m_animationPresenter.IsInitialized;
    }

    /// <summary>
    /// プレイヤー入力を有効化します。
    /// </summary>
    private void OnEnable()
    {
        if (!m_isInitialized)
        {
            return;
        }

        m_inputReader.EnableInput();
    }

    /// <summary>
    /// プレイヤー入力を無効化します。
    /// </summary>
    private void OnDisable()
    {
        if (m_inputReader == null)
        {
            return;
        }

        m_inputReader.DisableInput();
    }

    /// <summary>
    /// プレイヤー操作を有効化します。
    /// </summary>
    public void EnableControl()
    {
        if (!m_isInitialized)
        {
            return;
        }

        m_inputReader.EnableInput();
    }

    /// <summary>
    /// プレイヤー操作を無効化します。
    /// </summary>
    public void DisableControl()
    {
        if (!m_isInitialized)
        {
            return;
        }

        m_inputReader.DisableInput();
    }

    /// <summary>
    /// 必要なコンポーネント参照を取得します。
    /// </summary>
    private void ResolveReferences()
    {
        if (m_playerRigidbody == null)
        {
            m_playerRigidbody =
                GetComponent<Rigidbody>();
        }

        if (m_inputReader == null)
        {
            m_inputReader =
                GetComponent<PlayerInputReader>();
        }

        if (m_monitor == null)
        {
            m_monitor =
                GetComponent<PlayerMonitor>();
        }

        if (m_motor == null)
        {
            m_motor =
                GetComponent<PlayerMotor>();
        }

        if (m_animationPresenter == null)
        {
            m_animationPresenter =
                GetComponent<PlayerAnimationPresenter>();
        }

        if (m_stateMachineComponent == null)
        {
            m_stateMachineComponent =
                GetComponent<PlayerStateMachineComponent>();
        }

        if (m_attackController == null)
        {
            m_attackController =
                GetComponent<PlayerAttackController>();
        }

        if (m_splineGrindController == null)
        {
            m_splineGrindController =
                GetComponent<SplineGrindController>();
        }
    }

    /// <summary>
    /// 初期化に必要な参照を確認します。
    /// </summary>
    /// <returns>
    /// true：必要な参照があります。
    /// false：必要な参照が不足しています。
    /// </returns>
    private bool ValidateReferences()
    {
        bool isValid = true;

        if (m_movementParameterAsset == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerRootController)}] " +
                $"{nameof(PlayerMovementParameterAsset)}" +
                "が設定されていません。",
                this);

            isValid = false;
        }

        if (m_vBoostMovementParameterAsset == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerRootController)}] " +
                $"{nameof(PlayerVBoostMovementParameterAsset)}" +
                "が設定されていません。",
                this);

            isValid = false;
        }

        if (m_playerRigidbody == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerRootController)}] " +
                "Rigidbodyが見つかりません。",
                this);

            isValid = false;
        }

        if (m_inputReader == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerRootController)}] " +
                "PlayerInputReaderが見つかりません。",
                this);

            isValid = false;
        }

        if (m_monitor == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerRootController)}] " +
                "PlayerMonitorが見つかりません。",
                this);

            isValid = false;
        }

        if (m_motor == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerRootController)}] " +
                "PlayerMotorが見つかりません。",
                this);

            isValid = false;
        }

        if (m_animationPresenter == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerRootController)}] " +
                "PlayerAnimationPresenterが見つかりません。",
                this);

            isValid = false;
        }

        if (m_stateMachineComponent == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerRootController)}] " +
                "PlayerStateMachineComponentが見つかりません。",
                this);

            isValid = false;
        }

        if (m_attackController == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerRootController)}] " +
                "PlayerAttackControllerが見つかりません。",
                this);

            isValid = false;
        }

        if (m_splineGrindController == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerRootController)}] " +
                "SplineGrindControllerが見つかりません。",
                this);

            isValid = false;
        }

        return isValid;
    }
}