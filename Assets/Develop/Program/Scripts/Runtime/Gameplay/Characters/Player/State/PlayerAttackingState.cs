/// <summary>通常の地上攻撃状態を管理します。</summary>
public sealed class PlayerAttackingState : PlayerGroundAttackingState
{
    /// <summary>通常攻撃の演出を生成します。</summary>
    protected override PlayerGroundAttack CreateAttackPresentation()
    {
        return new PlayerNormalAttack();
    }
}
