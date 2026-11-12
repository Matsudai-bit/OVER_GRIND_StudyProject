using System;
using UnityEngine;

/// <summary>
/// Animation Eventのオブジェクト参照から音声演出IDを取得し、AudioEmitterへ転送します。
/// </summary>
[DisallowMultipleComponent]
public sealed class AnimationAudioCueEventReceiver : MonoBehaviour
{
    // 対象キャラクターが使用する音声発生コンポーネント。
    [SerializeField, Header("音声演出の発生元（省略時は親から検索）")]
    private AudioEmitter m_audioEmitter;

    // 既存のInt Parameterを使用するAnimation Eventの移行用設定。
    [SerializeField, Header("旧Int Parameter方式を許可（移行用）")]
    private bool m_allowLegacyIntParameter = true;

    // 旧方式を使っている警告を、同じReceiverでは一度だけ表示します。
    private bool m_hasLoggedLegacyWarning;

    /// <summary>
    /// Object Parameterに指定した音声演出を再生します。
    /// </summary>
    /// <param name="animationEvent">Animation Eventのパラメーター。</param>
    public void OnAnimationAudioCuePlay(AnimationEvent animationEvent)
    {
        if (!TryGetCueId(animationEvent, out AudioCueId cueId) ||
            !TryGetEmitter(out AudioEmitter emitter))
        {
            return;
        }

        // 単発SEとシーケンスはAudioEmitter側で振り分けます。
        emitter.TryPlayCue(cueId, out _, out _);
    }

    /// <summary>
    /// 指定した音声演出を通常停止します。
    /// シーケンスはENDへ移行し、単発SEは即時停止します。
    /// </summary>
    /// <param name="animationEvent">Animation Eventのパラメーター。</param>
    public void OnAnimationAudioCueStop(AnimationEvent animationEvent)
    {
        if (!TryGetCueId(animationEvent, out AudioCueId cueId) ||
            !TryGetEmitter(out AudioEmitter emitter))
        {
            return;
        }

        emitter.StopCue(cueId);
    }

    /// <summary>
    /// 指定した音声演出をENDなしで即時停止します。
    /// </summary>
    /// <param name="animationEvent">Animation Eventのパラメーター。</param>
    public void OnAnimationAudioCueStopImmediate(AnimationEvent animationEvent)
    {
        if (!TryGetCueId(animationEvent, out AudioCueId cueId) ||
            !TryGetEmitter(out AudioEmitter emitter))
        {
            return;
        }

        emitter.StopCueImmediate(cueId);
    }

    /// <summary>
    /// Animation Eventから、参照アセットまたは旧整数IDを取得します。
    /// </summary>
    /// <param name="animationEvent">再生中のAnimation Event。</param>
    /// <param name="cueId">取得した音声演出ID。</param>
    /// <returns>true：有効なIDを取得。false：取得失敗。</returns>
    private bool TryGetCueId(AnimationEvent animationEvent, out AudioCueId cueId)
    {
        cueId = AudioCueId.NONE;
        if (animationEvent == null)
        {
            Debug.LogWarning("[AnimationAudioCueEventReceiver] Animation Eventがnullです。", this);
            return false;
        }

        UnityEngine.Object parameter = animationEvent.objectReferenceParameter;

        if (parameter is AudioCueReferenceAsset reference)
        {
            // 新方式：Asset内のenumを参照します。
            cueId = reference.CueId;
        }
        else if (parameter != null)
        {
            // 不正なObject Parameterを旧Int Parameterとして解釈しません。
            Debug.LogWarning(
                $"[AnimationAudioCueEventReceiver] AudioCueReferenceAsset以外が指定されています: {parameter.name}",
                this);
            return false;
        }
        else if (m_allowLegacyIntParameter)
        {
            // 旧方式：既存のアニメーションを壊さないための移行措置です。
            int value = animationEvent.intParameter;
            if (value == (int)AudioCueId.NONE || !Enum.IsDefined(typeof(AudioCueId), value))
            {
                Debug.LogWarning(
                    $"[AnimationAudioCueEventReceiver] 参照Assetが未設定、または旧IDが無効です: {value}",
                    this);
                return false;
            }

            cueId = (AudioCueId)value;
            if (!m_hasLoggedLegacyWarning)
            {
                m_hasLoggedLegacyWarning = true;
                Debug.LogWarning(
                    "[AnimationAudioCueEventReceiver] 旧Int Parameter方式が使用されています。Object Parameterへ移行してください。",
                    this);
            }
        }
        else
        {
            Debug.LogWarning(
                "[AnimationAudioCueEventReceiver] Object ParameterにAudioCueReferenceAssetを設定してください。",
                this);
            return false;
        }

        if (cueId == AudioCueId.NONE || !Enum.IsDefined(typeof(AudioCueId), cueId))
        {
            Debug.LogWarning($"[AnimationAudioCueEventReceiver] 無効なAudioCueIdです: {cueId}", this);
            return false;
        }

        return true;
    }

    /// <summary>
    /// 自身または親階層からAudioEmitterを取得します。
    /// </summary>
    /// <param name="emitter">取得したAudioEmitter。</param>
    /// <returns>true：取得成功。false：取得失敗。</returns>
    private bool TryGetEmitter(out AudioEmitter emitter)
    {
        if (m_audioEmitter == null)
        {
            m_audioEmitter = GetComponentInParent<AudioEmitter>(true);
        }

        emitter = m_audioEmitter;
        if (emitter != null)
        {
            return true;
        }

        Debug.LogWarning("[AnimationAudioCueEventReceiver] AudioEmitterが見つかりません。", this);
        return false;
    }
}
