using UnityEngine;
using UnityEngine.UI;

public class BorderChaseUI : MonoBehaviour
{
    [Header("Shape")]
    [SerializeField] 
    int m_segmentCount = 4;
    // 1周期に対する線分の長さ
    [SerializeField, Range(0.05f, 1.0f)] 
    float m_lengthRatio = 0.35f;
    // グローのぼけ分も含めた太さ
    [SerializeField] 
    float m_thickness = 8.0f;

    [Header("Gradient (線の進行方向)")]
    // 後ろ側(アルファ0で消える)
    [SerializeField] 
    Color m_tailColor = new Color32(0x3D, 0x37, 0x28, 0);
    // 先頭側
    [SerializeField] 
    Color m_headColor = new Color32(0xC8, 0x84, 0x1A, 255);
    // 1=直線、大きいほど先頭に色が寄る
    [SerializeField, Range(0.1f, 4f)] 
    float m_gradientPower = 1f;  

    [Header("Cross Fade (線の太さ方向のぼかし)")]
    [SerializeField, Range(0f, 1f)] 
    float m_edgeSoftness = 0.5f;

    [Header("Motion")]
    // 1秒あたりの周回数
    [SerializeField] 
    float m_speed = 0.1f;
    // AEの回転+90°相当
    [SerializeField] 
    float m_startPhase = 0.25f;
    [SerializeField] 
    bool m_useUnscaledTime = true;

    [Header("Reverse")]
    // 移動・先頭と後ろの色を反転させるかどうか
    [SerializeField] bool m_reverseDirection = false;

    const int ALONG_RES = 256;
    const int ACROSS_RES = 32;

    RectTransform m_rt;
    RectTransform m_barRoot;
    RawImage[,,] m_bars; // [segment, edge, shift]
    Texture2D m_texH, m_texV;

    void Awake()
    {
        m_rt = (RectTransform)transform;
        BuildTextures();

        var root = new GameObject("Bars", typeof(RectTransform));
        m_barRoot = (RectTransform)root.transform;
        m_barRoot.SetParent(m_rt, false);
        m_barRoot.anchorMin = Vector2.zero;
        m_barRoot.anchorMax = Vector2.one;
        m_barRoot.offsetMin = m_barRoot.offsetMax = Vector2.zero;
        m_barRoot.pivot = new Vector2(0, 1);

        m_bars = new RawImage[m_segmentCount, 4, 2];
        for (int s = 0; s < m_segmentCount; s++)
        {
            for (int e = 0; e < 4; e++)
            {
                for (int k = 0; k < 2; k++)
                {
                    var go = new GameObject($"bar_{s}_{e}_{k}", typeof(RectTransform), typeof(RawImage));

                    var rt = (RectTransform)go.transform;
                    rt.SetParent(m_barRoot, false);
                    rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);

                    var img = go.GetComponent<RawImage>();
                    img.texture = (e % 2 == 0) ? m_texH : m_texV;
                    img.raycastTarget = false;
                    img.enabled = false;

                    m_bars[s, e, k] = img;
                }
            }
        }
    }

    void BuildTextures()
    {
        // 横向き: x=進行方向, y=太さ方向
        m_texH = new Texture2D(ALONG_RES, ACROSS_RES, TextureFormat.RGBA32, false)
        { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        // 縦向き: x=太さ方向, y=進行方向
        m_texV = new Texture2D(ACROSS_RES, ALONG_RES, TextureFormat.RGBA32, false)
        { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };

        for (int i = 0; i < ALONG_RES; i++)
        {
            float t = Mathf.Pow(i / (float)(ALONG_RES - 1), m_gradientPower);
            Color along = Color.Lerp(m_tailColor, m_headColor, t);
            
            for (int j = 0; j < ACROSS_RES; j++)
            {
                float c = j / (float)(ACROSS_RES - 1);
                float edge = 1f - Mathf.Abs(2f * c - 1f);
                float a = m_edgeSoftness <= 0f ? 1f : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(edge / m_edgeSoftness));
                
                Color col = along;
                col.a *= a;
                
                m_texH.SetPixel(i, j, col);
                m_texV.SetPixel(j, i, col);
            }
        }
        m_texH.Apply();
        m_texV.Apply();
    }

    void Update()
    {
        float W = m_rt.rect.width, H = m_rt.rect.height;
        float P = 2 * (W + H);
        float t = m_useUnscaledTime ? Time.unscaledTime : Time.time;
        float dir = m_reverseDirection ? -1f : 1f;
        float phase = m_startPhase + dir * t * m_speed;
        float segLen = P / m_segmentCount * m_lengthRatio;

        float[] edgeStart = { 0, W, W + H, 2 * W + H };
        float[] edgeLen = { W, H, W, H };

        for (int s = 0; s < m_segmentCount; s++)
        {
            float a0 = Mathf.Repeat(phase + (float)s / m_segmentCount, 1f) * P;
            for (int k = 0; k < 2; k++)
            {
                float a = a0 - k * P;
                float b = a + segLen;
                for (int e = 0; e < 4; e++)
                {
                    float lo = Mathf.Max(a, edgeStart[e]);
                    float hi = Mathf.Min(b, edgeStart[e] + edgeLen[e]);
                    var img = m_bars[s, e, k];
                    if (hi <= lo) 
                    { 
                        img.enabled = false; continue; 
                    }
                    img.enabled = true;

                    float u0 = lo - edgeStart[e], u1 = hi - edgeStart[e], len = u1 - u0;
                    // この断片が線分全体(0=後ろ, 1=先頭)のどこに当たるか
                    float g0 = (lo - a) / segLen;
                    float g1 = (hi - a) / segLen;

                    // 反転時はグラデーションの向きも逆にする
                    float gA = m_reverseDirection ? 1f - g0 : g0;
                    float gB = m_reverseDirection ? 1f - g1 : g1;

                    var rt = (RectTransform)img.transform;
                    switch (e)
                    {
                        // 上: 左→右
                        case 0:
                            rt.anchoredPosition = new Vector2(u0, 0);
                            rt.sizeDelta = new Vector2(len, m_thickness);
                            img.uvRect = new Rect(gA, 0, gB - gA, 1);
                            break;

                        // 右: 上→下(上が後ろ側)
                        case 1:
                            rt.anchoredPosition = new Vector2(W - m_thickness, -u0);
                            rt.sizeDelta = new Vector2(m_thickness, len);
                            img.uvRect = new Rect(0, gB, 1, gA - gB);
                            break;

                        // 下: 右→左(右が後ろ側)
                        case 2:
                            rt.anchoredPosition = new Vector2(W - u1, -(H - m_thickness));
                            rt.sizeDelta = new Vector2(len, m_thickness);
                            img.uvRect = new Rect(gB, 0, gA - gB, 1);
                            break;

                        // 左: 下→上(下が後ろ側)
                        case 3:
                            rt.anchoredPosition = new Vector2(0, -(H - u1));
                            rt.sizeDelta = new Vector2(m_thickness, len);
                            img.uvRect = new Rect(0, gA, 1, gB - gA);
                            break;
                    }
                }
            }
        }
    }

    void OnDestroy()
    {
        if (m_texH)
        {
            Destroy(m_texH);
        }
        if (m_texV)
        {
            Destroy(m_texV);
        }
    }
}
