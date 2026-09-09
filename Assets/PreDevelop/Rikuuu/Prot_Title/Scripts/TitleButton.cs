using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class TitleButton : MonoBehaviour
{
    // 通常状態で表示する画像
    [SerializeField]
    private Sprite m_defaultTexture;

    // カーソルが乗っているときに表示する画像
    [SerializeField]
    private Sprite m_onCursorTexture;

    // 現在表示している画像を入れるコンポーネント
    [SerializeField]
    private UnityEngine.UI.Image m_texture;

    private void Awake()
    {
        // 通常状態画像を表示する
        m_texture.sprite = m_defaultTexture;
        m_texture.SetNativeSize();
    }

    // 押されたときの処理 ---------------------------------------

    public void OnCursor()
    {
        // カーソル状態の画像を表示する
        m_texture.sprite = m_onCursorTexture;
        m_texture.SetNativeSize();
    }

    public void OnCursorExit()
    {
        // 通常状態画像を表示する
        m_texture.sprite = m_defaultTexture;
        m_texture.SetNativeSize();
    }
}
