using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class FadeManager : MonoBehaviour
{
    private CanvasGroup m_canvasGroup;

    private void Awake()
    {
        m_canvasGroup = GetComponent<CanvasGroup>();
    }

    public IEnumerator FadeOut(float duration)
    {

        float time = 0;
        while (time < duration)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame)
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
            if (Keyboard.current.enterKey.wasPressedThisFrame)
            {
          
                break;
            }
            time += Time.deltaTime;
            m_canvasGroup.alpha = Mathf.Lerp(1, 0, time / duration);
            yield return null;
        }
        m_canvasGroup.alpha = 0;
    }

    public void SkipLogo()
    {
        m_canvasGroup.alpha = 1;
    }
}
