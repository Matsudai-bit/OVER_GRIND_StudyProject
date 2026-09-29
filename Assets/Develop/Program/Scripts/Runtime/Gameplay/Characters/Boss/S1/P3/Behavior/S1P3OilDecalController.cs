using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// オイル着弾後に残る痕Decalを制御します。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(DecalProjector))]
public sealed class S1P3OilDecalController :
    MonoBehaviour
{
    // Decal Projector
    private DecalProjector m_decalProjector;

    // 表示時間
    private float m_displayDuration;

    // 表示経過時間
    private float m_elapsedTime;

    // 表示中か
    private bool m_isVisible;

    /// <summary>
    /// 着弾痕の表示が終了したときに通知します。
    /// </summary>
    public event Action Hidden;

    /// <summary>
    /// 初期化します。
    /// </summary>
    private void Awake()
    {
        m_decalProjector =
            GetComponent<DecalProjector>();

        Hide(
            false);
    }

    /// <summary>
    /// オイルの着弾痕を表示します。
    /// </summary>
    /// <param name="position">着弾位置。</param>
    /// <param name="displayDuration">表示時間。</param>
    /// <param name="groundOffset">地面からのオフセット。</param>
    public void Show(
        Vector3 position,
        float displayDuration,
        float groundOffset)
    {
        transform.position =
            position +
            Vector3.up * groundOffset;

        m_displayDuration =
            Mathf.Max(
                0.0f,
                displayDuration);

        m_elapsedTime =
            0.0f;

        m_isVisible =
            true;

        m_decalProjector.enabled =
            true;

        if (m_displayDuration <= 0.0f)
        {
            Hide();
        }
    }

    /// <summary>
    /// 着弾痕の表示時間を更新します。
    /// </summary>
    private void Update()
    {
        if (!m_isVisible)
        {
            return;
        }

        m_elapsedTime +=
            Time.deltaTime;

        if (m_elapsedTime <
            m_displayDuration)
        {
            return;
        }

        Hide();
    }

    /// <summary>
    /// 着弾痕を非表示にします。
    /// </summary>
    public void Hide()
    {
        Hide(
            true);
    }

    /// <summary>
    /// 着弾痕を非表示にします。
    /// </summary>
    /// <param name="notify">終了通知を行うか。</param>
    private void Hide(
        bool notify)
    {
        bool wasVisible =
            m_isVisible;

        m_isVisible =
            false;

        if (m_decalProjector != null)
        {
            m_decalProjector.enabled =
                false;
        }

        if (notify &&
            wasVisible)
        {
            Hidden?.Invoke();
        }
    }
}