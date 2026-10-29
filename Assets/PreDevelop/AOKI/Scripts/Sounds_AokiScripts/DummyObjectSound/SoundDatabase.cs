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

/// @ className :: サウンドデータ管理用データベース
/// @ name :: Aoki Hayate (Update: Category & 3D Distance added)
/// @ date :: 2026/10/28 
[CreateAssetMenu(fileName = "SoundDatabase", menuName = "Audio/Sound Database")]
public class SoundDatabase : ScriptableObject
{
    /// @ className :: サウンドの各設定データ
    [Serializable]
    public class SoundData
    {
        public AudioID m_id;             // 識別子
        public AudioClip m_clip;              // 音声ファイル
        public SoundCategory m_category;      // サウンドのカテゴリ

        [Range(0f, 1f)] public float m_volume = 1.0f; // 音量
        [Range(0.1f, 3f)] public float m_pitch = 1.0f; // ピッチ
        public bool m_loop = false;           // ループ再生するか

        public bool m_is3D = false;           // 3D音響か
        [Tooltip("3D再生時の音が届く最大距離")]
        public float m_maxDistance = 30f;     // 3D音響の最大距離
    }

    public List<SoundData> m_soundList = new List<SoundData>(); // 登録されているサウンドのリスト

    // IDを指定して該当のデータを取得する
    public SoundData GetSoundData(AudioID id)
    {
        for (int i = 0; i < m_soundList.Count; i++)
        {
            if (m_soundList[i].m_id == id)
            {
                return m_soundList[i];
            }
        }
        return null;
    }
}