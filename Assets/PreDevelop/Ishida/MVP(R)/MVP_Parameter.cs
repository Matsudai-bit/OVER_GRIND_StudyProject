using UniRx;
using UnityEngine;

//値を保持するだけのクラス
//: MonoBehaviourはいらない
public class MVP_Parameter 
{
    //Controllerで設定したパラメータを保持するための変数（読み取り専用{get;}で作成）
    //ReactiveProperty<int>変化を検知できる
    //ここでの変数名はHpのようにすると使いやすくなるかも


    /// <summary>
    /// コンストラクタ
    /// </summary>
    public MVP_Parameter(/*欲しいパラメータを引数にする*/)
    {

    }
}
