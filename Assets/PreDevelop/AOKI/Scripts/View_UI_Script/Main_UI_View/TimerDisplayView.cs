using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// タイマーのUI表示を管理するクラスです。
/// </summary>
public class TimerDisplayView : MonoBehaviour
{
    /// <summary>数字スプライト素材 (0～9)</summary>
    [Header("数字スプライト素材 (0～9)")]
    [SerializeField] private Sprite[] m_numberSprites = new Sprite[10];

    /// <summary>分の10の位を表示する画像</summary>
    [Header("タイマー表示用Image")]
    [SerializeField] private Image m_min10Image;

    /// <summary>分の1の位を表示する画像</summary>
    [SerializeField] private Image m_min1Image;

    /// <summary>秒の10の位を表示する画像</summary>
    [SerializeField] private Image m_sec10Image;

    /// <summary>秒の1の位を表示する画像</summary>
    [SerializeField] private Image m_sec1Image;

    /// <summary>ミリ秒の10の位を表示する画像</summary>
    [SerializeField] private Image m_ms10Image;

    /// <summary>ミリ秒の1の位を表示する画像</summary>
    [SerializeField] private Image m_ms1Image;

    /// <summary>
    /// 分・秒・ミリ秒をもとにタイマー表示を更新します。
    /// </summary>
    /// <param name="min">分。</param>
    /// <param name="sec">秒。</param>
    /// <param name="ms">ミリ秒。</param>
    public void SetTime(int min, int sec, int ms)
    {
        if (m_numberSprites == null || m_numberSprites.Length < 10) return;

        // 各桁のスプライトを更新する
        if (m_min10Image != null) m_min10Image.sprite = m_numberSprites[(min / 10) % 10];
        if (m_min1Image != null) m_min1Image.sprite = m_numberSprites[min % 10];
        if (m_sec10Image != null) m_sec10Image.sprite = m_numberSprites[(sec / 10) % 10];
        if (m_sec1Image != null) m_sec1Image.sprite = m_numberSprites[sec % 10];
        if (m_ms10Image != null) m_ms10Image.sprite = m_numberSprites[(ms / 10) % 10];
        if (m_ms1Image != null) m_ms1Image.sprite = m_numberSprites[ms % 10];
    }
}