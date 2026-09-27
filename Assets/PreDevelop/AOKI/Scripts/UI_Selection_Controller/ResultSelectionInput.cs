using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using DG.Tweening;

public class ResultSelectionInput : MonoBehaviour
{
    [Header("Next Stage (画像切り替え & スライド移動)")]
    [SerializeField] private RectTransform nextStageVisual;
    [SerializeField] private Image nextStageImage;
    [SerializeField] private Sprite nextStageUnselectedSprite; 
    [SerializeField] private Sprite nextStageSelectedSprite; 
    [Tooltip("非選択時にどれくらい横へずらしておくか")]
    [SerializeField] private float slideOffset = -50f;

    [Header("Stage Select (枠フェード & 拡縮アニメーション)")]
    [SerializeField] private RectTransform stageSelectVisual;
    [SerializeField] private Image stageSelectFrameImage;    
    [Tooltip("非選択時の縮小サイズ割合")]
    [SerializeField] private float unselectedScale = 0.8f;

    [Header("アニメーション設定")]
    [SerializeField] private float duration = 0.25f;

    [Header("ループ設定")]
    [SerializeField] private float loopDuration = 0.8f;
    [Tooltip("オレンジが選択中に横にスライドし続ける幅")]
    [SerializeField] private float loopSlideAmount = -10f;
    [Tooltip("白枠が選択中に拡大し続ける割合")]
    [SerializeField] private float loopScaleAmount = 1.05f;

    private bool isNextStageSelected = true;

    private float nsOriginalPosX;
    private Vector3 ssOriginalScale;

    void Start()
    {
        if (nextStageVisual != null) nsOriginalPosX = nextStageVisual.anchoredPosition.x;
        if (stageSelectVisual != null) ssOriginalScale = stageSelectVisual.localScale;

        UpdateVisuals(true);
    }

    public void OnNavigate(InputValue value)
    {
        Vector2 input = value.Get<Vector2>();

        if (input.y > 0.5f && !isNextStageSelected)
        {
            SelectNextStage();
        }
        else if (input.y < -0.5f && isNextStageSelected)
        {
            SelectStageSelect();
        }
    }

    public void OnSubmit(InputValue value)
    {
        if (value.isPressed)
        {
            if (isNextStageSelected) Debug.Log("Next Stage 決定！");
            else Debug.Log("Stage Select 決定！");
        }
    }

    public void SelectNextStage()
    {
        if (isNextStageSelected) return;
        isNextStageSelected = true;
        UpdateVisuals();
    }

    public void SelectStageSelect()
    {
        if (!isNextStageSelected) return;
        isNextStageSelected = false;
        UpdateVisuals();
    }

    private void UpdateVisuals(bool isInstant = false)
    {
        float t = isInstant ? 0f : duration;

        // Next Stage (選択中：2本線 / 非選択：1本線)
        if (nextStageImage != null)
        {
            nextStageImage.sprite = isNextStageSelected ? nextStageSelectedSprite : nextStageUnselectedSprite;
        }

        if (nextStageVisual != null)
        {
            nextStageVisual.DOKill();

            if (isNextStageSelected)
            {
                nextStageVisual.DOAnchorPosX(nsOriginalPosX, t).SetEase(Ease.OutCubic)
                    .OnComplete(() =>
                    {
                        if (isInstant) return;
                        nextStageVisual.DOAnchorPosX(nsOriginalPosX + loopSlideAmount, loopDuration)
                            .SetEase(Ease.InOutSine)
                            .SetLoops(-1, LoopType.Yoyo);
                    });
            }
            else
            {
                nextStageVisual.DOAnchorPosX(nsOriginalPosX + slideOffset, t).SetEase(Ease.OutCubic);
            }
        }

        // Stage Select (選択中：枠表示 / 非選択：枠消去)
        if (stageSelectFrameImage != null)
        {
            stageSelectFrameImage.DOKill();
            stageSelectFrameImage.DOFade(isNextStageSelected ? 0f : 1f, t);
        }

        if (stageSelectVisual != null)
        {
            stageSelectVisual.DOKill();

            if (!isNextStageSelected)
            {
                stageSelectVisual.DOScale(ssOriginalScale, t).SetEase(Ease.OutBack)
                    .OnComplete(() =>
                    {
                        if (isInstant) return;
                        stageSelectVisual.DOScale(ssOriginalScale * loopScaleAmount, loopDuration)
                            .SetEase(Ease.InOutSine)
                            .SetLoops(-1, LoopType.Yoyo);
                    });
            }
            else
            {
                stageSelectVisual.DOScale(ssOriginalScale * unselectedScale, t).SetEase(Ease.OutCubic);
            }
        }
    }
}