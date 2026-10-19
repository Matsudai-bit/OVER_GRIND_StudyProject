using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// 宣伝画面の進行とシーン遷移を管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class PromotionScreenController : MonoBehaviour
{
    [SerializeField, Header("フェード制御")]
    private UIFadeController m_fadeController;

    [SerializeField, Header("Submit入力")]
    private InputActionReference m_submitActionReference;

    [SerializeField, Header("遷移先シーン名")]
    private string m_nextSceneName = "TitleScene";

    // 入力受付中か
    private bool m_isInputEnabled = false;

    // シーン遷移中か
    private bool m_isTransitioning = false;

    // 入力アクションを自分で有効化したか
    private bool m_enabledAction = false;

    private void OnEnable()
    {
        if (m_submitActionReference == null ||
            m_submitActionReference.action == null)
        {
            Debug.LogError("Submit入力が設定されていません。", this);
            return;
        }

        InputAction action = m_submitActionReference.action;

        action.performed += OnSubmitPerformed;

        if (!action.enabled)
        {
            action.Enable();
            m_enabledAction = true;
        }
    }

    private void Start()
    {
        if (m_fadeController == null)
        {
            Debug.LogError("UIFadeControllerが設定されていません。", this);
            return;
        }

        // 初期状態は完全透明
        m_fadeController.SetAlpha(0.0f);

        // 表示フェード開始
        m_fadeController.FadeIn(() =>
        {
            m_isInputEnabled = true;
        });
    }

    private void OnDisable()
    {
        if (m_submitActionReference == null ||
            m_submitActionReference.action == null)
        {
            return;
        }

        InputAction action = m_submitActionReference.action;

        action.performed -= OnSubmitPerformed;

        if (m_enabledAction)
        {
            action.Disable();
            m_enabledAction = false;
        }

        m_isInputEnabled = false;
    }

    /// <summary>
    /// Submit入力を受け取ります。
    /// </summary>
    private void OnSubmitPerformed(InputAction.CallbackContext context)
    {
        if (!m_isInputEnabled || m_isTransitioning)
        {
            return;
        }

        RequestClose();
    }

    /// <summary>
    /// 宣伝画面の終了処理を開始します。
    /// </summary>
    public void RequestClose()
    {
        if (!m_isInputEnabled || m_isTransitioning)
        {
            return;
        }

        m_isInputEnabled = false;
        m_isTransitioning = true;

        // フェードアウト完了後にシーン遷移
        m_fadeController.FadeOut(() =>
        {
            StartCoroutine(LoadNextSceneCoroutine());
        });
    }

    /// <summary>
    /// 次のシーンを読み込みます。
    /// </summary>
    private IEnumerator LoadNextSceneCoroutine()
    {
        if (string.IsNullOrEmpty(m_nextSceneName))
        {
            Debug.LogError("遷移先シーン名が設定されていません。", this);
            m_isTransitioning = false;
            yield break;
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(
            m_nextSceneName,
            LoadSceneMode.Single);

        if (operation == null)
        {
            Debug.LogError(
                $"シーンの読み込みに失敗しました: {m_nextSceneName}",
                this);

            m_isTransitioning = false;
            yield break;
        }

        yield return operation;
    }
}