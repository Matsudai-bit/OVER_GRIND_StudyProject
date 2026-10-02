using UnityEngine;
using System;

public class EffectNode_Aoki : MonoBehaviour
{
    [Header("エフェクト識別子（自動設定されます）")]
    [SerializeField] private EffectID m_effectID;
    public EffectID EffectID => m_effectID;

    private ParticleSystem m_particle;
    private Action<EffectNode_Aoki> m_onComplete;
    private bool m_isPlaying = false; // 再生完了の誤発火を防ぐフラグ

    public int HandleID { get; private set; }

    private void Awake()
    {
        // 自身または子オブジェクトから ParticleSystem を取得（構造を選ばない）
        m_particle = GetComponentInChildren<ParticleSystem>();
    }

    // 自動登録ツールから ID をセットするためのメソッド
    public void SetEffectID(EffectID id)
    {
        m_effectID = id;
    }

    public void Init(int handleID, Action<EffectNode_Aoki> onComplete)
    {
        HandleID = handleID;
        m_onComplete = onComplete;
        m_isPlaying = false;
    }

    public void Play()
    {
        if (m_particle != null)
        {
            m_particle.Play(true);
            m_isPlaying = true;
        }
    }

    public void Stop()
    {
        if (m_particle != null)
        {
            m_particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        OnFinished();
    }

    public void Pause()
    {
        if (m_particle != null)
        {
            m_particle.Pause(true);
        }
    }

    public void Resume()
    {
        if (m_particle != null)
        {
            m_particle.Play(true);
        }
    }

    private void Update()
    {
        // 再生が開始されており、かつループしない単発エフェクトの場合のみ完了チェック
        if (m_isPlaying && m_particle != null && !m_particle.main.loop)
        {
            // 全ての粒子が消滅したらマネージャーへ完了通知
            if (!m_particle.IsAlive(true))
            {
                OnFinished();
            }
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