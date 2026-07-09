using UniRx;
using UnityEngine;

//パラメータの変更を行うクラス
public class Obstacle_Model
{
    private readonly Obstacle_Parameter _parameter;

    // Presenterから読み取れるように公開（Parameterの値をそのまま橋渡し）
    public IReadOnlyReactiveProperty<int> Hp => _parameter.Hp;
    public int MaxHp => _parameter.MaxHp;
    public int Speed => _parameter.Speed;

    public Obstacle_Model(Obstacle_Parameter parameter)
    {
        _parameter = parameter;
    }

    // ダメージを受けHpを減らす処理
    public void TakeDamage(int damage)
    {
        int newHp = Mathf.Max(0, _parameter.Hp.Value - damage);
        _parameter.Hp.Value = newHp;
        DebugManager.Log("HPの変化："+MaxHp + "→" + newHp);
    }
}
