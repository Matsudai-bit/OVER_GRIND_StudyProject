using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// UI のデバッグ操作を行うコントローラーです。
/// </summary>
public class UiDebugController : MonoBehaviour
{
    [Header("操作対象のModel")]

    /// <summary>HP を管理するモデル。</summary>
    [SerializeField] private HpUiModel m_hpModel;

    /// <summary>車両の速度やゲージを管理するモデル。</summary>
    [SerializeField] private VehicleUiModel m_vehicleModel;

    /// <summary>タイマーを管理するモデル。</summary>
    [SerializeField] private TimerUiModel m_timerModel;

    [Header("デバッグ用パラメータ")]

    /// <summary>加速時の増加スピード。</summary>
    [SerializeField] private float m_speedAccelerate = 25.0f;

    /// <summary>減速時の減少スピード。</summary>
    [SerializeField] private float m_speedDecelerate = 15.0f;

    /// <summary>ゲージが最大になるまでの時間（秒）。</summary>
    [SerializeField] private float m_chargeDuration = 1.5f;

    /// <summary>
    /// 毎フレームのキー入力判定と各モデルの更新を行います。
    /// </summary>
    private void Update()
    {
        // キーボードが接続されていない場合は処理を中断
        if (Keyboard.current == null) return;

        // Spaceキー：HPにダメージを与える
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (m_hpModel != null) m_hpModel.TakeDamage(15f);
        }

        // Zキー：車両のスピードを加減速する
        if (Keyboard.current.zKey.isPressed)
        {
            if (m_vehicleModel != null) m_vehicleModel.AddSpeed(m_speedAccelerate * Time.deltaTime);
        }
        else
        {
            if (m_vehicleModel != null) m_vehicleModel.AddSpeed(-m_speedDecelerate * Time.deltaTime);
        }

        // Xキー：ゲージを増減させる
        if (Keyboard.current.xKey.isPressed)
        {
            if (m_vehicleModel != null) m_vehicleModel.AddGauge(Time.deltaTime / m_chargeDuration);
        }
        else
        {
            if (m_vehicleModel != null) m_vehicleModel.AddGauge(-Time.deltaTime * 2f);
        }

        // 自動進行：毎フレームタイマーを進める
        if (m_timerModel != null) m_timerModel.AddTime(Time.deltaTime);
    }
}