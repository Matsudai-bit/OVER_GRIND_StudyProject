/// @ using :: システムとエンジンの使用
using System;
using System.Collections.Generic;
using UnityEngine;

/// @ className :: サウンドデータ管理用データベース
/// @ name :: Aoki Hayate
/// @ date :: 2026/09/12
[CreateAssetMenu(fileName = "SoundDatabase", menuName = "Audio/Sound Database")]
public class SoundDatabase : ScriptableObject
{
    /// @ className :: サウンドの各設定データ
    [Serializable]
    public class SoundData
    {
        public SoundID_Aoki m_id;             // 識別子
        public AudioClip m_clip;              // 音声ファイル
        [Range(0f, 1f)] public float m_volume = 1.0f; // 音量
        [Range(0.1f, 3f)] public float m_pitch = 1.0f; // ピッチ
        public bool m_loop = false;           // ループ再生するか
        public bool m_is3D = false;           // 3D音響か
    }

    public List<SoundData> m_soundList = new List<SoundData>(); // 登録されているサウンドのリスト

    // IDを指定して該当のデータを取得する
    public SoundData GetSoundData(SoundID_Aoki id)
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