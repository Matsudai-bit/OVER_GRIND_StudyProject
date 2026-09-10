using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class TitleButton : MonoBehaviour
{
    // 画像の最大スケール
    const float MAX_TEXTURE_SCALE = 1.0f;
    // 画像の最小スケール
    const float MIN_TEXTURE_SCALE = 0.6f;

    // スケールの変化量
    [SerializeField]
    const float SCALE_SPEED = 2.0f;

    // 通常状態で表示する画像
    [SerializeField]
    private Sprite m_defaultTexture;

    // カーソルが乗っているときに表示する画像
    [SerializeField]
    private Sprite m_onCursorTexture;

    // 現在表示している画像を入れるコンポーネント
    [SerializeField]
    private UnityEngine.UI.Image m_texture;

    // 表示する文字列
    [SerializeField]
    private string m_string = "null";

    // テキストコンポーネント
    [SerializeField]
    private TextMeshProUGUI m_text;

    // 初期の画像サイズ（カーソル状態）
    private Vector2 m_textureSize;
    // 現在表示してる画像サイズ
    private float m_currentScale = MAX_TEXTURE_SCALE;

    private void Awake()
    {
        // 通常状態画像を表示する
        m_texture.sprite = m_defaultTexture;
        m_texture.SetNativeSize();

        // 画像サイズを取得する
        m_textureSize.x = m_texture.rectTransform.sizeDelta.x;
        m_textureSize.y = m_texture.rectTransform.sizeDelta.y;

        // 拡縮の基準点を決める
        Vector2 oldSize = m_texture.rectTransform.sizeDelta;
        Vector2 oldPivot = m_texture.rectTransform.pivot;
        m_texture.rectTransform.pivot = new Vector2(1.0f, 0.5f);

        // Pivot変更によるズレを補正
        m_texture.rectTransform.anchoredPosition += new Vector2(
            (m_texture.rectTransform.pivot.x - oldPivot.x) * oldSize.x,
            (m_texture.rectTransform.pivot.y - oldPivot.y) * oldSize.y
        );
    }

    private void Update()
    {
        // カーソルで選択されている場合
        if(m_texture.sprite == m_onCursorTexture)
        {

            // 変更
            m_currentScale += SCALE_SPEED * Time.deltaTime;
            

        }

        // スケールを範囲内に収める
        Clamp();

        // サイズを適用させる
        m_texture.rectTransform.sizeDelta = new Vector2(
            m_textureSize.x * m_currentScale,
            m_textureSize.y * m_currentScale
        );
    }

    // 押されたときの処理 ---------------------------------------

    public void OnCursor()
    {
        // カーソル状態の画像を表示する
        m_texture.sprite = m_onCursorTexture;
        m_texture.SetNativeSize();

        // 画像サイズの変更
        m_currentScale = MIN_TEXTURE_SCALE;

        // 文字の置き換え
        m_text.text = m_string;
    }

    public void OnCursorExit()
    {
        // 通常状態画像を表示する
        m_texture.sprite = m_defaultTexture;
        m_texture.SetNativeSize();

        // 画像サイズの変更
        m_currentScale = MAX_TEXTURE_SCALE;



    }

    // ----------------------------------------------------------

    private void Clamp()
    {
        if(m_currentScale > MAX_TEXTURE_SCALE)
        {
            m_currentScale = MAX_TEXTURE_SCALE;
        }
        if(m_currentScale < MIN_TEXTURE_SCALE)
        {
            m_currentScale = MIN_TEXTURE_SCALE;
        }
    }
}
