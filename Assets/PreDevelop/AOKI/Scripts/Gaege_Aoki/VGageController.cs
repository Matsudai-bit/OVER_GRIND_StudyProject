using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class VGageController : MonoBehaviour
{
    [Header("数字スプライト素材 (0～9の順番でアタッチ)")]
    [SerializeField] private Sprite[] numberSprites = new Sprite[10];

    [Header("スピード表示用Image (桁ごとのUI)")]
    [SerializeField] private Image digit10Image;     // 十の位
    [SerializeField] private Image digit1Image;      // 一の位
    [SerializeField] private Image digitDecimalImage; // 小数第一位

    [Header("その他のUI要素")]
    [SerializeField] private Image gaugeFillImage;   // Vgauge_bar (Filled Image)

    [Header("スピード(数字)の設定パラメータ")]
    [SerializeField] private float maxSpeed = 40.0f;       // 最高速度
    [SerializeField] private float speedAccelerate = 25.0f; // Zキーでの加速度
    [SerializeField] private float speedDecelerate = 15.0f; // 離した時の減速度

    [Header("ゲージの設定パラメータ")]
    [SerializeField] private float chargeDuration = 1.5f;   // Xキーで満タンになるまでの秒数

    private float currentSpeed = 0f;
    private float currentCharge = 0f;

    void Update()
    {
        if (Keyboard.current == null) return;

        // --- 1. スピードの計算 (Zキーで数字が上がる) ---
        if (Keyboard.current.zKey.isPressed)
        {
            currentSpeed += speedAccelerate * Time.deltaTime;
        }
        else
        {
            currentSpeed -= speedDecelerate * Time.deltaTime;
        }

        // スピードが 0 ～ 40 の間に収まるように制限
        currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);
        UpdateSpeedDisplay(currentSpeed);


        // --- 2. ゲージの計算 (Xキーでゲージが伸びる) ---
        if (Keyboard.current.xKey.isPressed)
        {
            // chargeDuration(1.5秒) かけて 0 から 1 になるように足す
            currentCharge += Time.deltaTime / chargeDuration;
        }
        else
        {
            // 離すと2倍の速さで減る
            currentCharge -= Time.deltaTime * 2f;
        }

        // ゲージ量が 0.0 ～ 1.0 の間に収まるように制限
        currentCharge = Mathf.Clamp(currentCharge, 0f, 1f);
        UpdateGaugeUI(currentCharge);
    }

    void UpdateSpeedDisplay(float speed)
    {
        if (numberSprites == null || numberSprites.Length < 10) return;

        // 整数部分と小数第一位を取得 (例: 39.8 -> 十の位:3, 一の位:9, 小数:8)
        int speedInt = Mathf.FloorToInt(speed);
        int digit10 = (speedInt / 10) % 10;
        int digit1 = speedInt % 10;
        int digitDec = Mathf.FloorToInt((speed - speedInt) * 10f) % 10;

        // 各Imageにスプライトをセット
        if (digit10Image != null) digit10Image.sprite = numberSprites[digit10];
        if (digit1Image != null) digit1Image.sprite = numberSprites[digit1];
        if (digitDecimalImage != null) digitDecimalImage.sprite = numberSprites[digitDec];
    }

    void UpdateGaugeUI(float chargeAmount)
    {
        if (gaugeFillImage != null)
        {
            gaugeFillImage.fillAmount = chargeAmount;
        }
    }
}