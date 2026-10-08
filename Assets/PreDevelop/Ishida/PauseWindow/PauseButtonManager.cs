using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class PauseButtonManager : MonoBehaviour
{
    [SerializeField] private float m_hoverDistance = 3f; // 左右に動く幅
    [SerializeField] private float m_hoverDuration = 0.5f; // 往復のスピード

    private bool m_isAnimationInitialized = false;

    public void MoveCursor(
        UnityEngine.UI.Image  cursor,             // カーソル
        Vector3     targetPosition)     // 目標座標
    {
        RectTransform cursorRect = cursor.rectTransform;
        
        // 1. 初回のみ「左右のゆらゆらループ」を開始する
        if (!m_isAnimationInitialized)
        {
            StartHoverAnimation(cursorRect);
            m_isAnimationInitialized = true;
        }

        // 2. X軸は現在のゆらゆら位置を維持し、Y軸（高さ）だけ瞬時に目標位置に切替
        Vector3 newPosition = cursorRect.position;
        //newPosition.x = cursorRect.position.x - cursorRect.rect.width;
        newPosition.y = targetPosition.y;
        
        cursorRect.position = newPosition;
        
    }

    /// <summary>
    /// 左右に小刻みに揺れるアニメーションの開始
    /// </summary>
    private void StartHoverAnimation(RectTransform cursorRect)
    {
        // 既存のTweenがあればKill
        cursorRect.DOKill();

        // 左右に小さく往復（ゆらゆら）する無限ループ
        cursorRect.DOAnchorPosX(m_hoverDistance, m_hoverDuration)
                  .SetRelative(true)           // 現在位置からの相対移動
                  .SetEase(Ease.InOutSine)     // 滑らかな往復
                  .SetLoops(-1, LoopType.Yoyo) // 無限ループ
                  .SetLink(cursorRect.gameObject);
    }
}
