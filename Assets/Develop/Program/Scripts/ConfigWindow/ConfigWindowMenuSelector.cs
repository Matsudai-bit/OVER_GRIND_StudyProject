using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// コンフィグ画面の選択を管理します。
/// </summary>
public class ConfigWindowMenuSelector : MonoBehaviour
{
    [Header("Config Item")]

    [SerializeField]
    private ConfigWindowItem[] m_items;

    [Header("Back")]

    [SerializeField]
    private GameObject m_backObject;

    [SerializeField]
    private ConfigWindowMenuManager m_configMenuManager;

    [SerializeField]
    private Image m_backImage;

    /// <summary>
    /// 現在選択中
    /// </summary>
    private int m_currentIndex;

    /// <summary>
    /// 選択項目数
    /// </summary>
    private int ItemCount => m_items.Length + 1;

    [SerializeField]
    private float m_repeatStartTime = 0.4f;

    [SerializeField]
    private float m_repeatInterval = 0.08f;

    private float m_holdTimer;
    private float m_repeatTimer;
    private int m_inputDirection;

    /// <summary>
    /// 初期化します。
    /// </summary>
    public void Initialize()
    {
        m_currentIndex = 0;

        foreach (ConfigWindowItem item in m_items)
        {
            item.Initialize();
        }

        RefreshSelection();
    }

    private void Update()
    {
        UpdateInput();
    }

    /// <summary>
    /// 入力更新
    /// </summary>
    private void UpdateInput()
    {
        if (Keyboard.current.downArrowKey.wasPressedThisFrame ||
            Gamepad.current?.dpad.down.wasPressedThisFrame == true ||
            Gamepad.current?.leftStick.down.wasPressedThisFrame == true)
        {
            MoveNext();
        }

        if (Keyboard.current.upArrowKey.wasPressedThisFrame ||
            Gamepad.current?.dpad.up.wasPressedThisFrame == true ||
            Gamepad.current?.leftStick.up.wasPressedThisFrame == true)
        {
            MovePrevious();
        }

        // ←追加
        UpdateHorizontalInput();

        if (Keyboard.current.escapeKey.wasPressedThisFrame ||
            Gamepad.current?.buttonEast.wasPressedThisFrame == true)
        {
            m_configMenuManager.Close();
        }

        if (Keyboard.current.enterKey.wasPressedThisFrame ||
            Gamepad.current?.buttonSouth.wasPressedThisFrame == true)
        {
            if (m_currentIndex == m_items.Length)
            {
                m_configMenuManager.Close();
            }
        }
    }

    private void UpdateHorizontalInput()
    {
        int direction = 0;

        // 右入力
        if (Keyboard.current.rightArrowKey.isPressed ||
            Gamepad.current?.dpad.right.isPressed == true ||
            (Gamepad.current != null &&
             Gamepad.current.leftStick.ReadValue().x > 0.8f))
        {
            direction = 1;
        }

        // 左入力
        else if (Keyboard.current.leftArrowKey.isPressed ||
                 Gamepad.current?.dpad.left.isPressed == true ||
                 (Gamepad.current != null &&
                  Gamepad.current.leftStick.ReadValue().x < -0.8f))
        {
            direction = -1;
        }

        // 入力なし
        if (direction == 0)
        {
            m_inputDirection = 0;
            m_holdTimer = 0f;
            m_repeatTimer = 0f;
            return;
        }

        // 入力方向が変わった
        if (direction != m_inputDirection)
        {
            m_inputDirection = direction;
            m_holdTimer = 0f;
            m_repeatTimer = 0f;

            if (direction > 0)
                Increase();
            else
                Decrease();

            return;
        }

        m_holdTimer += Time.unscaledDeltaTime;

        if (m_holdTimer < m_repeatStartTime)
            return;

        m_repeatTimer += Time.unscaledDeltaTime;

        if (m_repeatTimer >= m_repeatInterval)
        {
            m_repeatTimer = 0f;

            if (direction > 0)
                Increase();
            else
                Decrease();
        }
    }

    /// <summary>
    /// 次の項目
    /// </summary>
    private void MoveNext()
    {
        m_currentIndex++;

        if (m_currentIndex >= ItemCount)
        {
            m_currentIndex = 0;
        }

        RefreshSelection();
    }

    /// <summary>
    /// 前の項目
    /// </summary>
    private void MovePrevious()
    {
        m_currentIndex--;

        if (m_currentIndex < 0)
        {
            m_currentIndex = ItemCount - 1;
        }

        RefreshSelection();
    }

    /// <summary>
    /// 値を増やします。
    /// </summary>
    private void Increase()
    {
        if (m_currentIndex >= m_items.Length)
            return;

        m_items[m_currentIndex].Increase();
    }

    /// <summary>
    /// 値を減らします。
    /// </summary>
    private void Decrease()
    {
        if (m_currentIndex >= m_items.Length)
            return;

        m_items[m_currentIndex].Decrease();
    }

    /// <summary>
    /// 選択状態を更新します。
    /// </summary>
    private void RefreshSelection()
    {
        for (int i = 0; i < m_items.Length; i++)
        {
            m_items[i].SetSelected(i == m_currentIndex);
        }

        bool backSelected = m_currentIndex == m_items.Length;

        m_backImage.color = backSelected
            ? Color.yellow
            : Color.white;

        m_backImage.DOKill();

        if (backSelected)
        {
            m_backImage.color = Color.yellow;

            m_backImage
                .DOFade(0.35f, 0.6f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
        else
        {
            Color c = Color.white;
            c.a = 1f;

            m_backImage.color = c;
        }
    }
}