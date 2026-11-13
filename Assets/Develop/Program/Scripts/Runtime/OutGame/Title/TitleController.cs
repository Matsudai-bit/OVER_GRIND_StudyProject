using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleController : MonoBehaviour
{
    [SerializeField]
    private string m_nextSceneName;

    [SerializeField]
    private SaveDataController saveDataController;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Application.targetFrameRate = 60;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void MarkOpeningWatched()
    {
        if (!saveDataController.MarkOpeningWatched())
        {
            Debug.LogError("オープニング視聴済みにできない");
            return;
        }
        Debug.Log("オープニング視聴済みにした");
    }


    public void OnClickStartButton()
    {
        SceneManager.LoadScene(m_nextSceneName);
    }
}
