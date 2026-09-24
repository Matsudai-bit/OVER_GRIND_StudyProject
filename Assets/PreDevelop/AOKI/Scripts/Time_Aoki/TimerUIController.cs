using UnityEngine;

public class TimerUIController : MonoBehaviour
{
    [Header("Viewの参照")]
    [SerializeField] private TimerDisplayView timerView; 
    private float currentTime = 0f;
    private bool isRunning = true;

    void Update()
    {
        if (isRunning)
        {
            currentTime += Time.deltaTime;
            UpdateTimerDisplay(currentTime);
        }
    }

    void UpdateTimerDisplay(float time)
    {
        if (timerView == null) return;

        // Controllerは「時間の計算」というロジックに集中する
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int milliseconds = Mathf.FloorToInt((time * 100f) % 100f);
        minutes = Mathf.Clamp(minutes, 0, 99);

        // 計算した結果をViewに渡すだけ
        timerView.SetTime(minutes, seconds, milliseconds);
    }
}