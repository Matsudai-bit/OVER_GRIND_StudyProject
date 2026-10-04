/// @ using :: システムとエンジンの使用
using UnityEngine;
using System;

/// @ className :: 個別のサウンド再生・制御ノード
/// @ name :: Aoki Hayate
/// @ date :: 2026/09/12
[RequireComponent(typeof(AudioSource))]
public class SoundNode_Aoki : MonoBehaviour
{
    [Header("サウンド識別子")]
    [SerializeField] private SoundID_Aoki m_soundID; // このノードに割り当てられたID
    public SoundID_Aoki SoundID => m_soundID;

    private AudioSource m_audioSource;             // 再生コンポーネント
    private Action<SoundNode_Aoki> m_onComplete;   // 再生終了時に呼ばれるコールバック
    private bool m_isPlaying = false;              // 現在再生中かどうか

    public int HandleID { get; private set; }      // 外部から個別に停止するためのハンドルID

    // 再生前の初期設定を行う
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

    // 再生開始
    public void Play()
    {
        if (m_audioSource != null && m_audioSource.clip != null)
        {
            m_audioSource.Play();
            m_isPlaying = true;
        }
    }

    // 強制停止
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
        // ループ再生でない場合、再生が終了したかを監視する
        if (m_isPlaying && m_audioSource != null && !m_audioSource.loop)
        {
            if (!m_audioSource.isPlaying) OnFinished();
        }
    }

    // 再生終了時の処理
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