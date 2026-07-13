using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TimeManager : MonoBehaviour
{
    const float TIME_LIMIT = 10.0f;

    [SerializeField] HUD timeText;
    float elapsedTime = 0;

    void Start()
    {
        // 時間の初期化
        elapsedTime = TIME_LIMIT;
    }

    void Update()
    {
        // 時間の更新
        if (elapsedTime > 0)
        {
            elapsedTime -= Time.deltaTime;
        }
        // 一定時間経過したら
        else
        {
            // シーン遷移する
            SceneManager.LoadScene("ResultScene");
        }

        // 描画
        timeText.DrawText((int)elapsedTime);
    }
}
