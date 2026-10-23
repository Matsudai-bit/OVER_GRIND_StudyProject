using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// コントローラーの振動（単発・ループ・オプション適用）を一括管理するマネージャー
/// </summary>
public class VibrationManager : MonoBehaviour
{
    public static VibrationManager Instance { get; private set; }

    /// <summary>
    /// 単発振動のコルーチン参照
    /// </summary>
    private Coroutine activeVibrationCoroutine;

    /// <summary>
    /// ループ振動のコルーチン参照
    /// </summary>
    private Coroutine activeLoopCoroutine;

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

    /// <summary>
    /// 指定された振動データに基づいてゲームパッドを1回だけ振動（減衰再生）させます
    /// </summary>
    public void Play(VibrationData data)
    {
        if (data == null) return;

        // 進行中のすべての振動（単発・ループ）をリセットして上書き
        StopAllVibrations();

        activeVibrationCoroutine = StartCoroutine(PlayVibrationRoutine(data));
    }

    /// <summary>
    /// 停止命令（StopLoop）が呼ばれるまで振動し続けます（グラインド・壁走り用）
    /// </summary>
    public void PlayLoop(VibrationData data)
    {
        if (data == null) return;

        // 進行中のすべての振動（単発・ループ）をリセットして上書き
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
    /// すべての振動処理を強制停止し、物理モーターをゼロリセットします
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
            // オプションで振動がOFFに設定されている場合は即座に停止して抜ける
            if (!VibrationSettings.IsEnabled)
            {
                ResetGamepadMotors();
                yield break;
            }

            // ポーズ時（Time.timeScale = 0）でもタイマーが止まらないよう unscaledDeltaTime を使用
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsedTime / data.m_duration);
            float dampingMultiplier = data.dampingCurve.Evaluate(progress);

            // オプションの全体強度（MasterVolume）を掛け合わせる
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
            // オプションで振動がOFFに設定されている場合は即座に停止して抜ける
            if (!VibrationSettings.IsEnabled)
            {
                ResetGamepadMotors();
                yield break;
            }

            // オプションの全体強度（MasterVolume）を掛け合わせる
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