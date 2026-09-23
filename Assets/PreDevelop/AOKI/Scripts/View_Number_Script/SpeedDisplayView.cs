using UnityEngine;
using UnityEngine.UI;

public class SpeedDisplayView : MonoBehaviour
{
    [Header("数字スプライト素材 (0～9)")]
    [SerializeField] private Sprite[] numberSprites = new Sprite[10];

    [Header("スピード表示用Image")]
    [SerializeField] private Image digit10Image;     // 十の位
    [SerializeField] private Image digit1Image;      // 一の位
    [SerializeField] private Image digitDecimalImage; // 小数第一位

    /// <summary>
    /// Presenterから渡されたスピードの値をもとにスプライトを更新する
    /// </summary>
    public void SetSpeed(float speed)
    {
        if (numberSprites == null || numberSprites.Length < 10) return;

        int speedInt = Mathf.FloorToInt(speed);
        int digit10 = (speedInt / 10) % 10;
        int digit1 = speedInt % 10;
        int digitDec = Mathf.FloorToInt((speed - speedInt) * 10f) % 10;

        if (digit10Image != null) digit10Image.sprite = numberSprites[digit10];
        if (digit1Image != null) digit1Image.sprite = numberSprites[digit1];
        if (digitDecimalImage != null) digitDecimalImage.sprite = numberSprites[digitDec];
    }
}