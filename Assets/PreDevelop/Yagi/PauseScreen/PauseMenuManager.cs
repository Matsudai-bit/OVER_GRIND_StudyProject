using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ポーズ画面を管理します。
/// </summary>
public class PauseMenuManager : MonoBehaviour
{
    /// <summary>
    /// タイトルシーン名
    /// </summary>
    private const string TITLE_SCENE_NAME = "TitleScene";

    [Header("Buttons")]
    [SerializeField]
    private Button m_resumeButton;

    [SerializeField]
    private Button m_tutorialButton;

    [SerializeField]
    private Button m_titleButton;

    [Header("Menu")]
    [SerializeField]
    private GameObject m_menuRoot;

    [Header("Tutorial")]
    [SerializeField]
    private GameObject m_tutorialPrefab;

    [Header("Menu Selector")]
    [SerializeField]
    private MenuItemSelector m_menuItemSelector;

    /// <summary>
    /// 初期設定
    /// </summary>
    private IEnumerator Start()
    {
        // UI初期化待ち
        yield return null;

        // ボタンイベント登録
        m_resumeButton.onClick.AddListener(OnResume);
        m_tutorialButton.onClick.AddListener(OnTutorial);
        m_titleButton.onClick.AddListener(OnBackTitle);

        // 操作説明は閉じておく
        if (m_tutorialPrefab != null)
        {
            m_tutorialPrefab.SetActive(false);
        }
    }

    /// <summary>
    /// ゲームへ戻ります。
    /// </summary>
    private void OnResume()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 操作説明を表示します。
    /// </summary>
    private void OnTutorial()
    {
        if (m_menuItemSelector != null)
        {
            m_menuItemSelector.EnableInput(false);
        }

        m_menuRoot.SetActive(false);

        m_tutorialPrefab.SetActive(true);

        // 最前面へ表示
        m_tutorialPrefab.transform.SetAsLastSibling();

        TutorialManager tutorialManager = m_tutorialPrefab.GetComponent<TutorialManager>();

        tutorialManager.OnTutorialClosed = OnTutorialClosed;
        tutorialManager.ResetTutorial();

        if (tutorialManager != null)
        {
            tutorialManager.ResetTutorial();
        }
    }

    /// <summary>
    /// 操作説明を閉じます。
    /// </summary>
    public void OnTutorialClosed()
    {
        m_tutorialPrefab.SetActive(false);

        m_menuRoot.SetActive(true);

        if (m_menuItemSelector != null)
        {
            m_menuItemSelector.EnableInput(true);
            m_menuItemSelector.Select(m_tutorialButton);
        }
    }

    /// <summary>
    /// タイトルへ戻ります。
    /// </summary>
    private void OnBackTitle()
    {
        SceneManager.LoadScene(TITLE_SCENE_NAME);
    }
}