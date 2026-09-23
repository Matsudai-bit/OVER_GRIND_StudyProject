using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class VGaugeUIController : MonoBehaviour
{
    [Header("Viewへの参照")]
    [SerializeField] private NumberSpriteView numberView; 

    [Header("スピード表示用Image (桁ごとのUI)")]
    [SerializeField] private Image digit10Image;
    [SerializeField] private Image digit1Image;
    [SerializeField] private Image digitDecimalImage;

    [Header("その他のUI要素")]
    [SerializeField] private Image gaugeFillImage;

    [Header("スピード(数字)の設定パラメータ")]
    [SerializeField] private float maxSpeed = 40.0f;
    [SerializeField] private float speedAccelerate = 25.0f;
    [SerializeField] private float speedDecelerate = 15.0f;

    [Header("ゲージの設定パラメータ")]
    [SerializeField] private float chargeDuration = 1.5f;

    private float currentSpeed = 0f;
    private float currentCharge = 0f;

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.zKey.isPressed)
        {
            currentSpeed += speedAccelerate * Time.deltaTime;
        }
        else
        {
            currentSpeed -= speedDecelerate * Time.deltaTime;
        }

        currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);
        UpdateSpeedDisplay(currentSpeed);

        if (Keyboard.current.xKey.isPressed)
        {
            currentCharge += Time.deltaTime / chargeDuration;
        }
        else
        {
            currentCharge -= Time.deltaTime * 2f;
        }

        currentCharge = Mathf.Clamp(currentCharge, 0f, 1f);
        UpdateGaugeUI(currentCharge);
    }

    void UpdateSpeedDisplay(float speed)
    {
        if (numberView == null) return; // Viewがない場合は処理しない

        int speedInt = Mathf.FloorToInt(speed);

        // 共通のViewを利用して各桁の画像を設定
        numberView.SetDigit(digit10Image, (speedInt / 10) % 10);
        numberView.SetDigit(digit1Image, speedInt % 10);
        numberView.SetDigit(digitDecimalImage, Mathf.FloorToInt((speed - speedInt) * 10f) % 10);
    }

    void UpdateGaugeUI(float chargeAmount)
    {
        if (gaugeFillImage != null)
        {
            gaugeFillImage.fillAmount = chargeAmount;
        }
    }
}