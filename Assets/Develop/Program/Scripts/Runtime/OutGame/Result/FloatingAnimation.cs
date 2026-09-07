using DG.Tweening;
using UnityEngine;

/// <summary>
/// UIを上下にアニメーションさせる汎用クラス
/// </summary>
public class FloatingAnimation : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField]
    private float m_moveDistance = 20.0f;

    [SerializeField]
    private float m_duration = 2.0f;

    [SerializeField]
    private Ease m_ease = Ease.InOutSine;

    private RectTransform m_rectTransform;

    private void Awake()
    {
        m_rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        m_rectTransform.DOAnchorPosY(
            m_rectTransform.anchoredPosition.y + m_moveDistance,
            m_duration)
            .SetEase(m_ease)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void OnDestroy()
    {
        DOTween.Kill(m_rectTransform);
    }
}