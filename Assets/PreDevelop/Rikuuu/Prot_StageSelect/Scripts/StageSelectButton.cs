using UnityEngine;

public class StageSelectButton : MonoBehaviour
{
    [Header("テクスチャ")]
    // 通常時テクスチャ
    [SerializeField]
    private Sprite m_defaultTexture;
    // 選択時テクスチャ
    [SerializeField]
    private Sprite m_OnCursorTexture;
    // イメージコンポーネント
    [SerializeField]
    private UnityEngine.UI.Image m_image;

    // アニメーション
    [SerializeField]
    private ReadyButtonAnimation m_animation;

    private SelectButton selectButton;

    /// <summary>
    /// カーソルが乗っているかどうかを取得します。
    /// </summary>
    public bool IsOnCursor => m_isOnCursor;

    // カーソルが合わさっているかどうか
    public bool m_isOnCursor = false;

    private void Awake()
    {
        // 通常時テクスチャを表示する
        m_image.sprite = m_defaultTexture;
        // サイズを調整する
        m_image.SetNativeSize();

        selectButton = GetComponent<SelectButton>();
    }

    // 押されたときの処理 ---------------------------------------

    public void OnCursor()
    {
        // 選択時テクスチャを表示する
        m_image.sprite = m_OnCursorTexture;
        // カーソルが合わさっている
        m_isOnCursor = true;
        // アニメーションを再生させる
        m_animation.Play();
    }

    public void OnCursorExit()
    {
        // 通常時テクスチャを表示する
        m_image.sprite = m_defaultTexture;
        // カーソルが合わさっていない
        m_isOnCursor = false;
        // アニメーションを停止させる
        m_animation.Stop();
    }

    public void OnClick()
    {
        selectButton?.OnClick();
    }

    public void OnClickExit()
    {
        selectButton?.OnClickExit();
    }
}
