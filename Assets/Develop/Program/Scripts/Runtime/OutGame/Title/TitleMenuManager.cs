using System.Collections;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// タイトル画面の管理クラス
/// ・ゲーム開始
/// ・操作説明の表示/非表示
/// ・ゲーム終了
/// を管理する。
/// </summary>
public class TitleMenuManager : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button operationCheckButton;
    [SerializeField] private Button configButton;
    [SerializeField] private Button quitButton;

    [Header("Menu")]
    // タイトルメニュー全体（ボタンをまとめた親オブジェクト）
    [SerializeField] private GameObject menuRoot;

    [Header("Config")]
    [SerializeField] private GameObject configPrefab;

    [Header("Tutorial")]
    // 操作説明ウィンドウ
    [SerializeField] private GameObject tutorialPrefab;

    /// <summary>
    /// 初期設定
    /// </summary>
    private IEnumerator Start()
    {
        // UIが初期化されるまで1フレーム待機
        yield return null;

        // マウスカーソルを非表示にする
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // 各ボタンに処理を登録
        playButton.onClick.AddListener(OnStartGame);
        operationCheckButton.onClick.AddListener(OnTutorial);
        quitButton.onClick.AddListener(OnExit);

        configButton.onClick.AddListener(OnConfig);

        configPrefab.SetActive(false);

        // 操作説明は開始時は非表示
        tutorialPrefab.SetActive(false);
    }

    /// <summary>
    /// ゲーム開始
    /// </summary>
    private void OnStartGame()
    {
        Debug.Log("ゲーム開始");

        // TODO : ゲームシーンへ遷移
        SceneManager.LoadScene("Prot_Stage1");
    }

    /// <summary>
    /// 操作説明を表示
    /// </summary>
    /// <summary>
    /// 操作説明を表示
    /// </summary>
    private void OnTutorial()
    {
        // タイトルメニューを非表示
        menuRoot.SetActive(false);

        // 操作説明を表示
        tutorialPrefab.SetActive(true);

        // 最前面に表示
        tutorialPrefab.transform.SetAsLastSibling();

        // TutorialManagerを取得
        TutorialManager tutorialManager = tutorialPrefab.GetComponent<TutorialManager>();

        // 操作説明を閉じた時のコールバックを登録
        tutorialManager.OnTutorialClosed = OnTutorialClosed;

        // 毎回1ページ目から開始
        tutorialManager.ResetTutorial();
    }


    /// <summary>
    /// 操作説明終了時に呼ばれる
    /// </summary>
    public void OnTutorialClosed()
    {
        // 操作説明を閉じる
        tutorialPrefab.SetActive(false);

        // タイトルメニューを再表示
        menuRoot.SetActive(true);
    }

    /// <summary>
    /// コンフィグを表示します。
    /// </summary>
    private void OnConfig()
    {
        Debug.Log("Config Open");

        menuRoot.SetActive(false);

        ConfigWindowMenuManager configMenu = configPrefab.GetComponent<ConfigWindowMenuManager>();

        configMenu.OnClosed = OnConfigClosed;

        configMenu.Open();
    }

    /// <summary>
    /// コンフィグを閉じます。
    /// </summary>
    public void OnConfigClosed()
    {
        configPrefab.SetActive(false);

        menuRoot.SetActive(true);
    }

    /// <summary>
    /// ゲーム終了
    /// </summary>
    private void OnExit()
    {
#if UNITY_EDITOR
        // Unityエディタ実行時は再生停止
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // ビルド版ではアプリケーション終了
        Application.Quit();
#endif
    }
}