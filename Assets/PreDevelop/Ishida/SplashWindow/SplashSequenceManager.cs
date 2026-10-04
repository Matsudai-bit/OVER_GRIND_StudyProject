using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // New Input System

public class SplashSequenceManager : MonoBehaviour
{

    [System.Serializable]
    public struct LogoData
    {
        public Sprite m_sprite;      // ロゴ画像
        public float m_displayTime;  // 完全表示状態を維持する時間（秒）
    }

    [Header("コンポーネント設定")]
    [SerializeField] private FadeController m_fadeController; // 最初のFadeManagerを指定
    [SerializeField] private Image m_logoImage;         // ロゴ表示用のImage

    [Header("フェード設定")]
    [SerializeField] private float m_fadeInDuration = 0.0f;  // フェードインにかかる時間
    [SerializeField] private float m_fadeOutDuration = 0.0f; // フェードアウトにかかる時間

    [Header("ロゴと表示順")]
    [SerializeField] private LogoData[] m_logoSequence;

    [Header("遷移先のタイトルシーン")]
    [SerializeField] private string m_titleSceneName = "TitleScene";

    private bool m_isSkipped = false;

    private void Start()
    {
        StartCoroutine(PlayLogoSequence());
    }

    private void OnSubmit(InputValue value)
    {
        if(value.isPressed)
        {
            Debug.Log("呼ばれた");  
            m_isSkipped=true;
            if (m_fadeController != null)
            {
                m_fadeController.IsSkipped = true; // プロパティ経由で渡す
            }
        }
    }


    private IEnumerator PlayLogoSequence()
    {
        foreach (var logo in m_logoSequence)
        {
            // 1. 画像をセット
            
            m_logoImage.sprite = logo.m_sprite;
            Debug.Log("1");
            // 2. フェードイン（画面を明るくする）完了まで待機
            yield return m_fadeController.FadeIn(m_fadeInDuration);
            ResetSkipFlag();
            Debug.Log("2");
            // 3. 指定時間だけロゴを表示維持
            yield return WaitDisplayTime(logo.m_displayTime);
            ResetSkipFlag();
            Debug.Log("3");
            // 4. フェードアウト（画面を暗くする）完了まで待機
            yield return m_fadeController.FadeOut(m_fadeInDuration);
            ResetSkipFlag();
            Debug.Log("4");
            // 3. 指定時間だけロゴを表示維持
            yield return new WaitForSeconds(1.0f);
        }

        // すべてのロゴ表示が終わったらタイトルへ遷移
        SceneManager.LoadScene(m_titleSceneName);
    }

    private IEnumerable WaitDisplayTime(float displayTime)
    {
        float timer = 0.0f;
        while(timer < displayTime && !m_isSkipped)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        
    }

    private void ResetSkipFlag()
    {
        m_isSkipped = false;
        if (m_fadeController != null)
        {
            m_fadeController.IsSkipped = false;
        }
    }
}
