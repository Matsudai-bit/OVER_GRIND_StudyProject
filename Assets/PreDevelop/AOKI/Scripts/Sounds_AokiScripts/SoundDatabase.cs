using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundDatabase", menuName = "Audio/Sound Database")]
public class SoundDatabase : ScriptableObject
{
    [Serializable]
    public class SoundData
    {
        public SoundID_Aoki m_id;
        public AudioClip m_clip;
        [Range(0f, 1f)] public float m_volume = 1.0f;
        [Range(0.1f, 3f)] public float m_pitch = 1.0f;
        public bool m_loop = false;
        public bool m_is3D = false;
    }

    public List<SoundData> m_soundList = new List<SoundData>();

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