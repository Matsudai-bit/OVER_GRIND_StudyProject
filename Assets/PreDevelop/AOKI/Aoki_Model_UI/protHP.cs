using UnityEngine;
using System;

// HPのモデル
public class protHP
{
    private int m_currentHP; //自機の現在あるHP

    // 値が変更されたときに発火するイベントを取得する
    public event Action<int> OnHPChanged;   

    //コンストラクタ
    public protHP(int initialHP)
    {
        //HPを初期化
        m_currentHP = initialHP;
    }

    // HPの値を変更するメソッド
    public void SetHP(int value)
    {
        // 値が変わった時だけイベントを通知する
        if (m_currentHP != value)
        {
            m_currentHP = value;
            //モデル自身に教える
            OnHPChanged?.Invoke(m_currentHP);
        }
    }

    //HPを取得
    public int GetHP() => m_currentHP;
}
