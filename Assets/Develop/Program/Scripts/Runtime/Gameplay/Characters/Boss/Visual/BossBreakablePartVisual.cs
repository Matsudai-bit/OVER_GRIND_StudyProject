using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// ボス破壊可能部位の見た目を制御します。
/// </summary>
[DisallowMultipleComponent]
public sealed class BossBreakablePartVisual : MonoBehaviour
{
    [Header("メッシュ参照")]

    // メッシュ参照管理
    [SerializeField]
    private MeshReferenceRegistry m_meshReferenceRegistry;

    // 通常時のメッシュID
    [SerializeField]
    private string m_normalMeshId;

    // 破壊後のメッシュID
    [SerializeField]
    private string m_brokenMeshId;

    // Emissiveを操作するMaterial Slot
    [SerializeField, Header("Material")]
    [Min(0)]
    private int m_materialSlotIndex;

    // Emissive強度のShader Property名
    [SerializeField]
    private string m_emissivePropertyName =
        "_EmissiveIntencity";

    // 被弾演出パラメータ
    [SerializeField, Header("被弾演出")]
    private BossBreakablePartFeedbackParameterAsset
        m_feedbackParameter;

    // 通常時のRenderer
    private Renderer m_normalRenderer;

    // 破壊後のRenderer
    private Renderer m_brokenRenderer;

    // Emissive操作用
    private MaterialPropertyBlock m_materialPropertyBlock;

    // Shader Property ID
    private int m_emissivePropertyID;

    // 通常時のEmissive値
    private float m_initialEmissiveValue;

    // 被弾演出Coroutine
    private Coroutine m_feedbackCoroutine;

    // Emissive制御が使用可能か
    private bool m_canControlEmissive;

    /// <summary>
    /// 被弾演出が終了したときに通知されます。
    /// </summary>
    public event Action FeedbackCompleted;

    /// <summary>
    /// 初期化します。
    /// </summary>
    private void Awake()
    {
        InitializeMeshReferences();
        InitializeEmissive();

        if (m_normalRenderer != null)
        {
            m_normalRenderer.gameObject.SetActive(true);
        }

        if (m_brokenRenderer != null)
        {
            m_brokenRenderer.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 被弾演出を最初から再生します。
    /// </summary>
    public void PlayDamageFeedback()
    {
        if (m_feedbackCoroutine != null)
        {
            StopCoroutine(m_feedbackCoroutine);

            m_feedbackCoroutine = null;
        }

        /*
         * 上昇途中でも一度基準値へ戻すことで、
         * 必ず「最初から上げる」挙動にする。
         */
        RestoreEmissive();

        m_feedbackCoroutine =
            StartCoroutine(
                DamageFeedbackCoroutine());
    }

    /// <summary>
    /// 破壊後の見た目へ切り替えます。
    /// </summary>
    public void SetBrokenVisual()
    {
        StopDamageFeedback();

        if (m_normalRenderer != null)
        {
            m_normalRenderer.gameObject.SetActive(false);
        }

        if (m_brokenRenderer != null)
        {
            m_brokenRenderer.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 被弾演出を停止します。
    /// </summary>
    public void StopDamageFeedback()
    {
        if (m_feedbackCoroutine != null)
        {
            StopCoroutine(m_feedbackCoroutine);

            m_feedbackCoroutine = null;
        }

        RestoreEmissive();
    }

    /// <summary>
    /// メッシュ参照を取得します。
    /// </summary>
    private void InitializeMeshReferences()
    {
        if (m_meshReferenceRegistry == null)
        {
            Debug.LogError(
                $"[{nameof(BossBreakablePartVisual)}] " +
                "MeshReferenceRegistryが設定されていません。",
                this);

            return;
        }

        if (!m_meshReferenceRegistry.TryGetRenderer(
                m_normalMeshId,
                out m_normalRenderer))
        {
            Debug.LogError(
                $"[{nameof(BossBreakablePartVisual)}] " +
                $"通常メッシュID「{m_normalMeshId}」が見つかりません。",
                this);
        }

        if (!m_meshReferenceRegistry.TryGetRenderer(
                m_brokenMeshId,
                out m_brokenRenderer))
        {
            Debug.LogError(
                $"[{nameof(BossBreakablePartVisual)}] " +
                $"破壊メッシュID「{m_brokenMeshId}」が見つかりません。",
                this);
        }
    }

    /// <summary>
    /// Emissive操作に必要な情報を初期化します。
    /// </summary>
    private void InitializeEmissive()
    {
        m_canControlEmissive = false;

        if (m_normalRenderer == null ||
            string.IsNullOrEmpty(
                m_emissivePropertyName))
        {
            return;
        }

        Material[] materials =
            m_normalRenderer.sharedMaterials;

        if (m_materialSlotIndex < 0 ||
            m_materialSlotIndex >= materials.Length)
        {
            Debug.LogError(
                $"Material Slot Index " +
                $"{m_materialSlotIndex} が範囲外です。",
                this);

            return;
        }

        Material targetMaterial =
            materials[m_materialSlotIndex];

        if (targetMaterial == null)
        {
            Debug.LogError(
                "対象Materialが設定されていません。",
                this);

            return;
        }

        m_emissivePropertyID =
            Shader.PropertyToID(
                m_emissivePropertyName);

        if (!targetMaterial.HasProperty(
                m_emissivePropertyID))
        {
            Debug.LogError(
                $"Materialに " +
                $"{m_emissivePropertyName} " +
                "が存在しません。",
                this);

            return;
        }

        m_initialEmissiveValue =
            targetMaterial.GetFloat(
                m_emissivePropertyID);

        m_materialPropertyBlock =
            new MaterialPropertyBlock();

        m_canControlEmissive = true;
    }

    /// <summary>
    /// 被弾時のEmissive演出を実行します。
    /// </summary>
    private IEnumerator DamageFeedbackCoroutine()
    {
        if (!m_canControlEmissive ||
            m_feedbackParameter == null)
        {
            FeedbackCompleted?.Invoke();

            m_feedbackCoroutine = null;

            yield break;
        }

        float maxEmissive =
            m_initialEmissiveValue +
            m_feedbackParameter.EmissiveIncrease;

        // Emissive上昇
        yield return ChangeEmissiveCoroutine(
            m_initialEmissiveValue,
            maxEmissive,
            m_feedbackParameter.RiseDuration,
            m_feedbackParameter.RiseCurve);

        // Emissive下降
        yield return ChangeEmissiveCoroutine(
            maxEmissive,
            m_initialEmissiveValue,
            m_feedbackParameter.FallDuration,
            m_feedbackParameter.FallCurve);

        RestoreEmissive();

        m_feedbackCoroutine = null;

        FeedbackCompleted?.Invoke();
    }

    /// <summary>
    /// Emissive値を指定時間で補間します。
    /// </summary>
    /// <param name="startValue">開始値。</param>
    /// <param name="endValue">終了値。</param>
    /// <param name="duration">補間時間。</param>
    /// <param name="curve">イージングカーブ。</param>
    private IEnumerator ChangeEmissiveCoroutine(
        float startValue,
        float endValue,
        float duration,
        AnimationCurve curve)
    {
        if (duration <= 0.0f)
        {
            SetEmissive(endValue);

            yield break;
        }

        float elapsedTime = 0.0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    elapsedTime / duration);

            float curveValue =
                curve != null
                    ? curve.Evaluate(normalizedTime)
                    : normalizedTime;

            float emissive =
                Mathf.LerpUnclamped(
                    startValue,
                    endValue,
                    curveValue);

            SetEmissive(emissive);

            yield return null;
        }

        SetEmissive(endValue);
    }

    /// <summary>
    /// Emissive値を設定します。
    /// </summary>
    /// <param name="value">設定値。</param>
    private void SetEmissive(float value)
    {
        if (!m_canControlEmissive ||
            m_normalRenderer == null)
        {
            return;
        }

        m_normalRenderer.GetPropertyBlock(
            m_materialPropertyBlock,
            m_materialSlotIndex);

        m_materialPropertyBlock.SetFloat(
            m_emissivePropertyID,
            value);

        m_normalRenderer.SetPropertyBlock(
            m_materialPropertyBlock,
            m_materialSlotIndex);
    }

    /// <summary>
    /// Emissiveを通常値へ戻します。
    /// </summary>
    private void RestoreEmissive()
    {
        if (!m_canControlEmissive)
        {
            return;
        }

        SetEmissive(
            m_initialEmissiveValue);
    }
}