using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

enum ManualElementType
{
    NONE = 0,

    IMAGE,        // 画像
    TEXT,         // テキスト
}

[Serializable]
struct ManualElement
{
    // 種類
    public ManualElementType ElementType;

    // キー画像名・文章
    public string Content;
}

public class ManualContainerButton : MonoBehaviour
{
    [SerializeField, Header("入力画像Manager")]
    private InputCommandSpriteManager m_spriteManager;

    // 再生するビデオ
    [SerializeField]
    private VideoClip m_video;
    // ビデオプレイヤー
    [SerializeField]
    private VideoPlayer m_videoPlayer;

    [Space]

    // 説明文文章
    [SerializeField]
    private ManualElement[] m_elements;
    // 説明文を生成する親オブジェクト
    [SerializeField]
    private GameObject m_parentObject;

    [Space]

    // キー画像Prefab
    [SerializeField]
    private Image m_image;
    // テキストPrefab
    [SerializeField]
    private TextMeshProUGUI m_text;

    public void OnCursor()
    {
        // 再生するビデオを入れ替える
        m_videoPlayer.clip = m_video;

        // 親の子を全て削除する
        ClearChildren(m_parentObject.transform);

        // 説明文を変更する
        foreach(ManualElement element in m_elements)
        {
            switch (element.ElementType)
            {
                case ManualElementType.IMAGE:
                    // オブジェクトを生成する
                    Image image = Instantiate(m_image, m_parentObject.transform, false);

                    // 画像を入れ替える
                    image.sprite = m_spriteManager.GetSprite(element.Content);
                    // 画像のサイズを整える
                    image.SetNativeSize();
                    break;

                case ManualElementType.TEXT:
                    // オブジェクトを生成する
                    TextMeshProUGUI text = Instantiate(m_text, m_parentObject.transform, false);
                    
                    // 文字列を入れ替える
                    text.text = element.Content;
                    break;

                default:
                    break;
            }

        }
    }

    private void ClearChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
            // 先に非表示にして、レイアウトの計算から外す
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }
}
