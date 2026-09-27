using UnityEngine;
using System;

//変更UIモデル｛HP｝
public class HP_UI_Model : MonoBehaviour
{

    public float MaxHP { get; private set; } = 100f; //最大値
    public float CurrentHP { get; private set; }     //変更値


    public event Action<float, float> OnHPChanged;//

    void Start()
    {
        CurrentHP = MaxHP;
    }

    public void TakeDamage(float damage)
    {
        CurrentHP = Mathf.Max(0,  CurrentHP - damage);
        OnHPChanged.Invoke(CurrentHP, MaxHP);
    }
}
