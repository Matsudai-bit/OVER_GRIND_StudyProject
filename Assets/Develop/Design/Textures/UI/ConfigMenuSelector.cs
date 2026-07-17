using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// コンフィグ画面の選択を管理します。
/// </summary>
public class ConfigMenuSelector : MonoBehaviour
{
    [Header("Config Item")]

    [SerializeField]
    private ConfigItem[] m_items;

    [Header("Back")]

    [SerializeField]
    private GameObject m_backObject;

    [SerializeField]
    private ConfigMenuManager m_configMenuManager;

    /// <summary>
    /// 現在選択中
    /// </summary>
    private int m_currentIndex;

    /// <summary>
    /// 選択項目数
    /// </summary>
    private int ItemCount => m_items.Length + 1;

    /// <summary>
    /// 初期化します。
    /// </summary>
    public void Initialize()
    {
        m_currentIndex = 0;

        foreach (ConfigItem item in m_items)
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
            Gamepad.current?.dpad.down.wasPressedThisFrame == true)
        {
            MoveNext();
        }

        if (Keyboard.current.upArrowKey.wasPressedThisFrame ||
            Gamepad.current?.dpad.up.wasPressedThisFrame == true)
        {
            MovePrevious();
        }

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame ||
            Gamepad.current?.dpad.right.wasPressedThisFrame == true)
        {
            Increase();
        }

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame ||
            Gamepad.current?.dpad.left.wasPressedThisFrame == true)
        {
            Decrease();
        }

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

        TMP_Text backText = m_backObject.GetComponent<TMP_Text>();

        if (backText != null)
        {
            backText.color = backSelected ? Color.yellow : Color.white;
        }
    }
}