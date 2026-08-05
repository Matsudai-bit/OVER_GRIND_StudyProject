using System;
using UnityEngine;

/// <summary>
/// シーン遷移の演出インターフェース
/// </summary>
public interface ISceneTransitionEffect
{
    /// <summary>
    /// シーン遷移の開始
    /// </summary>
    /// <param name="onComplete">シーン遷移が完了した際に呼び出されるコールバック</param>
    public void StartTransition(System.Action onComplete);

    // トランジション中かどうか
    public bool IsTransitioning();

}

/// <summary>
/// シーン遷移を管理するマネージャークラス
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    private static SceneTransitionManager m_instance;
    public static SceneTransitionManager GetInstance
    {
        get
        {
            if (m_instance == null)
            {
                GameObject obj = new GameObject("SceneTransitionManager");
                m_instance = obj.AddComponent<SceneTransitionManager>();
                DontDestroyOnLoad(obj);
            }
            return m_instance;
        }
    }
    private void Awake()
    {
        if (m_instance == null)
        {
            m_instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (m_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// シーン遷移を実行する
    /// </summary>
    /// <param name="sceneName"></param>
    /// <param name="sceneTransitionEffect"></param>
    public void TransitionToScene(string sceneName, ISceneTransitionEffect sceneTransitionEffect, bool inLoadingScene = false)
    {
        // シーン遷移エフェクトを開始し、完了時にシーンをロードする
        sceneTransitionEffect.StartTransition(() =>
        {
            if (inLoadingScene)
            {
                LoadingSceneRequest.GetInstance.RequestLoadingScene(sceneName);

            }
            else
            {
                // シーンのロード
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);

            }
        });

    }
}
