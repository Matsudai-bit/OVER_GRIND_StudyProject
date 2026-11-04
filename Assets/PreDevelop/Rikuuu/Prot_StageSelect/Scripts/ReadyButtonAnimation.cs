using UnityEngine;
using UnityEngine.UI;

public class ReadyButtonAnimation : MonoBehaviour
{
    // 明滅させる画像(ボタン本体)
    [SerializeField]
    Image m_image;

    [Header("Frame")]
    // フレームのみの画像(ボタン本体より強く光らせる)
    [SerializeField]
    Image m_frameImage;

    [Header("Color")]
    // 暗いときの色(白=元の見た目、小さいほど暗い)
    [SerializeField]
    Color m_dimColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    // 明るいときの色
    [SerializeField]
    Color m_brightColor = Color.white;

    [Header("Glow (HDR) - ボタン本体")]
    // 重ねる発光色。RGB を 1 より大きくするとBloomで光る
    [SerializeField, ColorUsage(false, true)]
    Color m_glowColor = new Color(3f, 2f, 0.5f, 1f);
    // 明るいときの発光レイヤーの最大alpha
    [SerializeField, Range(0f, 1f)]
    float m_glowMaxAlpha = 0.5f;

    [Header("Glow (HDR) - フレーム")]
    // フレームの発光色(本体より大きい値にすると強く光る)
    [SerializeField, ColorUsage(false, true)]
    Color m_frameGlowColor = new Color(8f, 5f, 1f, 1f);
    // 明るいときのフレーム発光レイヤーの最大alpha
    [SerializeField, Range(0f, 1f)]
    float m_frameGlowMaxAlpha = 1.0f;

    [Header("Ripple (フレームの波紋)")]
    // 波紋の発光色(フレーム本体とは別に明るさを調整できる)
    [SerializeField, ColorUsage(false, true)]
    Color m_rippleColor = new Color(3f, 2f, 0.5f, 1f);
    // 波紋の最大alpha
    [SerializeField, Range(0f, 1f)]
    float m_rippleMaxAlpha = 0.5f;
    // 点滅の明るさ(k)がこの値を下から上に超えた瞬間に波紋を発生させる
    [SerializeField, Range(0.01f, 1f)]
    float m_rippleTriggerLevel = 0.5f;
    // 波紋の本数
    [SerializeField, Range(1, 4)]
    int m_rippleCount = 2;
    // 波紋が広がる距離(px)
    [SerializeField]
    float m_rippleExpand = 14f;
    // 波紋が広がって消えるまでの長さ(周期に対する割合)
    [SerializeField, Range(0.05f, 1f)]
    float m_rippleDuration = 0.4f;
    // 2本目以降の発生遅延(周期に対する割合)
    [SerializeField, Range(0f, 0.5f)]
    float m_rippleStagger = 0.08f;

    [Header("Press (ボタンを押したとき)")]
    // 点滅から「光り続ける状態」へ移るまでの秒数
    [SerializeField]
    float m_pressFadeTime = 0.15f;

    [Header("Timing")]
    // 1回の明滅にかかる秒数
    [SerializeField]
    float m_period = 1.7f;
    // 再生開始時の位置(0.3前後だと最初に「ぱっ」と明るく始まる)
    [SerializeField, Range(0f, 1f)]
    float m_startPhase = 0.3f;
    // ゲームが一時停止中でも動き続けるかどうか
    [SerializeField]
    bool m_useUnscaledTime = true;

    [Header("Curve (横: 1周期 0〜1 / 縦: 0=暗い 1=明るい)")]
    [SerializeField]
    AnimationCurve m_curve = new AnimationCurve(
        new Keyframe(0.00f, 0f),
        new Keyframe(0.30f, 1f),
        new Keyframe(0.55f, 1f),
        new Keyframe(0.85f, 0f),
        new Keyframe(1.00f, 0f));

    // 停止時に戻す色
    Color m_idleColor;
    // 再生中かどうか
    bool m_playing;
    // ボタンが押されて、光り続ける状態かどうか
    bool m_pressed;
    // 押された瞬間の明るさ(ここから 1 へ滑らかに移る)
    float m_pressStartK;
    // 光り続ける状態への移行具合(0〜1)
    float m_pressBlend;
    // 現在の周期内の位置(0〜1)
    float m_phase;
    // 現在の点滅の明るさ
    float m_currentK;

    // ボタン本体の発光用
    RawImage m_glow;
    Texture2D m_glowTex;

    // フレームの発光用
    RawImage m_frameGlow;
    Texture2D m_frameGlowTex;

    // フレームの波紋用
    RawImage[] m_ripples;
    Texture2D m_rippleTex;
    // 各波紋の発生からの経過(周期に対する割合)。負の値は発生待ち
    float[] m_rippleElapsed;
    // 1フレーム前の点滅の明るさ
    float m_prevK;

    void Awake()
    {
        m_idleColor = m_image.color;

        BuildGlow();
        BuildFrameGlow();
        BuildRipples();

        // 再生するまでフレームは非表示
        SetFrameVisible(false);
    }

    // ボタン本体の発光レイヤー(矩形全体)
    void BuildGlow()
    {
        m_glowTex = CreateSolidHdrTexture(m_glowColor);

        m_glow = CreateOverlay("Glow", m_image.rectTransform);
        m_glow.texture = m_glowTex;
    }

    // フレームの発光レイヤー(フレームの形に沿う)
    void BuildFrameGlow()
    {
        if (m_frameImage == null || m_frameImage.sprite == null)
        {
            return;
        }

        m_frameGlowTex = CreateHdrSpriteTexture(m_frameImage.sprite, m_frameGlowColor);

        m_frameGlow = CreateOverlay("FrameGlow", m_frameImage.rectTransform);
        m_frameGlow.texture = m_frameGlowTex;
    }

    // フレームの波紋レイヤー(波紋専用の色でテクスチャを作る)
    void BuildRipples()
    {
        if (m_frameImage == null || m_frameImage.sprite == null)
        {
            return;
        }

        m_rippleTex = CreateHdrSpriteTexture(m_frameImage.sprite, m_rippleColor);

        m_ripples = new RawImage[m_rippleCount];
        m_rippleElapsed = new float[m_rippleCount];
        for (int i = 0; i < m_rippleCount; i++)
        {
            var ripple = CreateOverlay($"FrameRipple_{i}", m_frameImage.rectTransform);
            ripple.texture = m_rippleTex;
            m_ripples[i] = ripple;
            // 最初は発生していない状態(消え切った扱い)
            m_rippleElapsed[i] = 999f;
        }
    }

    // 親と同じ大きさで重ねる RawImage を作る
    RawImage CreateOverlay(string name, RectTransform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var img = go.GetComponent<RawImage>();
        img.raycastTarget = false;
        img.color = new Color(1f, 1f, 1f, 0f);
        return img;
    }

    // HDR値を保持するため RGBAHalf のテクスチャに色を焼き込む
    // (RawImage.color は 0〜1 にクランプされるため)
    Color ToTexColor(Color c)
    {
        if (QualitySettings.activeColorSpace == ColorSpace.Linear)
        {
            c = c.linear;
        }
        c.a = 1f;
        return c;
    }

    Texture2D CreateSolidHdrTexture(Color hdr)
    {
        var tex = new Texture2D(1, 1, TextureFormat.RGBAHalf, false, true)
        { wrapMode = TextureWrapMode.Clamp };
        tex.SetPixel(0, 0, ToTexColor(hdr));
        tex.Apply();
        return tex;
    }

    // スプライトの alpha を引き継いだ HDR テクスチャを作る
    // RenderTexture 経由で読み出すので、元テクスチャの Read/Write 設定は不要
    Texture2D CreateHdrSpriteTexture(Sprite sprite, Color hdr)
    {
        Rect r = sprite.textureRect;
        int w = Mathf.RoundToInt(r.width);
        int h = Mathf.RoundToInt(r.height);

        var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        var prev = RenderTexture.active;

        // スプライト部分だけを切り出して RenderTexture にコピー
        Vector2 scale = new Vector2(r.width / sprite.texture.width, r.height / sprite.texture.height);
        Vector2 offset = new Vector2(r.x / sprite.texture.width, r.y / sprite.texture.height);
        Graphics.Blit(sprite.texture, rt, scale, offset);

        RenderTexture.active = rt;
        var src = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
        src.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        src.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        // alpha だけを使い、色は HDR 値で塗りつぶす
        Color baseColor = ToTexColor(hdr);
        Color[] pixels = src.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color(baseColor.r, baseColor.g, baseColor.b, pixels[i].a);
        }
        Destroy(src);

        var tex = new Texture2D(w, h, TextureFormat.RGBAHalf, false, true)
        { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// 明滅を開始します。
    /// </summary>
    public void Play()
    {
        m_playing = true;
        m_pressed = false;
        m_phase = m_startPhase;
        // 開始直後に明るければ、すぐ波紋が出る(出したくない場合は 1f にする)
        m_prevK = 0f;

        SetFrameVisible(true);
    }

    /// <summary>
    /// ボタンを押したときの処理です。OnClick からこれ1つを呼びます。
    /// 点滅を止めて光り続ける状態にし、波紋を1回再生します。
    /// </summary>
    public void Press()
    {
        // 連打されても、押された状態は1回だけ開始する
        if (m_pressed)
        {
            return;
        }

        m_pressed = true;
        m_playing = true;
        m_pressStartK = m_currentK;
        m_pressBlend = 0f;

        SetFrameVisible(true);
        TriggerRipples();
    }

    /// <summary>
    /// 明滅を止めて、元の色に戻します。押された状態も解除します。
    /// </summary>
    public void Stop()
    {
        m_playing = false;
        m_pressed = false;
        m_currentK = 0f;
        m_image.color = m_idleColor;
        SetGlowAlpha(m_glow, 0f);
        SetGlowAlpha(m_frameGlow, 0f);
        HideRipples();

        SetFrameVisible(false);
    }

    void Update()
    {
        if (!m_playing)
        {
            return;
        }

        float dt = m_useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        float k;
        if (m_pressed)
        {
            // 点滅を止めて、押された瞬間の明るさから 1(光り続ける)へ移る
            m_pressBlend = (m_pressFadeTime <= 0f)
                ? 1f
                : Mathf.MoveTowards(m_pressBlend, 1f, dt / m_pressFadeTime);
            k = Mathf.Lerp(m_pressStartK, 1f, m_pressBlend);
        }
        else
        {
            m_phase = Mathf.Repeat(m_phase + dt / m_period, 1f);
            k = m_curve.Evaluate(m_phase);
        }
        m_currentK = k;

        m_image.color = Color.Lerp(m_dimColor, m_brightColor, k);
        SetGlowAlpha(m_glow, k * m_glowMaxAlpha);
        SetGlowAlpha(m_frameGlow, k * m_frameGlowMaxAlpha);

        UpdateRipples(k, dt, !m_pressed);
    }

    // 全ての波紋を発生させる
    void TriggerRipples()
    {
        if (m_rippleElapsed == null)
        {
            return;
        }

        for (int i = 0; i < m_rippleElapsed.Length; i++)
        {
            // 2本目以降は stagger 分だけ遅らせる
            m_rippleElapsed[i] = -i * m_rippleStagger;
        }
    }

    // 波紋を外側へ広げながらフェードアウトさせる
    // autoTrigger が true のときは、点滅が明るくなる瞬間に自動で発生させる
    void UpdateRipples(float k, float dt, bool autoTrigger)
    {
        if (m_ripples == null)
        {
            return;
        }

        if (autoTrigger && m_prevK < m_rippleTriggerLevel && k >= m_rippleTriggerLevel)
        {
            TriggerRipples();
        }
        m_prevK = k;

        float step = dt / m_period;
        for (int i = 0; i < m_ripples.Length; i++)
        {
            m_rippleElapsed[i] += step;

            float elapsed = m_rippleElapsed[i];
            float p = elapsed / m_rippleDuration;

            var ripple = m_ripples[i];
            if (elapsed < 0f || p >= 1f)
            {
                ripple.color = new Color(1f, 1f, 1f, 0f);
                continue;
            }

            // 最初は速く、だんだんゆっくり広がる
            float ease = 1f - (1f - p) * (1f - p);
            float expand = m_rippleExpand * ease;

            var rt = ripple.rectTransform;
            rt.offsetMin = new Vector2(-expand, -expand);
            rt.offsetMax = new Vector2(expand, expand);

            // 発生直後に素早く現れ、その後フェードアウトする
            float fadeIn = Mathf.Clamp01(p * 8f);
            float fadeOut = (1f - p) * (1f - p);
            ripple.color = new Color(1f, 1f, 1f, m_rippleMaxAlpha * fadeIn * fadeOut);
        }
    }

    void HideRipples()
    {
        if (m_ripples == null)
        {
            return;
        }

        for (int i = 0; i < m_ripples.Length; i++)
        {
            m_ripples[i].color = new Color(1f, 1f, 1f, 0f);
            m_rippleElapsed[i] = 999f;
        }
    }

    void SetGlowAlpha(RawImage glow, float a)
    {
        if (glow == null)
        {
            return;
        }
        glow.color = new Color(1f, 1f, 1f, a);
    }

    // フレーム画像の表示/非表示を切り替える
    void SetFrameVisible(bool visible)
    {
        if (m_frameImage != null)
        {
            m_frameImage.enabled = visible;
        }
    }

    void OnDisable()
    {
        if (m_playing)
        {
            Stop();
        }
    }

    void OnDestroy()
    {
        if (m_glowTex)
        {
            Destroy(m_glowTex);
        }
        if (m_frameGlowTex)
        {
            Destroy(m_frameGlowTex);
        }
        if (m_rippleTex)
        {
            Destroy(m_rippleTex);
        }
    }
}