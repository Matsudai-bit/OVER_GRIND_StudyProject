using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// タイム（秒数）を受け取り、スプライト画像を用いて各桁の数字を表示する View クラスです。
/// </summary>
public class ResultTimeView : MonoBehaviour
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

    [Header("0～9の数字スプライト ")]
    [Tooltip("0～9に対応するスプライト画像配列")]
    [SerializeField] private Sprite[] m_numberSprites = new Sprite[SPRITE_COUNT]; // 数字スプライト配列

    [Header("各桁のImageオブジェクト")]
    [Tooltip("分 10の位の Image")]
    [SerializeField] private Image m_min10Image; // 分 10の位の Image

    [Tooltip("分 1の位の Image")]
    [SerializeField] private Image m_min1Image; // 分 1の位の Image

    [Tooltip("秒 10の位 innocent Image")]
    [SerializeField] private Image m_sec10Image; // 秒 10の位の Image

    [Tooltip("秒 1の位の Image")]
    [SerializeField] private Image m_sec1Image; // 秒 1の位の Image

    [Tooltip("ミリ秒 10の位の Image")]
    [SerializeField] private Image m_ms10Image; // ミリ秒 10の位の Image

    [Tooltip("ミリ秒 1の位の Image")]
    [SerializeField] private Image m_ms1Image; // ミリ秒 1の位の Image

    [Header("画像サイズ補正設定")]
    [Tooltip("チェックを入れると、画像サイズの違いに合わせてアスペクト比（縦横比）を自動で維持・補正します")]
    [SerializeField] private bool m_autoPreserveAspect = true; // アスペクト比自動補正フラグ

    /// <summary>
    /// Presenter から受け取った秒数を各桁に分解して表示します。
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