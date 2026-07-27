using UnityEngine;

/// <summary>
/// プレイヤーを構成するコンポーネントを初期化します。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInputReader))]
[RequireComponent(typeof(PlayerMonitor))]
[RequireComponent(typeof(PlayerMotor))]
[RequireComponent(typeof(PlayerStateMachineComponent))]
public sealed class PlayerRootController : MonoBehaviour
{
    // プレイヤーの物理ボディ
    [SerializeField]
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

    // プレイヤーのステートマシン
    [SerializeField]
    private PlayerStateMachineComponent m_stateMachineComponent;

    // 初期化されているか
    private bool m_isInitialized;

    /// <summary>
    /// プレイヤーを構成するコンポーネントを初期化します。
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

        m_monitor.Initialize(m_playerRigidbody);
        m_motor.Initialize(m_playerRigidbody);

        m_stateMachineComponent.Initialize(
            m_inputReader,
            m_monitor,
            m_motor);

        m_isInitialized =
            m_stateMachineComponent.IsInitialized;
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
    /// 同一GameObjectから必要なコンポーネントを取得します。
    /// </summary>
    private void ResolveReferences()
    {
        if (m_playerRigidbody == null)
        {
            m_playerRigidbody = GetComponent<Rigidbody>();
        }

        if (m_inputReader == null)
        {
            m_inputReader = GetComponent<PlayerInputReader>();
        }

        if (m_monitor == null)
        {
            m_monitor = GetComponent<PlayerMonitor>();
        }

        if (m_motor == null)
        {
            m_motor = GetComponent<PlayerMotor>();
        }

        if (m_stateMachineComponent == null)
        {
            m_stateMachineComponent =
                GetComponent<PlayerStateMachineComponent>();
        }
    }

    /// <summary>
    /// 必要なコンポーネントが設定されているか確認します。
    /// </summary>
    /// <returns>
    /// true：必要な参照が設定されています。
    /// false：必要な参照が不足しています。
    /// </returns>
    private bool ValidateReferences()
    {
        bool isValid = true;

        if (m_playerRigidbody == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerRootController)}] Rigidbodyが見つかりません。",
                this);

            isValid = false;
        }

        if (m_inputReader == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerRootController)}] PlayerInputReaderが見つかりません。",
                this);

            isValid = false;
        }

        if (m_monitor == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerRootController)}] PlayerMonitorが見つかりません。",
                this);

            isValid = false;
        }

        if (m_motor == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerRootController)}] PlayerMotorが見つかりません。",
                this);

            isValid = false;
        }

        if (m_stateMachineComponent == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerRootController)}] PlayerStateMachineComponentが見つかりません。",
                this);

            isValid = false;
        }

        return isValid;
    }
}