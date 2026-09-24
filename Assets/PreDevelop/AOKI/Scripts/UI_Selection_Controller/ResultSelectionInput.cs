using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class ResultUIController : MonoBehaviour
{
    [Header("Next Stage ")]
    [SerializeField] private RectTransform nextStageVisual;
    [Tooltip("非選択時にどれくらい横へずらしておくか")]
    [SerializeField] private float slideOffset = -50f;

    [Header("Stage Select ")]
    [SerializeField] private RectTransform stageSelectVisual;
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

    // 元の座標とサイズを記憶する用
    private float nsOriginalPosX;
    private Vector3 ssOriginalScale;

    void Start()
    {
        // 最初の位置・サイズを記憶
        if (nextStageVisual != null) nsOriginalPosX = nextStageVisual.anchoredPosition.x;
        if (stageSelectVisual != null) ssOriginalScale = stageSelectVisual.localScale;

        // 起動時はアニメーションなしで即座に初期状態を反映
        UpdateVisuals(true);
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // キーボード操作
        if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
        {
            if (isNextStageSelected) SelectStageSelect();
        }
        else if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame)
        {
            if (!isNextStageSelected) SelectNextStage();
        }

        // 決定キー
        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            if (isNextStageSelected) Debug.Log("Next Stage 決定！");
            else Debug.Log("Stage Select 決定！");
        }
    }

    // マウスホバーなどから呼ぶ用
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

    /// <summary>
    /// 消去処理を行わず、常時表示したまま状態を切り替える
    /// </summary>
    private void UpdateVisuals(bool isInstant = false)
    {
        float t = isInstant ? 0f : duration;

        
        // オレンジの奴
        
        if (nextStageVisual != null)
        {
            nextStageVisual.DOKill();

            if (isNextStageSelected)
            {
                // 選択時: 定位置へスライドして戻り、完了後にループ開始
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
                // 非選択時: 消さずに非選択位置へ移動して維持
                nextStageVisual.DOAnchorPosX(nsOriginalPosX + slideOffset, t).SetEase(Ease.OutCubic);
            }
        }

        
        // 白の奴
        if (stageSelectVisual != null)
        {
            stageSelectVisual.DOKill();

            if (!isNextStageSelected)
            {
                // 選択時: 元のサイズに拡大し、完了後にループ開始
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
                // 非選択時: 消さずに非選択サイズへ変更して維持
                stageSelectVisual.DOScale(ssOriginalScale * unselectedScale, t).SetEase(Ease.OutCubic);
            }
        }
    }
}