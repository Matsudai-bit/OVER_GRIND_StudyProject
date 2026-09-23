using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class VGaugeUIController : MonoBehaviour
{
    [Header("Viewへの参照")]
    [SerializeField] private SpeedDisplayView speedView;

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

        // スピードの計算
        if (Keyboard.current.zKey.isPressed) currentSpeed += speedAccelerate * Time.deltaTime;
        else currentSpeed -= speedDecelerate * Time.deltaTime;

        currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);

        // 小数の計算はViewに任せ、生のfloat値を渡す
        if (speedView != null) speedView.SetSpeed(currentSpeed);

        // ゲージの計算
        if (Keyboard.current.xKey.isPressed) currentCharge += Time.deltaTime / chargeDuration;
        else currentCharge -= Time.deltaTime * 2f;

        currentCharge = Mathf.Clamp(currentCharge, 0f, 1f);
        if (gaugeFillImage != null) gaugeFillImage.fillAmount = currentCharge;
    }
}