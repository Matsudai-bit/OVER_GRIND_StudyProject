using UnityEngine;
using UnityEngine.UI;

public class ResultTimeView : MonoBehaviour
{
    [Header("0～9の数字スプライト")]
    [SerializeField] private Sprite[] numberSprites = new Sprite[10];

    [Header("各桁のImageオブジェクト")]
    [SerializeField] private Image min10Image; // 分 
    [SerializeField] private Image min1Image;  // 分 
    [SerializeField] private Image sec10Image; // 秒 
    [SerializeField] private Image sec1Image;  // 秒 
    [SerializeField] private Image ms10Image;  // ミリ秒 
    [SerializeField] private Image ms1Image;   // ミリ秒 

    /// <summary>
    /// Presenterから受け取った秒数を各桁に分解して表示する
    /// </summary>
    public void DisplayTime(float totalSeconds)
    {
        // 時間の計算
        int minutes = Mathf.FloorToInt(totalSeconds / 60f);
        int seconds = Mathf.FloorToInt(totalSeconds % 60f);
        int milliseconds = Mathf.FloorToInt((totalSeconds * 100f) % 100f);

        // 各桁の数値を抽出
        int min10 = (minutes / 10) % 10;
        int min1 = minutes % 10;
        int sec10 = (seconds / 10) % 10;
        int sec1 = seconds % 10;
        int ms10 = (milliseconds / 10) % 10;
        int ms1 = milliseconds % 10;

        // スプライトの適用
        SetDigitSprite(min10Image, min10);
        SetDigitSprite(min1Image, min1);
        SetDigitSprite(sec10Image, sec10);
        SetDigitSprite(sec1Image, sec1);
        SetDigitSprite(ms10Image, ms10);
        SetDigitSprite(ms1Image, ms1);
    }

    private void SetDigitSprite(Image img, int number)
    {
        if (img != null && number >= 0 && number < numberSprites.Length)
        {
            img.sprite = numberSprites[number];
        }
    }
}