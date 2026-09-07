using UnityEngine;

/// <summary>
/// ローディングシーンのリクエスト
/// </summary>
public class LoadingSceneRequest : MonoBehaviour 
{
    private static LoadingSceneRequest m_instance = null;

    private string m_nextSceneName; // 次のシーン名

    public string NextSceneName
    {
        get { return m_nextSceneName; }
        set { m_nextSceneName = value; }
    }

    public static LoadingSceneRequest GetInstance
    {
        get
        {
            if (m_instance == null)
            {
                GameObject obj = new GameObject("LoadingSceneRequest");
                m_instance = obj.AddComponent<LoadingSceneRequest>();
                DontDestroyOnLoad(obj);
            }
            return m_instance;
        }
    }

    /// <summary>
    /// シーンのロード要求
    /// </summary>
    /// <param name="sceneName"></param>
    public void RequestLoadingScene(string sceneName)
    {
        m_nextSceneName = sceneName;
        UnityEngine.SceneManagement.SceneManager.LoadScene("LoadingScene");
    }



}
