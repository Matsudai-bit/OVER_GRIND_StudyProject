using System;
using UnityEngine;

/// <summary>
/// カテゴリー1つ分のミキサー設定（音量・ミュート）です。
/// </summary>
[Serializable]
public sealed class SoundCategoryMix
{
    // 対象のカテゴリー
    [SerializeField] private SoundCategory m_category;

    // カテゴリー音量（0～1）
    [SerializeField, Range(0.0f, 1.0f)] private float m_volume = 1.0f;

    // ミュートしているか
    [SerializeField] private bool m_isMuted = false;

    /// <summary>
    /// 対象のカテゴリーを取得します。
    /// </summary>
    public SoundCategory Category => m_category;

    /// <summary>
    /// カテゴリー音量（0～1）を取得または設定します。
    /// </summary>
    public float Volume
    {
        get => m_volume;
        set => m_volume = Mathf.Clamp01(value);
    }

    /// <summary>
    /// ミュートしているかを取得または設定します。
    /// </summary>
    public bool IsMuted
    {
        get => m_isMuted;
        set => m_isMuted = value;
    }

    /// <summary>
    /// ミュートを考慮した実効音量（0～1）を取得します。
    /// </summary>
    public float Gain => m_isMuted ? 0.0f : m_volume;

    /// <summary>
    /// カテゴリー設定を生成します。
    /// </summary>
    /// <param name="category">対象のカテゴリー。</param>
    public SoundCategoryMix(SoundCategory category)
    {
        m_category = category;
    }
}