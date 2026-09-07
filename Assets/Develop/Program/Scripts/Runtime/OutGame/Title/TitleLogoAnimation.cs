using DG.Tweening;
using UnityEngine;

public class TitleLogoAnimation : MonoBehaviour
{
    [SerializeField] private float moveDistance = 20f;
    [SerializeField] private float duration = 2f;
    [SerializeField] private Ease ease = Ease.InOutSine;

    private RectTransform rect;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    private void Start()
    {
        rect.DOAnchorPosY(rect.anchoredPosition.y + moveDistance, duration)
            .SetEase(ease)
            .SetLoops(-1, LoopType.Yoyo);
    }
}