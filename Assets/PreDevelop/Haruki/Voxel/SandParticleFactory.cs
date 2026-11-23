using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 砂エフェクト用のテクスチャ、マテリアル、および ParticleSystem を生成する静的ファクトリクラス。
/// </summary>
public static class SandParticleFactory
{
    /// <summary>
    /// 砂ぼこりマテリアルに適用するソフトパーティクル設定。
    /// </summary>
    public struct SoftParticleSettings
    {
        /// <summary>ソフトパーティクルを有効にするか。</summary>
        public bool m_enableSoftParticles;
        /// <summary>他の面との距離がこの値以下で完全に透明になる。</summary>
        public float m_softNearFade;
        /// <summary>他の面との距離がこの値以上で完全に不透明になる。</summary>
        public float m_softFarFade;
        /// <summary>カメラフェードを有効にするか。</summary>
        public bool m_enableCameraFade;
        /// <summary>カメラからの距離がこの値以下で完全に透明になる。</summary>
        public float m_cameraNearFade;
        /// <summary>カメラからの距離がこの値以上で完全に不透明になる。</summary>
        public float m_cameraFarFade;
    }

    // ---- 定数 ----
    private const int GRAIN_MAX_PARTICLES = 3000;      // 砂粒パーティクルの最大数
    private const int DUST_MAX_PARTICLES = 800;        // 砂ぼこりパーティクルの最大数
    private const float DUST_GRAVITY_MODIFIER = -0.03f; // 砂ぼこりの重力係数（わずかに浮く）
    private const float NOISE_OCTAVE_NORM = 0.9375f;   // 4オクターブパーリンノイズの正規化係数
    private const float MIN_FADE_SPAN = 0.01f;         // フェード範囲の最小幅

    /// <summary>
    /// 白い円形のアルファテクスチャを生成します。
    /// </summary>
    /// <param name="size">テクスチャサイズ(px)。</param>
    /// <param name="hardness">輪郭の硬さ(0〜1)。</param>
    /// <returns>生成された Texture2D。</returns>
    public static Texture2D CreateRoundTexture(int size, float hardness)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        Color32[] pixels = new Color32[size * size];
        float denom = Mathf.Max(0.001f, 1f - hardness);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = CalculateSmoothStep(0f, 1f, (1f - dist) / denom);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply(false);
        return tex;
    }

    /// <summary>
    /// ノイズで輪郭を崩した煙の塊をタイル状に並べたテクスチャシートを生成します。
    /// </summary>
    /// <param name="tileSize">1タイルのサイズ(px)。</param>
    /// <param name="tiles">1辺のタイル分割数。</param>
    /// <returns>生成された Texture2D。</returns>
    public static Texture2D CreatePuffSheet(int tileSize, int tiles)
    {
        int totalSize = tileSize * tiles;
        Texture2D tex = new Texture2D(totalSize, totalSize, TextureFormat.RGBA32, true);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color32[] pixels = new Color32[totalSize * totalSize];

        for (int ty = 0; ty < tiles; ty++)
        {
            for (int tx = 0; tx < tiles; tx++)
            {
                int tileIndex = ty * tiles + tx;
                float ox = tileIndex * 37.1f + 5.3f;
                float oy = tileIndex * 19.7f + 11.9f;

                for (int y = 0; y < tileSize; y++)
                {
                    for (int x = 0; x < tileSize; x++)
                    {
                        float u = (x + 0.5f) / tileSize;
                        float v = (y + 0.5f) / tileSize;
                        float dx = u * 2f - 1f;
                        float dy = v * 2f - 1f;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);

                        // 4オクターブのフラクタルノイズ
                        float noise = 0f;
                        float amp = 0.5f;
                        float freq = 2.5f;
                        for (int o = 0; o < 4; o++)
                        {
                            noise += amp * Mathf.PerlinNoise(ox + u * freq, oy + v * freq);
                            amp *= 0.5f;
                            freq *= 2f;
                        }
                        noise = Mathf.Clamp01(noise / NOISE_OCTAVE_NORM);

                        float edge = Mathf.Clamp01((1f - dist) * 1.6f);
                        float density = edge * edge * (0.45f + noise * 1.1f);
                        float alpha = CalculateSmoothStep(0.12f, 1.1f, density);

                        int index = (ty * tileSize + y) * totalSize + (tx * tileSize + x);
                        pixels[index] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                    }
                }
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply(true);
        return tex;
    }

    /// <summary>
    /// パーティクル用の半透明マテリアルを生成します。
    /// </summary>
    /// <param name="tex">割り当てるテクスチャ。</param>
    /// <param name="isDust">砂ぼこり用マテリアルかどうか。</param>
    /// <param name="softSettings">ソフトパーティクル設定。</param>
    /// <param name="logContext">警告ログ出力時のコンテキスト。</param>
    /// <returns>生成された Material。シェーダーが見つからない場合は null。</returns>
    public static Material CreateParticleMaterial(
        Texture2D tex,
        bool isDust,
        SoftParticleSettings softSettings,
        Object logContext)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        if (shader == null)
        {
            Debug.LogWarning("[SandEffects] パーティクル用シェーダーが見つかりません。grainMaterial / dustMaterial を割り当ててください。", logContext);
            return null;
        }

        Material mat = new Material(shader);
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);

        // URP の Particles/Unlit を半透明(アルファブレンド)に設定
        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", 5f);       // SrcAlpha
            mat.SetFloat("_DstBlend", 10f);      // OneMinusSrcAlpha
            mat.SetFloat("_SrcBlendAlpha", 1f);
            mat.SetFloat("_DstBlendAlpha", 10f);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        if (isDust)
        {
            ApplySoftParticleSettings(mat, softSettings, logContext);
        }

        return mat;
    }

    /// <summary>
    /// 砂粒用の ParticleSystem を構築します。
    /// </summary>
    public static ParticleSystem CreateGrainSystem(Transform parent, Material mat, bool enableCollision, LayerMask collisionMask)
    {
        ParticleSystem ps = CreateBaseSystem("SandGrains", parent, mat, GRAIN_MAX_PARTICLES, 1f);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.75f, 1f),
            new Keyframe(1f, 0.2f)
        ));

        var col = ps.collision;
        col.enabled = enableCollision;
        col.type = ParticleSystemCollisionType.World;
        col.mode = ParticleSystemCollisionMode.Collision3D;
        col.collidesWith = collisionMask;
        col.enableDynamicColliders = false;
        col.dampen = 0.5f;
        col.bounce = 0.25f;
        col.lifetimeLoss = 0.1f;
        col.quality = ParticleSystemCollisionQuality.Medium;
        col.maxCollisionShapes = 256;
        col.radiusScale = 1f;

        ps.Play();
        return ps;
    }

    /// <summary>
    /// 砂ぼこり用の ParticleSystem を構築します。
    /// </summary>
    public static ParticleSystem CreateDustSystem(Transform parent, Material mat, bool useSheet, int puffTiles, float turbulence)
    {
        ParticleSystem ps = CreateBaseSystem("SandDust", parent, mat, DUST_MAX_PARTICLES, DUST_GRAVITY_MODIFIER);

        // サイズ変化カーブ
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.25f),
            new Keyframe(0.2f, 0.8f),
            new Keyframe(1f, 1.5f)
        );
        for (int i = 0; i < sizeCurve.length; i++)
        {
            sizeCurve.SmoothTangents(i, 0f);
        }
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // アルファフェードカーブ
        var color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.04f),
                new GradientAlphaKey(0.55f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        color.color = gradient;

        // 空気抵抗による減速
        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = new ParticleSystem.MinMaxCurve(0.3f);
        limit.dampen = 0.1f;

        // 渦（ノイズ）
        var noise = ps.noise;
        noise.enabled = turbulence > 0f;
        noise.strength = new ParticleSystem.MinMaxCurve(turbulence);
        noise.frequency = 0.6f;
        noise.scrollSpeed = new ParticleSystem.MinMaxCurve(0.3f);
        noise.damping = true;
        noise.octaveCount = 2;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);

        if (useSheet)
        {
            var tsa = ps.textureSheetAnimation;
            tsa.enabled = true;
            tsa.mode = ParticleSystemAnimationMode.Grid;
            tsa.numTilesX = puffTiles;
            tsa.numTilesY = puffTiles;
            tsa.animation = ParticleSystemAnimationType.WholeSheet;
            tsa.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
            tsa.startFrame = new ParticleSystem.MinMaxCurve(0f, 0.999f);
        }

        var psr = ps.GetComponent<ParticleSystemRenderer>();
        psr.sortMode = ParticleSystemSortMode.Distance;

        ps.Play();
        return ps;
    }

    /// <summary>
    /// URP の Depth Texture 設定が無効な場合に警告を出力します。
    /// </summary>
    public static void WarnIfNoDepthTexture(Object logContext)
    {
        RenderPipelineAsset rp = GraphicsSettings.currentRenderPipeline;
        if (rp == null)
        {
            return;
        }

        PropertyInfo prop = rp.GetType().GetProperty("supportsCameraDepthTexture");
        if (prop != null && prop.PropertyType == typeof(bool) && !(bool)prop.GetValue(rp))
        {
            Debug.LogWarning("[SandEffects] ソフトパーティクルには URP Asset の「Depth Texture」が必要です。"
                             + "使用中の URP Asset（Project Settings > Graphics）で有効にしてください。", logContext);
        }
    }

    /// <summary>
    /// 共通設定の ParticleSystem を生成します。
    /// </summary>
    private static ParticleSystem CreateBaseSystem(string objName, Transform parent, Material mat, int maxParticles, float gravity)
    {
        GameObject go = new GameObject(objName);
        go.transform.SetParent(parent, false);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.maxParticles = maxParticles;
        main.gravityModifier = gravity;
        main.startSpeed = 0f;
        main.startLifetime = 1f;
        main.startSize = 0.05f;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        var emission = ps.emission;
        emission.enabled = false;
        var shape = ps.shape;
        shape.enabled = false;

        var psr = go.GetComponent<ParticleSystemRenderer>();
        psr.renderMode = ParticleSystemRenderMode.Billboard;
        psr.sharedMaterial = mat;
        psr.shadowCastingMode = ShadowCastingMode.Off;
        psr.receiveShadows = false;

        return ps;
    }

    /// <summary>
    /// マテリアルへソフトパーティクル・カメラフェードの設定を適用します。
    /// </summary>
    private static void ApplySoftParticleSettings(Material mat, SoftParticleSettings settings, Object logContext)
    {
        if (settings.m_enableSoftParticles)
        {
            if (mat.HasProperty("_SoftParticlesEnabled"))
            {
                float near = Mathf.Max(0f, settings.m_softNearFade);
                float far = Mathf.Max(near + MIN_FADE_SPAN, settings.m_softFarFade);
                mat.SetFloat("_SoftParticlesEnabled", 1f);
                mat.SetFloat("_SoftParticlesNearFadeDistance", near);
                mat.SetFloat("_SoftParticlesFarFadeDistance", far);
                mat.SetVector("_SoftParticleFadeParams", new Vector4(near, 1f / (far - near), 0f, 0f));
                mat.EnableKeyword("_SOFTPARTICLES_ON");
            }
            else
            {
                Debug.LogWarning("[SandEffects] このシェーダーはソフトパーティクルに対応していません（URP の Particles/Unlit が必要です）。", logContext);
            }
        }

        if (settings.m_enableCameraFade && mat.HasProperty("_CameraFadingEnabled"))
        {
            float near = Mathf.Max(0f, settings.m_cameraNearFade);
            float far = Mathf.Max(near + MIN_FADE_SPAN, settings.m_cameraFarFade);
            mat.SetFloat("_CameraFadingEnabled", 1f);
            mat.SetFloat("_CameraNearFadeDistance", near);
            mat.SetFloat("_CameraFarFadeDistance", far);
            mat.SetVector("_CameraFadeParams", new Vector4(near, 1f / (far - near), 0f, 0f));
            mat.EnableKeyword("_FADING_ON");
        }
    }

    /// <summary>
    /// 0〜1 のエルミート補間（Smoothstep）を計算します。
    /// </summary>
    private static float CalculateSmoothStep(float a, float b, float x)
    {
        float t = Mathf.Clamp01((x - a) / (b - a));
        return t * t * (3f - 2f * t);
    }
}