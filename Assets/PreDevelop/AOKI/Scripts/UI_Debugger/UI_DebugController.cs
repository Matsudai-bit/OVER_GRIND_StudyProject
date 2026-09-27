using UnityEngine;
using UnityEngine.InputSystem;

public class UI_DebugController : MonoBehaviour
{
    [Header("操作対象のModel")]
    [SerializeField] private HP_UI_Model hpModel;
    [SerializeField] private Veicle_UI_Model vehicleModel;
    [SerializeField] private Timer_UI_Model timerModel;

    [Header("デバッグ用パラメータ")]
    [SerializeField] private float speedAccelerate = 25.0f;
    [SerializeField] private float speedDecelerate = 15.0f;
    [SerializeField] private float chargeDuration = 1.5f;

    void Update()
    {
        if (Keyboard.current == null) return;

        // HPのデバッグ処理（Spaceキー）
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (hpModel != null) hpModel.TakeDamage(15f);
        }

        // スピードのデバッグ処理（Zキー）
        if (Keyboard.current.zKey.isPressed)
        {
            if (vehicleModel != null) vehicleModel.AddSpeed(speedAccelerate * Time.deltaTime);
        }
        else
        {
            if (vehicleModel != null) vehicleModel.AddSpeed(-speedDecelerate * Time.deltaTime);
        }

        // ゲージのデバッグ処理（Xキー）
        if (Keyboard.current.xKey.isPressed)
        {
            if (vehicleModel != null) vehicleModel.AddGauge(Time.deltaTime / chargeDuration);
        }
        else
        {
            if (vehicleModel != null) vehicleModel.AddGauge(-Time.deltaTime * 2f);
        }

        // タイマーの進行（自動）
        if (timerModel != null) timerModel.AddTime(Time.deltaTime);
    }
}