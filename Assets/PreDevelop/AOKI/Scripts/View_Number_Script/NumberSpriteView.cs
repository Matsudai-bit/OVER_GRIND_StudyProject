// 新規作成: NumberSpriteView.cs
using UnityEngine;
using UnityEngine.UI;

public class NumberSpriteView : MonoBehaviour
{
    [Header("数字スプライト素材 (0 9)")]
    [SerializeField] private Sprite[] numberSprites = new Sprite[10];

    /// <summary>
    /// 指定されたImageに、0 9の該当する数字スプライトをセットする
    /// </summary>
    public void SetDigit(Image targetImage, int digit)
    {
        if (targetImage == null || numberSprites == null || numberSprites.Length < 10) return;

        int safeDigit = Mathf.Clamp(digit, 0, 9);
        targetImage.sprite = numberSprites[safeDigit];
    }
}