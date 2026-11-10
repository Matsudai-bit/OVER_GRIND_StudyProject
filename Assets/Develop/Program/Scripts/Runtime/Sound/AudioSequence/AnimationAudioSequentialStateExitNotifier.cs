using UnityEngine;

/// <summary>
/// Animator State終了時に該当チャンネルの連続音声を自動停止します。
/// </summary>
public sealed class AnimationAudioSequentialStateExitNotifier : StateMachineBehaviour
{
    [SerializeField, Header("自動終了させるAnimation Eventチャンネル")]
    private int m_channel = 0;

    /// <summary>
    /// State遷移・CrossFade・中断時に終了を通知します。
    /// </summary>
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator == null)
        {
            return;
        }

        AnimationAudioSequentialEventReceiver receiver =
            animator.GetComponent<AnimationAudioSequentialEventReceiver>();
        if (receiver != null)
        {
            receiver.NotifyAnimationEnded(m_channel);
        }
    }
}
