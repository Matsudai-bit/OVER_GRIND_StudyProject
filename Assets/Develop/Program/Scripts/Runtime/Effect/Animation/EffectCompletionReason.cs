/// <summary>
/// エフェクトの再生が終了した理由を表します。
/// </summary>
public enum EffectCompletionReason
{
    // ParticleSystemが自然終了した
    COMPLETED,

    // GRACEFUL停止後に残存粒子が消滅した
    STOPPED,

    // IMMEDIATE停止や強制破棄で終了した
    CANCELLED
}
