using UnityEngine;
using DG.Tweening;

public class UISelectionFrameController : MonoBehaviour
{
    [Header("操作する選択枠のRectTransform")]
    [SerializeField] private RectTransform selectionFrame;

    [Header("アニメーション設定")]
    [SerializeField] private float duration = 0.3f;        // 移動・拡縮にかかる時間
    [SerializeField] private Ease easeType = Ease.OutCubic; // 動きのカーブ（滑らかな減速）
    [SerializeField] private Vector2 padding = new Vector2(16f, 16f); // 枠の余白（幅, 高さ）

    private Tween moveTween;
    private Tween sizeTween;

    /// <summary>
    /// ターゲットのUI要素へ枠を移動＆リサイズさせる
    /// </summary>
    public void MoveTo(RectTransform target)
    {
        if (selectionFrame == null || target == null) return;

        // すでに実行中のアニメーションがあればキャンセルして上書きする
        if (moveTween != null && moveTween.IsActive()) moveTween.Kill();
        if (sizeTween != null && sizeTween.IsActive()) sizeTween.Kill();

        // 1. 位置の移動（WorldPositionを使うことで、Canvas内の階層が違っても正しく追従します）
        moveTween = selectionFrame.DOMove(target.position, duration)
            .SetEase(easeType);

        // 2. サイズの変更（ターゲットの本来のサイズに、指定した余白を足して拡縮します）
        Vector2 targetSize = target.rect.size + padding;
        sizeTween = selectionFrame.DOSizeDelta(targetSize, duration)
            .SetEase(easeType);
    }
}