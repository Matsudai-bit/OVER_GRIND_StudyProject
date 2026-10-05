using TMPro;
using UnityEngine;

/// <summary>
/// モデルから渡された速度を表示します。速度の保持・更新は行いません。
/// </summary>
public class VSpeedUI : MonoBehaviour
{
    [SerializeField, Header("Speed")]
    private TMP_Text integerText;

    [SerializeField]
    private TMP_Text decimalText;

    /// <summary>速度の整数部分と小数部分を従来の形式で表示します。</summary>
    /// <param name="speed">SpeedPlaceModelから取得した速度。</param>
    public void SetSpeed(float speed)
    {
        int integerPart = Mathf.FloorToInt(speed);
        int decimalPart = Mathf.RoundToInt((speed - integerPart) * 10f);

        if (integerText != null)
        {
            integerText.text = integerPart.ToString();
        }

        if (decimalText != null)
        {
            decimalText.text = $".{decimalPart}";
        }
    }
}