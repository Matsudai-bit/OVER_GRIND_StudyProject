using TMPro;
using UnityEngine;

public class ContainerButton : MonoBehaviour
{
    [Header("ボタンテクスチャ")]
    // 通常時ボタンテクスチャ
    [SerializeField]
    private Sprite m_defaultTexture;
    // 選択時ボタンテクスチャ
    [SerializeField]
    private Sprite m_OnCursorTexture;
    // ボタンイメージコンポーネント
    [SerializeField]
    private UnityEngine.UI.Image m_manualImage;

    [Header("説明文設定")]
    // 表示する文字列
    [SerializeField]
    private string m_tooltip = "null";
    // テキストコンポーネント
    [SerializeField]
    private TextMeshProUGUI m_tipText;

    private void Awake()
    {
        // イメージコンポーネントにテクスチャを設定する
        m_manualImage.sprite = m_defaultTexture;   
    }

    public void OnCursor()
    {
        // テクスチャを設定する
        m_manualImage.sprite = m_OnCursorTexture;
        m_manualImage.SetNativeSize();

        // 文字の置き換え
        m_tipText.text = m_tooltip;
    }

    public void OnCursorExit()
    {
        // テクスチャを設定する
        m_manualImage.sprite = m_defaultTexture;
        m_manualImage.SetNativeSize();
    }
}
