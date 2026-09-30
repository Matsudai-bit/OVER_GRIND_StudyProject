using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 着地点を示す予告Decalを制御します。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(DecalProjector))]
public sealed class TargetDecalController :
    MonoBehaviour
{
    // 高さ計算時の最小値
    private const float MIN_HEIGHT = 0.0001f;

    // Decal Projector
    private DecalProjector m_decalProjector;

    // 追跡するオイル
    private Transform m_oilTransform;

    // 着地点
    private Vector3 m_landingPosition;

    // 表示開始時のオイルの高さ
    private float m_startHeight;

    // 基準となるDecalサイズ
    private Vector3 m_baseSize;

    // 最小サイズ倍率
    private float m_minScale;

    // 最大サイズ倍率
    private float m_maxScale;

    // 最小不透明度
    private float m_minOpacity;

    // 最大不透明度
    private float m_maxOpacity;

    // 表示中か
    private bool m_isVisible;

    /// <summary>
    /// 初期化します。
    /// </summary>
    private void Awake()
    {
        m_decalProjector =
            GetComponent<DecalProjector>();

        m_baseSize =
            m_decalProjector.size;

        Hide();
    }

    /// <summary>
    /// 着地点予告を表示します。
    /// </summary>
    /// <param name="oilTransform">追跡するオイル。</param>
    /// <param name="landingPosition">着地点。</param>
    /// <param name="minScale">最小サイズ倍率。</param>
    /// <param name="maxScale">最大サイズ倍率。</param>
    /// <param name="minOpacity">最小不透明度。</param>
    /// <param name="maxOpacity">最大不透明度。</param>
    /// <param name="groundOffset">地面からのオフセット。</param>
    public void Show(
        Transform oilTransform,
        Vector3 landingPosition,
        float minScale,
        float maxScale,
        float minOpacity,
        float maxOpacity,
        float groundOffset)
    {
        if (oilTransform == null)
        {
            return;
        }

        m_oilTransform =
            oilTransform;

        m_landingPosition =
            landingPosition;

        m_minScale =
            Mathf.Max(0.0f, minScale);

        m_maxScale =
            Mathf.Max(m_minScale, maxScale);

        m_minOpacity =
            Mathf.Clamp01(minOpacity);

        m_maxOpacity =
            Mathf.Clamp(
                maxOpacity,
                m_minOpacity,
                1.0f);

        transform.position =
            landingPosition +
            Vector3.up * groundOffset;

        m_startHeight =
            Mathf.Max(
                oilTransform.position.y -
                landingPosition.y,
                MIN_HEIGHT);

        m_decalProjector.enabled =
            true;

        m_isVisible =
            true;

        ApplyProgress(
            0.0f);
    }

    /// <summary>
    /// 着地点予告を更新します。
    /// </summary>
    private void LateUpdate()
    {
        if (!m_isVisible ||
            m_oilTransform == null)
        {
            return;
        }

        float currentHeight =
            Mathf.Max(
                0.0f,
                m_oilTransform.position.y -
                m_landingPosition.y);

        float progress =
            1.0f -
            Mathf.Clamp01(
                currentHeight /
                m_startHeight);

        ApplyProgress(
            progress);
    }

    /// <summary>
    /// 落下進行率をDecalへ反映します。
    /// </summary>
    /// <param name="progress">落下進行率。</param>
    private void ApplyProgress(
        float progress)
    {
        progress =
            Mathf.Clamp01(progress);

        float scale =
            Mathf.Lerp(
                m_minScale,
                m_maxScale,
                progress);

        Vector3 decalSize =
            m_baseSize;

        // Projection Depthは変更せず、
        // WidthとHeightのみ拡大する
        decalSize.x =
            m_baseSize.x * scale;

        decalSize.y =
            m_baseSize.y * scale;

        m_decalProjector.size =
            decalSize;

        m_decalProjector.fadeFactor =
            Mathf.Lerp(
                m_minOpacity,
                m_maxOpacity,
                progress);
    }

    /// <summary>
    /// 着地点予告を非表示にします。
    /// </summary>
    public void Hide()
    {
        m_isVisible =
            false;

        m_oilTransform =
            null;

        if (m_decalProjector != null)
        {
            m_decalProjector.enabled =
                false;
        }
    }
}