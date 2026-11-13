using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ゲームパッドの振動再生を一元管理します。
/// </summary>
public class GamepadRumbleController : MonoBehaviour
{
    /// <summary>シングルトンインスタンス。</summary>
    public static GamepadRumbleController Instance { get; private set; }

    [SerializeField, Header("振動要求イベント")]
    private GamepadRumbleRequestEvent m_rumbleRequestEvent;

    [SerializeField, Header("ポーズ変更イベント")]
    private PauseChangedEvent m_pauseChangedEvent;

    [SerializeField, Header("競合制御方式")]
    private GamepadRumbleConflictMode m_conflictMode = GamepadRumbleConflictMode.PRIORITY;

    [Header("将来の拡張用設定")]
    [SerializeField, Header("振動機能の有効/無効")]
    private bool m_isEnabled = true;

    [SerializeField, Range(0f, 1f), Header("全体倍率 (0.0 ~ 1.0)")]
    private float m_masterStrength = 1.0f;

    // 現在再生中の振動アセット
    private GamepadRumbleAsset m_currentAsset;

    // 現在の振動の経過時間
    private float m_currentElapsedTime = 0f;

    // ポーズ中かどうか
    private bool m_isPaused = false;

    // 実行中の振動コルーチン
    private Coroutine m_activeRumbleCoroutine;

    /// <summary>振動機能の有効/無効を取得または設定します。</summary>
    public bool IsEnabled
    {
        get => m_isEnabled;
        set
        {
            m_isEnabled = value;
            if (!m_isEnabled)
            {
                StopAndResetMotors();
            }
        }
    }

    /// <summary>振動の全体倍率を取得または設定します。</summary>
    public float MasterStrength
    {
        get => m_masterStrength;
        set => m_masterStrength = Mathf.Clamp01(value);
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        if (m_rumbleRequestEvent != null)
        {
            m_rumbleRequestEvent.OnRequested += OnRumbleRequested;
        }

        if (m_pauseChangedEvent != null)
        {
            m_pauseChangedEvent.RegisterListener(OnPauseChanged);
        }
    }

    private void OnDisable()
    {
        if (m_rumbleRequestEvent != null)
        {
            m_rumbleRequestEvent.OnRequested -= OnRumbleRequested;
        }

        if (m_pauseChangedEvent != null)
        {
            m_pauseChangedEvent.UnregisterListener(OnPauseChanged);
        }

        StopAndResetMotors();
    }

    /// <summary>
    /// ポーズ状態の変更を受け取ります。
    /// </summary>
    /// <param name="eventData">ポーズ状態変更情報。</param>
    private void OnPauseChanged(PauseChangedEventData eventData)
    {
        // 既存のPauseChangedEventData構造体のプロパティ名に合わせてください
        bool isPaused = eventData.IsPaused;

        if (isPaused)
        {
            PauseHaptics();
        }
        else
        {
            ResumeHaptics();
        }
    }

    /// <summary>
    /// 振動を一時停止します。
    /// </summary>
    public void PauseHaptics()
    {
        m_isPaused = true;
        ResetGamepadMotors();
    }

    /// <summary>
    /// 振動を再開します。
    /// </summary>
    public void ResumeHaptics()
    {
        m_isPaused = false;
    }

    /// <summary>
    /// 振動要求を受け取ります。
    /// </summary>
    /// <param name="requestAsset">要求された振動アセット。</param>
    private void OnRumbleRequested(GamepadRumbleAsset requestAsset)
    {
        if (requestAsset == null || !m_isEnabled)
        {
            return;
        }

        if (m_isPaused)
        {
            return;
        }

        if (m_currentAsset != null && m_activeRumbleCoroutine != null)
        {
            if (!CheckOverride(requestAsset))
            {
                return;
            }
        }

        StartRumble(requestAsset);
    }

    /// <summary>
    /// 新しい振動で上書きするかどうかを判定します。
    /// </summary>
    /// <param name="newAsset">新しく要求された振動アセット。</param>
    /// <returns>
    /// true：上書きします。
    /// false：上書きしません。
    /// </returns>
    private bool CheckOverride(GamepadRumbleAsset newAsset)
    {
        if (m_currentAsset == null)
        {
            return true;
        }

        if (m_conflictMode == GamepadRumbleConflictMode.PRIORITY)
        {
            return newAsset.Priority >= m_currentAsset.Priority;
        }
        else
        {
            // 時間経過による現在の強さを計算
            float currentProgress = m_currentAsset.Duration > 0f ? (m_currentElapsedTime / m_currentAsset.Duration) : 1f;
            float currentDamping = m_currentAsset.DampingCurve != null ? m_currentAsset.DampingCurve.Evaluate(Mathf.Clamp01(currentProgress)) : 1f;
            float currentRealtimePower = (m_currentAsset.LowFrequencyStrength + m_currentAsset.HighFrequencyStrength) * currentDamping;

            // 新しい振動の初期強さを計算
            float newInitialPower = newAsset.LowFrequencyStrength + newAsset.HighFrequencyStrength;

            return newInitialPower >= currentRealtimePower;
        }
    }

    /// <summary>
    /// 振動を開始します。
    /// </summary>
    /// <param name="asset">再生する振動アセット。</param>
    private void StartRumble(GamepadRumbleAsset asset)
    {
        if (m_activeRumbleCoroutine != null)
        {
            StopCoroutine(m_activeRumbleCoroutine);
        }

        m_currentAsset = asset;
        m_currentElapsedTime = 0f;
        m_activeRumbleCoroutine = StartCoroutine(RumbleRoutine());
    }

    /// <summary>
    /// 振動処理を実行するコルーチンです。
    /// </summary>
    private IEnumerator RumbleRoutine()
    {
        while (m_currentElapsedTime < m_currentAsset.Duration)
        {
            while (m_isPaused)
            {
                ResetGamepadMotors();
                yield return null;
            }

            var gamepad = Gamepad.current;
            if (gamepad == null)
            {
                yield return null;
                continue;
            }

            if (!m_isEnabled)
            {
                ResetGamepadMotors();
                yield break;
            }

            m_currentElapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(m_currentElapsedTime / m_currentAsset.Duration);
            float damping = m_currentAsset.DampingCurve != null ? m_currentAsset.DampingCurve.Evaluate(progress) : 1f;

            float low = m_currentAsset.LowFrequencyStrength * damping * m_masterStrength;
            float high = m_currentAsset.HighFrequencyStrength * damping * m_masterStrength;

            gamepad.SetMotorSpeeds(low, high);

            yield return null;
        }

        StopAndResetMotors();
    }

    /// <summary>
    /// 進行中の振動を停止し、モーターをリセットします。
    /// </summary>
    public void StopAndResetMotors()
    {
        if (m_activeRumbleCoroutine != null)
        {
            StopCoroutine(m_activeRumbleCoroutine);
            m_activeRumbleCoroutine = null;
        }

        m_currentAsset = null;
        m_currentElapsedTime = 0f;
        ResetGamepadMotors();
    }

    /// <summary>
    /// ゲームパッドのモーター出力をゼロにします。
    /// </summary>
    private void ResetGamepadMotors()
    {
        if (Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(0f, 0f);
        }
    }

    private void OnApplicationQuit()
    {
        ResetGamepadMotors();
    }
}