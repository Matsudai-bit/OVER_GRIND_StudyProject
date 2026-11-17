/// @ using :: システムとエンジンの使用
using System;
using System.Collections.Generic;
using UnityEngine;

/// @ enumName :: サウンドのカテゴリ定義（インゲーム/アウトゲーム制御やMixer設定に使用）
public enum SoundCategory
{
    BGM,
    SE_InGame,  // バトル中などに鳴るSE（ポーズやシーン遷移等で止める対象）
    SE_System,  // UIなど常に鳴るSE
    Voice
}


/// <summary>
/// AudioIDに対応する音声データと3D距離減衰設定を管理します。
/// </summary>
[CreateAssetMenu(fileName = "SoundDatabase", menuName = "Audio/Sound Database")]
public sealed class SoundDatabase : ScriptableObject
{
    /// <summary>
    /// 音声ごとの再生設定です。
    /// </summary>
    [Serializable]
    public sealed class SoundData
    {
        private const float DEFAULT_MIN_DISTANCE = 1.0f;
        private const float DEFAULT_MAX_DISTANCE = 30.0f;

        [Header("音声の識別とリソース")]
        public AudioID m_id;
        public AudioClip m_clip;
        public SoundCategory m_category;

        [Header("基本再生設定")]
        [Range(0.0f, 1.0f)] public float m_volume = 1.0f;
        [Range(0.1f, 3.0f)] public float m_pitch = 1.0f;
        public bool m_loop = false;
        public bool m_is3D = false;

        [Header("3D音声の距離減衰")]
        [Min(0.01f)] public float m_minDistance = DEFAULT_MIN_DISTANCE;
        [Min(0.02f)] public float m_maxDistance = DEFAULT_MAX_DISTANCE;
        public AudioRolloffMode m_rolloffMode = AudioRolloffMode.Logarithmic;
        // Customの横軸は距離の正規化値(0～1)、縦軸は音量倍率です。
        public AnimationCurve m_customRolloffCurve = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 0.0f);
    }

    // ID登録ツールからも編集される音声リスト。既存のシリアライズ名を維持します。
    [SerializeField, Header("音声一覧")]
    public List<SoundData> m_soundList = new List<SoundData>();

    /// <summary>
    /// 指定IDの音声情報を取得します。
    /// </summary>
    /// <param name="id">検索対象の音声ID。</param>
    /// <returns>音声設定。見つからない場合はnull。</returns>
    public SoundData GetSoundData(AudioID id)
    {
        if (m_soundList == null)
        {
            return null;
        }

        foreach (SoundData data in m_soundList)
        {
            if (data != null && data.m_id == id)
            {
                return data;
            }
        }

        return null;
    }
}
