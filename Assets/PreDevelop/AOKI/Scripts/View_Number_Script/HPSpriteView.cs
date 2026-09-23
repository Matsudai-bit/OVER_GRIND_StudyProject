using UnityEngine;
using UnityEngine.UI;

public class HPDisplayView : MonoBehaviour
{
    [Header("数字スプライト素材 (0～9)")]
    [SerializeField] private Sprite[] numberSprites = new Sprite[10];

    [Header("HP表示用Image")]
    [SerializeField] private Image digit100Image; // 百の位
    [SerializeField] private Image digit10Image;  // 十の位
    [SerializeField] private Image digit1Image;   // 一の位

    /// <summary>
    /// Presenterから渡されたHPの数値をもとにスプライトを更新する
    /// </summary>
    public void SetValue(int value)
    {
        if (numberSprites == null || numberSprites.Length < 10) return;

        int safeValue = Mathf.Clamp(value, 0, 999);
        int digit100 = (safeValue / 100) % 10;
        int digit10 = (safeValue / 10) % 10;
        int digit1 = safeValue % 10;

        if (digit100Image != null) digit100Image.sprite = numberSprites[digit100];
        if (digit10Image != null) digit10Image.sprite = numberSprites[digit10];
        if (digit1Image != null) digit1Image.sprite = numberSprites[digit1];
    }
}