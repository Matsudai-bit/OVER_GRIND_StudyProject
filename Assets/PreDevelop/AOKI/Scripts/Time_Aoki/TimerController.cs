using UnityEngine;
using UnityEngine.UI;

public class TimerController : MonoBehaviour
{
    [Header("数字スプライト素材 (0～9の順番)")]
    [SerializeField] private Sprite[] numberSprites = new Sprite[10];

    [Header("タイマー表示用Image (桁ごとのUI)")]
    [SerializeField] private Image min10Image; // 分 
    [SerializeField] private Image min1Image;  // 分 
    [SerializeField] private Image sec10Image; // 秒 
    [SerializeField] private Image sec1Image;  // 秒 
    [SerializeField] private Image ms10Image;  // ミリ秒 
    [SerializeField] private Image ms1Image;   // ミリ秒 

    private float currentTime = 0f;
    private bool isRunning = true; // trueの間だけ時間が進む

    void Update()
    {
        if (isRunning)
        {
            currentTime += Time.deltaTime; // 毎フレーム時間を足す
            UpdateTimerDisplay(currentTime);
        }
    }

    void UpdateTimerDisplay(float time)
    {
        if (numberSprites == null || numberSprites.Length < 10) return;

        // 分・秒・ミリ秒をそれぞれ計算
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int milliseconds = Mathf.FloorToInt((time * 100f) % 100f);

        // 分は99分でストップさせる
        minutes = Mathf.Clamp(minutes, 0, 99);

        // 各桁の数字を割り出す
        int min10 = (minutes / 10) % 10;
        int min1 = minutes % 10;
        int sec10 = (seconds / 10) % 10;
        int sec1 = seconds % 10;
        int ms10 = (milliseconds / 10) % 10;
        int ms1 = milliseconds % 10;

        // それぞれのImageに画像をセット
        if (min10Image != null) min10Image.sprite = numberSprites[min10];
        if (min1Image != null) min1Image.sprite = numberSprites[min1];
        if (sec10Image != null) sec10Image.sprite = numberSprites[sec10];
        if (sec1Image != null) sec1Image.sprite = numberSprites[sec1];
        if (ms10Image != null) ms10Image.sprite = numberSprites[ms10];
        if (ms1Image != null) ms1Image.sprite = numberSprites[ms1];
    }
}