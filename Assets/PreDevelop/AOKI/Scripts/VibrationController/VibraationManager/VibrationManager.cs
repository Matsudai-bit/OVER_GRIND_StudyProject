using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// コントローラーの振動（単発・ループ・オプション適用・安全制御）を一括管理するマネージャー
/// </summary>
public class VibrationManager : MonoBehaviour
{
    public static VibrationManager Instance { get; private set; }

    private Coroutine activeVibrationCoroutine;
    private Coroutine activeLoopCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // シーン切り替え時に自動で振動を全停止させるイベント登録
            SceneManager.activeSceneChanged += OnSceneChanged;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
        }
    }

    private void OnSceneChanged(Scene current, Scene next)
    {
        StopAllVibrations();
    }

    /// <summary>
    /// 指定された振動データに基づいてゲームパッドを1回だけ振動（減衰再生）させます
    /// </summary>
    public void Play(VibrationData data)
    {
        if (data == null) return;

        StopAllVibrations();
        activeVibrationCoroutine = StartCoroutine(PlayVibrationRoutine(data));
    }

    /// <summary>
    /// 停止命令が呼ばれるまで振動し続けます（グラインド・壁走り用）
    /// </summary>
    public void PlayLoop(VibrationData data)
    {
        if (data == null) return;

        StopAllVibrations();
        activeLoopCoroutine = StartCoroutine(PlayLoopRoutine(data));
    }

    /// <summary>
    /// 単発振動のみを即座に停止します
    /// </summary>
    public void StopVibration()
    {
        if (activeVibrationCoroutine != null)
        {
            StopCoroutine(activeVibrationCoroutine);
            activeVibrationCoroutine = null;
        }

        ResetGamepadMotors();
    }

    /// <summary>
    /// ループ振動のみを停止します
    /// </summary>
    public void StopLoop()
    {
        if (activeLoopCoroutine != null)
        {
            StopCoroutine(activeLoopCoroutine);
            activeLoopCoroutine = null;
        }

        ResetGamepadMotors();
    }

    /// <summary>
    /// すべての振動処理（単発・ループ）を強制停止し、物理モーターをゼロリセットします
    /// </summary>
    public void StopAllVibrations()
    {
        if (activeVibrationCoroutine != null)
        {
            StopCoroutine(activeVibrationCoroutine);
            activeVibrationCoroutine = null;
        }

        if (activeLoopCoroutine != null)
        {
            StopCoroutine(activeLoopCoroutine);
            activeLoopCoroutine = null;
        }

        ResetGamepadMotors();
    }

    private IEnumerator PlayVibrationRoutine(VibrationData data)
    {
        var gamepad = Gamepad.current;
        if (gamepad == null) yield break;

        float elapsedTime = 0f;

        while (elapsedTime < data.m_duration)
        {
            // オプションでOFF設定なら即時停止
            if (!VibrationSettings.IsEnabled)
            {
                ResetGamepadMotors();
                yield break;
            }

            // ポーズ時（Time.timeScale = 0）でも正しく時間を計測
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsedTime / data.m_duration);
            float dampingMultiplier = data.dampingCurve.Evaluate(progress);

            // オプションのマスターボリューム適用
            float masterVol = VibrationSettings.MasterVolume;
            float currentLow = data.m_lowFrequency * dampingMultiplier * masterVol;
            float currentHigh = data.m_highFrequency * dampingMultiplier * masterVol;

            gamepad.SetMotorSpeeds(currentLow, currentHigh);

            yield return null;
        }

        ResetGamepadMotors();
        activeVibrationCoroutine = null;
    }

    private IEnumerator PlayLoopRoutine(VibrationData data)
    {
        var gamepad = Gamepad.current;
        if (gamepad == null) yield break;

        while (true)
        {
            // オプションでOFF設定なら即時停止
            if (!VibrationSettings.IsEnabled)
            {
                ResetGamepadMotors();
                yield break;
            }

            // オプションのマスターボリューム適用
            float masterVol = VibrationSettings.MasterVolume;
            float currentLow = data.m_lowFrequency * masterVol;
            float currentHigh = data.m_highFrequency * masterVol;

            gamepad.SetMotorSpeeds(currentLow, currentHigh);

            yield return null;
        }
    }

    private void ResetGamepadMotors()
    {
        if (Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(0f, 0f);
        }
    }

    private void OnDisable()
    {
        StopAllVibrations();
    }

    private void OnApplicationQuit()
    {
        StopAllVibrations();
    }
}