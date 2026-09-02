using UnityEngine;
using UnityEngine.InputSystem; // ← これを追加します

public class DinosaurDamage : MonoBehaviour
{
    private Renderer _renderer;
    private Material _material;

    private readonly int DamageAmountProp = Shader.PropertyToID("_DamageAmount");

    public float maxHP = 100f;
    public float currentHP = 100f;

    void Start()
    {
        _renderer = GetComponentInChildren<Renderer>();
        _material = _renderer.material;
    }

    void Update()
    {
        // 新しいInput Systemでの「Aキーが押された瞬間」の判定
        if (Keyboard.current != null && Keyboard.current.aKey.wasPressedThisFrame)
        {
            TakeDamage(10f);
            Debug.Log("Aキー入力：10ダメージ！ 現在のHP: " + currentHP);
        }
    }

    public void TakeDamage(float damage)
    {
        currentHP -= damage;
        if (currentHP < 0) currentHP = 0;

        float damageRatio = 1.0f - (currentHP / maxHP);
        _material.SetFloat(DamageAmountProp, damageRatio);
    }
}