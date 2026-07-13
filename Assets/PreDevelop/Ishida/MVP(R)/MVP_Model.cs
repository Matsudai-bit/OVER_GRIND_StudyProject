using UniRx;
using UnityEngine;

//パラメータの変更を行うクラス
//: MonoBehaviourはいらない
public class MVP_Model
{

    private readonly MVP_Parameter m_parameter;

    // Presenterから読み取れるように公開（Parameterの値をそのまま橋渡し）
    //public IReadOnlyReactiveProperty<int> Hp =>


    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="parameter">Parameterクラス</param>
    public MVP_Model(MVP_Parameter parameter)
    {
        m_parameter = parameter;
    }

    //　何かが起きた場合にパラメータが変化する関数を作る↓
}
