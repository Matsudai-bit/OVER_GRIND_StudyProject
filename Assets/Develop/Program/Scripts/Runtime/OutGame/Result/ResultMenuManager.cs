using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// リザルト画面を管理します。
/// </summary>
public class ResultMenuManager : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField]
    private Button m_titleButton;

    [SerializeField]
    private Button m_exitButton;

    [Header("Menu")]
    [SerializeField]
    private GameObject m_menuRoot;

    [Header("Result")]
    [SerializeField]
    private TMP_Text m_clearTimeText;

    private IEnumerator Start()
    {
        // UI初期化待ち
        yield return null;

        // マウスカーソルを非表示
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // ボタンイベント登録
        m_titleButton.onClick.AddListener(OnBackTitle);
        m_exitButton.onClick.AddListener(OnExit);

        // クリア時間表示
        UpdateClearTime();
    }

    /// <summary>
    /// クリア時間を表示します。
    /// </summary>
    private void UpdateClearTime()
    {
        // TODO : 後でゲーム側から取得するように変更
        m_clearTimeText.text = $"{ResultData.ClearTime} sec";
    }

    /// <summary>
    /// タイトルへ戻ります。
    /// </summary>
    private void OnBackTitle()
    {
        SceneManager.LoadScene("TitleScene");
    }

    /// <summary>
    /// ゲームを終了します。
    /// </summary>
    private void OnExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}