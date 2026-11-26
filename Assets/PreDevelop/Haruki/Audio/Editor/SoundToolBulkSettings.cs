#if UNITY_EDITOR

using UnityEngine;

/// <summary>
/// 複数サウンドへ一括適用する詳細項目（カテゴリー・3D音源・距離減衰・ピッチ・ループ）の設定です。
/// 「適用する」にチェックした項目だけが反映されます。
/// </summary>
public sealed class SoundToolBulkSettings
{
    // 最小距離の下限値
    private const float MIN_DISTANCE_LIMIT = 0.01f;

    // 最大距離を最小距離より大きく保つための余白
    private const float DISTANCE_MARGIN = 0.01f;

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
    /// 3D 音源設定を変更するかを取得または設定します。
    /// </summary>
    public bool ApplyIs3D { get; set; } = false;

    /// <summary>
    /// 距離減衰の設定（モード・最小距離・最大距離・カスタムカーブ）を変更するかを取得または設定します。
    /// </summary>
    public bool ApplyRolloff { get; set; } = false;

    /// <summary>
    /// 一括適用する値を保持する設定データを取得します。
    /// </summary>
    public SoundDatabase.SoundData Values { get; } = new SoundDatabase.SoundData();

    /// <summary>
    /// いずれか1つ以上の適用項目がチェックされているかを確認します。
    /// </summary>
    /// <returns>
    /// true：1つ以上の項目が有効です。
    /// false：どの項目も選択されていません。
    /// </returns>
    public bool HasAnyOptionEnabled()
    {
        return ApplyCategory || ApplyPitch || ApplyLoop || ApplyIs3D || ApplyRolloff;
    }

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
            float clampedMin = Mathf.Max(MIN_DISTANCE_LIMIT, Values.m_minDistance);
            float clampedMax = Mathf.Max(clampedMin + DISTANCE_MARGIN, Values.m_maxDistance);

            target.m_rolloffMode = Values.m_rolloffMode;
            target.m_minDistance = clampedMin;
            target.m_maxDistance = clampedMax;

            AnimationCurve sourceCurve = Values.m_customRolloffCurve;
            target.m_customRolloffCurve = sourceCurve != null
                ? new AnimationCurve(sourceCurve.keys)
                : new AnimationCurve();
        }
    }
}

#endif