using UnityEngine;
using UnityEngine.UI;

public class BorderChaseUI : MonoBehaviour
{
    [Header("Shape")]
    // 線分の本数
    [SerializeField]
    int m_segmentCount = 4;
    // 線分の長さ
    [SerializeField, Range(0.05f, 1.0f)]
    float m_lengthRatio = 0.35f;
    // 線分の太さ
    [SerializeField]
    float m_thickness = 8.0f;

    [Header("Gradient (線の進行方向)")]
    // 線分の先頭側の色
    [SerializeField, ColorUsage(true, true)]
    Color m_headColor = new Color32(0xC8, 0x84, 0x1A, 255);
    // 線分の後ろ側の色
    [SerializeField, ColorUsage(true, true)]
    Color m_tailColor = new Color32(0x3D, 0x37, 0x28, 0);
    // 色の変化カーブ
    [SerializeField, Range(0.1f, 4f)]
    float m_gradientPower = 1f;
    // 線分の先端をぼかす長さ
    [SerializeField, Range(0f, 0.5f)]
    float m_headFade = 0.15f;

    [Header("Cross Fade (線の縁のにじみ具合)")]
    // 線の縁のにじみ具合
    [SerializeField, Range(0f, 1f)]
    float m_edgeSoftness = 0.5f;

    [Header("Motion")]
    // 周回速度
    [SerializeField]
    float m_speed = 0.1f;
    // 開始地点
    [SerializeField]
    float m_startPhase = 0.25f;
    // ゲームが一時停止中でも動き続けるかどうか
    [SerializeField]
    bool m_useUnscaledTime = true;

    [Header("Reverse")]
    // 移動・先頭と後ろの色を反転させるかどうか
    [SerializeField] 
    bool m_reverseDirection = false;

    // 線の進行方向のテクスチャ解像度
    const int ALONG_RES = 256;
    // 太さ方向の解像度
    const int ACROSS_RES = 32;
    // 角パッチ画像一辺の解像度
    const int PATCH_RES = 48;
    // 角パッチの色の段階数
    const int PATCH_LEVELS = 16;

    // オブジェクト自身の矩形
    RectTransform m_rt;
    // 線分をまとめる親 
    RectTransform m_barRoot;
    // 辺ごとに線を描く細長い画像の一覧
    RawImage[,,] m_bars;
    // 角を描く画像の一覧
    RawImage[,] m_patches;
    // 上下の辺/左右の辺用グラデーション画像
    Texture2D m_texH, m_texV;
    // 角パッチ画像
    Texture2D[] m_patchTex;

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
        m_patches = new RawImage[m_segmentCount, 4];

        for (int s = 0; s < m_segmentCount; s++)
        {
            for (int e = 0; e < 4; e++)
            {
                for (int k = 0; k < 2; k++)
                {
                    m_bars[s, e, k] = CreateImage($"bar_{s}_{e}_{k}", (e % 2 == 0) ? m_texH : m_texV);
                }
                m_patches[s, e] = CreateImage($"corner_{s}_{e}", m_patchTex[0]);
            }
        }
    }

    RawImage CreateImage(string name, Texture tex)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
        var rt = (RectTransform)go.transform;
        rt.SetParent(m_barRoot, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);

        var img = go.GetComponent<RawImage>();
        img.texture = tex;
        img.raycastTarget = false;
        img.enabled = false;
        return img;
    }

    // x: 0=後ろ, 1=先頭
    Color GetAlongColor(float x)
    {
        float t = Mathf.Pow(Mathf.Clamp01(x), m_gradientPower);
        Color c = Color.Lerp(m_tailColor, m_headColor, t);

        if (m_headFade > 0f)
        {
            c.a *= Mathf.SmoothStep(1f, 0f, Mathf.Clamp01((x - (1f - m_headFade)) / m_headFade));
        }
        return c;
    }

    // 線の中心からの正規化距離 d(0=中心, 1=端)に対するぼかし
    float CrossProfile(float d)
    {
        float edge = 1f - d;
        if (m_edgeSoftness <= 0f)
        {
            return edge > 0f ? 1f : 0f;
        }
        return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(edge / m_edgeSoftness));
    }

    // Linear カラースペースのプロジェクトでは見た目を合わせるために変換
    Color ToTexColor(Color c)
    {
        if (QualitySettings.activeColorSpace == ColorSpace.Linear)
        {
            float alpha = c.a;
            c = c.linear;
            c.a = alpha;
        }
        return c;
    }

    Texture2D NewHalfTexture(int w, int h)
    {
        return new Texture2D(w, h, TextureFormat.RGBAHalf, false, true)
        { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
    }

    void BuildTextures()
    {
        // 横向き: x=進行方向, y=太さ方向 / 縦向き: x=太さ方向, y=進行方向
        m_texH = NewHalfTexture(ALONG_RES, ACROSS_RES);
        m_texV = NewHalfTexture(ACROSS_RES, ALONG_RES);

        for (int i = 0; i < ALONG_RES; i++)
        {
            Color along = GetAlongColor(i / (float)(ALONG_RES - 1));

            for (int j = 0; j < ACROSS_RES; j++)
            {
                float c = j / (float)(ACROSS_RES - 1);
                float d = Mathf.Abs(2f * c - 1f);

                Color col = along;
                col.a *= CrossProfile(d);
                col = ToTexColor(col);

                m_texH.SetPixel(i, j, col);
                m_texV.SetPixel(j, i, col);
            }
        }
        m_texH.Apply();
        m_texV.Apply();

        // 角パッチ: 角を中心にした正方形(一辺 = 太さ)。
        // 基準は「左から来る線」と「下へ向かう線」のL字(右上の角)。
        // 2本の線までの距離の近い方だけでぼかすので、重なって明るくなることがない。
        m_patchTex = new Texture2D[PATCH_LEVELS];
        for (int k = 0; k < PATCH_LEVELS; k++)
        {
            Color baseColor = GetAlongColor(k / (float)(PATCH_LEVELS - 1));
            var tex = NewHalfTexture(PATCH_RES, PATCH_RES);

            for (int py = 0; py < PATCH_RES; py++)
            {
                for (int px = 0; px < PATCH_RES; px++)
                {
                    float cx = px / (float)(PATCH_RES - 1) * 2f - 1f;   // -1(左)〜+1(右)
                    float cy = py / (float)(PATCH_RES - 1) * 2f - 1f;   // -1(下)〜+1(上)
                    float ax = Mathf.Abs(cx), ay = Mathf.Abs(cy);

                    float d;
                    if (cx > 0f && cy > 0f)
                    {
                        d = Mathf.Sqrt(ax * ax + ay * ay); // 外側の角は丸く
                    }
                    else if (cx > 0f)
                    {
                        d = ax;
                    }
                    else if (cy > 0f)
                    {
                        d = ay;
                    }
                    else
                    {
                        d = Mathf.Min(ax, ay);             // 内側は近い方の線だけ
                    }

                    Color col = baseColor;
                    col.a *= (d >= 1f) ? 0f : CrossProfile(d);
                    tex.SetPixel(px, py, ToTexColor(col));
                }
            }
            tex.Apply();
            m_patchTex[k] = tex;
        }
    }

    void Update()
    {
        float W = m_rt.rect.width, H = m_rt.rect.height;
        float P = 2 * (W + H);
        float t = m_useUnscaledTime ? Time.unscaledTime : Time.time;
        float dir = m_reverseDirection ? -1f : 1f;
        float phase = m_startPhase + dir * t * m_speed;
        float segLen = P / m_segmentCount * m_lengthRatio;
        float half = m_thickness * 0.5f;

        float[] edgeStart = { 0, W, W + H, 2 * W + H };
        float[] edgeLen = { W, H, W, H };
        // 角の外周位置: 0=左上, 1=右上, 2=右下, 3=左下
        float[] cornerS = { 0, W, W + H, 2 * W + H };

        for (int s = 0; s < m_segmentCount; s++)
        {
            float a0 = Mathf.Repeat(phase + (float)s / m_segmentCount, 1f) * P;

            // 辺の棒(両端を half ずつ縮めて、角の領域には入らない)
            for (int k = 0; k < 2; k++)
            {
                float a = a0 - k * P;
                float b = a + segLen;
                for (int e = 0; e < 4; e++)
                {
                    float lo = Mathf.Max(a, edgeStart[e] + half);
                    float hi = Mathf.Min(b, edgeStart[e] + edgeLen[e] - half);
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
                            rt.anchoredPosition = new Vector2(u0, half);
                            rt.sizeDelta = new Vector2(len, m_thickness);
                            img.uvRect = new Rect(gA, 0, gB - gA, 1);
                            break;

                        // 右: 上→下(上が後ろ側)
                        case 1:
                            rt.anchoredPosition = new Vector2(W - half, -u0);
                            rt.sizeDelta = new Vector2(m_thickness, len);
                            img.uvRect = new Rect(0, gB, 1, gA - gB);
                            break;

                        // 下: 右→左(右が後ろ側)
                        case 2:
                            rt.anchoredPosition = new Vector2(W - u1, -(H - half));
                            rt.sizeDelta = new Vector2(len, m_thickness);
                            img.uvRect = new Rect(gB, 0, gA - gB, 1);
                            break;

                        // 左: 下→上(下が後ろ側)
                        case 3:
                            rt.anchoredPosition = new Vector2(-half, -(H - u1));
                            rt.sizeDelta = new Vector2(m_thickness, len);
                            img.uvRect = new Rect(0, gA, 1, gB - gA);
                            break;
                    }
                }
            }

            // 角のパッチ(線分が角をまたいでいるときだけ表示)
            for (int c = 0; c < 4; c++)
            {
                var patch = m_patches[s, c];
                bool shown = false;
                float g = 0f;

                for (int k = 0; k < 2; k++)
                {
                    float a = a0 - k * P;
                    float b = a + segLen;
                    if (cornerS[c] >= a && cornerS[c] <= b)
                    {
                        shown = true;
                        g = (cornerS[c] - a) / segLen;
                        break;
                    }
                }

                if (!shown)
                {
                    patch.enabled = false;
                    continue;
                }

                if (m_reverseDirection)
                {
                    g = 1f - g;
                }
                int level = Mathf.Clamp(Mathf.RoundToInt(g * (PATCH_LEVELS - 1)), 0, PATCH_LEVELS - 1);
                patch.texture = m_patchTex[level];
                patch.enabled = true;

                var prt = (RectTransform)patch.transform;
                prt.sizeDelta = new Vector2(m_thickness, m_thickness);

                // 角を中心にした正方形を置き、L字の向きは uv の反転で合わせる
                switch (c)
                {
                    case 0: // 左上(右・下へ伸びるL)
                        prt.anchoredPosition = new Vector2(-half, half);
                        patch.uvRect = new Rect(1, 0, -1, 1);
                        break;
                    case 1: // 右上(左・下へ伸びるL)
                        prt.anchoredPosition = new Vector2(W - half, half);
                        patch.uvRect = new Rect(0, 0, 1, 1);
                        break;
                    case 2: // 右下(左・上へ伸びるL)
                        prt.anchoredPosition = new Vector2(W - half, -H + half);
                        patch.uvRect = new Rect(0, 1, 1, -1);
                        break;
                    case 3: // 左下(右・上へ伸びるL)
                        prt.anchoredPosition = new Vector2(-half, -H + half);
                        patch.uvRect = new Rect(1, 1, -1, -1);
                        break;
                }
            }
        }
    }

    void OnDestroy()
    {
        if (m_texH) Destroy(m_texH);
        if (m_texV) Destroy(m_texV);
        if (m_patchTex != null)
        {
            foreach (var tex in m_patchTex)
            {
                if (tex) Destroy(tex);
            }
        }
    }
}