using UnityEngine;

/// <summary>共通プール型エフェクトのPrefabと再生方式を定義します。</summary>
[CreateAssetMenu(menuName = "Game/Effects/Particle Effect Definition", fileName = "EffectDefinition")]
public sealed class EffectDefinition : ScriptableObject
{
    [SerializeField, Header("エフェクトID")]
    private EffectId m_id = EffectId.NONE;
    [SerializeField, Header("PrefabのEffectInstance")]
    private EffectInstance m_prefab;
    [SerializeField, Header("再生方式")]
    private EffectPlaybackMode m_playbackMode = EffectPlaybackMode.ONE_SHOT;
    [SerializeField, Header("プールの初期保持数"), Min(0)]
    private int m_initialPoolSize = 1;
    [SerializeField, Header("プールの最大保持数"), Min(1)]
    private int m_maxRetainedCount = 16;
    [SerializeField, Header("最大同時再生数（0なら無制限）"), Min(0)]
    private int m_maxActiveCount;

    /// <summary>エフェクトID。</summary>
    public EffectId Id => m_id;
    /// <summary>Prefab。</summary>
    public EffectInstance Prefab => m_prefab;
    /// <summary>再生方式。</summary>
    public EffectPlaybackMode PlaybackMode => m_playbackMode;
    /// <summary>初期プール数。</summary>
    public int InitialPoolSize => Mathf.Max(0, m_initialPoolSize);
    /// <summary>未使用オブジェクトの保持上限。</summary>
    public int MaxRetainedCount => Mathf.Max(1, m_maxRetainedCount);
    /// <summary>同時再生上限（0は無制限）。</summary>
    public int MaxActiveCount => Mathf.Max(0, m_maxActiveCount);

#if UNITY_EDITOR
    // 個別の定義がInspectorで変更されたときにCatalogへ通知します。
    internal static event System.Action DefinitionChanged;

    /// <summary>
    /// 設定変更を検索マップへ通知します。
    /// </summary>
    private void OnValidate()
    {
        // OnValidateからはUnity APIを呼ばず、マップの更新要求だけを通知します。
        DefinitionChanged?.Invoke();
    }
#endif
}
