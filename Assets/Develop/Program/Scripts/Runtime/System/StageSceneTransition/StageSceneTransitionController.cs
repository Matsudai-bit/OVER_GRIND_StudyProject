using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// StageSceneContextの設定とステージScene遷移を管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class StageSceneTransitionController : MonoBehaviour
{
    [SerializeField, Header("ローディング直前に呼ばれる")]
    private UnityEvent m_beforeLoadingAction;
    // Scene間で引き継ぐステージContext
    [SerializeField, Header("Context")]
    private StageSceneContext m_stageSceneContext;

    // Scene遷移時のInエフェクト
    [SerializeField, Header("Scene遷移")]
    private SceneTransitionEffect m_sceneTransitionInEffect;

    // Scene遷移時のOutエフェクト
    [SerializeField]
    private SceneTransitionEffect m_sceneTransitionOutEffect;

    // LoadingSceneを経由するか
    [SerializeField]
    private bool m_useLoadingScene;

    // ステージ選択画面Scene名
    [SerializeField, Header("遷移先")]
    private string m_stageSelectSceneName;

    /// <summary>
    /// 指定ステージを新規プレイとして開始します。
    /// </summary>
    /// <param name="stageDefinition">
    /// 開始するステージ定義。
    /// </param>
    public void LoadNewGame(
        StageDefinitionAsset stageDefinition)
    {
        if (!TryPrepareNewGame(
            stageDefinition))
        {
            return;
        }

        TransitionToScene(
            stageDefinition.SceneName);
    }

    /// <summary>
    /// 現在のインゲームSceneを再読み込みします。
    /// Contextの開始要求は変更しません。
    /// </summary>
    public void ReloadCurrentStage()
    {
        Scene currentScene =
            SceneManager.GetActiveScene();

        if (!currentScene.IsValid()
            || string.IsNullOrWhiteSpace(
                currentScene.name))
        {
            Debug.LogWarning(
                "現在のSceneを取得できませんでした。",
                this);

            return;
        }

        TransitionToScene(
            currentScene.name);
    }

    /// <summary>
    /// ステージ選択画面へ遷移します。
    /// </summary>
    public void LoadStageSelect()
    {
        if (string.IsNullOrWhiteSpace(
            m_stageSelectSceneName))
        {
            Debug.LogWarning(
                "ステージ選択画面Scene名が設定されていません。",
                this);

            return;
        }

        if (m_stageSceneContext != null)
        {
            m_stageSceneContext.Clear();
        }

        TransitionToScene(
            m_stageSelectSceneName);
    }

    /// <summary>
    /// 新規プレイ用のContextを設定します。
    /// </summary>
    /// <param name="stageDefinition">
    /// 開始するステージ定義。
    /// </param>
    /// <returns>
    /// true：設定しました。
    /// false：必要な設定が不足しています。
    /// </returns>
    private bool TryPrepareNewGame(
        StageDefinitionAsset stageDefinition)
    {
        if (m_stageSceneContext == null)
        {
            Debug.LogError(
                $"{nameof(StageSceneContext)} が設定されていません。",
                this);

            return false;
        }

        if (stageDefinition == null)
        {
            Debug.LogError(
                $"{nameof(StageDefinitionAsset)} が指定されていません。",
                this);

            return false;
        }

        if (string.IsNullOrWhiteSpace(
            stageDefinition.SceneName))
        {
            Debug.LogError(
                "ステージScene名が設定されていません。",
                stageDefinition);

            return false;
        }

        if (stageDefinition.StageSettings == null)
        {
            Debug.LogError(
                $"{nameof(StageSettingsAsset)} が設定されていません。",
                stageDefinition);

            return false;
        }

        m_stageSceneContext.SetStage(
            stageDefinition.StageNumber,
            stageDefinition.StageName);

        m_stageSceneContext.SetNewGame();

        return true;
    }

    /// <summary>
    /// SceneTransitionManagerを使用して指定Sceneへ遷移します。
    /// </summary>
    /// <param name="sceneName">
    /// 遷移先Scene名。
    /// </param>
    private void TransitionToScene(
        string sceneName)
    {
        if (string.IsNullOrWhiteSpace(
            sceneName))
        {
            Debug.LogWarning(
                "遷移先Scene名が設定されていません。",
                this);

            return;
        }

        if (m_sceneTransitionInEffect == null
            || m_sceneTransitionOutEffect == null)
        {
            Debug.LogWarning(
                "Scene遷移用のIn/Outエフェクトが設定されていません。",
                this);

            return;
        }

        SceneTransitionManager transitionManager =
            SceneTransitionManager.GetInstance;

        if (transitionManager == null)
        {
            Debug.LogError(
                $"{nameof(SceneTransitionManager)} を取得できませんでした。",
                this);

            return;
        }

        if (transitionManager.IsTransitioning)
        {
            return;
        }
        m_beforeLoadingAction?.Invoke();
        // Pause中のTimeScaleを遷移先へ持ち越さない
        Time.timeScale = 1.0f;

        transitionManager.TransitionToScene(
            sceneName,
            m_sceneTransitionInEffect,
            m_sceneTransitionOutEffect,
            m_useLoadingScene);
    }
}
