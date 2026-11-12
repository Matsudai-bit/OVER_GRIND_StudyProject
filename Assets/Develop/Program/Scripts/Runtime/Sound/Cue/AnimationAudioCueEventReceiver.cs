using UnityEngine;

/// <summary>
/// Animation Eventの数値引数を音声演出IDとしてAudioEmitterへ転送します。
/// </summary>
[DisallowMultipleComponent]
public sealed class AnimationAudioCueEventReceiver : MonoBehaviour
{
    [SerializeField, Header("音声演出の発生元（省略時は親から検索）")]
    private AudioEmitter m_audioEmitter;

    /// <summary>
    /// Animation EventのInt Parameterに指定したAudioCueIdを再生します。
    /// </summary>
    /// <param name="animationEvent">Int Parameterを持つAnimation Event。</param>
    public void OnAnimationAudioCuePlay(AnimationEvent animationEvent)
    {
        if (animationEvent != null && TryGetEmitter(out AudioEmitter emitter))
        {
            emitter.PlayCue(animationEvent.intParameter);
        }
    }

    /// <summary>
    /// Animation EventのInt ParameterのシーケンスをENDへ移行します。
    /// 単発音声は即停止します。
    /// </summary>
    /// <param name="animationEvent">Int Parameterを持つAnimation Event。</param>
    public void OnAnimationAudioCueStop(AnimationEvent animationEvent)
    {
        if (animationEvent != null && TryGetEmitter(out AudioEmitter emitter))
        {
            emitter.StopCue(animationEvent.intParameter);
        }
    }

    /// <summary>
    /// Animation EventのInt Parameterの音声を即停止します。
    /// </summary>
    /// <param name="animationEvent">Int Parameterを持つAnimation Event。</param>
    public void OnAnimationAudioCueStopImmediate(AnimationEvent animationEvent)
    {
        if (animationEvent != null && TryGetEmitter(out AudioEmitter emitter))
        {
            emitter.StopCueImmediate(animationEvent.intParameter);
        }
    }

    /// <summary>同じAnimator階層に属するAudioEmitterを取得します。</summary>
    private bool TryGetEmitter(out AudioEmitter emitter)
    {
        if (m_audioEmitter == null)
        {
            m_audioEmitter = GetComponentInParent<AudioEmitter>(true);
        }

        emitter = m_audioEmitter;
        if (emitter != null) return true;

        Debug.LogWarning("[AnimationAudioCueEventReceiver] AudioEmitterが見つかりません。", this);
        return false;
    }
}
