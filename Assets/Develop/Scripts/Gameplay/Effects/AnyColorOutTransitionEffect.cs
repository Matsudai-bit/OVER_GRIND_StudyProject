using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 色フェードシーン遷移エフェクト
/// </summary>
public class AnyColorOutTransitionEffect : SceneTransitionEffect
{
    [Header("色")]
    [SerializeField]
    private Color m_color;

    [Header("フェード時間")]
    [SerializeField]
    private float m_fadeDuration = 2.0f;

    [Header("白飛びイメージ")]
    [SerializeField]
    Image m_whiteImage;

    bool m_isTransitioning = false;

    /// <summary>
    /// トランジション中かどうか
    /// </summary>
    /// <returns>
    /// true    トランジション中
    /// false   トランジション中ではない
    /// </returns>
    public override bool IsTransitioning()
    {
        return m_isTransitioning;
    }

    /// <summary>
    /// トランジションの開始する
    /// </summary>
    /// <param name="onComplete">トランジション終了時に呼ばれるデリゲート</param>
    public override void StartTransition(Action onComplete)
    {
        m_isTransitioning = true;

        // 白飛びエフェクトの開始
        m_whiteImage.gameObject.SetActive(true);

        // 色の設定
        m_whiteImage.color = new Color(m_color.r, m_color.g, m_color.b, 0);

        // フェードアウトアニメーションの開始
        m_whiteImage.DOFade(1.1f, m_fadeDuration).SetEase(Ease.OutSine).OnComplete(() =>
        {
            // エフェクト完了時にコールバックを呼び出す
            onComplete?.Invoke();

            m_isTransitioning = false;

        });



    }
}
