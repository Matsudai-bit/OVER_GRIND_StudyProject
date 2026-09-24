using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class ResultUIController : MonoBehaviour
{
    [Header("Next Stage (オレンジ - 横スライド)")]
    [SerializeField] private RectTransform nextStageVisual;
    [Tooltip("非選択時にどれくらい横へずらしておくか（例: -50 で左からスライド）")]
    [SerializeField] private float slideOffset = -50f;

    [Header("Stage Select (白の枠 - 拡大縮小)")]
    [SerializeField] private RectTransform stageSelectVisual;
    [Tooltip("非選択時の縮小サイズ割合（例: 0.8）")]
    [SerializeField] private float unselectedScale = 0.8f;

    [Header("アニメーション設定")]
    [SerializeField] private float duration = 0.25f;

    private bool isNextStageSelected = true;

    // 元の座標とサイズを記憶する用
    private float nsOriginalPosX;
    private Vector3 ssOriginalScale;

    // フェード（透明度）用のCanvasGroup
    private CanvasGroup nsCG;
    private CanvasGroup ssCG;

    void Start()
    {
        // 最初の位置・サイズを記憶
        if (nextStageVisual != null) nsOriginalPosX = nextStageVisual.anchoredPosition.x;
        if (stageSelectVisual != null) ssOriginalScale = stageSelectVisual.localScale;

        nsCG = GetOrAddCanvasGroup(nextStageVisual?.gameObject);
        ssCG = GetOrAddCanvasGroup(stageSelectVisual?.gameObject);

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
    /// ご指示通りの「横スライド」と「拡大縮小」アニメーションを実行
    /// </summary>
    private void UpdateVisuals(bool isInstant = false)
    {
        float t = isInstant ? 0f : duration;

        // ==========================================
        // オレンジの奴（横スライド）
        // ==========================================
        if (nextStageVisual != null)
        {
            nextStageVisual.gameObject.SetActive(true);
            nextStageVisual.DOKill();
            nsCG?.DOKill();

            if (isNextStageSelected)
            {
                // 選択時: 横から元の位置へスライドしてくる
                nextStageVisual.DOAnchorPosX(nsOriginalPosX, t).SetEase(Ease.OutCubic);
                nsCG?.DOFade(1f, t);
            }
            else
            {
                // 非選択時: 横にスライドして消える
                nextStageVisual.DOAnchorPosX(nsOriginalPosX + slideOffset, t).SetEase(Ease.OutCubic);
                nsCG?.DOFade(0f, t).OnComplete(() => { if (!isInstant) nextStageVisual.gameObject.SetActive(false); });
            }
        }

        // ==========================================
        // 白の奴（拡大縮小）
        // ==========================================
        if (stageSelectVisual != null)
        {
            stageSelectVisual.gameObject.SetActive(true);
            stageSelectVisual.DOKill();
            ssCG?.DOKill();

            if (!isNextStageSelected)
            {
                // 選択時: フワッと元のサイズに拡大
                stageSelectVisual.DOScale(ssOriginalScale, t).SetEase(Ease.OutBack);
                ssCG?.DOFade(1f, t);
            }
            else
            {
                // 非選択時: シュッと縮小して消える
                stageSelectVisual.DOScale(ssOriginalScale * unselectedScale, t).SetEase(Ease.OutCubic);
                ssCG?.DOFade(0f, t).OnComplete(() => { if (!isInstant) stageSelectVisual.gameObject.SetActive(false); });
            }
        }
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject obj)
    {
        if (obj == null) return null;
        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg == null) cg = obj.AddComponent<CanvasGroup>();
        return cg;
    }
}