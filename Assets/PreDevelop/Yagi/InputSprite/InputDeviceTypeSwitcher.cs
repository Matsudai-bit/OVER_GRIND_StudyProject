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
    /// 初期値を設定して入力イベントを購読します。
    /// </summary>
    private void OnEnable()
    {
        CurrentInputDeviceType = m_initialInputDeviceType;
        m_spriteManager?.SetInputDeviceType(CurrentInputDeviceType);
        InputSystem.onEvent += OnInputEvent;
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
        if (CurrentInputDeviceType == inputDeviceType)
        {
            return;
        }

        CurrentInputDeviceType = inputDeviceType;
        m_spriteManager?.SetInputDeviceType(inputDeviceType);
        InputDeviceTypeChanged?.Invoke(inputDeviceType);
    }
}
