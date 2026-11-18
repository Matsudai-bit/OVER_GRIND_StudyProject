using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 砂山が削られたとき・砂が崩れたときのエフェクト（砂粒・砂ぼこり・サウンド）を制御するコンポーネント。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(MarchingCubesMountain))]
public class SandEffects : MonoBehaviour
{
    // ---- 遅延発生キュー用構造体 ----
    private struct PendingEmit
    {
        public float m_time;              // 発生予定時刻
        public ParticleSystem m_system;   // 発生先の ParticleSystem
        public Vector3 m_position;        // 発生位置
        public Vector3 m_velocity;        // 初速度
        public float m_size;              // サイズ
        public float m_lifetime;          // 生存時間
        public Color m_color;             // 色
    }

    // ---- 定数 ----
    private const int MAX_PENDING_EMITS = 600;         // 遅延キューの最大保持数
    private const int PUFF_TILES = 2;                  // 煙シートの縦横タイル数
    private const int GRAIN_TEXTURE_SIZE = 32;         // 自動生成する砂粒テクスチャのサイズ
    private const float GRAIN_TEXTURE_HARDNESS = 0.6f; // 自動生成する砂粒テクスチャの硬さ
    private const int DUST_TILE_SIZE = 128;            // 自動生成する煙シート1タイルのサイズ
    private const float MIN_EMIT_DELAY = 0.001f;       // 遅延キューへ積む最小遅延時間(秒)
    private const float MIN_FOOTPRINT = 0.05f;         // 最小フットプリント半径(m)
    private const int MAX_GRAINS_ON_SLIDE = 4;         // 崩れ1回あたりの最大砂粒数
    private const float SLIDE_DUST_PROBABILITY = 0.08f; // 崩れ時に薄い砂ぼこりを出す確率

    private static readonly Vector3[] UP_DIRECTIONS = { Vector3.up };

    // ---- Inspector 設定項目 ----
    [Header("色")]
    [Tooltip("砂粒の基本色")]
    [SerializeField] private Color m_sandColor = new Color(0.78f, 0.66f, 0.46f, 1f);
    [Tooltip("砂粒の色のばらつき度合い")]
    [Range(0f, 0.3f)]
    [SerializeField] private float m_colorVariation = 0.1f;
    [Tooltip("砂ぼこりの色。アルファが濃さ")]
    [SerializeField] private Color m_dustColor = new Color(0.86f, 0.77f, 0.62f, 0.4f);
    [Tooltip("シーンの光（太陽光と環境光）の色・明るさを砂ぼこり・砂粒に反映する度合い。0 = 反映しない")]
    [Range(0f, 1f)]
    [SerializeField] private float m_lightInfluence = 0.8f;

    [Header("マテリアル（空なら自動生成）")]
    [Tooltip("空でなければ、そのマテリアルをそのまま使う（この場合、下のソフトパーティクル設定は自動では適用されない）")]
    [SerializeField] private Material m_grainMaterial;
    [Tooltip("砂ぼこり用マテリアル（空なら自動生成）")]
    [SerializeField] private Material m_dustMaterial;
    [Tooltip("m_dustMaterial のテクスチャが、書き出した 2x2 の煙シート(SandDustSheet.png)のときON。単一の煙テクスチャを使うならOFF")]
    [SerializeField] private bool m_dustMaterialUsesSheet = true;

    [Header("砂粒（飛び散る粒）")]
    [Tooltip("取り除いた砂 1m³ あたりに出す粒の数")]
    [SerializeField] private float m_grainsPerCubicMeter = 1500f;
    [Tooltip("1回の削りで発生させる砂粒の最大数")]
    [SerializeField] private int m_maxGrainsPerCarve = 60;
    [Tooltip("砂粒サイズの最小・最大値")]
    [SerializeField] private Vector2 m_grainSize = new Vector2(0.02f, 0.05f);
    [Tooltip("砂粒生存時間の最小・最大値")]
    [SerializeField] private Vector2 m_grainLifetime = new Vector2(1.0f, 2.0f);
    [Tooltip("飛び散る初速(m/s)")]
    [SerializeField] private float m_ejectSpeed = 2.5f;
    [Tooltip("砂粒が地面や砂山に当たって跳ねる／止まる")]
    [SerializeField] private bool m_grainsCollide = true;
    [Tooltip("砂粒が衝突するレイヤー。プレイヤーなどは外しておくとよい")]
    [SerializeField] private LayerMask m_grainCollisionMask = ~0;

    [Header("砂ぼこり")]
    [Tooltip("取り除いた砂 1m³ あたりに出す砂ぼこりの数")]
    [SerializeField] private float m_dustPerCubicMeter = 300f;
    [Tooltip("1回の削りで発生させる砂ぼこりの最大数")]
    [SerializeField] private int m_maxDustPerCarve = 8;
    [Tooltip("砂ぼこりサイズの最小・最大値")]
    [SerializeField] private Vector2 m_dustSize = new Vector2(0.3f, 0.7f);
    [Tooltip("砂ぼこり生存時間の最小・最大値")]
    [SerializeField] private Vector2 m_dustLifetime = new Vector2(1.2f, 2.4f);
    [Tooltip("舞い上がる速さ(m/s)")]
    [SerializeField] private float m_dustRiseSpeed = 0.8f;
    [Tooltip("地面を這うように横へ広がる砂ぼこりの割合（衝撃で広がる煙）")]
    [Range(0f, 1f)]
    [SerializeField] private float m_rollingDustRatio = 0.4f;
    [Tooltip("横へ広がる速さ(m/s)")]
    [SerializeField] private float m_rollingSpeed = 1.2f;
    [Tooltip("渦（ゆらぎ）の強さ。大きいほどもくもく動く")]
    [Range(0f, 1.5f)]
    [SerializeField] private float m_turbulence = 0.35f;

    [Header("広範囲を削ったとき")]
    [Tooltip("削り跡がこの半径(m)を超えると「広範囲」として扱い、粒を削った砂の体積内に散らして中心から外へ順に出す")]
    [SerializeField] private float m_bulkFootprint = 1.4f;
    [Tooltip("広範囲を削ったとき、煙が中心から端まで広がるのにかける時間(秒)")]
    [SerializeField] private float m_bulkWaveDuration = 0.3f;
    [Tooltip("広範囲のとき、砂ぼこりの個数上限を面積に合わせて増やす（倍率の上限）")]
    [SerializeField] private float m_maxDustCountScale = 5f;

    [Header("ソフトパーティクル（自動生成マテリアルに適用）")]
    [Tooltip("ソフトパーティクルを有効にするかどうか")]
    [SerializeField] private bool m_softParticles = true;
    [Tooltip("他の面との距離がこの値(m)以下で完全に透明になる")]
    [SerializeField] private float m_softNearFade = 0f;
    [Tooltip("他の面との距離がこの値(m)以上で完全に不透明になる。砂ぼこりの大きさ程度が目安")]
    [SerializeField] private float m_softFarFade = 0.6f;
    [Tooltip("カメラに近づいた砂ぼこりを透明にして、画面が白く塞がるのを防ぐ")]
    [SerializeField] private bool m_cameraFade = true;
    [Tooltip("カメラフェード開始距離(m)")]
    [SerializeField] private float m_cameraNearFade = 0.4f;
    [Tooltip("カメラフェード完了距離(m)")]
    [SerializeField] private float m_cameraFarFade = 1.4f;

    [Header("さらさら崩れる")]
    [Tooltip("崩れ発生時に砂粒・砂ぼこりエフェクトを出すかどうか")]
    [SerializeField] private bool m_trickleEffect = true;
    [Tooltip("崩れた砂の粒数の倍率")]
    [SerializeField] private float m_trickleBoost = 3f;

    [Header("サウンド（任意）")]
    [Tooltip("削り時に再生する効果音クリップ")]
    [SerializeField] private AudioClip[] m_carveClips;
    [Tooltip("再生音量")]
    [Range(0f, 1f)]
    [SerializeField] private float m_soundVolume = 0.6f;
    [Tooltip("ピッチのランダム範囲")]
    [SerializeField] private Vector2 m_pitchRange = new Vector2(0.9f, 1.1f);
    [Tooltip("連続で鳴らす最小間隔(秒)")]
    [SerializeField] private float m_minSoundInterval = 0.12f;

    // ---- メンバー変数 ----
    private MarchingCubesMountain m_mountain;                 // 対象の砂山コンポーネント
    private ParticleSystem m_grainSystem;                     // 砂粒パーティクルシステム
    private ParticleSystem m_dustSystem;                      // 砂ぼこりパーティクルシステム
    private Material m_createdGrainMat;                       // 自動生成した砂粒マテリアル
    private Material m_createdDustMat;                        // 自動生成した砂ぼこりマテリアル
    private Texture2D m_grainTex;                             // 自動生成した砂粒テクスチャ
    private Texture2D m_dustTex;                              // 自動生成した煙シートテクスチャ
    private Light m_sunLight;                                 // シーンのディレクショナルライトキャッシュ
    private bool m_sunSearched;                               // ライト検索済みフラグ
    private Color m_tintCache = Color.white;                  // 算出済みライティングティントのキャッシュ
    private int m_tintFrame = -1;                             // ティントをキャッシュしたフレーム番号
    private readonly Color[] m_ambientResult = new Color[1];  // 環境光評価用バッファ
    private readonly List<PendingEmit> m_pendingEmits = new List<PendingEmit>(); // 遅延発生キュー
    private readonly SandAudioPlayer m_audioPlayer = new SandAudioPlayer();      // サウンド再生担当

    // ---- Unity ライフサイクル ----
    private void Awake()
    {
        m_mountain = GetComponent<MarchingCubesMountain>();

        Material gm = m_grainMaterial;
        Material dm = m_dustMaterial;

        var softSettings = new SandParticleFactory.SoftParticleSettings
        {
            m_enableSoftParticles = m_softParticles,
            m_softNearFade = m_softNearFade,
            m_softFarFade = m_softFarFade,
            m_enableCameraFade = m_cameraFade,
            m_cameraNearFade = m_cameraNearFade,
            m_cameraFarFade = m_cameraFarFade
        };

        if (gm == null)
        {
            m_grainTex = SandParticleFactory.CreateRoundTexture(GRAIN_TEXTURE_SIZE, GRAIN_TEXTURE_HARDNESS);
            gm = m_createdGrainMat = SandParticleFactory.CreateParticleMaterial(m_grainTex, false, softSettings, this);
        }

        if (dm == null)
        {
            m_dustTex = SandParticleFactory.CreatePuffSheet(DUST_TILE_SIZE, PUFF_TILES);
            dm = m_createdDustMat = SandParticleFactory.CreateParticleMaterial(m_dustTex, true, softSettings, this);
        }

        m_grainSystem = SandParticleFactory.CreateGrainSystem(transform, gm, m_grainsCollide, m_grainCollisionMask);
        m_dustSystem = SandParticleFactory.CreateDustSystem(
            transform,
            dm,
            m_dustMaterial == null || m_dustMaterialUsesSheet,
            PUFF_TILES,
            m_turbulence
        );

        m_audioPlayer.Initialize(transform, m_carveClips);

        if (m_softParticles && m_dustMaterial == null)
        {
            SandParticleFactory.WarnIfNoDepthTexture(this);
        }
    }

    private void OnEnable()
    {
        if (m_mountain == null)
        {
            m_mountain = GetComponent<MarchingCubesMountain>();
        }

        if (m_mountain != null)
        {
            m_mountain.Carved += HandleCarved;
            m_mountain.SandSlid += HandleSandSlid;
        }
    }

    private void OnDisable()
    {
        if (m_mountain != null)
        {
            m_mountain.Carved -= HandleCarved;
            m_mountain.SandSlid -= HandleSandSlid;
        }
    }

    private void Update()
    {
        if (m_pendingEmits.Count == 0)
        {
            return;
        }

        ProcessPendingEmits();
    }

    private void OnDestroy()
    {
        if (m_createdGrainMat != null) Destroy(m_createdGrainMat);
        if (m_createdDustMat != null) Destroy(m_createdDustMat);
        if (m_grainTex != null) Destroy(m_grainTex);
        if (m_dustTex != null) Destroy(m_dustTex);
    }

    // ---- イベントハンドラ ----
    /// <summary>
    /// 砂山が削られたときにパーティクルとサウンドを発生させます。
    /// </summary>
    private void HandleCarved(Vector3 point, Vector3 normal, float footprint, float volume)
    {
        footprint = Mathf.Max(footprint, MIN_FOOTPRINT);
        float power = Mathf.Lerp(0.6f, 1.4f, Mathf.Clamp01(volume / 0.05f));
        Color tint = GetLightTint();

        float thickness = volume / (Mathf.PI * footprint * footprint);
        float bulk = Mathf.Clamp01((footprint - 0.4f) / Mathf.Max(0.01f, m_bulkFootprint - 0.4f));

        EmitCarveGrains(point, normal, footprint, volume, thickness, power, bulk, tint);
        EmitCarveDust(point, normal, footprint, volume, thickness, power, bulk, tint);

        m_audioPlayer.PlaySound(point, volume, m_carveClips, m_soundVolume, m_pitchRange, m_minSoundInterval);
    }

    /// <summary>
    /// 斜面の砂が滑り落ちたときにさらさら崩れる演出を発生させます。
    /// </summary>
    private void HandleSandSlid(Vector3 point, Vector3 downhill, float volume)
    {
        if (!m_trickleEffect)
        {
            return;
        }

        Color tint = GetLightTint();
        int grainCount = Mathf.Min(RoundRandom(volume * m_grainsPerCubicMeter * m_trickleBoost), MAX_GRAINS_ON_SLIDE);

        for (int i = 0; i < grainCount; i++)
        {
            Vector3 velocity = downhill * Random.Range(0.3f, 0.9f)
                             + Vector3.up * Random.Range(0f, 0.3f)
                             + Random.insideUnitSphere * 0.1f;

            EmitOne(
                m_grainSystem,
                point + Vector3.up * 0.03f,
                velocity,
                Random.Range(m_grainSize.x, m_grainSize.y) * 0.8f,
                Random.Range(m_grainLifetime.x * 0.5f, m_grainLifetime.y * 0.6f),
                GetRandomSandColor(tint)
            );
        }

        if (Random.value < SLIDE_DUST_PROBABILITY)
        {
            EmitOne(
                m_dustSystem,
                point + Vector3.up * 0.05f,
                Vector3.up * 0.2f + downhill * 0.2f,
                Random.Range(m_dustSize.x, m_dustSize.y) * 0.6f,
                Random.Range(m_dustLifetime.x, m_dustLifetime.y) * 0.7f,
                GetDustColor(tint, 0.6f)
            );
        }
    }

    // ---- 発生処理 ----
    /// <summary>
    /// 削り時の砂粒パーティクルを発生させます。
    /// </summary>
    private void EmitCarveGrains(
        Vector3 point,
        Vector3 normal,
        float footprint,
        float volume,
        float thickness,
        float power,
        float bulk,
        Color tint)
    {
        int grainCount = Mathf.Min(RoundRandom(volume * m_grainsPerCubicMeter), m_maxGrainsPerCarve);
        float upFactor = Mathf.Lerp(1f, 0.3f, bulk);

        for (int i = 0; i < grainCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * footprint;
            GetSurfaceOrFallback(point, normal, offset, out Vector3 surfacePos, out Vector3 surfaceNormal);

            Vector3 outward = new Vector3(offset.x, 0f, offset.y);
            outward = outward.sqrMagnitude > 1e-6f ? outward.normalized : Random.onUnitSphere;

            Vector3 velocity = (outward * Random.Range(0.2f, 1f)
                             + surfaceNormal * (Random.Range(0.4f, 1.2f) * upFactor)
                             + Random.insideUnitSphere * 0.3f) * (m_ejectSpeed * power);

            float lift = Random.value * thickness * 1.5f + Random.Range(0.01f, 0.05f);

            QueueEmit(
                m_grainSystem,
                surfacePos + surfaceNormal * lift,
                velocity,
                Random.Range(m_grainSize.x, m_grainSize.y),
                Random.Range(m_grainLifetime.x, m_grainLifetime.y),
                GetRandomSandColor(tint),
                CalculateWaveDelay(offset, footprint, bulk) * 0.6f
            );
        }
    }

    /// <summary>
    /// 削り時の砂ぼこりパーティクルを発生させます。
    /// </summary>
    private void EmitCarveDust(
        Vector3 point,
        Vector3 normal,
        float footprint,
        float volume,
        float thickness,
        float power,
        float bulk,
        Color tint)
    {
        float areaScale = Mathf.Clamp(footprint * footprint, 1f, m_maxDustCountScale);
        int maxPuffs = Mathf.CeilToInt(m_maxDustPerCarve * areaScale);
        int puffCount = Mathf.Min(RoundRandom(volume * m_dustPerCubicMeter), maxPuffs);
        float densityMul = Mathf.Lerp(1f, 0.7f, bulk);

        for (int i = 0; i < puffCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * footprint;
            GetSurfaceOrFallback(point, normal, offset, out Vector3 surfacePos, out Vector3 surfaceNormal);

            Vector3 outward = new Vector3(offset.x, 0f, offset.y);
            outward = outward.sqrMagnitude > 1e-6f ? outward.normalized : Random.onUnitSphere;

            Vector3 velocity;
            float sizeMul;
            float lifeMul;
            float alphaMul;

            if (Random.value < m_rollingDustRatio)
            {
                Vector3 tangent = Vector3.ProjectOnPlane(outward, surfaceNormal);
                tangent = tangent.sqrMagnitude > 1e-6f ? tangent.normalized : Vector3.zero;
                velocity = tangent * Random.Range(0.5f, 1f) * m_rollingSpeed * power
                         + surfaceNormal * Random.Range(0.05f, 0.2f);
                sizeMul = 1.2f;
                lifeMul = 1.3f;
                alphaMul = 0.8f;
            }
            else
            {
                velocity = surfaceNormal * Random.Range(0.3f, 1f) * m_dustRiseSpeed * power
                         + outward * Random.Range(0f, 0.4f);
                sizeMul = 1f;
                lifeMul = 1f;
                alphaMul = 1f;
            }

            float lift = Random.value * thickness * 1.2f + 0.05f;

            QueueEmit(
                m_dustSystem,
                surfacePos + surfaceNormal * lift,
                velocity,
                Random.Range(m_dustSize.x, m_dustSize.y) * (0.7f + footprint) * sizeMul,
                Random.Range(m_dustLifetime.x, m_dustLifetime.y) * lifeMul,
                GetDustColor(tint, alphaMul * densityMul),
                CalculateWaveDelay(offset, footprint, bulk)
            );
        }
    }

    /// <summary>
    /// 遅延発生キュー内の予定時刻を過ぎたパーティクルを発生させます。
    /// </summary>
    private void ProcessPendingEmits()
    {
        for (int i = m_pendingEmits.Count - 1; i >= 0; i--)
        {
            PendingEmit emit = m_pendingEmits[i];
            if (Time.time < emit.m_time)
            {
                continue;
            }

            EmitOne(emit.m_system, emit.m_position, emit.m_velocity, emit.m_size, emit.m_lifetime, emit.m_color);
            m_pendingEmits[i] = m_pendingEmits[m_pendingEmits.Count - 1];
            m_pendingEmits.RemoveAt(m_pendingEmits.Count - 1);
        }
    }

    /// <summary>
    /// 広範囲掘削時に中心から外へ広がるための遅延時間を算出します。
    /// </summary>
    private float CalculateWaveDelay(Vector2 offset, float footprint, float bulk)
    {
        float t = Mathf.Clamp01(offset.magnitude / footprint);
        return t * m_bulkWaveDuration * bulk + Random.Range(0f, 0.05f) * bulk;
    }

    /// <summary>
    /// 指定された遅延後にパーティクルを発生させます（微小な遅延は即時発生）。
    /// </summary>
    private void QueueEmit(ParticleSystem ps, Vector3 pos, Vector3 vel, float size, float life, Color color, float delay)
    {
        if (delay <= MIN_EMIT_DELAY)
        {
            EmitOne(ps, pos, vel, size, life, color);
            return;
        }

        if (m_pendingEmits.Count >= MAX_PENDING_EMITS)
        {
            return;
        }

        m_pendingEmits.Add(new PendingEmit
        {
            m_time = Time.time + delay,
            m_system = ps,
            m_position = pos,
            m_velocity = vel,
            m_size = size,
            m_lifetime = life,
            m_color = color
        });
    }

    /// <summary>
    /// ParticleSystem へ1粒のパーティクルを即座に出力します。
    /// </summary>
    private static void EmitOne(ParticleSystem ps, Vector3 position, Vector3 velocity, float size, float lifetime, Color color)
    {
        if (ps == null)
        {
            return;
        }

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

    // ---- 補助処理 ----
    /// <summary>
    /// 小数部を確率として扱い、期待値を整数へ丸めます。
    /// </summary>
    private static int RoundRandom(float value)
    {
        int n = Mathf.FloorToInt(value);
        if (Random.value < value - n)
        {
            n++;
        }

        return n;
    }

    /// <summary>
    /// ランダムなばらつきとライティングを加味した砂粒の色を返します。
    /// </summary>
    private Color GetRandomSandColor(Color tint)
    {
        float v = Random.Range(-m_colorVariation, m_colorVariation);
        return new Color(
            Mathf.Clamp01((m_sandColor.r + v) * tint.r),
            Mathf.Clamp01((m_sandColor.g + v) * tint.g),
            Mathf.Clamp01((m_sandColor.b + v) * tint.b),
            m_sandColor.a
        );
    }

    /// <summary>
    /// ライティングを加味した砂ぼこりの色を返します。
    /// </summary>
    private Color GetDustColor(Color tint, float alphaMul)
    {
        float v = 1f + Random.Range(-0.06f, 0.06f);
        return new Color(
            Mathf.Clamp01(m_dustColor.r * tint.r * v),
            Mathf.Clamp01(m_dustColor.g * tint.g * v),
            Mathf.Clamp01(m_dustColor.b * tint.b * v),
            m_dustColor.a * alphaMul
        );
    }

    /// <summary>
    /// シーンの環境光と太陽光からライティング乗算色を取得します（フレーム単位キャッシュ）。
    /// </summary>
    private Color GetLightTint()
    {
        if (m_tintFrame == Time.frameCount)
        {
            return m_tintCache;
        }

        m_tintFrame = Time.frameCount;

        if (m_lightInfluence <= 0f)
        {
            m_tintCache = Color.white;
            return m_tintCache;
        }

        if (!m_sunSearched)
        {
            m_sunSearched = true;
            m_sunLight = RenderSettings.sun;
            if (m_sunLight == null)
            {
                foreach (Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))
                {
                    if (light.type == LightType.Directional && (m_sunLight == null || light.intensity > m_sunLight.intensity))
                    {
                        m_sunLight = light;
                    }
                }
            }
        }

        SphericalHarmonicsL2 probe = RenderSettings.ambientProbe;
        probe.Evaluate(UP_DIRECTIONS, m_ambientResult);
        Color ambient = m_ambientResult[0];
        if (QualitySettings.activeColorSpace == ColorSpace.Linear)
        {
            ambient = ambient.gamma;
        }

        Color direct = (m_sunLight != null && m_sunLight.isActiveAndEnabled)
            ? m_sunLight.color * (Mathf.Min(m_sunLight.intensity, 2f) * 0.55f)
            : Color.black;

        Color lit = ambient + direct;
        lit = new Color(
            Mathf.Clamp(lit.r, 0.2f, 1.1f),
            Mathf.Clamp(lit.g, 0.2f, 1.1f),
            Mathf.Clamp(lit.b, 0.2f, 1.1f),
            1f
        );

        m_tintCache = Color.Lerp(Color.white, lit, m_lightInfluence);
        return m_tintCache;
    }

    /// <summary>
    /// 中心からずらした位置の砂表面座標と法線を取得します（範囲外の場合は基準値を返します）。
    /// </summary>
    private void GetSurfaceOrFallback(Vector3 center, Vector3 fallbackNormal, Vector2 offset, out Vector3 pos, out Vector3 normal)
    {
        Vector3 probe = new Vector3(center.x + offset.x, center.y, center.z + offset.y);
        if (m_mountain == null || !m_mountain.TrySampleSurface(probe, out pos, out normal))
        {
            pos = center;
            normal = fallbackNormal;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// 自動生成した砂粒・砂ぼこり用テクスチャを Assets 直下に PNG として書き出します。
    /// </summary>
    [ContextMenu("生成テクスチャをPNGに書き出す")]
    private void ExportTextures()
    {
        Texture2D sheet = m_dustTex != null ? m_dustTex : SandParticleFactory.CreatePuffSheet(DUST_TILE_SIZE, PUFF_TILES);
        Texture2D grain = m_grainTex != null ? m_grainTex : SandParticleFactory.CreateRoundTexture(GRAIN_TEXTURE_SIZE, GRAIN_TEXTURE_HARDNESS);

        System.IO.File.WriteAllBytes(Application.dataPath + "/SandDustSheet.png", sheet.EncodeToPNG());
        System.IO.File.WriteAllBytes(Application.dataPath + "/SandGrain.png", grain.EncodeToPNG());
        UnityEditor.AssetDatabase.Refresh();

        if (sheet != m_dustTex) DestroyImmediate(sheet);
        if (grain != m_grainTex) DestroyImmediate(grain);

        Debug.Log("[SandEffects] Assets/SandDustSheet.png と Assets/SandGrain.png を書き出しました。");
    }
#endif
}