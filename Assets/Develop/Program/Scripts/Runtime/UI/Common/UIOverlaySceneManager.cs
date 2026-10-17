using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// UI用のAdditiveシーンを表示・解放します。
/// </summary>
[DisallowMultipleComponent]
public sealed class UIOverlaySceneManager : MonoBehaviour
{
    [SerializeField, Header("開くシーン名")]
    private string m_defaultSceneName = "ManualScene";

    [SerializeField, Header("背面UIの入力制御コンポーネント")]
    private MonoBehaviour m_inputTarget;

    // 背面UIの入力制御を行います。
    private IUIInputControllable m_inputController;

    // 現在表示しているAdditiveシーンです。
    private Scene m_loadedScene;

    // 読み込んだシーンのルートコンポーネントです。
    private UIOverlaySceneRoot m_overlayRoot;

    // ロード・表示・非表示・解放中かどうかです。
    private bool m_isTransitioning = false;

    private void Awake()
    {
        m_inputController = m_inputTarget as IUIInputControllable;
        if (m_inputController == null)
        {
            Debug.LogError(
                "入力制御対象にはIUIInputControllableを実装したコンポーネントを指定してください。",
                this);
        }
    }

    /// <summary>
    /// Inspectorで設定したシーンを開きます。
    /// </summary>
    public void OpenOverlayScene()
    {
        OpenOverlayScene(m_defaultSceneName);
    }

    /// <summary>
    /// 指定したシーンをAdditiveで開きます。
    /// </summary>
    /// <param name="sceneName">シーン名（重複しない名前）。</param>
    public void OpenOverlayScene(string sceneName)
    {
        if (m_isTransitioning || m_overlayRoot != null || m_loadedScene.IsValid())
        {
            return;
        }

        if (m_inputTarget == null || m_inputController == null)
        {
            Debug.LogError("背面UIの入力制御対象が正しく設定されていません。", this);
            return;
        }

        if (string.IsNullOrWhiteSpace(sceneName) ||
            !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"読み込めないシーンです: {sceneName}", this);
            return;
        }

        if (SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            Debug.LogWarning($"シーンは既に読み込まれています: {sceneName}", this);
            return;
        }

        m_isTransitioning = true;
        m_inputController.SetInputEnabled(false);
        StartCoroutine(LoadOverlayRoutine(sceneName));
    }

    /// <summary>
    /// 現在のAdditiveシーンを閉じます。
    /// </summary>
    public void CloseOverlayScene()
    {
        if (m_isTransitioning || m_overlayRoot == null || !m_loadedScene.IsValid())
        {
            return;
        }

        m_isTransitioning = true;
        m_overlayRoot.SetInputEnabled(false);
        m_overlayRoot.Hide(() => StartCoroutine(UnloadOverlayRoutine()));
    }

    /// <summary>
    /// シーンを読み込み、表示処理を開始します。
    /// </summary>
    private IEnumerator LoadOverlayRoutine(string sceneName)
    {
        AsyncOperation loadOperation;
        try
        {
            loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            loadOperation = null;
        }

        if (loadOperation == null)
        {
            Debug.LogError("Additiveシーンの読み込みを開始できませんでした。", this);
            RestoreInput();
            yield break;
        }

        yield return loadOperation;

        m_loadedScene = SceneManager.GetSceneByName(sceneName);
        if (!m_loadedScene.IsValid() || !m_loadedScene.isLoaded)
        {
            Debug.LogError($"読み込んだシーンを取得できません: {sceneName}", this);
            m_loadedScene = default;
            RestoreInput();
            yield break;
        }

        m_overlayRoot = FindOverlayRoot(m_loadedScene);
        if (m_overlayRoot == null || !m_overlayRoot.HasWindow)
        {
            Debug.LogError("UIOverlaySceneRootとUIWindowをシーンに設定してください。", this);
            m_overlayRoot = null;
            yield return UnloadOverlayRoutine();
            yield break;
        }

        // 閉じるボタンから呼び出せるように管理側の処理を渡します。
        m_overlayRoot.Initialize(CloseOverlayScene);
        m_overlayRoot.Show(() =>
        {
            if (m_overlayRoot != null)
            {
                m_overlayRoot.SetInputEnabled(true);
            }

            m_isTransitioning = false;
        });
    }

    /// <summary>
    /// Additiveシーンを解放して背面UIの入力を復帰させます。
    /// </summary>
    private IEnumerator UnloadOverlayRoutine()
    {
        if (m_loadedScene.IsValid() && m_loadedScene.isLoaded)
        {
            AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(m_loadedScene);
            if (unloadOperation != null)
            {
                yield return unloadOperation;
            }
        }

        m_overlayRoot = null;
        m_loadedScene = default;
        RestoreInput();
    }

    /// <summary>
    /// 対象シーンからUIOverlaySceneRootを探します。
    /// </summary>
    /// <param name="scene">対象シーン。</param>
    /// <returns>見つかったルート。存在しない場合はnull。</returns>
    private UIOverlaySceneRoot FindOverlayRoot(Scene scene)
    {
        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            UIOverlaySceneRoot overlayRoot =
                rootObject.GetComponentInChildren<UIOverlaySceneRoot>(true);

            if (overlayRoot != null)
            {
                return overlayRoot;
            }
        }

        return null;
    }

    /// <summary>
    /// 背面UIの入力を復帰して切り替えを終了します。
    /// </summary>
    private void RestoreInput()
    {
        if (m_inputTarget != null && m_inputController != null)
        {
            m_inputController.SetInputEnabled(true);
        }

        m_isTransitioning = false;
    }
}
