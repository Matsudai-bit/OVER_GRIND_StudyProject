using UniRx;
using UnityEngine;

public class MVRP_Model : MonoBehaviour
{
    private readonly ReactiveProperty<int> _hp;
    public IReadOnlyReactiveProperty<int> Hp => _hp; // 外部には読み取り専用で公開

    public int MaxHp { get; private set; }

    public MVRP_Model(int maxHp)
    {
        MaxHp = maxHp;
        _hp = new ReactiveProperty<int>(maxHp);
    }

    // Presenterから呼ばれる「パラメータ更新」の窓口
    public void TakeDamage(int damage)
    {
        var newHp = Mathf.Max(0, _hp.Value - damage);
        _hp.Value = newHp; // ここで値が変わると自動的に購読者(Presenter)に通知される
    }
}
