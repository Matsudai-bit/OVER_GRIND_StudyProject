using System;
using UnityEngine;

/// <summary>
/// キャラクター共通のイベントを管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class CharacterEventHub : MonoBehaviour
{
    /// <summary>
    /// 攻撃開始時に通知されます。
    /// </summary>
    public event Action<CharacterAttackEventData> AttackStarted;

    /// <summary>
    /// 攻撃終了時に通知されます。
    /// </summary>
    public event Action<CharacterAttackEventData> AttackFinished;

    /// <summary>
    /// 攻撃演出タイミングで通知されます。
    /// </summary>
    public event Action<CharacterAttackCueEventData> AttackCue;

    /// <summary>
    /// 攻撃ヒット時に通知されます。
    /// </summary>
    public event Action<CharacterAttackHitEventData> AttackHit;

    /// <summary>
    /// 移動開始時に通知されます。
    /// </summary>
    public event Action<CharacterMoveEventData> MoveStarted;

    /// <summary>
    /// 移動終了時に通知されます。
    /// </summary>
    public event Action<CharacterMoveEventData> MoveStopped;

    /// <summary>
    /// ダメージを受けた時に通知されます。
    /// </summary>
    public event Action<CharacterDamageEventData> Damaged;

    /// <summary>
    /// 死亡時に通知されます。
    /// </summary>
    public event Action<CharacterDeathEventData> Died;

    /// <summary>
    /// 攻撃開始イベントを発行します。
    /// </summary>
    /// <param name="attackId">攻撃ID。</param>
    public void RaiseAttackStarted(AttackIdentifier attackId)
    {
        CharacterAttackEventData eventData =
            new CharacterAttackEventData(gameObject, attackId);

        AttackStarted?.Invoke(eventData);
    }

    /// <summary>
    /// 攻撃終了イベントを発行します。
    /// </summary>
    /// <param name="attackId">攻撃ID。</param>
    public void RaiseAttackFinished(AttackIdentifier attackId)
    {
        CharacterAttackEventData eventData =
            new CharacterAttackEventData(gameObject, attackId);

        AttackFinished?.Invoke(eventData);
    }

    /// <summary>
    /// 攻撃演出イベントを発行します。
    /// </summary>
    /// <param name="attackId">攻撃ID。</param>
    /// <param name="cueType">演出タイミング種別。</param>
    /// <param name="position">イベント発生位置。</param>
    public void RaiseAttackCue(
        AttackIdentifier attackId,
        CharacterAttackCueType cueType,
        Vector3 position)
    {
        CharacterAttackCueEventData eventData =
            new CharacterAttackCueEventData(
                gameObject,
                attackId,
                cueType,
                position);

        AttackCue?.Invoke(eventData);
    }

    /// <summary>
    /// 攻撃ヒットイベントを発行します。
    /// </summary>
    /// <param name="attackId">攻撃ID。</param>
    /// <param name="target">攻撃対象。</param>
    /// <param name="hitPosition">ヒット位置。</param>
    public void RaiseAttackHit(
        AttackIdentifier attackId,
        GameObject target,
        Vector3 hitPosition)
    {
        CharacterAttackHitEventData eventData =
            new CharacterAttackHitEventData(
                gameObject,
                attackId,
                target,
                hitPosition);

        AttackHit?.Invoke(eventData);
    }

    /// <summary>
    /// 移動開始イベントを発行します。
    /// </summary>
    /// <param name="direction">移動方向。</param>
    /// <param name="speed">移動速度。</param>
    public void RaiseMoveStarted(
        Vector3 direction,
        float speed)
    {
        CharacterMoveEventData eventData =
            new CharacterMoveEventData(
                gameObject,
                transform.position,
                direction,
                speed);

        MoveStarted?.Invoke(eventData);
    }

    /// <summary>
    /// 移動終了イベントを発行します。
    /// </summary>
    public void RaiseMoveStopped()
    {
        CharacterMoveEventData eventData =
            new CharacterMoveEventData(
                gameObject,
                transform.position,
                Vector3.zero,
                0.0f);

        MoveStopped?.Invoke(eventData);
    }

    /// <summary>
    /// ダメージイベントを発行します。
    /// </summary>
    /// <param name="attacker">攻撃元。</param>
    /// <param name="damage">ダメージ量。</param>
    /// <param name="hitPosition">ヒット位置。</param>
    public void RaiseDamaged(
        GameObject attacker,
        float damage,
        Vector3 hitPosition)
    {
        CharacterDamageEventData eventData =
            new CharacterDamageEventData(
                gameObject,
                attacker,
                damage,
                hitPosition);

        Damaged?.Invoke(eventData);
    }

    /// <summary>
    /// 死亡イベントを発行します。
    /// </summary>
    public void RaiseDied()
    {
        CharacterDeathEventData eventData =
            new CharacterDeathEventData(
                gameObject,
                transform.position);

        Died?.Invoke(eventData);
    }
}