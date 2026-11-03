using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class CursorSwayAnimator : MonoBehaviour
{
    // 左右に動く幅
    [SerializeField]
    private float m_hoverDistance = 3.0f;
    // 往復のスピード
    [SerializeField] 
    private float m_hoverDuration = 0.5f;

    private Tween m_cursorTween;

    public void MoveCursor(UnityEngine.UI.Image cursor, Vector3 targetPosition)
    {
        if (cursor == null)
        {
            return;
        }

        // 現在のRectTransformを取得
        RectTransform cursorRect = cursor.rectTransform;

        // Tween が無い場合
        if (m_cursorTween == null || !m_cursorTween.IsActive())
        {
            // 新規作成を行う
            StartHoverAnimation(cursorRect);
        }
        // Tweenがあって停止中の場合
        else if (!m_cursorTween.IsPlaying())
        {
            // 停止した位置から再開
            m_cursorTween.Play();
        }

        // Y座標を目標へ
        Vector3 newPosition = cursorRect.position;
        newPosition.y = targetPosition.y;
        cursorRect.position = newPosition;
    }

    private void StartHoverAnimation(RectTransform cursorRect)
    {
        cursorRect.DOKill();

        m_cursorTween = cursorRect.DOAnchorPosX(m_hoverDistance, m_hoverDuration)
                                  .SetRelative(true)
                                  .SetEase(Ease.InOutSine)
                                  .SetLoops(-1, LoopType.Yoyo)
                                  .SetLink(cursorRect.gameObject);
    }

    // 一時停止
    public void Stop(UnityEngine.UI.Image cursor)
    {
        if (m_cursorTween != null && m_cursorTween.IsActive())
        {
            m_cursorTween.Pause();
        }
    }
}
