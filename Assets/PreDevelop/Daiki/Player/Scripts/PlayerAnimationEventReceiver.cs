using System;
using UnityEngine;

/// <summary>
/// Animation Eventをゲームプレイ側へ通知します。
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerAnimationEventReceiver : MonoBehaviour
{
    /// <summary>
    /// 攻撃判定開始イベントです。
    /// </summary>
    public event Action<int> AttackHitboxStarted;

    /// <summary>
    /// 攻撃判定終了イベントです。
    /// </summary>
    public event Action<int> AttackHitboxEnded;

    /// <summary>
    /// 攻撃アニメーション終了イベントです。
    /// </summary>
    public event Action<int> AttackAnimationStarted;
    public event Action<int> AttackAnimationFinished;

    /// <summary>
    /// 攻撃判定の開始を通知します。
    /// </summary>
    /// <param name="comboStage">コンボ段階。</param>
    public void NotifyAttackHitboxStarted(int comboStage)
    {
        AttackHitboxStarted?.Invoke(comboStage);
    }

    /// <summary>
    /// 攻撃判定の終了を通知します。
    /// </summary>
    /// <param name="comboStage">コンボ段階。</param>
    public void NotifyAttackHitboxEnded(int comboStage)
    {
        AttackHitboxEnded?.Invoke(comboStage);
    }

    /// <summary>
    /// 攻撃アニメーションの開始を通知します。
    /// </summary>
    /// <param name="comboStage">コンボ段階。</param>
    public void NotifyAttackAnimationStarted(int comboStage)
    {
        AttackAnimationStarted?.Invoke(comboStage);
    }

    /// <summary>
    /// 攻撃アニメーションの終了を通知します。
    /// </summary>
    /// <param name="comboStage">コンボ段階。</param>
    public void NotifyAttackAnimationFinished(int comboStage)
    {
        AttackAnimationFinished?.Invoke(comboStage);
    }
}