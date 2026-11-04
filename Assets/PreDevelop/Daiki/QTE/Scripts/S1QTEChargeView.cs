using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// QTEの長押し進行状態をUIへ描画します。
/// </summary>
[DisallowMultipleComponent]
public sealed class S1QTEChargeView : MonoBehaviour
{
    // ShaderのGlowEnabledプロパティ
    private static readonly int GLOW_ENABLED_PROPERTY =
        Shader.PropertyToID("_GlowEnabled");

    // ShaderのProgressプロパティ
    private static readonly int PROGRESS_PROPERTY =
        Shader.PropertyToID("_Progress");

    [SerializeField, Header("UI")]
    private Image m_chargeFillImage;

    // このUI専用の実行時Material
    private Material m_runtimeMaterial;

    private void Awake()
    {
        InitializeMaterial();
    }

    private void OnEnable()
    {
        SetProgress(
            0.0f,
            false);
    }

    private void OnDestroy()
    {
        ReleaseMaterial();
    }

    /// <summary>
    /// QTEの溜め状態をUIへ反映します。
    /// </summary>
    /// <param name="progress">0～1の進行率。</param>
    /// <param name="isHolding">現在長押し中か。</param>
    public void SetProgress(
        float progress,
        bool isHolding)
    {
        if (m_chargeFillImage == null ||
            m_runtimeMaterial == null)
        {
            return;
        }

        progress =
            Mathf.Clamp01(progress);

        /*
         * Image側のFilledを使用して、
         * 下から上へゲージを充填します。
         */
        m_chargeFillImage.fillAmount =
            progress;

        /*
         * Shader側にも進行率を渡します。
         *
         * Progressが1になった場合の最大発光固定は
         * Shader側で処理します。
         */
        m_runtimeMaterial.SetFloat(
            PROGRESS_PROPERTY,
            progress);

        /*
         * 長押し中のみ発光の明滅を有効にします。
         */
        m_runtimeMaterial.SetFloat(
            GLOW_ENABLED_PROPERTY,
            isHolding ? 1.0f : 0.0f);
    }

    /// <summary>
    /// UI専用のMaterialを生成します。
    /// </summary>
    private void InitializeMaterial()
    {
        if (m_chargeFillImage == null)
        {
            Debug.LogError(
                "QTEゲージ用Imageが設定されていません。",
                this);

            return;
        }

        if (m_chargeFillImage.material == null)
        {
            Debug.LogError(
                "QTEゲージ用Materialが設定されていません。",
                this);

            return;
        }

        /*
         * Inspectorで設定したMaterialを直接変更すると、
         * 同じMaterialを使用している他のUIにも
         * パラメータ変更が反映されます。
         *
         * そのため、このUI専用のMaterialを生成します。
         */
        m_runtimeMaterial =
            new Material(
                m_chargeFillImage.material);

        m_chargeFillImage.material =
            m_runtimeMaterial;

        if (!m_runtimeMaterial.HasProperty(
                PROGRESS_PROPERTY))
        {
            Debug.LogError(
                "Shaderに_Progressプロパティがありません。",
                this);
        }

        if (!m_runtimeMaterial.HasProperty(
                GLOW_ENABLED_PROPERTY))
        {
            Debug.LogError(
                "Shaderに_GlowEnabledプロパティがありません。",
                this);
        }
    }

    /// <summary>
    /// 実行時に生成したMaterialを破棄します。
    /// </summary>
    private void ReleaseMaterial()
    {
        if (m_runtimeMaterial == null)
        {
            return;
        }

        Destroy(m_runtimeMaterial);

        m_runtimeMaterial = null;
    }
}