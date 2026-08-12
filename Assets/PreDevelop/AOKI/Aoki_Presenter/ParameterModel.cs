using System;
using UnityEngine;

public class ParameterModel
{
    public string Name { get; }
    public int CurrentValue { get; private set; }
    public int MaxValue { get; private set; }

    public event Action OnValueChanged;

    public ParameterModel(string name, int initialValue, int maxValue)
    {
        Name = name;
        MaxValue = maxValue;
        CurrentValue = Mathf.Clamp(initialValue, 0, maxValue);
    }

    public void SetValue(int value)
    {
        int clamped = Mathf.Clamp(value, 0, MaxValue);
        if (CurrentValue == clamped) return;

        CurrentValue = clamped;
        OnValueChanged?.Invoke();
    }

    public string GetValueString() => $"{CurrentValue} / {MaxValue}";
}