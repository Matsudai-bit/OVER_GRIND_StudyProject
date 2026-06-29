using UnityEngine;
using UniRx;

//プレイヤーのダメージ通知イベント
public class PlayerHealthModel
{
    public ReactiveProperty<int> CurrentHp { get; private set; }　//体力
    public ReactiveProperty<int> CurrentMp { get; private set; }　//魔力
    public ReactiveProperty<int> Gold { get; private set; }　     //カネ

    //挿入
    public PlayerHealthModel(int maxHp, int maxMp, int gold)
    {
        CurrentHp = new ReactiveProperty<int>(maxHp);   //体力
        CurrentMp = new ReactiveProperty<int>(maxMp);   //魔力
        Gold      = new ReactiveProperty<int>(gold);    //カネ
    }



    //与えたダメージを減らす
    public void TakeDamage(int damage)
    {
        CurrentHp.Value -= damage;
        CurrentHp.Value = Mathf.Max(0, CurrentHp.Value);

    }

    //魔力を使用した場合減らす
    public void UseMagic(int mpCost)
    {
        CurrentMp.Value = Mathf.Max(0, CurrentMp.Value - mpCost);
    }

    //カネをためる
    public void AddGold(int amount)
    {
        Gold.Value += amount;
    }

}
