using UnityEngine;
using System;

//•ÏXUIƒ‚ƒfƒ‹oHPp
public class HP_UI_Model : MonoBehaviour
{
    public float MaxHp { get; private set; } = 100f;
    public float CurrentHp { get; private set; }

    public event Action<float, float> OnHpChanged;

    private void Start()
    {
        CurrentHp = MaxHp;
    }

    public void TakeDamage(float damage)
    {
        CurrentHp = Mathf.Max(0, CurrentHp - damage);
        OnHpChanged?.Invoke(CurrentHp, MaxHp);
    }
}
