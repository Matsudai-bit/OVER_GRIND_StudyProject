/// <summary>通常地上攻撃の演出を管理します。</summary>
public sealed class PlayerNormalAttack : PlayerGroundAttack
{
    /// <summary>通常攻撃音を取得します。</summary>
    public override PlayerAudioActionID AudioActionID =>
        PlayerAudioActionUtility.GetAttackActionID(false);

    /// <summary>通常攻撃の命中エフェクトを取得します。</summary>
    public override EffectId HitEffectId => EffectId.PLAYER_ATTACK_NORMAL_L;

    /// <summary>通常攻撃アニメーションを開始します。</summary>
    /// <param name="owner">攻撃するプレイヤー。</param>
    public override void PlayAnimation(PlayerStateMachineComponent owner)
    {
        owner.AnimationPresenter.PlayAttackAnimation();
    }
}
