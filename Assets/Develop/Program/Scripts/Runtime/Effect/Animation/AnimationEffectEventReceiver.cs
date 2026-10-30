using UnityEngine;

/// <summary>
/// AnimatorのAnimation Eventを共通のエフェクト制御クラスへ転送します。
/// </summary>
[DisallowMultipleComponent]
public sealed class AnimationEffectEventReceiver : MonoBehaviour
{
    [SerializeField, Header("親オブジェクトのエフェクト制御クラス（未指定時は自動取得）")]
    private AnimationEffectController m_effectController;

    /// <summary>
    /// Animation Eventのエフェクト再生要求を受け取ります。
    /// </summary>
    /// <param name="animationEvent">ObjectにCue、Intにチャンネル番号を設定します。</param>
    public void OnAnimationEffectPlay(AnimationEvent animationEvent)
    {
        if (!TryGetCue(animationEvent, out EffectAnimationCue cue) || !TryGetController())
        {
            return;
        }

        m_effectController.Play(cue, animationEvent.intParameter);
    }

    /// <summary>
    /// Animation Eventのエフェクト停止要求を受け取ります。
    /// </summary>
    /// <param name="animationEvent">Objectに再生時と同じCue、Intに同じチャンネルを設定します。</param>
    public void OnAnimationEffectStop(AnimationEvent animationEvent)
    {
        if (!TryGetCue(animationEvent, out EffectAnimationCue cue) || !TryGetController())
        {
            return;
        }

        m_effectController.Stop(cue, animationEvent.intParameter);
    }

    /// <summary>
    /// 親オブジェクトからエフェクト制御クラスを検索します。
    /// </summary>
    private bool TryGetController()
    {
        if (m_effectController == null)
        {
            // Animatorが子のModelにある場合でも、PlayerRoot/BossRoot側を参照できます。
            m_effectController = GetComponentInParent<AnimationEffectController>(true);
        }

        if (m_effectController != null)
        {
            return true;
        }

        Debug.LogWarning("[AnimationEffectEventReceiver] AnimationEffectControllerがありません。", this);
        return false;
    }

    /// <summary>
    /// Animation Eventに指定されたCueを取得します。
    /// </summary>
    private bool TryGetCue(AnimationEvent animationEvent, out EffectAnimationCue cue)
    {
        cue = animationEvent != null
            ? animationEvent.objectReferenceParameter as EffectAnimationCue
            : null;

        if (cue != null)
        {
            return true;
        }

        Debug.LogWarning("[AnimationEffectEventReceiver] Object ParameterにEffectAnimationCueを設定してください。", this);
        return false;
    }
}
