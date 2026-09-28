using UnityEngine;
using System;

/// <summary>
/// 車両の速度とゲージのデータを管理するモデルです。
/// </summary>
public class VehicleUiModel : MonoBehaviour
{
    /// <summary>車両の最大速度を取得します。</summary>
    public float MaxSpeed { get; private set; } = 40.0f;

    /// <summary>現在の速度を取得します。</summary>
    public float CurrentSpeed { get; private set; }

    /// <summary>現在のゲージ量を取得します。</summary>
    public float CurrentGauge { get; private set; }

    /// <summary>速度が変更された際に発火するイベントです。</summary>
    public event Action<float> OnSpeedChanged;

    /// <summary>ゲージ量が変更された際に発火するイベントです。</summary>
    public event Action<float> OnGaugeChanged;

    /// <summary>
    /// 現在の速度を加算または減算し、変更後の値をイベントで通知します。
    /// </summary>
    /// <param name="amount">変化させる速度の量（マイナス値で減速）。</param>
    public void AddSpeed(float amount)
    {
        CurrentSpeed = Mathf.Clamp(CurrentSpeed + amount, 0f, MaxSpeed);
        OnSpeedChanged?.Invoke(CurrentSpeed);
    }

    /// <summary>
    /// 現在のゲージ量を加算または減算し、変更後の値をイベントで通知します。
    /// </summary>
    /// <param name="amount">変化させるゲージの量（マイナス値で減少）。</param>
    public void AddGauge(float amount)
    {
        CurrentGauge = Mathf.Clamp(CurrentGauge + amount, 0f, 1f);
        OnGaugeChanged?.Invoke(CurrentGauge);
    }
}