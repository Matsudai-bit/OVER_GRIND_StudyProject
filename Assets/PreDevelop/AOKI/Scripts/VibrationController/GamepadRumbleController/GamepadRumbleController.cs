using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ゲームパッドの振動再生を一元管理します。
/// シングルトンではなく、シーン上のコンポーネントとして動作します。
/// </summary>
public class GamepadRumbleController : MonoBehaviour
{
    /// <summary>
    /// 現在進行中の振動再生状態を保持する内部クラス。
    /// </summary>
    private class RumbleState
    {
        /// <summary>再生中の振動アセット。</summary>
        public GamepadRumbleAsset Asset { get; }

        /// <summary>再生開始からの経過時間 (秒)。</summary>
        public float ElapsedTime { get; set; }

        /// <summary>ループ再生フラグ。</summary>
        public bool IsLoop { get; set; }

        public RumbleState(GamepadRumbleAsset asset)
        {
            Asset = asset;
            ElapsedTime = 0f;
            IsLoop = asset.IsLoop;
        }
    }

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

    // 現在管理中の振動リスト
    private readonly List<RumbleState> m_activeRumbleList = new List<RumbleState>();

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
    /// 特定の振動アセットのループ再生フラグを切り替えます。
    /// </summary>
    /// <param name="asset">対象の振動アセット。</param>
    /// <param name="isLoop">ループさせる場合は true、止める場合は false。</param>
    public void SetLoop(GamepadRumbleAsset asset, bool isLoop)
    {
        if (asset == null)
        {
            return;
        }

        asset.IsLoop = isLoop;

        RumbleState state = m_activeRumbleList.Find(s => s.Asset == asset);
        if (state != null)
        {
            state.IsLoop = isLoop;
        }
    }

    /// <summary>
    /// 指定された振動アセットの再生を個別に停止します。
    /// </summary>
    /// <param name="asset">停止させる振動アセット。</param>
    public void StopRumble(GamepadRumbleAsset asset)
    {
        if (asset == null)
        {
            return;
        }

        m_activeRumbleList.RemoveAll(s => s.Asset == asset);

        if (m_activeRumbleList.Count == 0)
        {
            StopAndResetMotors();
        }
    }

    /// <summary>
    /// 振動要求を受け取ります。
    /// </summary>
    /// <param name="requestAsset">要求された振動アセット。</param>
    private void OnRumbleRequested(GamepadRumbleAsset requestAsset)
    {
        if (requestAsset == null || !m_isEnabled || m_isPaused)
        {
            return;
        }

        // 新しい要求が入った際、それより優先度の低くなる既存振動のうち「復帰不可」のものを破棄
        for (int i = m_activeRumbleList.Count - 1; i >= 0; i--)
        {
            RumbleState existingState = m_activeRumbleList[i];

            // 新要求の方が強くて上書きされる場合
            if (CheckOverride(existingState, requestAsset))
            {
                // アセット側で復帰不可(CanResumeAfterInterrupt == false)が設定されている場合は削除
                if (!existingState.Asset.CanResumeAfterInterrupt)
                {
                    m_activeRumbleList.RemoveAt(i);
                }
            }
        }

        RumbleState targetState = m_activeRumbleList.Find(s => s.Asset == requestAsset);
        if (targetState != null)
        {
            targetState.ElapsedTime = 0f;
            targetState.IsLoop = requestAsset.IsLoop;
        }
        else
        {
            m_activeRumbleList.Add(new RumbleState(requestAsset));
        }

        if (m_activeRumbleCoroutine == null)
        {
            m_activeRumbleCoroutine = StartCoroutine(RumbleRoutine());
        }
    }

    /// <summary>
    /// 新しい振動で上書き可能かを判定します。
    /// </summary>
    /// <param name="currentState">現在再生中の振動状態。</param>
    /// <param name="newAsset">新しく要求された振動アセット。</param>
    /// <returns>
    /// true：上書き可能です。
    /// false：上書き不可能です。
    /// </returns>
    private bool CheckOverride(RumbleState currentState, GamepadRumbleAsset newAsset)
    {
        if (currentState == null || currentState.Asset == null)
        {
            return true;
        }

        if (m_conflictMode == GamepadRumbleConflictMode.PRIORITY)
        {
            return newAsset.Priority >= currentState.Asset.Priority;
        }
        else
        {
            float currentProgress = currentState.Asset.Duration > 0f ? (currentState.ElapsedTime / currentState.Asset.Duration) : 1f;
            float currentDamping = currentState.Asset.DampingCurve != null ? currentState.Asset.DampingCurve.Evaluate(Mathf.Clamp01(currentProgress)) : 1f;
            float currentRealtimePower = (currentState.Asset.LowFrequencyStrength + currentState.Asset.HighFrequencyStrength) * currentDamping;

            float newInitialPower = newAsset.LowFrequencyStrength + newAsset.HighFrequencyStrength;

            return newInitialPower >= currentRealtimePower;
        }
    }

    /// <summary>
    /// 現在のリストの中で最も優先度が高い振動状態を取得します。
    /// </summary>
    /// <returns>最も優先度が高い RumbleState。</returns>
    private RumbleState GetHighestPriorityState()
    {
        if (m_activeRumbleList.Count == 0)
        {
            return null;
        }

        RumbleState highest = m_activeRumbleList[0];

        for (int i = 1; i < m_activeRumbleList.Count; i++)
        {
            RumbleState candidate = m_activeRumbleList[i];
            if (CheckOverride(highest, candidate.Asset))
            {
                highest = candidate;
            }
        }

        return highest;
    }

    /// <summary>
    /// 振動処理を実行するコルーチンです。
    /// </summary>
    private IEnumerator RumbleRoutine()
    {
        while (m_activeRumbleList.Count > 0)
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
                StopAndResetMotors();
                yield break;
            }

            float deltaTime = Time.unscaledDeltaTime;

            // 全ての振動のタイマーを進行させ、期限切れのものを自動削除
            for (int i = m_activeRumbleList.Count - 1; i >= 0; i--)
            {
                RumbleState state = m_activeRumbleList[i];
                state.ElapsedTime += deltaTime;

                if (!state.IsLoop && state.ElapsedTime >= state.Asset.Duration)
                {
                    m_activeRumbleList.RemoveAt(i);
                }
            }

            if (m_activeRumbleList.Count == 0)
            {
                break;
            }

            // 現在最も優先度の高い振動を選択して制御
            RumbleState highestState = GetHighestPriorityState();
            if (highestState != null)
            {
                float progress = highestState.Asset.Duration > 0f ? Mathf.Clamp01(highestState.ElapsedTime / highestState.Asset.Duration) : 0f;
                float damping = highestState.Asset.DampingCurve != null ? highestState.Asset.DampingCurve.Evaluate(progress) : 1f;

                float low = highestState.Asset.LowFrequencyStrength * damping * m_masterStrength;
                float high = highestState.Asset.HighFrequencyStrength * damping * m_masterStrength;

                gamepad.SetMotorSpeeds(low, high);
            }
            else
            {
                ResetGamepadMotors();
            }

            yield return null;
        }

        StopAndResetMotors();
    }

    /// <summary>
    /// 全ての振動を停止し、モーターをリセットします。
    /// </summary>
    public void StopAndResetMotors()
    {
        if (m_activeRumbleCoroutine != null)
        {
            StopCoroutine(m_activeRumbleCoroutine);
            m_activeRumbleCoroutine = null;
        }

        m_activeRumbleList.Clear();
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