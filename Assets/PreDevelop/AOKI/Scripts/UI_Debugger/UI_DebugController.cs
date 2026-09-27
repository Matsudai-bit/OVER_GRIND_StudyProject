using UnityEngine;
using UnityEngine.InputSystem;
using static Unity.Cinemachine.CinemachineTriggerAction.ActionSettings;

public class UI_DebugController : MonoBehaviour
{
    [Header("操作対象のModel")]
    [SerializeField] private HP_UI_Model hpModel;
    [SerializeField] private Veicle_UI_Model vehicleModel;
    [SerializeField] private Timer_UI_Model  timerModel;

    [Header("デバッグ用パラメータ")]
    [SerializeField] private float speedAccelerate = 25.0f;
    [SerializeField] private float speedDecelerate = 15.0f;
    [SerializeField] private float chargeDuration = 1.5f;

    void Update()
    {
        if (Keyboard.current == null) return;

        // HPのデバッグ処理
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            hpModel.TakeDamage(15f);
        }

        // スピードのデバッグ処理
        if (Keyboard.current.zKey.isPressed)
        {
            vehicleModel.AddSpeed(speedAccelerate * Time.deltaTime);
        }
        else
        {
            vehicleModel.AddSpeed(-speedDecelerate * Time.deltaTime);
        }

        // ゲージのデバッグ処理
        if (Keyboard.current.xKey.isPressed)
        {
            vehicleModel.AddGauge(Time.deltaTime / chargeDuration);
        }
        else
        {
            vehicleModel.AddGauge(-Time.deltaTime * 2f);
        }

        // タイマーの進行
        timerModel.AddTime(Time.deltaTime);
    }
}
