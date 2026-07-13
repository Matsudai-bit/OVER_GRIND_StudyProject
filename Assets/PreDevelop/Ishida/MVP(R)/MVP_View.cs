using System;
using UnityEngine;
using UnityEngine.UI;

//見た目・入力・当たり判定クラス（オブジェクトの移動などもここで行う）
//UIの調整？変動なども行う
//このクラスではパラメータに関する計算や比較などはしない
//変更点やパラメータに関する事象が起きた場合Presenterに知らせる
public class MVP_View : MonoBehaviour
{
    // Presenterへ知らせるイベントなどを記入
    


    /// <summary>
    /// 初期化
    /// </summary>
    public void Initialize(/* パラメータが必要ならばPresenterから貰う*/)
    {
        
    }

    /// <summary>
    /// 更新処理
    /// </summary>
    void Update()
    {

    }

    /// <summary>
    /// 当たった際何をするか
    /// </summary>
    /// <param name="other">当たったもの</param>
    private void OnTriggerEnter(Collider other)
    {

    }



    //普通の動きもここで行うのでDebug用関数も必要なら入れる

}
