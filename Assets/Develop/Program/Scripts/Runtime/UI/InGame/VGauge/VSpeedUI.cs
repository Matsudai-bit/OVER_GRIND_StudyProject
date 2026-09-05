using TMPro;
using UnityEngine;

/// <summary>
/// プレイヤーの速度表示を管理します。
/// 外部から渡された速度をUIへ表示します。
/// </summary>
public class VSpeedUI : MonoBehaviour
{
    [Header("Speed")]

    // 速度の整数部分を表示するテキスト
    [SerializeField]
    private TMP_Text integerText;

    // 速度の小数部分を表示するテキスト
    [SerializeField]
    private TMP_Text decimalText;

    /// <summary>
    /// 現在表示している速度
    /// </summary>
    private float currentSpeed;

    /// <summary>
    /// 速度を設定します。
    /// 外部から速度を受け取り、表示を更新します。
    /// </summary>
    /// <param name="speed">表示する速度</param>
    public void SetSpeed(float speed)
    {
        // 速度が0～99.9の範囲に収まるよう制限する
        currentSpeed = Mathf.Clamp(speed, 0f, 99.9f);

        // 表示内容を更新する
        UpdateSpeedText();
    }

    /// <summary>
    /// 現在の速度を取得します。
    /// </summary>
    public float GetSpeed()
    {
        return currentSpeed;
    }

    /// <summary>
    /// 速度表示を更新します。
    /// </summary>
    private void UpdateSpeedText()
    {
        // 整数部分を取得
        int integerPart = Mathf.FloorToInt(currentSpeed);

        // 小数第1位を取得
        int decimalPart =
            Mathf.RoundToInt((currentSpeed - integerPart) * 10f);

        // 整数部分を表示
        integerText.text = integerPart.ToString();

        // 小数部分を表示
        decimalText.text = $".{decimalPart}";
    }
}