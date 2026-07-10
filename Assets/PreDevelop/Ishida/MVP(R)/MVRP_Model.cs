using UniRx;
using UnityEngine;

//パラメータの変更を行うクラス
//: MonoBehaviourはいらない
public class MVRP_Model 
{

    private readonly MVRP_Parameter _parameter;

    // Presenterから読み取れるように公開（Parameterの値をそのまま橋渡し）
    public IReadOnlyReactiveProperty<int> Hp => _parameter.Hp;
    public int MaxHp => _parameter.MaxHp;

    //コンストラクタ
    public MVRP_Model(MVRP_Parameter parameter)
    {
        _parameter = parameter;
    }

    //　このようにダメージを受けるなど何かが起きた場合にパラメータが変化する関数を作る↓
    // ダメージを受けHpを減らす処理
    public void TakeDamage(int damage)
    {
        int newHp = Mathf.Max(0, _parameter.Hp.Value - damage);
        _parameter.Hp.Value = newHp;
        DebugManager.Log("HPの変化：" + MaxHp + "→" + newHp);
    }
}
