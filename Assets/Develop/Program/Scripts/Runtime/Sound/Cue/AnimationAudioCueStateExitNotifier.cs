using UnityEngine;

/// <summary>
/// Animator Stateの終了時に、指定チャンネルの音声停止を通知します。
/// </summary>
public sealed class AnimationAudioCueStateExitNotifier : StateMachineBehaviour
{
    // Animation EventのInt Parameterで指定したチャンネルと対応させます。
    [SerializeField, Header("終了通知する音声チャンネル")]
    private int m_channel = 0;

    /// <summary>
    /// Animator Stateの遷移完了・中断時に音声停止を通知します。
    /// </summary>
    /// <param name="animator">通知元のAnimator。</param>
    /// <param name="stateInfo">終了したState。</param>
    /// <param name="layerIndex">Animatorレイヤー。</param>
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator == null)
        {
            return;
        }

        // Animation EventはAnimatorと同じGameObject上のReceiverが受け取ります。
        AnimationAudioCueEventReceiver receiver =
            animator.GetComponent<AnimationAudioCueEventReceiver>();

        if (receiver != null)
        {
            receiver.NotifyAnimationStateExited(m_channel, stateInfo);
        }
    }
}
