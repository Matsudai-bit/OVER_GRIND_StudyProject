using System;
using UnityEngine;
using UnityEngine.UI;

public class MVRP_View : MonoBehaviour
{
    // Presenterがここを購読して「知る」
    public event Action<int> OnDamaged;

    [SerializeField] private Renderer _renderer;
    [SerializeField] private Image _hpBar; // UI

    //private void OnTriggerEnter(Collider other)
    //{
    //    if (other.TryGetComponent<EnemyAttack>(out var enemyAttack))
    //    {
    //        int damage = enemyAttack.AttackPower;

    //        FlashRed(); // 見た目の演出はView内で完結させてOK
    //        OnDamaged?.Invoke(damage); // Presenterに知らせる
    //    }
    //}

    private void FlashRed()
    {
        _renderer.material.color = Color.red;
        // 一定時間後に戻す処理は別途コルーチンやDOTweenなどで
    }

    // Presenterから呼ばれてHPバーを更新する（Viewが「知る」側の役割）
    public void UpdateHpBar(int currentHp, int maxHp)
    {
        _hpBar.fillAmount = (float)currentHp / maxHp;
    }
}
