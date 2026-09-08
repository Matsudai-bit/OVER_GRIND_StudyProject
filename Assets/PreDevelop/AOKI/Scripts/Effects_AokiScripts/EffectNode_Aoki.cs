using UnityEngine;
using System;

[RequireComponent(typeof(ParticleSystem))]
public class EffectNode_Aoki : MonoBehaviour
{
    private ParticleSystem m_targetParticle;
    private Action<EffectNode_Aoki> m_returnAction; //プールへ帰還する用

    private bool m_isLooping;
    private bool m_isPaused;

    private void Awake()
    {
        //　子要素を含めたパーティクルを一括制御できるようにするキャッシュ
        m_targetParticle = GetComponent<ParticleSystem>();
    }

    /// <summary>
    /// エフェクト再生開始関数
    /// </summary>
    /// <param name="loop"></param>
    /// <param name="onReturnTopool"></param>
    public void EffectsPlayer(bool loop, Action<EffectNode_Aoki> onReturnTopool)
    {
        m_isLooping = loop;
        m_returnAction = onReturnTopool;
        m_isPaused = true;


        // プレハブの設定モスを防ぐため、コードからループ設定を上書き
        var main = m_targetParticle.main;
        main.loop = loop;
        m_targetParticle.Play(true);
    }

    /// <summary>
    /// エフェクト再生停止関数
    /// </summary>
    public void EffectsStop()
    {
       if(m_targetParticle != null)
        {
            m_targetParticle.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        
        }
        ReturnToPool();
    }

    /// <summary>
    /// 再開開始関数
    /// </summary>
    public void Pause()
    {
        if (m_targetParticle != null && m_isPaused)
        {
            m_targetParticle.Play(true);
            m_isPaused = false;
        }
    }

    /// <summary>
    ///  更新処理
    /// </summary>
    private void Update()
    {
        // すべてのパーティクルが消滅したかを監視
        if (!m_isLooping && !m_isPaused)
        {
            // 子要素も含めて生きているパーティクルが無いか確認
            if (m_targetParticle != null && !m_targetParticle.IsAlive(true))
            {
                ReturnToPool();
            }
        }
    }

    private void ReturnToPool()
    {
        if(m_returnAction != null)
        {
            m_returnAction.Invoke(this);
            m_returnAction = null;
        }
    }
}
