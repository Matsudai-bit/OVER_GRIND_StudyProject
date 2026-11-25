using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// オブジェクトに付けて、そのオブジェクトからサウンドを鳴らすコンポーネントです。
/// 複数のサウンドをスロットとして登録し、再生中でも即座に切り替えられます。
/// </summary>
[AddComponentMenu("Sound/Sound Emitter")]
public sealed class SoundEmitter : MonoBehaviour
{
    // 無効なスロット番号
    private const int INVALID_INDEX = -1;

    [Header("サウンドスロット（切り替え候補）")]
    // 切り替え候補のサウンド一覧
    [SerializeField] private List<SoundEmitterSlot> m_slots = new List<SoundEmitterSlot>();

    // 現在選択しているスロット番号
    [SerializeField] private int m_currentIndex = 0;

    [Header("エミッター音量（このオブジェクト全体の倍率）")]
    // エミッター全体の音量倍率
    [SerializeField, Range(0.0f, 1.0f)] private float m_emitterVolume = 1.0f;

    [Header("発生位置")]
    // 発音位置の基準（未設定時はこのオブジェクト）
    [SerializeField] private Transform m_emitPoint;

    // 3D 再生中に発音位置へ追従するか（false なら再生した瞬間の位置に固定）
    [SerializeField] private bool m_isFollowEmitPoint = true;

    [Header("再生・停止のタイミング")]
    // 有効化されたときに再生するか
    [SerializeField] private bool m_isPlayOnEnable = false;

    // 無効化されたときに停止するか
    [SerializeField] private bool m_isStopOnDisable = true;

    [Header("フェード（秒）")]
    // 停止状態から再生するときのフェードイン秒数
    [SerializeField, Min(0.0f)] private float m_fadeInSeconds = 0.0f;

    // 停止するときのフェードアウト秒数
    [SerializeField, Min(0.0f)] private float m_fadeOutSeconds = 0.0f;

    // 再生中に別スロットへ切り替えるときのクロスフェード秒数
    [SerializeField, Min(0.0f)] private float m_switchFadeSeconds = 0.0f;

    // 現在再生中のハンドル ID
    private int m_currentHandle = SoundHub.INVALID_HANDLE;

    // 再生中のスロット番号（スロット外の ID を直接鳴らした場合は無効）
    private int m_playingIndex = INVALID_INDEX;

    // 再生中のスロット音量倍率
    private float m_currentSlotVolume = 1.0f;

    /// <summary>
    /// 現在選択しているスロット番号を取得します。
    /// </summary>
    public int CurrentIndex => m_currentIndex;

    /// <summary>
    /// スロット数を取得します。
    /// </summary>
    public int SlotCount => m_slots.Count;

    /// <summary>
    /// 現在再生中のハンドル ID を取得します。
    /// </summary>
    public int CurrentHandle => m_currentHandle;

    /// <summary>
    /// エミッター全体の音量倍率（0～1）を取得または設定します。再生中の音にも反映されます。
    /// </summary>
    public float EmitterVolume
    {
        get => m_emitterVolume;
        set
        {
            m_emitterVolume = Mathf.Clamp01(value);
            ApplyVolumeToPlaying();
        }
    }

    private void OnEnable()
    {
        if (m_isPlayOnEnable)
        {
            Play();
        }
    }

    private void OnDisable()
    {
        if (!m_isStopOnDisable || m_currentHandle == SoundHub.INVALID_HANDLE)
        {
            return;
        }

        // 終了処理中に SoundHub を生成しないよう、存在する場合だけ停止する
        SoundHub hub = SoundHub.InstanceIfExists;

        if (hub != null)
        {
            hub.Stop(m_currentHandle, m_fadeOutSeconds);
        }

        m_currentHandle = SoundHub.INVALID_HANDLE;
    }

    private void OnValidate()
    {
        // 選択番号を範囲内に収める
        m_currentIndex = Mathf.Clamp(m_currentIndex, 0, Mathf.Max(0, m_slots.Count - 1));

        // 再生中に Inspector で音量を変えたら、すぐに反映する
        if (Application.isPlaying)
        {
            if (m_playingIndex >= 0 && m_playingIndex < m_slots.Count)
            {
                m_currentSlotVolume = m_slots[m_playingIndex].VolumeScale;
            }

            ApplyVolumeToPlaying();
        }
    }

    /// <summary>
    /// 現在選択しているスロットを再生します。
    /// </summary>
    public void Play()
    {
        PlaySlot(m_currentIndex);
    }

    /// <summary>
    /// 指定したスロットを再生します。再生中なら即座に切り替えます。
    /// </summary>
    /// <param name="index">スロット番号。</param>
    public void PlaySlot(int index)
    {
        if (!IsValidIndex(index))
        {
            Debug.LogWarning($"[SoundEmitter] スロット番号 {index} は存在しません。", this);
            return;
        }

        m_currentIndex = index;
        m_playingIndex = index;

        SoundEmitterSlot slot = m_slots[index];
        StartSound(slot.SoundId, slot.VolumeScale, slot.PitchScale, slot.SpatialMode);
    }

    /// <summary>
    /// ラベル名でスロットを指定して再生します。
    /// </summary>
    /// <param name="label">スロットのラベル。</param>
    public void PlayLabel(string label)
    {
        int index = FindSlotIndex(label);

        if (index == INVALID_INDEX)
        {
            Debug.LogWarning($"[SoundEmitter] ラベル「{label}」のスロットがありません。", this);
            return;
        }

        PlaySlot(index);
    }

    /// <summary>
    /// スロットに登録していないサウンドを、このオブジェクトから直接再生します。
    /// </summary>
    /// <param name="id">再生するサウンド ID。</param>
    public void PlaySound(AudioID id)
    {
        m_playingIndex = INVALID_INDEX;

        StartSound(id, 1.0f, 1.0f, SoundSpatialMode.USE_DATABASE);
    }

    /// <summary>
    /// スロットを選択します（再生はしません）。
    /// </summary>
    /// <param name="index">スロット番号。</param>
    public void SelectSlot(int index)
    {
        if (IsValidIndex(index))
        {
            m_currentIndex = index;
        }
    }

    /// <summary>
    /// 再生中なら指定スロットへ即座に切り替え、停止中なら選択だけ行います。
    /// </summary>
    /// <param name="index">スロット番号。</param>
    public void SwitchSlot(int index)
    {
        if (IsPlaying())
        {
            PlaySlot(index);
        }
        else
        {
            SelectSlot(index);
        }
    }

    /// <summary>
    /// 次のスロットへ切り替えます（最後の次は最初に戻ります）。
    /// </summary>
    public void SwitchNext()
    {
        if (m_slots.Count == 0)
        {
            return;
        }

        SwitchSlot((m_currentIndex + 1) % m_slots.Count);
    }

    /// <summary>
    /// 前のスロットへ切り替えます（最初の前は最後に戻ります）。
    /// </summary>
    public void SwitchPrevious()
    {
        if (m_slots.Count == 0)
        {
            return;
        }

        SwitchSlot((m_currentIndex - 1 + m_slots.Count) % m_slots.Count);
    }

    /// <summary>
    /// 再生を停止します。設定したフェードアウト秒数を使います。
    /// </summary>
    public void Stop()
    {
        StopWithFade(m_fadeOutSeconds);
    }

    /// <summary>
    /// 再生を即座に停止します。
    /// </summary>
    public void StopImmediately()
    {
        StopWithFade(0.0f);
    }

    /// <summary>
    /// 指定した秒数でフェードアウトして停止します。
    /// </summary>
    /// <param name="fadeOutSeconds">フェードアウト秒数。</param>
    public void StopWithFade(float fadeOutSeconds)
    {
        if (m_currentHandle == SoundHub.INVALID_HANDLE)
        {
            return;
        }

        SoundHub hub = SoundHub.InstanceIfExists;

        if (hub != null)
        {
            hub.Stop(m_currentHandle, fadeOutSeconds);
        }

        m_currentHandle = SoundHub.INVALID_HANDLE;
    }

    /// <summary>
    /// エミッター全体の音量倍率を設定します（UnityEvent 用）。
    /// </summary>
    /// <param name="volume">音量倍率（0～1）。</param>
    public void SetEmitterVolume(float volume)
    {
        EmitterVolume = volume;
    }

    /// <summary>
    /// 再生中かを確認します。
    /// </summary>
    /// <returns>
    /// true：再生中です。
    /// false：再生していません。
    /// </returns>
    public bool IsPlaying()
    {
        if (m_currentHandle == SoundHub.INVALID_HANDLE)
        {
            return false;
        }

        SoundHub hub = SoundHub.InstanceIfExists;

        return hub != null && hub.IsPlaying(m_currentHandle);
    }

    /// <summary>
    /// 指定番号のスロットを取得します。
    /// </summary>
    /// <param name="index">スロット番号。</param>
    /// <returns>スロット。範囲外の場合は null。</returns>
    public SoundEmitterSlot GetSlot(int index)
    {
        return IsValidIndex(index) ? m_slots[index] : null;
    }

    /// <summary>
    /// サウンドを再生します。再生中なら現在の音を止めて切り替えます。
    /// </summary>
    /// <param name="id">再生するサウンド ID。</param>
    /// <param name="slotVolume">スロットの音量倍率。</param>
    /// <param name="slotPitch">スロットのピッチ倍率。</param>
    /// <param name="spatialMode">2D / 3D の指定。</param>
    private void StartSound(AudioID id, float slotVolume, float slotPitch, SoundSpatialMode spatialMode)
    {
        SoundHub hub = SoundHub.Instance;

        if (hub == null || id == AudioID.None)
        {
            return;
        }

        // 再生中なら切り替え用のクロスフェード、停止中ならフェードインを使う
        bool wasPlaying = IsPlaying();
        float fadeSeconds = wasPlaying ? m_switchFadeSeconds : m_fadeInSeconds;

        if (wasPlaying)
        {
            hub.Stop(m_currentHandle, m_switchFadeSeconds);
        }

        // 発音位置を決めて再生する
        float volumeScale = slotVolume * m_emitterVolume;
        Transform point = m_emitPoint != null ? m_emitPoint : transform;

        if (m_isFollowEmitPoint)
        {
            m_currentHandle = hub.Play(id, point, volumeScale, slotPitch, fadeSeconds, spatialMode);
        }
        else
        {
            m_currentHandle = hub.PlayAt(id, point.position, volumeScale, slotPitch, fadeSeconds, spatialMode);
        }

        m_currentSlotVolume = slotVolume;
    }

    /// <summary>
    /// 再生中の音へ、最新の音量倍率を反映します。
    /// </summary>
    private void ApplyVolumeToPlaying()
    {
        if (m_currentHandle == SoundHub.INVALID_HANDLE)
        {
            return;
        }

        SoundHub hub = SoundHub.InstanceIfExists;

        if (hub != null)
        {
            hub.SetVolumeScale(m_currentHandle, m_currentSlotVolume * m_emitterVolume);
        }
    }

    /// <summary>
    /// スロット番号が有効かを確認します。
    /// </summary>
    /// <param name="index">スロット番号。</param>
    /// <returns>
    /// true：有効です。
    /// false：範囲外です。
    /// </returns>
    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < m_slots.Count;
    }

    /// <summary>
    /// ラベル名からスロット番号を検索します。
    /// </summary>
    /// <param name="label">スロットのラベル。</param>
    /// <returns>スロット番号。見つからない場合は INVALID_INDEX。</returns>
    private int FindSlotIndex(string label)
    {
        for (int i = 0; i < m_slots.Count; i++)
        {
            if (string.Equals(m_slots[i].Label, label, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return INVALID_INDEX;
    }
}