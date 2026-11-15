using System.Collections;
using UnityEngine;

public class GameQuitter : MonoBehaviour
{
    [SerializeField] float m_quitDelay = 0.3f;

    public void QuitGame()
    {
        StartCoroutine(QuitRoutine());
    }

    IEnumerator QuitRoutine()
    {
        yield return new WaitForSecondsRealtime(m_quitDelay);
        Application.Quit();
    }
}