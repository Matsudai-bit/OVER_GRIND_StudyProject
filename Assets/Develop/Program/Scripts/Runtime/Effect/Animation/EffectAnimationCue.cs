using UnityEngine;

/// <summary>
/// Animation Eventから呼び出すエフェクトの設定です。
/// </summary>
[CreateAssetMenu(menuName = "Game/Effects/Animation Effect Cue", fileName = "EffectAnimationCue")]
public sealed class EffectAnimationCue : ScriptableObject
{
    // 既存のIDに登録済みのエフェクトを再生します。
    [SerializeField, Header("エフェクトID")]
    private EffectId m_effectId = EffectId.NONE;

    // デフォルト（ローカル優先）か、指定地点への発生かを選びます。
    [SerializeField, Header("発生方式")]
    private EffectAnimationSpawnMode m_spawnMode = EffectAnimationSpawnMode.DEFAULT;

    // WORLD_AT_ANCHORでのみ使用します。
    [SerializeField, Header("ワールド発生用のアンカーID")]
    private EffectAnchorId m_anchorId = EffectAnchorId.ROOT;

    // OnAnimationEffectStopを受け取った際の停止方法です。
    [SerializeField, Header("停止イベントの停止方式")]
    private EffectStopMode m_stopMode = EffectStopMode.GRACEFUL;

    // Animator Stateの終了時に停止するかどうかです。
    [SerializeField, Header("アニメーション終了・中断時に停止")]
    private bool m_stopOnAnimationEnd = false;

    // 同じEffectIdが既に再生中なら再生要求を無視します。
    [SerializeField, Header("同じEffectIdの重複再生を防ぐ")]
    private bool m_preventDuplicatePlayback = false;

    /// <summary>アニメーション終了時に停止するか取得します。</summary>
    public bool StopOnAnimationEnd => m_stopOnAnimationEnd;

    /// <summary>再生中の同一EffectIdを重複再生させないか取得します。</summary>
    public bool PreventDuplicatePlayback => m_preventDuplicatePlayback;

    /// <summary>再生するエフェクトIDを取得します。</summary>
    public EffectId EffectId => m_effectId;

    /// <summary>発生方式を取得します。</summary>
    public EffectAnimationSpawnMode SpawnMode => m_spawnMode;

    /// <summary>ワールド発生用のアンカーIDを取得します。</summary>
    public EffectAnchorId AnchorId => m_anchorId;

    /// <summary>停止イベントで使用する停止方式を取得します。</summary>
    public EffectStopMode StopMode => m_stopMode;
}
