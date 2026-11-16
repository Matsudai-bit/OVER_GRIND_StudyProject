/// <summary>ブーストゲージを保持した地上攻撃の演出を管理します。</summary>
public sealed class PlayerChargeAttack : PlayerGroundAttack
{
    /// <summary>チャージ攻撃音を取得します。</summary>
    public override PlayerAudioActionID AudioActionID =>
        PlayerAudioActionUtility.GetAttackActionID(true);

    /// <summary>チャージ攻撃の命中エフェクトを取得します。</summary>
    public override EffectId HitEffectId => EffectId.PLAYER_ATTACK_VBOOST_NORMAL_L;

    /// <summary>チャージ攻撃アニメーションを開始します。</summary>
    /// <param name="owner">攻撃するプレイヤー。</param>
    public override void PlayAnimation(PlayerStateMachineComponent owner)
    {
        owner.AnimationPresenter.PlayChargeAttackingAnimation();
    }
}
