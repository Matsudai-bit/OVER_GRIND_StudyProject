using UnityEngine;

/// <summary>
/// Animation Eventを音声セット管理クラスへ転送します。
/// </summary>
[DisallowMultipleComponent]
public sealed class AnimationAudioSequentialEventReceiver : MonoBehaviour
{
    [SerializeField, Header("親オブジェクトの音声セット管理者（未指定時は自動取得）")]
    private AnimationAudioSequentialController m_audioController;

    /// <summary>
    /// 音声セットの再生要求を受け取ります。
    /// ObjectにAudioSequentialAsset、Intにチャンネルを指定します。
    /// </summary>
    public void OnAnimationAudioSequentialPlay(AnimationEvent animationEvent)
    {
        if (TryGetAsset(animationEvent, out AudioSequentialAsset asset) && TryGetController())
        {
            m_audioController.Play(asset, animationEvent.intParameter);
        }
    }

    /// <summary>
    /// 同じAssetとチャンネルの音声セットを終了音へ遷移させます。
    /// </summary>
    public void OnAnimationAudioSequentialStop(AnimationEvent animationEvent)
    {
        if (TryGetAsset(animationEvent, out AudioSequentialAsset asset) && TryGetController())
        {
            m_audioController.Stop(asset, animationEvent.intParameter);
        }
    }

    /// <summary>
    /// Animation Eventから単発音声を再生します。
    /// Object ParameterにAudioOneShotAssetを設定してください。
    /// </summary>
    public void OnAnimationAudioOneShot(AnimationEvent animationEvent)
    {
        AudioOneShotAsset asset = animationEvent != null
            ? animationEvent.objectReferenceParameter as AudioOneShotAsset
            : null;

        if (asset == null)
        {
            Debug.LogWarning("[AnimationAudioSequentialEventReceiver] 単発音声Assetがありません。", this);
            return;
        }

        if (TryGetController())
        {
            m_audioController.PlayOneShot(asset);
        }
    }

    /// <summary>
    /// AnimationClipの終了地点に配置したイベントを受け取ります。
    /// </summary>
    public void OnAnimationAudioSequentialEnd(AnimationEvent animationEvent)
    {
        if (animationEvent != null)
        {
            NotifyAnimationEnded(animationEvent.intParameter);
        }
    }

    /// <summary>
    /// Animator State終了時に自動停止対象を停止します。
    /// </summary>
    public void NotifyAnimationEnded(int channel)
    {
        if (TryGetController())
        {
            m_audioController.StopOnAnimationEnd(channel);
        }
    }

    /// <summary>
    /// 親オブジェクトから管理者を探します。
    /// </summary>
    private bool TryGetController()
    {
        if (m_audioController == null)
        {
            m_audioController = GetComponentInParent<AnimationAudioSequentialController>(true);
        }

        if (m_audioController != null)
        {
            return true;
        }

        Debug.LogWarning("[AnimationAudioSequentialEventReceiver] AnimationAudioSequentialControllerがありません。", this);
        return false;
    }

    /// <summary>
    /// Animation EventのObject ParameterからAssetを取得します。
    /// </summary>
    private bool TryGetAsset(AnimationEvent animationEvent, out AudioSequentialAsset asset)
    {
        asset = animationEvent != null
            ? animationEvent.objectReferenceParameter as AudioSequentialAsset
            : null;

        if (asset != null)
        {
            return true;
        }

        Debug.LogWarning("[AnimationAudioSequentialEventReceiver] Object ParameterにAudioSequentialAssetを指定してください。", this);
        return false;
    }
}
