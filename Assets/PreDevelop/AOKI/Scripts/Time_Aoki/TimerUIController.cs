using UnityEngine;
using UnityEngine.UI;

public class TimerUIController : MonoBehaviour
{
    [Header("Viewへの参照")]
    [SerializeField] private NumberSpriteView numberView; // 追加

    [Header("タイマー表示用Image ")]
    [SerializeField] private Image min10Image;
    [SerializeField] private Image min1Image;
    [SerializeField] private Image sec10Image;
    [SerializeField] private Image sec1Image;
    [SerializeField] private Image ms10Image;
    [SerializeField] private Image ms1Image;

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
        if (numberView == null) return; // Viewがない場合は処理しない

        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int milliseconds = Mathf.FloorToInt((time * 100f) % 100f);

        minutes = Mathf.Clamp(minutes, 0, 99);

        // 共通のViewを利用して各桁の画像を設定
        numberView.SetDigit(min10Image, (minutes / 10) % 10);
        numberView.SetDigit(min1Image, minutes % 10);
        numberView.SetDigit(sec10Image, (seconds / 10) % 10);
        numberView.SetDigit(sec1Image, seconds % 10);
        numberView.SetDigit(ms10Image, (milliseconds / 10) % 10);
        numberView.SetDigit(ms1Image, milliseconds % 10);
    }
}