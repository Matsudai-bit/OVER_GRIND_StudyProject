using UniRx;
using UnityEngine;

//値を保持するだけのクラス
//: MonoBehaviourはいらない
public class MVRP_Parameter 
{
    //Controllerで設定したパラメータを保持するための変数（読み取り専用{get;}で作成）
    //ReactiveProperty<int>変化を検知できる
    public ReactiveProperty<int> Hp { get; }
    public int MaxHp { get; }

    //コンストラクタ
    public MVRP_Parameter(int maxHp)
    {
        MaxHp = maxHp;
        Hp = new ReactiveProperty<int>(maxHp);

    }
}
