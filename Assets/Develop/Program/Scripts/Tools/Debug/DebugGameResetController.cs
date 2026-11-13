using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// デバッグ用のゲームリセットコマンドを管理します。
/// </summary>
public sealed class DebugGameResetController : MonoBehaviour
{
    private const string OBJECT_NAME =
        "[DebugGameResetController]";

    private const string SPLASH_SCENE_NAME =
        "SplashScene";

    // リセット処理中か
    private bool m_isResetting;

    /// <summary>
    /// ゲーム起動時にデバッグControllerを生成します。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateRuntimeController()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

        DebugGameResetController existingController =
            FindFirstObjectByType<DebugGameResetController>();

        if (existingController != null)
        {
            return;
        }

        GameObject gameObject =
            new GameObject(OBJECT_NAME);

        gameObject.AddComponent<DebugGameResetController>();

        DontDestroyOnLoad(gameObject);

#endif
    }

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

        if (m_isResetting)
        {
            return;
        }

        Keyboard keyboard =
            Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (!IsResetCommandPressed(keyboard))
        {
            return;
        }

        ResetGame();

#endif
    }

    /// <summary>
    /// ゲームリセット用のキーが同時押しされたか確認します。
    /// </summary>
    /// <param name="keyboard">
    /// 現在使用しているKeyboard。
    /// </param>
    /// <returns>
    /// true：リセットコマンドが入力されました。
    /// false：入力されていません。
    /// </returns>
    private bool IsResetCommandPressed(
        Keyboard keyboard)
    {
        bool isControlPressed =
            keyboard.leftCtrlKey.isPressed
            || keyboard.rightCtrlKey.isPressed;

        bool isShiftPressed =
            keyboard.leftShiftKey.isPressed
            || keyboard.rightShiftKey.isPressed;

        bool isResetKeyPressed =
            keyboard.rKey.wasPressedThisFrame;

        return isControlPressed
            && isShiftPressed
            && isResetKeyPressed;
    }

    /// <summary>
    /// ゲームデータを初期化してSplashSceneへ戻します。
    /// </summary>
    private void ResetGame()
    {
        if (m_isResetting)
        {
            return;
        }

        m_isResetting = true;

        Debug.Log(
            "[Debug] ゲームデータを初期化します。",
            this);

        // Pause状態を持ち越さない
        Time.timeScale = 1.0f;

        ResetSaveData();

        ClearStageSceneContexts();

        LoadSplashScene();
    }

    /// <summary>
    /// 現在のセーブデータと保存ファイルを初期化します。
    /// </summary>
    private void ResetSaveData()
    {
        SaveDataController saveDataController =
            FindFirstObjectByType<SaveDataController>();

        string saveFileName =
            SaveSystem.DEFAULT_SAVE_FILE_NAME;

        if (saveDataController != null)
        {
            saveFileName =
                saveDataController.SaveFileName;

            // Runtime上のデータも初期値へ戻す
            if (!saveDataController.ResetSaveData(
                false))
            {
                Debug.LogWarning(
                    "[Debug] ランタイムセーブデータの"
                    + "初期化に失敗しました。",
                    this);
            }
        }

        // 保存済みJSONを削除
        if (!SaveSystem.TryDelete(
            saveFileName))
        {
            Debug.LogError(
                "[Debug] セーブファイルの削除に失敗しました。",
                this);
        }
    }

    /// <summary>
    /// 読み込まれているStageSceneContextを初期化します。
    /// </summary>
    private void ClearStageSceneContexts()
    {
        StageSceneContext[] contexts =
            Resources.FindObjectsOfTypeAll<StageSceneContext>();

        foreach (StageSceneContext context in contexts)
        {
            if (context == null)
            {
                continue;
            }

            context.Clear();
        }
    }

    /// <summary>
    /// SplashSceneへ遷移します。
    /// </summary>
    private void LoadSplashScene()
    {
        if (!Application.CanStreamedLevelBeLoaded(
            SPLASH_SCENE_NAME))
        {
            Debug.LogError(
                $"[Debug] Scene '{SPLASH_SCENE_NAME}' "
                + "がBuild Settingsに登録されていません。",
                this);

            m_isResetting = false;

            return;
        }

        SceneManager.LoadScene(
            SPLASH_SCENE_NAME);
    }
    private void OnEnable()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

        SceneManager.sceneLoaded +=
            OnSceneLoaded;

#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

        SceneManager.sceneLoaded -=
            OnSceneLoaded;

#endif
    }

    /// <summary>
    /// Sceneロード完了時の処理です。
    /// </summary>
    /// <param name="scene">
    /// ロードされたScene。
    /// </param>
    /// <param name="loadSceneMode">
    /// Sceneのロード方式。
    /// </param>
    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode loadSceneMode)
    {
        m_isResetting = false;
    }
}