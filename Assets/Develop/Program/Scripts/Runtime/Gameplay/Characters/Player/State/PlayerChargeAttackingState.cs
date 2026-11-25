/// <summary>ブーストゲージを保持した地上チャージ攻撃状態を管理します。</summary>
public sealed class PlayerChargeAttackingState : PlayerGroundAttackingState
{
    /// <summary>チャージ攻撃の演出を生成します。</summary>
    protected override PlayerGroundAttack CreateAttackPresentation()
    {
        return new PlayerChargeAttack();
    }
}
