using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

/// <summary>
/// 直近に入力されたキーボードまたはコントローラーを判定し、Managerの表示デバイスを切り替えます。
/// </summary>
[DisallowMultipleComponent]
public sealed class InputDeviceTypeSwitcher : MonoBehaviour
{
    private static bool s_hasDetectedInputDevice;
    private static InputDeviceType s_lastInputDeviceType;

    [SerializeField, Header("入力画像Manager")]
    private InputCommandSpriteManager m_spriteManager;

    [SerializeField, Header("初期入力デバイス")]
    private InputDeviceType m_initialInputDeviceType = InputDeviceType.KEYBOARD;

    [SerializeField, Header("スティック・軸入力の検知しきい値"), Min(0.0f)]
    private float m_magnitudeThreshold = 0.1f;

    /// <summary>
    /// 現在選択されている入力デバイスを取得します。
    /// </summary>
    public InputDeviceType CurrentInputDeviceType { get; private set; }

    /// <summary>
    /// 入力デバイスが切り替わったときに通知します。
    /// </summary>
    public event Action<InputDeviceType> InputDeviceTypeChanged;

    /// <summary>
    /// Enter Play Mode設定に依存せず、再生開始時に共有デバイス状態を初期化します。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSharedInputDeviceState()
    {
        s_hasDetectedInputDevice = false;
        s_lastInputDeviceType = InputDeviceType.KEYBOARD;
    }

    /// <summary>
    /// 直近の入力デバイスを反映して入力イベントを購読します。
    /// </summary>
    private void OnEnable()
    {
        InputSystem.onEvent += OnInputEvent;

        InputDeviceType inputDeviceType = s_hasDetectedInputDevice
            ? s_lastInputDeviceType
            : m_initialInputDeviceType;

        ApplyInputDeviceType(inputDeviceType, true);
    }

    /// <summary>
    /// 入力イベントの購読を解除します。
    /// </summary>
    private void OnDisable()
    {
        InputSystem.onEvent -= OnInputEvent;
    }

    /// <summary>
    /// キーボードまたはゲームパッドの変更された入力を検出します。
    /// </summary>
    /// <param name="inputEvent">入力イベント。</param>
    /// <param name="device">入力イベントを発生させたデバイス。</param>
    private void OnInputEvent(InputEventPtr inputEvent, InputDevice device)
    {
        if (device is not Keyboard && device is not Gamepad)
        {
            return;
        }

        if (!inputEvent.IsA<StateEvent>() && !inputEvent.IsA<DeltaStateEvent>())
        {
            return;
        }

        foreach (InputControl control in inputEvent.EnumerateChangedControls(
                     device,
                     Mathf.Max(0.0f, m_magnitudeThreshold)))
        {
            if (device is Keyboard && control is KeyControl)
            {
                SetCurrentInputDeviceType(InputDeviceType.KEYBOARD);
                return;
            }

            if (device is Gamepad)
            {
                SetCurrentInputDeviceType(InputDeviceType.CONTROLLER);
                return;
            }
        }
    }

    /// <summary>
    /// 入力デバイスを更新し、変更時だけManagerと購読者へ通知します。
    /// </summary>
    /// <param name="inputDeviceType">新しい入力デバイス。</param>
    private void SetCurrentInputDeviceType(InputDeviceType inputDeviceType)
    {
        s_hasDetectedInputDevice = true;
        s_lastInputDeviceType = inputDeviceType;

        ApplyInputDeviceType(inputDeviceType, false);
    }

    /// <summary>
    /// デバイス状態をManagerへ反映し、必要な場合に購読者へ通知します。
    /// </summary>
    /// <param name="inputDeviceType">反映する入力デバイス。</param>
    /// <param name="forceNotify">状態が同じ場合も通知するか。</param>
    private void ApplyInputDeviceType(
        InputDeviceType inputDeviceType,
        bool forceNotify)
    {
        bool hasChanged = CurrentInputDeviceType != inputDeviceType;

        CurrentInputDeviceType = inputDeviceType;
        m_spriteManager?.SetInputDeviceType(inputDeviceType);

        if (hasChanged || forceNotify)
        {
            InputDeviceTypeChanged?.Invoke(inputDeviceType);
        }
    }
}
