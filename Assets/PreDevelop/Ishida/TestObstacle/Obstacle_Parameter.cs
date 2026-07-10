using UniRx;


//’l‚ğ•Û‚·‚é‚¾‚¯‚ÌƒNƒ‰ƒX
public class Obstacle_Parameter
{
    public ReactiveProperty<int> Hp { get; }
    public int MaxHp { get; }
    public int Speed { get; }

    
    public Obstacle_Parameter(int maxHp,   int speed)
    {
        MaxHp = maxHp;
        Hp = new ReactiveProperty<int>(maxHp);
        Speed = speed;
    }
}
