using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleController : MonoBehaviour
{
    [SerializeField]
    private string m_nextSceneName;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void OnClickStartButton()
    {
        SceneManager.LoadScene(m_nextSceneName);
    }
}
