#if UNITY_EDITOR

using UnityEngine;

/// <summary>
/// 複数サウンドへ一括適用する詳細項目の設定です。
/// 「適用する」にチェックした項目だけが反映されます。
/// </summary>
public sealed class SoundToolBulkSettings
{
    /// <summary>
    /// カテゴリーを変更するかを取得または設定します。
    /// </summary>
    public bool ApplyCategory { get; set; } = false;

    /// <summary>
    /// ピッチを変更するかを取得または設定します。
    /// </summary>
    public bool ApplyPitch { get; set; } = false;

    /// <summary>
    /// ループ設定を変更するかを取得または設定します。
    /// </summary>
    public bool ApplyLoop { get; set; } = false;

    /// <summary>
    /// 3D 設定を変更するかを取得または設定します。
    /// </summary>
    public bool ApplyIs3D { get; set; } = false;

    /// <summary>
    /// 距離減衰の設定を変更するかを取得または設定します。
    /// </summary>
    public bool ApplyRolloff { get; set; } = false;

    /// <summary>
    /// 一括適用する値を保持する設定データを取得します。
    /// </summary>
    public SoundDatabase.SoundData Values { get; } = new SoundDatabase.SoundData();

    /// <summary>
    /// チェックされた項目だけを対象のサウンドへ適用します。
    /// </summary>
    /// <param name="target">適用先のサウンド設定。</param>
    public void Apply(SoundDatabase.SoundData target)
    {
        if (target == null)
        {
            return;
        }

        if (ApplyCategory)
        {
            target.m_category = Values.m_category;
        }

        if (ApplyPitch)
        {
            target.m_pitch = Values.m_pitch;
        }

        if (ApplyLoop)
        {
            target.m_loop = Values.m_loop;
        }

        if (ApplyIs3D)
        {
            target.m_is3D = Values.m_is3D;
        }

        if (ApplyRolloff)
        {
            target.m_rolloffMode = Values.m_rolloffMode;
            target.m_minDistance = Values.m_minDistance;
            target.m_maxDistance = Mathf.Max(Values.m_minDistance + 0.01f, Values.m_maxDistance);
            target.m_customRolloffCurve = new AnimationCurve(Values.m_customRolloffCurve.keys);
        }
    }
}

#endif