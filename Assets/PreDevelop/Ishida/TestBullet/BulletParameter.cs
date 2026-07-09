using UniRx; 

public class BulletParameter
{

    public ReactiveProperty<int> Hp { get; }
    public int MaxHp { get; }
    public int AttackPower { get; }
    public int Speed { get; }

    public BulletParameter(int maxHp, int attack, int speed)
    //                      ‡@1”Ô–Ú   ‡A2”Ô–Ú   ‡B3”Ô–Ú
    {
        MaxHp = maxHp;
        Hp = new ReactiveProperty<int>(maxHp);
        AttackPower = attack;
        Speed = speed;
    }

}
