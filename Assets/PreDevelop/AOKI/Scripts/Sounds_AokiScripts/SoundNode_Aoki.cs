using UnityEngine;
using System;

[RequireComponent(typeof(AudioSource))]
public class SoundNode_Aoki : MonoBehaviour
{
    [Header("サウンド識別子")]
    [SerializeField] private SoundID_Aoki m_soundID;
    public SoundID_Aoki SoundID => m_soundID;

    private AudioSource m_audioSource;
    private Action<SoundNode_Aoki> m_onComplete;
    private bool m_isPlaying = false;

    public int HandleID { get; private set; }

    public void Init(int handleID, SoundDatabase.SoundData data, Action<SoundNode_Aoki> onComplete)
    {
        HandleID = handleID;
        m_onComplete = onComplete;
        m_isPlaying = false;

        if (m_audioSource == null) m_audioSource = GetComponent<AudioSource>();

        if (data != null)
        {
            m_soundID = data.m_id;
            m_audioSource.clip = data.m_clip;
            m_audioSource.volume = data.m_volume;
            m_audioSource.pitch = data.m_pitch;
            m_audioSource.loop = data.m_loop;
            m_audioSource.spatialBlend = data.m_is3D ? 1.0f : 0.0f;
        }
    }

    public void Play()
    {
        if (m_audioSource != null && m_audioSource.clip != null)
        {
            m_audioSource.Play();
            m_isPlaying = true;
        }
    }

    public void Stop()
    {
        if (m_audioSource != null) m_audioSource.Stop();
        OnFinished();
    }

    public void Pause()
    {
        if (m_audioSource != null) m_audioSource.Pause();
    }

    public void Resume()
    {
        if (m_audioSource != null) m_audioSource.UnPause();
    }

    private void Update()
    {
        if (m_isPlaying && m_audioSource != null && !m_audioSource.loop)
        {
            if (!m_audioSource.isPlaying) OnFinished();
        }
    }

    private void OnFinished()
    {
        m_isPlaying = false;
        if (m_onComplete != null)
        {
            var action = m_onComplete;
            m_onComplete = null;
            action.Invoke(this);
        }
    }
}