using System;
using UnityEngine;

/// <summary>
/// 音声演出IDと音声Asset、発生アンカーを対応付けます。
/// </summary>
[Serializable]
public sealed class AudioCueEntry
{
    // エフェクトIDではなく音声演出IDを登録します。
    [SerializeField, Header("音声演出ID")]
    private AudioCueId m_cueId;

    // 単発かシーケンスかを選択します。
    [SerializeField, Header("音声の種類")]
    private AudioCuePlaybackType m_playbackType;

    // 選択した種類に対応するAssetだけを使用します。
    [SerializeField, Header("シーケンス音声（SEQUENTIAL時）")]
    private AudioSequentialAsset m_sequentialAsset;

    [SerializeField, Header("単発音声（ONE_SHOT時）")]
    private AudioOneShotAsset m_oneShotAsset;

    // 2D再生の場合は使用しません。
    [SerializeField, Header("既定の発生アンカー")]
    private AudioAnchorId m_anchorId = AudioAnchorId.ROOT;

    /// <summary>音声演出ID。</summary>
    public AudioCueId CueId => m_cueId;

    /// <summary>音声の種類。</summary>
    public AudioCuePlaybackType PlaybackType => m_playbackType;

    /// <summary>連続音声のAsset。</summary>
    public AudioSequentialAsset SequentialAsset => m_sequentialAsset;

    /// <summary>単発音声のAsset。</summary>
    public AudioOneShotAsset OneShotAsset => m_oneShotAsset;

    /// <summary>既定の発生アンカー。</summary>
    public AudioAnchorId AnchorId => m_anchorId;
}
