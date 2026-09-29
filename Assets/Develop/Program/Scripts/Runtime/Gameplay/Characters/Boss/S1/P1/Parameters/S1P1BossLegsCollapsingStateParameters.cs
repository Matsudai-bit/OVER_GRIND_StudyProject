using System;
using UnityEngine;

/// <summary>
/// S1P1ボスの脚崩壊状態で使用するパラメータを保持します。
/// </summary>
[Serializable]
public sealed class S1P1BossLegsCollapsingStateParameters
{
    // 脚崩壊開始からフェーズ移行までの待機時間
    [SerializeField, Header("フェーズ移行"), Min(0.0f)]
    private float m_transitionDuration = 5.0f;

    /// <summary>
    /// フェーズ移行までの待機時間を取得します。
    /// </summary>
    public float TransitionDuration =>
        m_transitionDuration;
}
