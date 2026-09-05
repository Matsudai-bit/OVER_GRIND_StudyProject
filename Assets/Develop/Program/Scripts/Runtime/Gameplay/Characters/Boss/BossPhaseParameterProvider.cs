using UnityEngine;

/// <summary>
/// ボスフェーズのパラメータを生成します。
/// </summary>
public abstract class BossPhaseParameterProvider :
    MonoBehaviour
{
    /// <summary>
    /// フェーズで使用するパラメータを生成します。
    /// </summary>
    /// <returns>フェーズパラメータ。</returns>
    public abstract BossPhaseParameters CreatePhaseParameters();
}