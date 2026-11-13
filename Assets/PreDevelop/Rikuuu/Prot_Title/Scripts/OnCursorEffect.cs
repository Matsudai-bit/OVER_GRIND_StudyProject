using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
public class OnCursorEffect : MonoBehaviour
{
    [SerializeField] RectTransform m_glow;            // 基準にするオブジェクト（ボタンの箱）
    [SerializeField] Color m_color = Color.white;
    [SerializeField] float m_maxAlpha = 0.35f;
    [SerializeField, Range(0f, 1f)] float m_rightEdgeRatio = 0.26f; // 右端の位置（左辺から内側へ、箱の幅に対する割合）
    [SerializeField, Range(0f, 1f)] float m_maxWidthRatio = 0.26f;  // 最大の幅（箱の幅に対する割合）
    [SerializeField] float m_expandTime = 2.2f;       // 広がる時間
    [SerializeField] float m_fadeOutTime = 1.0f;      // フェードアウトの時間（変更）

    RectTransform m_fx;
    Image m_image;
    Sequence m_seq;

    void Awake()
    {
        var go = new GameObject("SelectPulseFx", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        m_fx = go.GetComponent<RectTransform>();
        m_image = go.GetComponent<Image>();
        go.GetComponent<LayoutElement>().ignoreLayout = true;

        m_fx.SetParent(m_glow, false);
        m_fx.SetAsFirstSibling(); // 親の背景より前、文字より後ろ

        // 左辺基準・高さは親に追従・ピボットは右端（幅が増えると左へ伸びる）
        m_fx.anchorMin = new Vector2(0f, 0f);
        m_fx.anchorMax = new Vector2(0f, 1f);
        m_fx.pivot = new Vector2(1f, 0.5f);
        m_fx.sizeDelta = Vector2.zero;

        m_image.sprite = CreateGradientSprite(64);
        m_image.type = Image.Type.Simple;
        m_image.raycastTarget = false;

        Stop();
    }

    // 選択されたときに1回だけ呼ぶ
    public void Play()
    {
        m_seq?.Kill();

        float baseWidth = m_glow.rect.width;
        float rightEdge = baseWidth * m_rightEdgeRatio;
        float maxWidth = baseWidth * m_maxWidthRatio;

        m_fx.anchoredPosition = new Vector2(rightEdge, 0f); // 右端をボタン内側に固定
        m_fx.gameObject.SetActive(true);
        SetWidth(0f);
        m_image.color = new Color(m_color.r, m_color.g, m_color.b, 0f);

        m_seq = DOTween.Sequence().SetUpdate(true)
            // 広がりながら明るくなる
            .Append(DOTween.To(() => m_fx.sizeDelta.x, SetWidth, maxWidth, m_expandTime)
                .SetEase(Ease.InOutSine))
            .Join(m_image.DOFade(m_maxAlpha, m_expandTime).SetEase(Ease.InOutSine))
            // 幅はそのまま、フェードアウトだけで消える（変更）
            .Append(m_image.DOFade(0f, m_fadeOutTime).SetEase(Ease.InOutSine))
            .OnComplete(() => m_fx.gameObject.SetActive(false))
            .SetLink(gameObject);
    }

    public void Stop()
    {
        m_seq?.Kill();
        SetWidth(0f);
        m_fx.gameObject.SetActive(false);
    }

    void SetWidth(float w)
    {
        var s = m_fx.sizeDelta;
        s.x = w;
        m_fx.sizeDelta = s;
    }

    // 左(透明) → 右(不透明)
    static Sprite CreateGradientSprite(int width)
    {
        var tex = new Texture2D(width, 1, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int x = 0; x < width; x++)
            tex.SetPixel(x, 0, new Color(1f, 1f, 1f, x / (float)(width - 1)));
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, 1), new Vector2(0.5f, 0.5f), 100f);
    }
}