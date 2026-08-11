using System;
using UnityEngine;

/// <summary>
/// AnimationEventからの通知を受け取ります。
/// </summary>
[DisallowMultipleComponent]
public sealed class AnimationEventReceiver : MonoBehaviour
{
    /// <summary>
    /// 攻撃アニメーションイベント受信時に通知されます。
    /// </summary>
    public event Action<AttackEventData> AttackEventReceived;

    /// <summary>
    /// 攻撃アニメーションイベントを通知します。
    /// </summary>
    /// <param name="attackEventData">攻撃アニメーションイベントの情報。</param>
    public void NotifyAttackEvent(AttackEventData attackEventData)
    {
        if (attackEventData == null)
        {
            Debug.LogWarning(
                $"{nameof(AttackEventData)}が設定されていません。",
                this);

            return;
        }

        AttackEventReceived?.Invoke(attackEventData);
    }
}