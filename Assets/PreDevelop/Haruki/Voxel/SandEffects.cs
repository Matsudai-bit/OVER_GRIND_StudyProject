using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 砂山が削られたとき・砂が崩れたときのエフェクト（砂粒・砂ぼこり・サウンド）。
/// MarchingCubesMountain と同じ GameObject にアタッチするだけで動く。
/// ParticleSystem はコードで自動生成するので、シーン側の準備は不要。
///
/// ■ ソフトパーティクル
///   砂ぼこりが地面や砂山と交差する所の硬い境界を、深度を使ってなじませる。
///   URP Asset の「Depth Texture」を有効にしておくこと。
///
/// ■ ビルドについて
///   自動生成マテリアルは実行時に作るため、ビルドではシェーダーバリアント
///   （半透明・ソフトパーティクル）が削られて効かなくなることがある。
///   ビルドする場合は、コンテキストメニュー「生成テクスチャをPNGに書き出す」で
///   テクスチャを出力し、Particles/Unlit のマテリアルを作って割り当てること。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(MarchingCubesMountain))]
public class SandEffects : MonoBehaviour
{
    [Header("色")]
    public Color sandColor = new Color(0.78f, 0.66f, 0.46f, 1f);
    [Range(0f, 0.3f)] public float colorVariation = 0.1f;
    [Tooltip("砂ぼこりの色。アルファが濃さ")]
    public Color dustColor = new Color(0.86f, 0.77f, 0.62f, 0.4f);
    [Tooltip("シーンの光（太陽光と環境光）の色・明るさを砂ぼこり・砂粒に反映する度合い。0 = 反映しない")]
    [Range(0f, 1f)] public float lightInfluence = 0.8f;

    [Header("マテリアル（空なら自動生成）")]
    [Tooltip("空でなければ、そのマテリアルをそのまま使う（この場合、下のソフトパーティクル設定は自動では適用されない）")]
    public Material grainMaterial;
    public Material dustMaterial;
    [Tooltip("dustMaterial のテクスチャが、書き出した 2x2 の煙シート(SandDustSheet.png)のときON。単一の煙テクスチャを使うならOFF")]
    public bool dustMaterialUsesSheet = true;

    [Header("砂粒（飛び散る粒）")]
    [Tooltip("取り除いた砂 1m³ あたりに出す粒の数")]
    public float grainsPerCubicMeter = 1500f;
    public int maxGrainsPerCarve = 60;
    public Vector2 grainSize = new Vector2(0.02f, 0.05f);
    public Vector2 grainLifetime = new Vector2(1.0f, 2.0f);
    [Tooltip("飛び散る初速(m/s)")]
    public float ejectSpeed = 2.5f;
    [Tooltip("砂粒が地面や砂山に当たって跳ねる／止まる")]
    public bool grainsCollide = true;
    [Tooltip("砂粒が衝突するレイヤー。プレイヤーなどは外しておくとよい")]
    public LayerMask grainCollisionMask = ~0;

    [Header("砂ぼこり")]
    public float dustPerCubicMeter = 300f;
    public int maxDustPerCarve = 8;
    public Vector2 dustSize = new Vector2(0.3f, 0.7f);
    public Vector2 dustLifetime = new Vector2(1.2f, 2.4f);
    [Tooltip("舞い上がる速さ(m/s)")]
    public float dustRiseSpeed = 0.8f;
    [Tooltip("地面を這うように横へ広がる砂ぼこりの割合（衝撃で広がる煙）")]
    [Range(0f, 1f)] public float rollingDustRatio = 0.4f;
    [Tooltip("横へ広がる速さ(m/s)")]
    public float rollingSpeed = 1.2f;
    [Tooltip("渦（ゆらぎ）の強さ。大きいほどもくもく動く")]
    [Range(0f, 1.5f)] public float turbulence = 0.35f;

    [Header("広範囲を削ったとき")]
    [Tooltip("削り跡がこの半径(m)を超えると「広範囲」として扱い、粒を削った砂の体積内に散らして中心から外へ順に出す")]
    public float bulkFootprint = 1.4f;
    [Tooltip("広範囲を削ったとき、煙が中心から端まで広がるのにかける時間(秒)")]
    public float bulkWaveDuration = 0.3f;
    [Tooltip("広範囲のとき、砂ぼこりの個数上限を面積に合わせて増やす（倍率の上限）")]
    public float maxDustCountScale = 5f;

    [Header("ソフトパーティクル（自動生成マテリアルに適用）")]
    public bool softParticles = true;
    [Tooltip("他の面との距離がこの値(m)以下で完全に透明になる")]
    public float softNearFade = 0f;
    [Tooltip("他の面との距離がこの値(m)以上で完全に不透明になる。砂ぼこりの大きさ程度が目安")]
    public float softFarFade = 0.6f;
    [Tooltip("カメラに近づいた砂ぼこりを透明にして、画面が白く塞がるのを防ぐ")]
    public bool cameraFade = true;
    public float cameraNearFade = 0.4f;
    public float cameraFarFade = 1.4f;

    [Header("さらさら崩れる")]
    public bool trickleEffect = true;
    [Tooltip("崩れた砂の粒数の倍率")]
    public float trickleBoost = 3f;

    [Header("サウンド（任意）")]
    public AudioClip[] carveClips;
    [Range(0f, 1f)] public float soundVolume = 0.6f;
    public Vector2 pitchRange = new Vector2(0.9f, 1.1f);
    [Tooltip("連続で鳴らす最小間隔(秒)")]
    public float minSoundInterval = 0.12f;

    private MarchingCubesMountain mountain;
    private ParticleSystem grainPS;
    private ParticleSystem dustPS;
    private Material createdGrainMat, createdDustMat;
    private Texture2D grainTex, dustTex;

    private AudioSource[] audioPool;
    private int audioIndex;
    private float nextSoundTime;

    // シーンの光の色（フレームごとにキャッシュ）
    private Light sun;
    private bool sunSearched;
    private Color tintCache = Color.white;
    private int tintFrame = -1;
    private static readonly Vector3[] UpDirs = { Vector3.up };
    private static readonly Color[] AmbientResult = new Color[1];

    // 中心から外へ波のように広げるための、遅延発生キュー
    private struct PendingEmit
    {
        public float time;
        public ParticleSystem ps;
        public Vector3 pos, vel;
        public float size, life;
        public Color color;
    }
    private readonly System.Collections.Generic.List<PendingEmit> pending = new System.Collections.Generic.List<PendingEmit>();
    private const int MaxPending = 600;

    private const int PuffTiles = 2; // 砂ぼこりテクスチャは 2x2 のシート（4種類の形をランダムに使う）

    // ------------------------------------------------------------

    void Awake()
    {
        mountain = GetComponent<MarchingCubesMountain>();

        Material gm = grainMaterial;
        Material dm = dustMaterial;

        if (gm == null)
        {
            grainTex = CreateRoundTexture(32, 0.6f);
            gm = createdGrainMat = CreateParticleMaterial(grainTex, false);
        }
        if (dm == null)
        {
            dustTex = CreatePuffSheet(128, PuffTiles);
            dm = createdDustMat = CreateParticleMaterial(dustTex, true);
        }

        grainPS = CreateGrainSystem(gm);
        dustPS = CreateDustSystem(dm, dustMaterial == null || dustMaterialUsesSheet);
        SetupAudio();

        if (softParticles && dustMaterial == null) WarnIfNoDepthTexture();
    }

    void OnEnable()
    {
        mountain = GetComponent<MarchingCubesMountain>();
        mountain.Carved += OnCarved;
        mountain.SandSlid += OnSandSlid;
    }

    void OnDisable()
    {
        if (mountain == null) return;
        mountain.Carved -= OnCarved;
        mountain.SandSlid -= OnSandSlid;
    }

    void OnDestroy()
    {
        if (createdGrainMat != null) Destroy(createdGrainMat);
        if (createdDustMat != null) Destroy(createdDustMat);
        if (grainTex != null) Destroy(grainTex);
        if (dustTex != null) Destroy(dustTex);
    }

    // ------------------------------------------------------------
    // イベント
    // ------------------------------------------------------------

    void OnCarved(Vector3 point, Vector3 normal, float footprint, float volume)
    {
        footprint = Mathf.Max(footprint, 0.05f);
        float power = Mathf.Lerp(0.6f, 1.4f, Mathf.Clamp01(volume / 0.05f)); // 大きく削るほど勢いよく
        Color tint = GetLightTint();

        // 削った砂の平均の厚み(m)。この厚みの範囲に、もともと砂があった
        float thickness = volume / (Mathf.PI * footprint * footprint);
        // 0 = 小さな削り（飛び散る）、1 = 広範囲（砂が崩れて煙になる）
        float bulk = Mathf.Clamp01((footprint - 0.4f) / Mathf.Max(0.01f, bulkFootprint - 0.4f));

        // 砂粒
        int grains = Mathf.Min(RoundRandom(volume * grainsPerCubicMeter), maxGrainsPerCarve);
        for (int i = 0; i < grains; i++)
        {
            Vector2 c = Random.insideUnitCircle * footprint;
            GetSurface(point, normal, c, out Vector3 pos, out Vector3 n);

            Vector3 outward = new Vector3(c.x, 0f, c.y);
            outward = outward.sqrMagnitude > 1e-6f ? outward.normalized : Random.onUnitSphere;

            // 広範囲のときは噴水のように上へ飛ばさず、横へ散らして落とす
            float up = Mathf.Lerp(1f, 0.3f, bulk);
            Vector3 v = (outward * Random.Range(0.2f, 1f)
                         + n * (Random.Range(0.4f, 1.2f) * up)
                         + Random.insideUnitSphere * 0.3f) * (ejectSpeed * power);

            // 削った砂があった高さ（穴の底から元の表面まで）のどこかから出す
            float lift = Random.value * thickness * 1.5f + Random.Range(0.01f, 0.05f);

            Queue(grainPS, pos + n * lift, v,
                Random.Range(grainSize.x, grainSize.y),
                Random.Range(grainLifetime.x, grainLifetime.y),
                RandomSandColor(tint),
                WaveDelay(c, footprint, bulk) * 0.6f);
        }

        // 砂ぼこり：「舞い上がる煙」と「地面を這って広がる煙」を混ぜる
        float areaScale = Mathf.Clamp(footprint * footprint, 1f, maxDustCountScale);
        int maxPuffs = Mathf.CeilToInt(maxDustPerCarve * areaScale);
        int puffs = Mathf.Min(RoundRandom(volume * dustPerCubicMeter), maxPuffs);
        float densityMul = Mathf.Lerp(1f, 0.7f, bulk); // 重なりすぎて真っ白にならないように

        for (int i = 0; i < puffs; i++)
        {
            Vector2 c = Random.insideUnitCircle * footprint;
            GetSurface(point, normal, c, out Vector3 pos, out Vector3 n);

            Vector3 outward = new Vector3(c.x, 0f, c.y);
            outward = outward.sqrMagnitude > 1e-6f ? outward.normalized : Random.onUnitSphere;

            Vector3 v;
            float sizeMul, lifeMul, alphaMul;

            if (Random.value < rollingDustRatio)
            {
                Vector3 tangent = Vector3.ProjectOnPlane(outward, n);
                tangent = tangent.sqrMagnitude > 1e-6f ? tangent.normalized : Vector3.zero;
                v = tangent * Random.Range(0.5f, 1f) * rollingSpeed * power + n * Random.Range(0.05f, 0.2f);
                sizeMul = 1.2f; lifeMul = 1.3f; alphaMul = 0.8f;
            }
            else
            {
                v = n * Random.Range(0.3f, 1f) * dustRiseSpeed * power + outward * Random.Range(0f, 0.4f);
                sizeMul = 1f; lifeMul = 1f; alphaMul = 1f;
            }

            // 穴の底ではなく、砂があった体積の中から立ち上がらせる
            float lift = Random.value * thickness * 1.2f + 0.05f;

            Queue(dustPS, pos + n * lift, v,
                Random.Range(dustSize.x, dustSize.y) * (0.7f + footprint) * sizeMul,
                Random.Range(dustLifetime.x, dustLifetime.y) * lifeMul,
                DustColor(tint, alphaMul * densityMul),
                WaveDelay(c, footprint, bulk));
        }

        PlaySound(point, volume);
    }

    // 広範囲のときは、中心に近い所から順に煙が出る（衝撃が外へ広がる見え方）
    float WaveDelay(Vector2 offset, float footprint, float bulk)
    {
        float t = Mathf.Clamp01(offset.magnitude / footprint);
        return t * bulkWaveDuration * bulk + Random.Range(0f, 0.05f) * bulk;
    }

    // delay 秒後に発生させる。0 なら即時
    void Queue(ParticleSystem ps, Vector3 pos, Vector3 vel, float size, float life, Color color, float delay)
    {
        if (delay <= 0.001f)
        {
            EmitOne(ps, pos, vel, size, life, color);
            return;
        }

        if (pending.Count >= MaxPending) return;
        pending.Add(new PendingEmit
        {
            time = Time.time + delay,
            ps = ps,
            pos = pos,
            vel = vel,
            size = size,
            life = life,
            color = color
        });
    }

    void Update()
    {
        for (int i = pending.Count - 1; i >= 0; i--)
        {
            PendingEmit e = pending[i];
            if (Time.time < e.time) continue;

            EmitOne(e.ps, e.pos, e.vel, e.size, e.life, e.color);
            pending[i] = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
        }
    }

    void OnSandSlid(Vector3 point, Vector3 downhill, float volume)
    {
        if (!trickleEffect) return;
        Color tint = GetLightTint();

        int grains = Mathf.Min(RoundRandom(volume * grainsPerCubicMeter * trickleBoost), 4);
        for (int i = 0; i < grains; i++)
        {
            Vector3 v = downhill * Random.Range(0.3f, 0.9f) + Vector3.up * Random.Range(0f, 0.3f)
                        + Random.insideUnitSphere * 0.1f;

            EmitOne(grainPS, point + Vector3.up * 0.03f, v,
                Random.Range(grainSize.x, grainSize.y) * 0.8f,
                Random.Range(grainLifetime.x * 0.5f, grainLifetime.y * 0.6f),
                RandomSandColor(tint));
        }

        // たまに薄い砂ぼこりを足す
        if (Random.value < 0.08f)
        {
            EmitOne(dustPS, point + Vector3.up * 0.05f, Vector3.up * 0.2f + downhill * 0.2f,
                Random.Range(dustSize.x, dustSize.y) * 0.6f,
                Random.Range(dustLifetime.x, dustLifetime.y) * 0.7f,
                DustColor(tint, 0.6f));
        }
    }

    // ------------------------------------------------------------
    // 補助
    // ------------------------------------------------------------

    // 期待値 x を、小数部は確率で切り上げて整数にする（0.3 → 30% の確率で 1）
    static int RoundRandom(float x)
    {
        int n = Mathf.FloorToInt(x);
        if (Random.value < x - n) n++;
        return n;
    }

    Color RandomSandColor(Color tint)
    {
        float v = Random.Range(-colorVariation, colorVariation);
        return new Color(
            Mathf.Clamp01((sandColor.r + v) * tint.r),
            Mathf.Clamp01((sandColor.g + v) * tint.g),
            Mathf.Clamp01((sandColor.b + v) * tint.b),
            sandColor.a);
    }

    Color DustColor(Color tint, float alphaMul)
    {
        // 粒ごとに少しだけ明るさを変えて、のっぺりしないようにする
        float v = 1f + Random.Range(-0.06f, 0.06f);
        return new Color(
            Mathf.Clamp01(dustColor.r * tint.r * v),
            Mathf.Clamp01(dustColor.g * tint.g * v),
            Mathf.Clamp01(dustColor.b * tint.b * v),
            dustColor.a * alphaMul);
    }

    // シーンの光（環境光＋太陽光）から、砂ぼこりに掛ける色を求める。
    // Unlit のパーティクルは光を受けないので、夕方や日陰でも白く浮いて見えるのを防ぐ
    Color GetLightTint()
    {
        if (tintFrame == Time.frameCount) return tintCache;
        tintFrame = Time.frameCount;

        if (lightInfluence <= 0f)
        {
            tintCache = Color.white;
            return tintCache;
        }

        if (!sunSearched)
        {
            sunSearched = true;
            sun = RenderSettings.sun;
            if (sun == null)
            {
                foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                {
                    if (l.type == LightType.Directional && (sun == null || l.intensity > sun.intensity)) sun = l;
                }
            }
        }

        SphericalHarmonicsL2 probe = RenderSettings.ambientProbe;
        probe.Evaluate(UpDirs, AmbientResult);
        Color ambient = AmbientResult[0];
        if (QualitySettings.activeColorSpace == ColorSpace.Linear) ambient = ambient.gamma;

        Color direct = (sun != null && sun.isActiveAndEnabled)
            ? sun.color * (Mathf.Min(sun.intensity, 2f) * 0.55f)
            : Color.black;

        Color lit = ambient + direct;
        lit = new Color(Mathf.Clamp(lit.r, 0.2f, 1.1f), Mathf.Clamp(lit.g, 0.2f, 1.1f), Mathf.Clamp(lit.b, 0.2f, 1.1f), 1f);

        tintCache = Color.Lerp(Color.white, lit, lightInfluence);
        return tintCache;
    }

    // 削り跡の中心から offset だけずらした位置で、砂の表面の位置と法線を取る
    void GetSurface(Vector3 center, Vector3 fallbackNormal, Vector2 offset, out Vector3 pos, out Vector3 normal)
    {
        Vector3 probe = new Vector3(center.x + offset.x, center.y, center.z + offset.y);
        if (!mountain.TrySampleSurface(probe, out pos, out normal))
        {
            pos = center;
            normal = fallbackNormal;
        }
    }

    void EmitOne(ParticleSystem ps, Vector3 position, Vector3 velocity, float size, float lifetime, Color color)
    {
        ParticleSystem.EmitParams ep = new ParticleSystem.EmitParams
        {
            position = position,
            velocity = velocity,
            startSize = size,
            startLifetime = lifetime,
            startColor = color
        };
        ps.Emit(ep, 1);
    }

    void WarnIfNoDepthTexture()
    {
        RenderPipelineAsset rp = GraphicsSettings.currentRenderPipeline;
        if (rp == null) return;

        System.Reflection.PropertyInfo prop = rp.GetType().GetProperty("supportsCameraDepthTexture");
        if (prop != null && prop.PropertyType == typeof(bool) && !(bool)prop.GetValue(rp))
        {
            Debug.LogWarning("[SandEffects] ソフトパーティクルには URP Asset の「Depth Texture」が必要です。"
                             + "使用中の URP Asset（Project Settings > Graphics）で有効にしてください。", this);
        }
    }

    // ------------------------------------------------------------
    // ParticleSystem の生成
    // ------------------------------------------------------------

    ParticleSystem CreateBaseSystem(string objName, Material mat, int maxParticles, float gravity)
    {
        GameObject go = new GameObject(objName);
        go.transform.SetParent(transform, false);

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

        // Emit() で手動発生させるので、自動発生と形状は使わない
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

    ParticleSystem CreateGrainSystem(Material mat)
    {
        ParticleSystem ps = CreateBaseSystem("SandGrains", mat, 3000, 1f);

        // 終わり際に小さくなって消える
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.75f, 1f), new Keyframe(1f, 0.2f)));

        // 地面・砂山に当たって跳ねる
        var col = ps.collision;
        col.enabled = grainsCollide;
        col.type = ParticleSystemCollisionType.World;
        col.mode = ParticleSystemCollisionMode.Collision3D;
        col.collidesWith = grainCollisionMask;
        col.enableDynamicColliders = false; // Rigidbody（プレイヤーなど）とは当たらない
        col.dampen = 0.5f;
        col.bounce = 0.25f;
        col.lifetimeLoss = 0.1f;
        col.quality = ParticleSystemCollisionQuality.Medium;
        col.maxCollisionShapes = 256;
        col.radiusScale = 1f;

        ps.Play();
        return ps;
    }

    ParticleSystem CreateDustSystem(Material mat, bool useSheet)
    {
        // 重力はごくわずかに負（暖かい空気に乗って、ゆっくり浮く）
        ParticleSystem ps = CreateBaseSystem("SandDust", mat, 800, -0.03f);

        // 煙は最初に勢いよく膨らみ、その後ゆっくり広がる
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.25f), new Keyframe(0.2f, 0.8f), new Keyframe(1f, 1.5f));
        for (int i = 0; i < sizeCurve.length; i++) sizeCurve.SmoothTangents(i, 0f);
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // 出だしはふわっと現れ（ポップしない）、ゆっくり薄れる
        var color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.04f),
                new GradientAlphaKey(0.55f, 0.5f), new GradientAlphaKey(0f, 1f)
            });
        color.color = g;

        // 空気抵抗：初速はすぐに落ち着き、あとは渦に流される
        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = new ParticleSystem.MinMaxCurve(0.3f);
        limit.dampen = 0.1f;

        // 渦（ノイズ）でもくもくと揺らす
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

        // 4種類の煙の形をランダムに使い分ける（フレームは固定）
        if (useSheet)
        {
            var tsa = ps.textureSheetAnimation;
            tsa.enabled = true;
            tsa.mode = ParticleSystemAnimationMode.Grid;
            tsa.numTilesX = PuffTiles;
            tsa.numTilesY = PuffTiles;
            tsa.animation = ParticleSystemAnimationType.WholeSheet;
            tsa.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
            tsa.startFrame = new ParticleSystem.MinMaxCurve(0f, 0.999f);
        }

        // 半透明は奥から手前の順に描かないと、煙同士の重なりが不自然になる
        var psr = ps.GetComponent<ParticleSystemRenderer>();
        psr.sortMode = ParticleSystemSortMode.Distance;

        ps.Play();
        return ps;
    }

    // ------------------------------------------------------------
    // 自動生成のテクスチャ・マテリアル
    // ------------------------------------------------------------

    static float Smooth(float a, float b, float x)
    {
        float t = Mathf.Clamp01((x - a) / (b - a));
        return t * t * (3f - 2f * t);
    }

    // 白い円（アルファのみ）。hardness 0 = ふんわり、1 に近いほど輪郭がくっきり
    static Texture2D CreateRoundTexture(int size, float hardness)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        Color32[] px = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Smooth(0f, 1f, (1f - d) / Mathf.Max(0.001f, 1f - hardness));
                px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        }

        tex.SetPixels32(px);
        tex.Apply(false);
        return tex;
    }

    // ノイズで輪郭を崩した「煙の塊」を tiles x tiles 枚ぶん並べたシート。
    // 中心は濃く、外側ほど不規則に薄くなる。各タイルの縁は必ず透明にしてある
    static Texture2D CreatePuffSheet(int tileSize, int tiles)
    {
        int size = tileSize * tiles;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color32[] px = new Color32[size * size];

        for (int ty = 0; ty < tiles; ty++)
        {
            for (int tx = 0; tx < tiles; tx++)
            {
                // タイルごとに違うノイズ位置を使う（固定値なので毎回同じ形になる）
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
                        float d = Mathf.Sqrt(dx * dx + dy * dy);

                        // フラクタルノイズ（4オクターブ）
                        float n = 0f, amp = 0.5f, freq = 2.5f;
                        for (int o = 0; o < 4; o++)
                        {
                            n += amp * Mathf.PerlinNoise(ox + u * freq, oy + v * freq);
                            amp *= 0.5f;
                            freq *= 2f;
                        }
                        n = Mathf.Clamp01(n / 0.9375f);

                        float edge = Mathf.Clamp01((1f - d) * 1.6f); // 外へ向かって薄く。d>=1 で 0
                        float density = edge * edge * (0.45f + n * 1.1f);
                        float a = Smooth(0.12f, 1.1f, density);

                        int index = (ty * tileSize + y) * size + (tx * tileSize + x);
                        px[index] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                    }
                }
            }
        }

        tex.SetPixels32(px);
        tex.Apply(true);
        return tex;
    }

    Material CreateParticleMaterial(Texture2D tex, bool dust)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogWarning("[SandEffects] パーティクル用シェーダーが見つかりません。grainMaterial / dustMaterial を割り当ててください。");
            return null;
        }

        Material m = new Material(shader);
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);

        // URP の Particles/Unlit を半透明(アルファブレンド)にする
        if (m.HasProperty("_Surface"))
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", 5f);       // SrcAlpha
            m.SetFloat("_DstBlend", 10f);      // OneMinusSrcAlpha
            m.SetFloat("_SrcBlendAlpha", 1f);
            m.SetFloat("_DstBlendAlpha", 10f);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent;
        }

        if (dust) ApplySoftParticleSettings(m);
        return m;
    }

    // URP の Particles/Unlit の「Soft Particles」「Camera Fading」を、
    // マテリアル Inspector のチェックを入れたときと同じ内容でコードから設定する
    void ApplySoftParticleSettings(Material m)
    {
        if (softParticles)
        {
            if (m.HasProperty("_SoftParticlesEnabled"))
            {
                float near = Mathf.Max(0f, softNearFade);
                float far = Mathf.Max(near + 0.01f, softFarFade);
                m.SetFloat("_SoftParticlesEnabled", 1f);
                m.SetFloat("_SoftParticlesNearFadeDistance", near);
                m.SetFloat("_SoftParticlesFarFadeDistance", far);
                m.SetVector("_SoftParticleFadeParams", new Vector4(near, 1f / (far - near), 0f, 0f));
                m.EnableKeyword("_SOFTPARTICLES_ON");
            }
            else
            {
                Debug.LogWarning("[SandEffects] このシェーダーはソフトパーティクルに対応していません（URP の Particles/Unlit が必要です）。", this);
            }
        }

        if (cameraFade && m.HasProperty("_CameraFadingEnabled"))
        {
            float near = Mathf.Max(0f, cameraNearFade);
            float far = Mathf.Max(near + 0.01f, cameraFarFade);
            m.SetFloat("_CameraFadingEnabled", 1f);
            m.SetFloat("_CameraNearFadeDistance", near);
            m.SetFloat("_CameraFarFadeDistance", far);
            m.SetVector("_CameraFadeParams", new Vector4(near, 1f / (far - near), 0f, 0f));
            m.EnableKeyword("_FADING_ON");
        }
    }

#if UNITY_EDITOR
    // ビルド用：自動生成テクスチャを Assets 直下に PNG として書き出す。
    // 書き出したテクスチャで Particles/Unlit のマテリアルを作り、dustMaterial / grainMaterial に割り当てる。
    // SandDustSheet.png は 2x2 の煙シートなので、dustMaterialUsesSheet は ON のままにする。
    [ContextMenu("生成テクスチャをPNGに書き出す")]
    void ExportTextures()
    {
        Texture2D sheet = dustTex != null ? dustTex : CreatePuffSheet(128, PuffTiles);
        Texture2D grain = grainTex != null ? grainTex : CreateRoundTexture(32, 0.6f);

        System.IO.File.WriteAllBytes(Application.dataPath + "/SandDustSheet.png", sheet.EncodeToPNG());
        System.IO.File.WriteAllBytes(Application.dataPath + "/SandGrain.png", grain.EncodeToPNG());
        UnityEditor.AssetDatabase.Refresh();

        if (sheet != dustTex) DestroyImmediate(sheet);
        if (grain != grainTex) DestroyImmediate(grain);

        Debug.Log("[SandEffects] Assets/SandDustSheet.png と Assets/SandGrain.png を書き出しました。");
    }
#endif

    // ------------------------------------------------------------
    // サウンド
    // ------------------------------------------------------------

    void SetupAudio()
    {
        if (carveClips == null || carveClips.Length == 0) return;

        audioPool = new AudioSource[4];
        for (int i = 0; i < audioPool.Length; i++)
        {
            GameObject go = new GameObject("SandAudio" + i);
            go.transform.SetParent(transform, false);

            AudioSource a = go.AddComponent<AudioSource>();
            a.playOnAwake = false;
            a.spatialBlend = 1f;
            a.minDistance = 2f;
            a.maxDistance = 30f;
            audioPool[i] = a;
        }
    }

    void PlaySound(Vector3 position, float volume)
    {
        if (audioPool == null || Time.time < nextSoundTime) return;
        nextSoundTime = Time.time + minSoundInterval;

        AudioSource a = audioPool[audioIndex];
        audioIndex = (audioIndex + 1) % audioPool.Length;

        a.transform.position = position;
        a.clip = carveClips[Random.Range(0, carveClips.Length)];
        a.pitch = Random.Range(pitchRange.x, pitchRange.y);
        a.volume = soundVolume * Mathf.Clamp(0.3f + volume * 10f, 0.3f, 1f);
        a.Play();
    }
}