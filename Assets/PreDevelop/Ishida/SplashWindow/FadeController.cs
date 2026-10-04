using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class FadeController : MonoBehaviour
{
    private CanvasGroup m_canvasGroup;
    private bool m_isSkipped = false;

    private void Awake()
    {
        m_canvasGroup = GetComponent<CanvasGroup>();
    }

    // 外部からスキップフラグを設定・初期化するためのプロパティ
    public bool IsSkipped
    {
        get => m_isSkipped;
        set => m_isSkipped = value;
    }
    public IEnumerator FadeOut(float duration)
    {

        float time = 0;
        while (time < duration)
        {
            if (m_isSkipped)
            {
                
                break;
            }
            time += Time.deltaTime;
               m_canvasGroup.alpha = Mathf.Lerp(0, 1, time / duration);
            yield return null;
        }
        m_canvasGroup.alpha = 1;
        m_canvasGroup.blocksRaycasts = true; // フェード完了後はクリック等を防止
    }

    /// <summary>
    /// フェードイン（画面が徐々に明るくなる）
    /// </summary>
    public IEnumerator FadeIn(float duration)
    {
        m_canvasGroup.blocksRaycasts = false; // 操作を可能にする
        float time = 0;
        while (time < duration)
        {
            if (m_isSkipped)
            {
          
                break;
            }
            time += Time.deltaTime;
            m_canvasGroup.alpha = Mathf.Lerp(1, 0, time / duration);
            yield return null;
        }
        m_canvasGroup.alpha = 0;
    }


}
