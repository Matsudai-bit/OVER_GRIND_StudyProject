using UnityEngine;
using System;

[RequireComponent(typeof(ParticleSystem))]
public class EffectNode_Aoki : MonoBehaviour
{
    private ParticleSystem m_particle;
    private Action<EffectNode_Aoki> m_onComplete;
    public int HandleID { get; private set; } // 管理用の識別ID

    private void Awake()
    {
        m_particle = GetComponent<ParticleSystem>();
    }

    public void Init(int handleID, Action<EffectNode_Aoki> onComplete)
    {
        HandleID = handleID;
        m_onComplete = onComplete;
    }

    public void Play()
    {
        m_particle.Play(true);
    }

    public void Stop()
    {
        m_particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        OnFinished();
    }

    public void Pause()
    {
        m_particle.Pause(true);
    }

    public void Resume()
    {
        m_particle.Play(true);
    }

    private void Update()
    {
        // ループしていない単発系で、再生が終わったらマネージャーへ返却通知
        if (m_particle != null && !m_particle.main.loop)
        {
            if (!m_particle.IsAlive(true))
            {
                OnFinished();
            }
        }
    }

    private void OnFinished()
    {
        if (m_onComplete != null)
        {
            var action = m_onComplete;
            m_onComplete = null;
            action.Invoke(this);
        }
    }
}