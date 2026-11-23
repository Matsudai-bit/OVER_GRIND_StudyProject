using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// デバッグ用のゲームリセットコマンドを管理します。
/// Editorおよび実行ビルドのどちらでも使用できます。
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
    /// ゲーム起動時にデバッグControllerを自動生成します。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateRuntimeController()
    {
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
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded +=
            OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -=
            OnSceneLoaded;
    }

    private void Update()
    {
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

        if (!IsResetCommandPressed(
            keyboard))
        {
            return;
        }

        ResetGame();
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
            keyboard.f12Key.wasPressedThisFrame;

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

        m_isResetting =
            true;

        Debug.Log(
            "[Debug] ゲームデータを初期化します。",
            this);

        // Pause状態を遷移先へ持ち越さない
        Time.timeScale =
            1.0f;

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

            /*
             * 現在SceneにSaveDataControllerが存在する場合は、
             * メモリ上のデータも初期状態へ戻します。
             *
             * この直後に保存ファイル自体を削除するため、
             * ここでは即時保存しません。
             */
            if (!saveDataController.ResetSaveData(
                false))
            {
                Debug.LogWarning(
                    "[Debug] ランタイムセーブデータの"
                    + "初期化に失敗しました。",
                    this);
            }
        }

        // 永続化されているJSONを削除
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

        foreach (
            StageSceneContext context
            in contexts)
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

            m_isResetting =
                false;

            return;
        }

        SceneManager.LoadScene(
            SPLASH_SCENE_NAME);
    }

    /// <summary>
    /// Sceneロード完了時にリセット処理中状態を解除します。
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
        m_isResetting =
            false;
    }
}
