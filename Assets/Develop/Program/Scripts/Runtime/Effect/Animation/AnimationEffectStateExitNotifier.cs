using UnityEngine;

/// <summary>
/// Animator Stateが終了した際に指定チャンネルの自動停止対象を終了します。
/// </summary>
public sealed class AnimationEffectStateExitNotifier : StateMachineBehaviour
{
    [SerializeField, Header("終了させるAnimation Eventチャンネル")]
    private int m_channel = 0;

    /// <summary>
    /// ステート遷移による終了やCrossFade時に通知します。
    /// </summary>
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator == null)
        {
            return;
        }

        AnimationEffectEventReceiver receiver = animator.GetComponent<AnimationEffectEventReceiver>();
        if (receiver != null)
        {
            receiver.NotifyAnimationEnded(m_channel);
        }
    }
}
