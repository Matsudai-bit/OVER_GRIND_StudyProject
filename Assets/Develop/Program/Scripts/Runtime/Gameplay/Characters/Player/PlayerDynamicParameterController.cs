using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>
/// 設定画面から受け取るPlayerの動的パラメータを保持し、入力デバイスに応じた感度を通知します。
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerDynamicParameterController : MonoBehaviour
{
    private const float MIN_CAMERA_SENSITIVITY = 0.1f;
    private const float MAX_CAMERA_SENSITIVITY = 1.0f;
    private const float DEFAULT_CAMERA_SENSITIVITY = 0.5f;
    private const float INPUT_MAGNITUDE_THRESHOLD = 0.1f;
    private const float VALUE_CHANGE_EPSILON = 0.0001f;

    private enum CameraInputDevice
    {
        MOUSE,
        CONTROLLER
    }

    [SerializeField, Header("Default Sensitivity")]
    [Range(MIN_CAMERA_SENSITIVITY, MAX_CAMERA_SENSITIVITY)]
    [Tooltip("設定側から値を受け取れない場合のマウス感度です。")]
    private float m_defaultMouseSensitivity = DEFAULT_CAMERA_SENSITIVITY;

    [SerializeField, Range(MIN_CAMERA_SENSITIVITY, MAX_CAMERA_SENSITIVITY)]
    [Tooltip("設定側から値を受け取れない場合のコントローラー感度です。")]
    private float m_defaultControllerSensitivity = DEFAULT_CAMERA_SENSITIVITY;

    [SerializeField]
    private CameraInputDevice m_initialInputDevice = CameraInputDevice.MOUSE;

    private float m_mouseSensitivity;
    private float m_controllerSensitivity;
    private CameraInputDevice m_currentInputDevice;

    /// <summary>有効なカメラ感度が変更されたときに通知します。</summary>
    public event Action<float> CameraSensitivityChanged;

    /// <summary>現在の入力デバイスを取得します。</summary>
    public string CurrentInputDevice => m_currentInputDevice.ToString();

    /// <summary>現在の入力デバイスに対応するカメラ感度を取得します。</summary>
    public float CameraSensitivity => GetCurrentSensitivity();

    /// <summary>マウス感度を取得します。</summary>
    public float MouseSensitivity => m_mouseSensitivity;

    /// <summary>コントローラー感度を取得します。</summary>
    public float ControllerSensitivity => m_controllerSensitivity;

    /// <summary>感度の初期値を設定します。</summary>
    private void Awake()
    {
        m_mouseSensitivity = ClampSensitivity(m_defaultMouseSensitivity);
        m_controllerSensitivity = ClampSensitivity(m_defaultControllerSensitivity);
        m_currentInputDevice = m_initialInputDevice;
    }

    /// <summary>Inspectorの初期値をPlayerCameraへ通知します。</summary>
    private void Start()
    {
        NotifyCurrentSensitivity();
    }

    /// <summary>入力デバイスの変更を検出します。</summary>
    private void OnEnable()
    {
        InputSystem.onEvent += OnInputEvent;
    }

    /// <summary>入力デバイスの変更検出を解除します。</summary>
    private void OnDisable()
    {
        InputSystem.onEvent -= OnInputEvent;
    }

    /// <summary>設定側からマウス感度を受け取ります。</summary>
    /// <param name="sensitivity">0.1～1.0のマウス感度。</param>
    public void SetMouseSensitivity(float sensitivity)
    {
        float clampedSensitivity = ClampSensitivity(sensitivity);
        if (Mathf.Abs(m_mouseSensitivity - clampedSensitivity) <= VALUE_CHANGE_EPSILON)
        {
            return;
        }

        m_mouseSensitivity = clampedSensitivity;
        NotifyCurrentSensitivity();
    }

    /// <summary>設定側からコントローラー感度を受け取ります。</summary>
    /// <param name="sensitivity">0.1～1.0のコントローラー感度。</param>
    public void SetControllerSensitivity(float sensitivity)
    {
        float clampedSensitivity = ClampSensitivity(sensitivity);
        if (Mathf.Abs(m_controllerSensitivity - clampedSensitivity) <= VALUE_CHANGE_EPSILON)
        {
            return;
        }

        m_controllerSensitivity = clampedSensitivity;
        NotifyCurrentSensitivity();
    }

    /// <summary>旧設定との互換用に、両デバイスの感度を同時に変更します。</summary>
    /// <param name="sensitivity">0.1～1.0の感度。</param>
    public void SetCameraSensitivity(float sensitivity)
    {
        float clampedSensitivity = ClampSensitivity(sensitivity);
        m_mouseSensitivity = clampedSensitivity;
        m_controllerSensitivity = clampedSensitivity;
        NotifyCurrentSensitivity();
    }

    /// <summary>入力イベントから使用中のデバイスを切り替えます。</summary>
    /// <param name="inputEvent">入力イベント。</param>
    /// <param name="device">入力元デバイス。</param>
    private void OnInputEvent(InputEventPtr inputEvent, InputDevice device)
    {
        if (!inputEvent.IsA<StateEvent>() && !inputEvent.IsA<DeltaStateEvent>())
        {
            return;
        }

        CameraInputDevice? inputDevice = null;
        if (device is Mouse)
        {
            inputDevice = CameraInputDevice.MOUSE;
        }
        else if (device is Gamepad)
        {
            inputDevice = CameraInputDevice.CONTROLLER;
        }

        if (!inputDevice.HasValue || !HasMeaningfulInput(inputEvent, device))
        {
            return;
        }

        if (m_currentInputDevice == inputDevice.Value)
        {
            return;
        }

        m_currentInputDevice = inputDevice.Value;
        NotifyCurrentSensitivity();
    }

    /// <summary>入力イベントに有効な変更が含まれているかを判定します。</summary>
    private bool HasMeaningfulInput(InputEventPtr inputEvent, InputDevice device)
    {
        foreach (InputControl control in inputEvent.EnumerateChangedControls(
                     device,
                     INPUT_MAGNITUDE_THRESHOLD))
        {
            if (device is Mouse && control.device == device)
            {
                return true;
            }

            if (device is Gamepad)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>現在の入力デバイスに対応する感度を取得します。</summary>
    private float GetCurrentSensitivity()
    {
        return m_currentInputDevice == CameraInputDevice.CONTROLLER
            ? m_controllerSensitivity
            : m_mouseSensitivity;
    }

    /// <summary>現在の感度をPlayerCameraへ通知します。</summary>
    private void NotifyCurrentSensitivity()
    {
        CameraSensitivityChanged?.Invoke(GetCurrentSensitivity());
    }

    /// <summary>感度を0.1～1.0に補正します。</summary>
    private float ClampSensitivity(float sensitivity)
    {
        return Mathf.Clamp(sensitivity, MIN_CAMERA_SENSITIVITY, MAX_CAMERA_SENSITIVITY);
    }

}
