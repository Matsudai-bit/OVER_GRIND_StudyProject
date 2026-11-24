using UnityEngine;

/// <summary>
/// Pause中にTutorial用Canvasの内容を隠し、解除時に元の表示状態へ戻します。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class TutorialCanvasPauseVisibility : MonoBehaviour
{
    [SerializeField, Header("Pause状態変更イベント")]
    private PauseChangedEvent m_pauseChangedEvent;

    private CanvasGroup m_canvasGroup;
    private float m_previousAlpha;
    private bool m_previousInteractable;
    private bool m_previousBlocksRaycasts;
    private bool m_isHiddenByPause;

    /// <summary>CanvasGroupを取得し、Pause通知を購読します。</summary>
    private void OnEnable()
    {
        m_canvasGroup = GetComponent<CanvasGroup>();
        if (m_pauseChangedEvent == null)
        {
            Debug.LogWarning($"{nameof(PauseChangedEvent)}が設定されていません。", this);
            return;
        }

        m_pauseChangedEvent.RegisterListener(OnPauseChanged);
    }

    /// <summary>Pause通知の購読を解除し、保存した表示状態へ戻します。</summary>
    private void OnDisable()
    {
        if (m_pauseChangedEvent != null)
        {
            m_pauseChangedEvent.UnregisterListener(OnPauseChanged);
        }

        RestoreCanvas();
    }

    /// <summary>Pause状態に応じてCanvasの表示を切り替えます。</summary>
    /// <param name="eventData">Pause状態の変更情報。</param>
    private void OnPauseChanged(PauseChangedEventData eventData)
    {
        if (eventData.IsPaused)
        {
            HideCanvas();
            return;
        }

        RestoreCanvas();
    }

    /// <summary>現在の状態を保存してCanvasの内容を非表示にします。</summary>
    private void HideCanvas()
    {
        if (m_isHiddenByPause || m_canvasGroup == null)
        {
            return;
        }

        m_previousAlpha = m_canvasGroup.alpha;
        m_previousInteractable = m_canvasGroup.interactable;
        m_previousBlocksRaycasts = m_canvasGroup.blocksRaycasts;
        m_isHiddenByPause = true;

        m_canvasGroup.alpha = 0f;
        m_canvasGroup.interactable = false;
        m_canvasGroup.blocksRaycasts = false;
    }

    /// <summary>Pause前に保存したCanvasの表示状態へ戻します。</summary>
    private void RestoreCanvas()
    {
        if (!m_isHiddenByPause || m_canvasGroup == null)
        {
            return;
        }

        m_canvasGroup.alpha = m_previousAlpha;
        m_canvasGroup.interactable = m_previousInteractable;
        m_canvasGroup.blocksRaycasts = m_previousBlocksRaycasts;
        m_isHiddenByPause = false;
    }
}
