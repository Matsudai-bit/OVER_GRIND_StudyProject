using UnityEngine;

/// <summary>
/// Vゲージのチャージ量と上限を保持します。
/// </summary>
public class VGaugePlaceModel :MonoBehaviour
{
    private int m_currentGauge;
    private int m_maxGauge = 100;

    /// <summary>
    /// ゲージの最大値を設定し、現在値を範囲内に収めます。
    /// </summary>
    /// <param name="maxGauge">最大値。1未満は1として扱います。</param>
    public void SetMaxGauge(int maxGauge)
    {
        m_maxGauge = Mathf.Max(1, maxGauge);
        SetGauge(m_currentGauge);
    }

    /// <summary>
    /// 現在のチャージ量を取得します。
    /// </summary>
    /// <returns>0から最大値までのチャージ量。</returns>
    public int GetGauge()
    {
        return m_currentGauge;
    }

    /// <summary>
    /// チャージ量を0から最大値の範囲で設定します。
    /// </summary>
    /// <param name="value">設定するチャージ量。</param>
    public void SetGauge(int value)
    {
        m_currentGauge = Mathf.Clamp(value, 0, m_maxGauge);
    }

    /// <summary>
    /// チャージ量を割合で設定します。
    /// </summary>
    /// <param name="rate">0から1までの割合。範囲外は制限します。</param>
    public void SetGaugeRate(float rate)
    {
        SetGauge(Mathf.RoundToInt(Mathf.Clamp01(rate) * m_maxGauge));
    }

    /// <summary>
    /// 現在のチャージ量を割合で取得します。
    /// </summary>
    /// <returns>0から1までのチャージ割合。</returns>
    public float GetGaugeRate()
    {
        return m_currentGauge / (float)m_maxGauge;
    }
}
