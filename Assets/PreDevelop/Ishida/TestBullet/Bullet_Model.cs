using UniRx;
using UnityEngine;

public class Bullet_Model : MonoBehaviour
{
    private readonly BulletParameter _parameter;

    // Presenterから読み取れるように公開（Parameterの値をそのまま橋渡し）
    public IReadOnlyReactiveProperty<int> Hp => _parameter.Hp;
    public int MaxHp => _parameter.MaxHp;
    public int AttackPower => _parameter.AttackPower;
    public int Speed => _parameter.Speed;

    public Bullet_Model(BulletParameter parameter)
    {
        _parameter = parameter;
    }

    // ここにロジックを実装していく
    public void TakeDamage(int damage)
    {
        int newHp = Mathf.Max(0, _parameter.Hp.Value - damage);
        _parameter.Hp.Value = newHp;
    }
}
