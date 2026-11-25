using System;
using UnityEngine;

/// <summary>
/// SoundEmitter が持つ、切り替え候補のサウンド1件分の設定です。
/// </summary>
[Serializable]
public sealed class SoundEmitterSlot
{
    // スロットの表示名（切り替え時に名前で指定できる）
    [SerializeField] private string m_label = "";

    // 再生するサウンド ID
    [SerializeField] private AudioID m_soundId = AudioID.None;

    // このスロットの音量倍率（DB 音量に掛ける）
    [SerializeField, Range(0.0f, 1.0f)] private float m_volumeScale = 1.0f;

    // このスロットのピッチ倍率（DB ピッチに掛ける）
    [SerializeField, Range(0.5f, 2.0f)] private float m_pitchScale = 1.0f;

    // 2D / 3D の指定
    [SerializeField] private SoundSpatialMode m_spatialMode = SoundSpatialMode.USE_DATABASE;

    /// <summary>
    /// スロットの表示名を取得します。
    /// </summary>
    public string Label => m_label;

    /// <summary>
    /// 再生するサウンド ID を取得します。
    /// </summary>
    public AudioID SoundId => m_soundId;

    /// <summary>
    /// 音量倍率（0～1）を取得します。
    /// </summary>
    public float VolumeScale => m_volumeScale;

    /// <summary>
    /// ピッチ倍率を取得します。
    /// </summary>
    public float PitchScale => m_pitchScale;

    /// <summary>
    /// 2D / 3D の指定を取得します。
    /// </summary>
    public SoundSpatialMode SpatialMode => m_spatialMode;

    /// <summary>
    /// 表示名を取得します。未設定の場合はサウンド ID の名前を返します。
    /// </summary>
    public string DisplayName => string.IsNullOrEmpty(m_label) ? m_soundId.ToString() : m_label;
}