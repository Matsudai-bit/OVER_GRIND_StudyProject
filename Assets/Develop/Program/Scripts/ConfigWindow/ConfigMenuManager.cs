using System;
using UnityEngine;

/// <summary>
/// コンフィグ画面を管理します。
/// </summary>
public class ConfigMenuManager : MonoBehaviour
{
    /// <summary>
    /// コンフィグを閉じた時の通知
    /// </summary>
    public Action OnClosed;

    [Header("Selector")]
    [SerializeField]
    private ConfigMenuSelector m_selector;

    /// <summary>
    /// 初期化
    /// </summary>
    private void Awake()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// コンフィグを開きます。
    /// </summary>
    public void Open()
    {
        gameObject.SetActive(true);

        transform.SetAsLastSibling();

        m_selector.Initialize();
    }

    /// <summary>
    /// コンフィグを閉じます。
    /// </summary>
    public void Close()
    {
        ConfigManager.Instance.Save();

        gameObject.SetActive(false);

        OnClosed?.Invoke();
    }
}