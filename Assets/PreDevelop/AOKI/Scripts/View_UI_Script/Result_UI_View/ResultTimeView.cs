using UnityEngine;
using UnityEngine.UI;

public class ResultTimeView : MonoBehaviour
{
    [Header("0～9の数字スプライト (Element 0 に『0』, Element 1 に『1』...)")]
    [SerializeField] private Sprite[] numberSprites = new Sprite[10];

    [Header("各桁のImageオブジェクト")]
    [SerializeField] private Image min10Image; // 分 10の位
    [SerializeField] private Image min1Image;  // 分 1の位
    [SerializeField] private Image sec10Image; // 秒 10の位
    [SerializeField] private Image sec1Image;  // 秒 1の位
    [SerializeField] private Image ms10Image;  // ミリ秒 10の位
    [SerializeField] private Image ms1Image;   // ミリ秒 1の位

    [Header("★ 画像サイズ補正設定")]
    [Tooltip("チェックを入れると、画像サイズの違いに合わせてアスペクト比（縦横比）を自動で維持・補正します")]
    [SerializeField] private bool m_autoPreserveAspect = true;

    /// <summary>
    /// Presenterから受け取った秒数を各桁に分解して表示する
    /// </summary>
    public void DisplayTime(float totalSeconds)
    {
        // 負数・オーバーフロー防止
        totalSeconds = Mathf.Clamp(totalSeconds, 0f, 5999.99f);

        int minutes = Mathf.FloorToInt(totalSeconds / 60f);
        int seconds = Mathf.FloorToInt(totalSeconds % 60f);

        // float の丸め誤差対策
        int milliseconds = Mathf.RoundToInt((totalSeconds * 100f) % 100f);
        if (milliseconds >= 100) milliseconds = 99;

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
        if (img == null) return;

        // 指定の番号が範囲内かつスプライトが存在する場合
        if (number >= 0 && number < numberSprites.Length && numberSprites[number] != null)
        {
            img.enabled = true;
            img.sprite = numberSprites[number];

            // ★ 画像サイズ・アスペクト比（縦横比）の違いを自動補正
            if (m_autoPreserveAspect)
            {
                img.preserveAspect = true;
            }
        }
        else
        {
            // スプライトが正常に渡されなかった場合に白箱を表示させず非表示にする安全策
            img.enabled = false;
        }
    }
}