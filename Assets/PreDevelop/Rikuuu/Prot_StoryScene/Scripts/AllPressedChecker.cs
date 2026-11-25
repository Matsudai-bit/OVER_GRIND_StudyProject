using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// InputAction に紐づくボタンを、デバイスごとに「全て押されたか」判定します。
/// キーボードとゲームパッドの同時押しは必要とせず、
/// いずれか1つのデバイスで全て押されていれば成立します。
/// </summary>
public class AllPressedChecker
{
    private readonly InputActionReference m_actionRef;

    // デバイスごとの判定用バッファ(毎フレームのメモリ確保を避ける)
    private readonly Dictionary<InputDevice, bool> m_deviceResults = new Dictionary<InputDevice, bool>();

    private bool m_wasAllPressed = false;
    private bool m_cachedResult = false;
    private int m_cachedFrame = -1;

    public AllPressedChecker(InputActionReference actionRef)
    {
        m_actionRef = actionRef;
    }

    /// <summary>
    /// いずれか1つのデバイスで、そのデバイスのコントロールが全て押されている間 true を返します。
    /// </summary>
    public bool IsAllPressed()
    {
        if (m_actionRef == null) return false;

        var controls = m_actionRef.action.controls;
        if (controls.Count == 0) return false;

        m_deviceResults.Clear();

        // デバイスごとに「全て押されているか」を集計する
        foreach (var control in controls)
        {
            if (!(control is ButtonControl button)) continue;

            InputDevice device = control.device;

            // そのデバイスで初めて見たコントロールなら true から始める
            if (!m_deviceResults.TryGetValue(device, out bool allPressed))
            {
                allPressed = true;
            }

            // 1つでも押されていなければ、そのデバイスは不成立
            m_deviceResults[device] = allPressed && button.isPressed;
        }

        // いずれか1つのデバイスが成立していれば true
        foreach (var result in m_deviceResults.Values)
        {
            if (result) return true;
        }

        return false;
    }

    /// <summary>
    /// 全て押された「瞬間」だけ true を返します。
    /// </summary>
    public bool WasAllPressedThisFrame()
    {
        // 同じフレームで複数回呼ばれても結果を変えない
        if (m_cachedFrame == Time.frameCount) return m_cachedResult;
        m_cachedFrame = Time.frameCount;

        bool now = IsAllPressed();
        m_cachedResult = now && !m_wasAllPressed;
        m_wasAllPressed = now;
        return m_cachedResult;
    }

    /// <summary>
    /// 現在押されているかを返します。
    /// requireAllKeys が ON ならデバイスごとの全キー判定、OFF なら登録キーのどれか1つで true。
    /// </summary>
    /// <param name="requireAllKeys">true: 全キー必須(デバイスごと) / false: どれか1つで可</param>
    public bool IsPressedNow(bool requireAllKeys)
    {
        if (m_actionRef == null) return false;

        return requireAllKeys
            ? IsAllPressed()
            : m_actionRef.action.IsPressed();
    }
}