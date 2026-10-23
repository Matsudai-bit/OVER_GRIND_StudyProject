using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GamepadRumbleController : MonoBehaviour
{
    public static GamepadRumbleController Instance { get; private set; }

    [SerializeField, Header("振動要求イベント")]
    private GamepadRumbleRequestEvent m_rumbleRequestEvent;

    [SerializeField, Header("ポーズ変更イベント")]
    private PauseChangedEvent m_pauseChangedEvent;

    [SerializeField, Header("競合制御方式")]
    private GamepadRumbleConflictMode m_conflictMode = GamepadRumbleConflictMode.Priority;

    [Header("将来の拡張用設定")]
    [SerializeField, Header("振動機能の有効/無効")]
    private bool m_isEnabled = true;

    [SerializeField, Range(0f, 1f), Header("全体倍率 (0.0 ~ 1.0)")]
    private float m_masterStrength = 1.0f;

    private GamepadRumbleAsset m_currentAsset;
    private float m_currentElapsedTime = 0f;
    private bool m_isPaused = false;
    private Coroutine m_activeRumbleCoroutine;

    public bool IsEnabled
    {
        get => m_isEnabled;
        set
        {
            m_isEnabled = value;
            if (!m_isEnabled) StopAndResetMotors();
        }
    }

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

        // 既存 PauseChangedEvent の登録用メソッドを使用
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

        // 既存 PauseChangedEvent の解除用メソッドを使用
        if (m_pauseChangedEvent != null)
        {
            m_pauseChangedEvent.UnregisterListener(OnPauseChanged);
        }

        StopAndResetMotors();
    }

    // 既存 PauseChangedEventData を受け取るイベントハンドラー
    private void OnPauseChanged(PauseChangedEventData eventData)
    {
        // struct（値型）のため null チェックは不要です
        bool isPaused = eventData.IsPaused; // ※実際のプロパティ名に合わせてください

        if (isPaused)
        {
            PauseHaptics();
        }
        else
        {
            ResumeHaptics();
        }
    }

    public void PauseHaptics()
    {
        m_isPaused = true;
        ResetGamepadMotors();
    }

    public void ResumeHaptics()
    {
        m_isPaused = false;
    }

    private void OnRumbleRequested(GamepadRumbleAsset requestAsset)
    {
        if (requestAsset == null || !m_isEnabled) return;

        if (m_isPaused) return;

        if (m_currentAsset != null && m_activeRumbleCoroutine != null)
        {
            if (!ShouldOverride(requestAsset))
            {
                return;
            }
        }

        StartRumble(requestAsset);
    }

    private bool ShouldOverride(GamepadRumbleAsset newAsset)
    {
        if (m_currentAsset == null) return true;

        if (m_conflictMode == GamepadRumbleConflictMode.Priority)
        {
            return newAsset.Priority >= m_currentAsset.Priority;
        }
        else
        {
            float currentProgress = m_currentAsset.Duration > 0f ? (m_currentElapsedTime / m_currentAsset.Duration) : 1f;
            float currentDamping = m_currentAsset.DampingCurve != null ? m_currentAsset.DampingCurve.Evaluate(Mathf.Clamp01(currentProgress)) : 1f;
            float currentRealtimePower = (m_currentAsset.LowFrequencyStrength + m_currentAsset.HighFrequencyStrength) * currentDamping;

            float newInitialPower = newAsset.LowFrequencyStrength + newAsset.HighFrequencyStrength;

            return newInitialPower >= currentRealtimePower;
        }
    }

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