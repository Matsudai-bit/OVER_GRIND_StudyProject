using UnityEngine;

/// <summary>
/// ミサイル発射・飛行・爆発の見た目だけを制御します。
/// </summary>
[DisallowMultipleComponent]
public sealed class MissileEffectController : MonoBehaviour
{
    [SerializeField, Header("共有プール型：発射エフェクト")]
    private EffectId m_launchEffectId = EffectId.NONE;

    [SerializeField, Header("子配置型：飛行パーティクル")]
    private ParticleSystem m_flightParticle;

    [SerializeField, Header("共有プール型：爆発エフェクト")]
    private EffectId m_explosionEffectId = EffectId.NONE;

    [SerializeField, Header("ミサイルエフェクト 発射->移動中")]
    AudioSequentialAsset m_missileAudioSequentialAsset;

    [SerializeField, Header("爆発サウンド")]
    AudioOneShotAsset m_explosionAudioOneShotAsset;
    [SerializeField, Header("エミッター")]
    private SequentialAudioPlayer m_sequentialAudioPlayer;

    // EffectServiceにつながるシーン共通Emitter
    private EffectEmitter m_sceneEmitter;

    /// <summary>共通Emitterを設定します。</summary>
    public void SetEmitter(EffectEmitter sceneEmitter)
    {
        m_sceneEmitter = sceneEmitter;
    }

    /// <summary>発射時の単発演出と、飛行中のループ演出を開始します。</summary>
    public void PlayLaunch()
    {
        if (m_sceneEmitter != null && m_launchEffectId != EffectId.NONE)
        {
            m_sceneEmitter.PlayAt(m_launchEffectId, transform.position, transform.rotation);
        }

        if (m_sequentialAudioPlayer != null)
        {
            m_sequentialAudioPlayer.PlayFollowing(m_missileAudioSequentialAsset, transform);
        }

        if (m_flightParticle != null)
        {
            m_flightParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            m_flightParticle.Play(true);
        }
    }

    /// <summary>飛行エフェクトを残さず停止します。</summary>
    public void StopFlight()
    {
        if (m_flightParticle != null)
        {
            m_flightParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    /// <summary>衝突位置で爆発の見た目を再生します。</summary>
    public void PlayExplosion(Vector3 position)
    {
        if (m_sceneEmitter != null && m_explosionEffectId != EffectId.NONE)
        {
            m_sceneEmitter.PlayAt(m_explosionEffectId, position, Quaternion.identity);

            m_sequentialAudioPlayer.PlayOneShotAt(m_explosionAudioOneShotAsset, position);
        }
    }

    private void OnDisable()
    {
        StopFlight();
    }
}
