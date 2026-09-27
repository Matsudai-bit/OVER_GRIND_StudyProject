using UnityEngine;
using System;

public class Timer_UI_Model : MonoBehaviour
{
    public float CurrentTime { get; private set; }
    public event Action<float> OnTimeChanged;

    public void AddTime(float deltaTime)
    {
        CurrentTime += deltaTime;
        OnTimeChanged?.Invoke(CurrentTime);
    }
}
