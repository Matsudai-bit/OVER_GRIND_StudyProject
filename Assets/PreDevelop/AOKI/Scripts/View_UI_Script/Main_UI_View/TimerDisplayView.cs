using UnityEngine;
using UnityEngine.UI;

public class TimerDisplayView : MonoBehaviour
{
    [Header("数字スプライト素材 (0～9)")]
    [SerializeField] private Sprite[] numberSprites = new Sprite[10];

    [Header("タイマー表示用Image")]
    [SerializeField] private Image min10Image; // 分 
    [SerializeField] private Image min1Image;  // 分 
    [SerializeField] private Image sec10Image; // 秒 
    [SerializeField] private Image sec1Image;  // 秒 
    [SerializeField] private Image ms10Image;  // ミリ秒 
    [SerializeField] private Image ms1Image;   // ミリ秒 

    /// <summary>
    /// Presenterから渡された分・秒・ミリ秒をもとにスプライトを更新する
    /// </summary>
    public void SetTime(int min, int sec, int ms)
    {
        if (numberSprites == null || numberSprites.Length < 10) return;

        if (min10Image != null) min10Image.sprite = numberSprites[(min / 10) % 10];
        if (min1Image != null) min1Image.sprite = numberSprites[min % 10];
        if (sec10Image != null) sec10Image.sprite = numberSprites[(sec / 10) % 10];
        if (sec1Image != null) sec1Image.sprite = numberSprites[sec % 10];
        if (ms10Image != null) ms10Image.sprite = numberSprites[(ms / 10) % 10];
        if (ms1Image != null) ms1Image.sprite = numberSprites[ms % 10];
    }
}