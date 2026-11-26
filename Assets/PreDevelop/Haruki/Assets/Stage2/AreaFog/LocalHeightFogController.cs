using System.Collections;
using UnityEngine;

/// <summary>
/// 室内の局所的な霧だまり（高さ制限フォグ）の濃度や高さを制御するクラス。
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshRenderer))]
public class LocalHeightFogController : MonoBehaviour
{
    // 霧の残量の最小値と最大値
    private const float MIN_FOG_AMOUNT = 0.0f;
    private const float MAX_FOG_AMOUNT = 1.0f;

    // 霧が完全に晴れたとみなす閾値
    private const float CLEARED_THRESHOLD = 0.01f;

    // 遷移時間の最小値
    private const float MIN_TRANSITION_DURATION = 0.001f;

    // 霧の高さ幅の最小値（ゼロ除算防止用）
    private const float MIN_HEIGHT_THICKNESS = 0.01f;

    // 晴れた際に霧の上面が沈み込む最低比率
    private const float MIN_SINK_HEIGHT_RATIO = 0.15f;

    // シェーダープロパティ名の定数
    private const string SHADER_PROP_FOG_COLOR = "_FogColor";
    private const string SHADER_PROP_BOTTOM_DARKNESS = "_BottomDarkness";
    private const string SHADER_PROP_DENSITY = "_BaseDensity";
    private const string SHADER_PROP_MIN_HEIGHT = "_MinHeight";
    private const string SHADER_PROP_MAX_HEIGHT = "_MaxHeight";
    private const string SHADER_PROP_HEIGHT_FALLOFF = "_HeightFalloff";
    private const string SHADER_PROP_NEAR_CLEAR_DIST = "_NearClearDistance";
    private const string SHADER_PROP_VISIBILITY_DIST = "_VisibilityDistance";
    private const string SHADER_PROP_NOISE_SCALE = "_NoiseScale";
    private const string SHADER_PROP_NOISE_SPEED = "_NoiseSpeed";
    private const string SHADER_PROP_NOISE_STRENGTH = "_NoiseDetailStrength";

    /// <summary>
    /// 霧を描画する対象のメッシュレンダラー。
    /// </summary>
    [Header("対象レンダラー設定")]
    [SerializeField]
    private MeshRenderer m_fogRenderer;

    /// <summary>
    /// 開始時の霧の量（0.0で快晴、1.0で最大濃度）。
    /// </summary>
    [Header("霧の基本制御")]
    [SerializeField, Range(MIN_FOG_AMOUNT, MAX_FOG_AMOUNT)]
    private float m_initialFogAmount = 1.0f;

    /// <summary>
    /// 霧が晴れる際、濃度だけでなく霧の高さも床へ沈み込むように下げるかどうか。
    /// </summary>
    [SerializeField]
    private bool m_sinkHeightOnClear = true;

    /// <summary>
    /// オブジェクト（Cube）の底面Y座標を自動的に霧の底面（床）として使用するかどうか。
    /// </summary>
    [Header("高さ制限（下に溜まる設定）")]
    [SerializeField]
    private bool m_useRendererBottomAsMinHeight = true;

    /// <summary>
    /// 手動指定する場合の霧の底面（床）のワールドY座標。
    /// </summary>
    [SerializeField]
    private float m_manualMinHeight = 0.0f;

    /// <summary>
    /// 底面（床）から霧が溜まる最大の高さ（メートル）。
    /// </summary>
    [SerializeField, Min(MIN_HEIGHT_THICKNESS)]
    private float m_maxFogHeight = 1.6f;

    /// <summary>
    /// 霧の上面境界の減衰具合（値が大きいほど足元に強く溜まり、上面が滑らかに消える）。
    /// </summary>
    [SerializeField, Range(0.5f, 5.0f)]
    private float m_heightFalloff = 2.0f;

    /// <summary>
    /// 霧の基本カラー。
    /// </summary>
    [Header("色と近距離の視界制限設定")]
    [SerializeField]
    private Color m_fogColor = new Color(0.75f, 0.8f, 0.85f, 1.0f);

    /// <summary>
    /// 底面に溜まった部分の暗さ（立体感を出すための下部減光率）。
    /// </summary>
    [SerializeField, Range(0.0f, 0.8f)]
    private float m_bottomDarkness = 0.35f;

    /// <summary>
    /// 霧の基本濃度（値が大きいほど視界が強く遮られる）。
    /// </summary>
    [SerializeField, Range(0.0f, 5.0f)]
    private float m_baseDensity = 1.8f;

    /// <summary>
    /// 三人称カメラ直前の視界確保距離（カメラからこの距離までは霧を抑えて自キャラを見やすくする）。
    /// </summary>
    [SerializeField, Min(0.0f)]
    private float m_nearClearDistance = 1.2f;

    /// <summary>
    /// 近距離で視界が制限される基準距離（メートル）。この距離付近で急激に視界が遮られる。
    /// </summary>
    [SerializeField, Min(0.5f)]
    private float m_visibilityDistance = 4.5f;

    /// <summary>
    /// 霧のうねり（3Dノイズ）の細かさ。
    /// </summary>
    [Header("リアルな霧だまりのうねり設定")]
    [SerializeField, Range(0.1f, 3.0f)]
    private float m_noiseScale = 0.65f;

    /// <summary>
    /// 霧が流れる速度（X, Y, Z）。
    /// </summary>
    [SerializeField]
    private Vector3 m_noiseSpeed = new Vector3(0.15f, -0.05f, 0.1f);

    /// <summary>
    /// 霧の濃淡ムラの強さ（上面の波打ちや塊感の強度）。
    /// </summary>
    [SerializeField, Range(0.0f, 1.0f)]
    private float m_noiseDetailStrength = 0.6f;

    // 現在の霧の量（0.0〜1.0）
    private float m_currentFogAmount;

    // マテリアルを複製せずにプロパティを変更するためのブロック
    private MaterialPropertyBlock m_propertyBlock;

    // 現在実行中のフェード用コルーチン
    private Coroutine m_transitionCoroutine;

    // シェーダープロパティIDのキャッシュ
    private int m_fogColorPropId;
    private int m_bottomDarknessPropId;
    private int m_densityPropId;
    private int m_minHeightPropId;
    private int m_maxHeightPropId;
    private int m_heightFalloffPropId;
    private int m_nearClearDistPropId;
    private int m_visibilityDistPropId;
    private int m_noiseScalePropId;
    private int m_noiseSpeedPropId;
    private int m_noiseStrengthPropId;

    // プロパティIDが初期化済みかどうかのフラグ
    private bool m_isInitialized;

    /// <summary>
    /// 初期化処理を行います。
    /// </summary>
    private void Awake()
    {
        InitializeController();
        SetFogAmount(m_initialFogAmount);
    }

    /// <summary>
    /// インスペクターで値が変更された際にプレビューを更新します。
    /// </summary>
    private void OnValidate()
    {
        InitializeController();
        m_currentFogAmount = Mathf.Clamp(m_initialFogAmount, MIN_FOG_AMOUNT, MAX_FOG_AMOUNT);
        ApplyFogProperties();
    }

    /// <summary>
    /// 現在の霧の量（0.0〜1.0）を取得します。
    /// </summary>
    /// <returns>現在の霧の量。</returns>
    public float GetFogAmount()
    {
        return m_currentFogAmount;
    }

    /// <summary>
    /// 霧の量（0.0〜1.0）を即座に設定します。
    /// </summary>
    /// <param name="fogAmount">設定する霧の量（0.0で快晴、1.0で最大）。</param>
    public void SetFogAmount(float fogAmount)
    {
        // 進行中のコルーチンがあれば停止
        StopActiveTransition();

        // 範囲内に丸めて適用
        m_currentFogAmount = Mathf.Clamp(fogAmount, MIN_FOG_AMOUNT, MAX_FOG_AMOUNT);
        ApplyFogProperties();
    }

    /// <summary>
    /// 指定した時間をかけて霧を完全に晴らします。
    /// </summary>
    /// <param name="duration">霧が晴れるまでにかかる秒数。</param>
    public void ClearFog(float duration)
    {
        TransitionFogAmount(MIN_FOG_AMOUNT, duration);
    }

    /// <summary>
    /// 指定した時間をかけて霧を最大濃度まで溜めます。
    /// </summary>
    /// <param name="duration">霧が溜まるまでにかかる秒数。</param>
    public void FillFog(float duration)
    {
        TransitionFogAmount(MAX_FOG_AMOUNT, duration);
    }

    /// <summary>
    /// 指定した目標の霧の量へ、時間をかけて滑らかに変化させます。
    /// </summary>
    /// <param name="targetAmount">目標の霧の量（0.0〜1.0）。</param>
    /// <param name="duration">変化にかかる秒数。</param>
    public void TransitionFogAmount(float targetAmount, float duration)
    {
        float clampedTarget = Mathf.Clamp(targetAmount, MIN_FOG_AMOUNT, MAX_FOG_AMOUNT);

        // 遷移時間がほぼ0秒、または非実行中の場合は即時反映
        if (duration <= MIN_TRANSITION_DURATION || !Application.isPlaying)
        {
            SetFogAmount(clampedTarget);
            return;
        }

        // 既存の遷移コルーチンを停止して新しく開始
        StopActiveTransition();
        m_transitionCoroutine = StartCoroutine(RunFogTransitionCoroutine(clampedTarget, duration));
    }

    /// <summary>
    /// 霧がほぼ完全に晴れている状態かを判定します。
    /// </summary>
    /// <returns>
    /// true：霧が完全に晴れています。
    /// false：霧が残っています。
    /// </returns>
    public bool IsFogCleared()
    {
        return m_currentFogAmount <= CLEARED_THRESHOLD;
    }

    /// <summary>
    /// コンポーネントとシェーダープロパティIDの初期化を行います。
    /// </summary>
    private void InitializeController()
    {
        // MeshRendererが未設定の場合は自身から取得
        if (m_fogRenderer == null)
        {
            m_fogRenderer = GetComponent<MeshRenderer>();
        }

        // MaterialPropertyBlockの生成
        if (m_propertyBlock == null)
        {
            m_propertyBlock = new MaterialPropertyBlock();
        }

        // プロパティIDをキャッシュ
        if (!m_isInitialized)
        {
            CacheShaderPropertyIds();
            m_isInitialized = true;
        }
    }

    /// <summary>
    /// シェーダープロパティのIDをキャッシュします。
    /// </summary>
    private void CacheShaderPropertyIds()
    {
        m_fogColorPropId = Shader.PropertyToID(SHADER_PROP_FOG_COLOR);
        m_bottomDarknessPropId = Shader.PropertyToID(SHADER_PROP_BOTTOM_DARKNESS);
        m_densityPropId = Shader.PropertyToID(SHADER_PROP_DENSITY);
        m_minHeightPropId = Shader.PropertyToID(SHADER_PROP_MIN_HEIGHT);
        m_maxHeightPropId = Shader.PropertyToID(SHADER_PROP_MAX_HEIGHT);
        m_heightFalloffPropId = Shader.PropertyToID(SHADER_PROP_HEIGHT_FALLOFF);
        m_nearClearDistPropId = Shader.PropertyToID(SHADER_PROP_NEAR_CLEAR_DIST);
        m_visibilityDistPropId = Shader.PropertyToID(SHADER_PROP_VISIBILITY_DIST);
        m_noiseScalePropId = Shader.PropertyToID(SHADER_PROP_NOISE_SCALE);
        m_noiseSpeedPropId = Shader.PropertyToID(SHADER_PROP_NOISE_SPEED);
        m_noiseStrengthPropId = Shader.PropertyToID(SHADER_PROP_NOISE_STRENGTH);
    }

    /// <summary>
    /// 現在のパラメータを計算してシェーダー（MaterialPropertyBlock）へ適用します。
    /// </summary>
    private void ApplyFogProperties()
    {
        if (!m_isInitialized)
        {
            InitializeController();
        }

        if (m_fogRenderer == null)
        {
            return;
        }

        // 現在の霧の残量に応じた実効濃度を計算
        float effectiveDensity = m_baseDensity * m_currentFogAmount;

        // 霧の底面（床）のワールドY座標を取得
        float baseMinHeight = CalculateBaseMinHeight();

        // 霧が晴れるときに高さを沈み込ませる計算
        float heightMultiplier = MAX_FOG_AMOUNT;
        if (m_sinkHeightOnClear)
        {
            heightMultiplier = Mathf.Lerp(MIN_SINK_HEIGHT_RATIO, MAX_FOG_AMOUNT, m_currentFogAmount);
        }

        float effectiveMaxHeight = baseMinHeight + Mathf.Max(MIN_HEIGHT_THICKNESS, m_maxFogHeight * heightMultiplier);

        // プロパティブロックを更新してレンダラーにセット
        m_fogRenderer.GetPropertyBlock(m_propertyBlock);

        m_propertyBlock.SetColor(m_fogColorPropId, m_fogColor);
        m_propertyBlock.SetFloat(m_bottomDarknessPropId, m_bottomDarkness);
        m_propertyBlock.SetFloat(m_densityPropId, effectiveDensity);
        m_propertyBlock.SetFloat(m_minHeightPropId, baseMinHeight);
        m_propertyBlock.SetFloat(m_maxHeightPropId, effectiveMaxHeight);
        m_propertyBlock.SetFloat(m_heightFalloffPropId, m_heightFalloff);
        m_propertyBlock.SetFloat(m_nearClearDistPropId, m_nearClearDistance);
        m_propertyBlock.SetFloat(m_visibilityDistPropId, Mathf.Max(MIN_HEIGHT_THICKNESS, m_visibilityDistance));
        m_propertyBlock.SetFloat(m_noiseScalePropId, m_noiseScale);
        m_propertyBlock.SetVector(m_noiseSpeedPropId, m_noiseSpeed);
        m_propertyBlock.SetFloat(m_noiseStrengthPropId, m_noiseDetailStrength);

        m_fogRenderer.SetPropertyBlock(m_propertyBlock);
    }

    /// <summary>
    /// 霧の底面となるワールドY座標を算出します。
    /// </summary>
    /// <returns>底面のワールドY座標。</returns>
    private float CalculateBaseMinHeight()
    {
        if (m_useRendererBottomAsMinHeight && m_fogRenderer != null)
        {
            return m_fogRenderer.bounds.min.y;
        }

        return m_manualMinHeight;
    }

    /// <summary>
    /// 霧の量を時間をかけて滑らかに変化させるコルーチン処理です。
    /// </summary>
    /// <param name="targetAmount">目標の霧の量。</param>
    /// <param name="duration">変化にかける秒数。</param>
    /// <returns>コルーチン用のIEnumerator。</returns>
    private IEnumerator RunFogTransitionCoroutine(float targetAmount, float duration)
    {
        float startAmount = m_currentFogAmount;
        float elapsedTime = 0.0f;

        // 目標値に到達するまで毎フレーム補間
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / duration);

            // 自然な加減速（SmoothStep）で霧の濃さと高さを補間
            float smoothRate = Mathf.SmoothStep(0.0f, 1.0f, normalizedTime);
            m_currentFogAmount = Mathf.Lerp(startAmount, targetAmount, smoothRate);

            ApplyFogProperties();
            yield return null;
        }

        // 最終値を確実に反映
        m_currentFogAmount = targetAmount;
        ApplyFogProperties();
        m_transitionCoroutine = null;
    }

    /// <summary>
    /// 実行中の遷移コルーチンが存在する場合は停止します。
    /// </summary>
    private void StopActiveTransition()
    {
        if (m_transitionCoroutine != null)
        {
            StopCoroutine(m_transitionCoroutine);
            m_transitionCoroutine = null;
        }
    }
}