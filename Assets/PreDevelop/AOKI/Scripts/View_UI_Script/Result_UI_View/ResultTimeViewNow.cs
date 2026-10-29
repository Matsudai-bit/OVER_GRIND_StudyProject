using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// タイム（秒数またはフォーマット済み文字列）を受け取り、スプライト画像を用いて各桁の数字を表示する View クラスです。
/// </summary>
public class ResultTimeViewNow : MonoBehaviour
{
    // タイムの上限・下限値
    private const float MIN_TOTAL_SECONDS = 0f;
    private const float MAX_TOTAL_SECONDS = 5999.99f;

    // 単位変換・計算用の定数
    private const float SECONDS_PER_MINUTE = 60f;
    private const float MILLISECONDS_PER_SECOND = 100f;
    private const int MAX_MILLISECONDS = 99;
    private const int NUMBER_BASE = 10;
    private const int SPRITE_COUNT = 10;
    private const int TOTAL_DIGIT_COUNT = 6;

    [Header("0～9の数字スプライト")]
    /// <summary>0～9に対応するスプライト画像配列。</summary>
    [Tooltip("0～9に対応するスプライト画像配列")]
    [SerializeField] private Sprite[] m_numberSprites = new Sprite[SPRITE_COUNT];

    [Header("各桁のImageオブジェクト")]
    /// <summary>分 10の位の Image。</summary>
    [Tooltip("分 10の位の Image")]
    [SerializeField] private Image m_min10Image;

    /// <summary>分 1の位の Image。</summary>
    [Tooltip("分 1の位の Image")]
    [SerializeField] private Image m_min1Image;

    /// <summary>秒 10の位の Image。</summary>
    [Tooltip("秒 10の位の Image")]
    [SerializeField] private Image m_sec10Image;

    /// <summary>秒 1の位の Image。</summary>
    [Tooltip("秒 1の位の Image")]
    [SerializeField] private Image m_sec1Image;

    /// <summary>ミリ秒 10の位の Image。</summary>
    [Tooltip("ミリ秒 10の位の Image")]
    [SerializeField] private Image m_ms10Image;

    /// <summary>ミリ秒 1の位の Image。</summary>
    [Tooltip("ミリ秒 1の位の Image")]
    [SerializeField] private Image m_ms1Image;

    [Header("画像サイズ補正設定")]
    /// <summary>画像サイズの違いに合わせてアスペクト比を補正するかどうか。</summary>
    [Tooltip("チェックを入れると、画像サイズの違いに合わせてアスペクト比（縦横比）を自動で維持・補正します")]
    [SerializeField] private bool m_autoPreserveAspect = true;

    /// <summary>
    /// Presenter から受け取った秒数を各桁に分解してスプライト画像で表示します。
    /// </summary>
    /// <param name="totalSeconds">表示する合計秒数。</param>
    public void DisplayTime(float totalSeconds)
    {
        // 負数・オーバーフローの防止処理を行います
        float clampedSeconds = Mathf.Clamp(totalSeconds, MIN_TOTAL_SECONDS, MAX_TOTAL_SECONDS);

        // 分と秒を算出します
        int minutes = Mathf.FloorToInt(clampedSeconds / SECONDS_PER_MINUTE);
        int seconds = Mathf.FloorToInt(clampedSeconds % SECONDS_PER_MINUTE);

        // ミリ秒を算出し、float の丸め誤差を補正します
        int milliseconds = Mathf.RoundToInt((clampedSeconds * MILLISECONDS_PER_SECOND) % MILLISECONDS_PER_SECOND);
        if (milliseconds > MAX_MILLISECONDS)
        {
            milliseconds = MAX_MILLISECONDS;
        }



        // 各桁の数値を抽出します
        int min10 = (minutes / NUMBER_BASE) % NUMBER_BASE;
        int min1 = minutes % NUMBER_BASE;
        int sec10 = (seconds / NUMBER_BASE) % NUMBER_BASE;
        int sec1 = seconds % NUMBER_BASE;
        int ms10 = (milliseconds / NUMBER_BASE) % NUMBER_BASE;
        int ms1 = milliseconds % NUMBER_BASE;

        // スプライトの適用を行います
        SetDigitSprite(m_min10Image, min10);
        SetDigitSprite(m_min1Image, min1);
        SetDigitSprite(m_sec10Image, sec10);
        SetDigitSprite(m_sec1Image, sec1);
        SetDigitSprite(m_ms10Image, ms10);
        SetDigitSprite(m_ms1Image, ms1);
    }

    /// <summary>
    /// Presenter から受け取ったタイム文字列（例: "01:23.45"）を解析し、各桁のスプライト画像に反映します。
    /// </summary>
    /// <param name="timeText">表示するタイム文字列（例: "01:23.45"）。</param>
    public void DisplayTime(string timeText)
    {
        if (string.IsNullOrEmpty(timeText)) return;

        // 文字列から数字（0～9）のみを順に抽出します
        List<int> digits = new List<int>();
        for (int i = 0; i < timeText.Length; i++)
        {
            if (char.IsDigit(timeText[i]))
            {
                digits.Add(timeText[i] - '0');
            }
        }

        // 6桁分の数字が存在する場合に各桁の Image へ順に適用します
        if (digits.Count >= TOTAL_DIGIT_COUNT)
        {
            SetDigitSprite(m_min10Image, digits[0]);
            SetDigitSprite(m_min1Image, digits[1]);
            SetDigitSprite(m_sec10Image, digits[2]);
            SetDigitSprite(m_sec1Image, digits[3]);
            SetDigitSprite(m_ms10Image, digits[4]);
            SetDigitSprite(m_ms1Image, digits[5]);
        }
    }

    /// <summary>
    /// 指定された Image オブジェクトに対応する数字のスプライトを設定します。
    /// </summary>
    /// <param name="targetImage">スプライトを設定する Image コンポーネント。</param>
    /// <param name="number">表示する数字 (0～9)。</param>
    private void SetDigitSprite(Image targetImage, int number)
    {
        if (targetImage == null)
        {
            return;
        }

        // 指定された番号が配列の範囲内かつスプライトが存在するかチェックします
        if (number >= 0 && number < m_numberSprites.Length && m_numberSprites[number] != null)
        {
            targetImage.enabled = true;
            targetImage.sprite = m_numberSprites[number];

            // 画像のアスペクト比（縦横比）を自動で補正します
            if (m_autoPreserveAspect)
            {
                targetImage.preserveAspect = true;
            }
        }
        else
        {
            // スプライトが存在しない場合は画像コンポーネントを非表示にします
            targetImage.enabled = false;
        }
    }
}