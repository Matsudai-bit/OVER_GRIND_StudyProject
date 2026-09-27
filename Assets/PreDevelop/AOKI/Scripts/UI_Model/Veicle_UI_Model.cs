using UnityEngine;
using System;

//ë¨ìxÇ∆ÉQÅ[ÉWUI
public class Veicle_UI_Model : MonoBehaviour
{
    public float MaxSpeed { get; private set; } = 40.0f;
    public float CurrentSpeed { get; private set; }
    public float CurrentGauge { get; private set; }

    public event Action<float> OnSpeedChanged;
    public event Action<float> OnGaugeChanged;

    public void AddSpeed(float amount)
    {
        CurrentSpeed = Mathf.Clamp(CurrentSpeed + amount, 0f, MaxSpeed);
        OnSpeedChanged?.Invoke(CurrentSpeed);
    }

    public void AddGauge(float amount)
    {
        CurrentGauge = Mathf.Clamp(CurrentGauge + amount, 0f, 1f);
        OnGaugeChanged?.Invoke(CurrentGauge);
    }
}
