using Unity.AppUI.UI;
using UnityEngine;

public class HeaderButton : MonoBehaviour
{
    [Header("スケール関連")]
    // イメージの最大スケール
    [SerializeField]
    private float MAX_IMAGE_SCALE = 1.5f;
    // イメージの最小スケール
    [SerializeField]
    private float MIN_IMAGE_SCALE = 1.0f;
    // スケールの変化量
    [SerializeField]
    private float SCALE_SPEED = 2.0f;

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

    // 現在のイメージスケール
    private float m_currentScale;
    // カーソルで選択されているかどうか
    private bool m_isOnCursor = false;

    private void Awake()
    {
        // 通常時テクスチャを表示する
        m_image.sprite = m_defaultTexture;
        // サイズを調整する
        m_image.SetNativeSize();

        // スケールの初期化
        m_currentScale = MIN_IMAGE_SCALE;
    }

    private void Update()
    {
        // 選択されている場合
        if(m_isOnCursor)
        {
            // スケールを大きくする
            m_currentScale += SCALE_SPEED * Time.deltaTime;
        }
        // 選択されていない場合
        else
        {
            // スケールを小さくする
            m_currentScale -= SCALE_SPEED * Time.deltaTime;
        }

        // 値の制限を行う
        m_currentScale = Mathf.Clamp(m_currentScale, MIN_IMAGE_SCALE, MAX_IMAGE_SCALE);

        // 画像にスケールを適用する
        m_image.rectTransform.localScale = new Vector3(
            m_currentScale,
            m_currentScale,
            1.0f
        );
    }

    // 押されたときの処理 ---------------------------------------

    public void OnCursor()
    {
        // 選択時テクスチャを表示する
        m_image.sprite = m_OnCursorTexture;
        // サイズを調整する
        //m_image.SetNativeSize();

        // カーソルで選択されている状態にする
        m_isOnCursor = true;
    }

    public void OnCursorExit()
    {
        // 通常時テクスチャを表示する
        m_image.sprite = m_defaultTexture;
        // サイズを調整する
        //m_image.SetNativeSize();

        // カーソルで選択されていない状態にする
        m_isOnCursor = false;
    }
}
