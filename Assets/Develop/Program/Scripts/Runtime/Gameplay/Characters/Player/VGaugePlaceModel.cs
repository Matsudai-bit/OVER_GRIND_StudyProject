using UnityEngine;
using System;

/// <summary>
/// Vゲージの現在値、ブーストへの引き継ぎ量と消費中の残量を保持します。
/// UIやStateを参照せず、更新タイミングは呼び出し元が決定します。
/// </summary>
[DisallowMultipleComponent]
public class VGaugePlaceModel : MonoBehaviour
{
    [SerializeField, Min(1)]
    private int m_maxGauge = 100;

    [SerializeField, Min(0)]
    private int m_currentGauge;

    public event Action<float> OnGaugeRateChanged;

    // 引き継ぎ量・残量は丸めず保持し、従来のブースト持続時間を維持します。
    private float m_carriedBoostGaugeRate;
    private float m_suspendedBoostGaugeRate;
    private float m_boostGaugeDepletionRatePerSecond;

    /// <summary>現在のチャージ量を取得・設定します。</summary>
    public int Gauge
    {
        get => m_currentGauge;
        set
        {
            int clampedValue = Mathf.Clamp(value, 0, MaxGauge);
            // 値が実際に変化した時のみ通知を飛ばす
            if (m_currentGauge != clampedValue)
            {
                m_currentGauge = clampedValue;
                OnGaugeRateChanged?.Invoke(GaugeRate);
            }
        }
    }

    /// <summary>ゲージの最大値を取得・設定します。</summary>
    public int MaxGauge
    {
        get => Mathf.Max(1, m_maxGauge);
        set
        {
            m_maxGauge = Mathf.Max(1, value);
            Gauge = m_currentGauge;
        }
    }

    /// <summary>現在のチャージ割合を取得・設定します。表示用の整数単位に丸めます。</summary>
    public float GaugeRate
    {
        get => Gauge / (float)MaxGauge;
        set => Gauge = Mathf.RoundToInt(Mathf.Clamp01(value) * MaxGauge);
    }

    /// <summary>チャージ終了時にブーストへ渡す割合を取得・設定します。</summary>
    public float CarriedBoostGaugeRate
    {
        get => m_carriedBoostGaugeRate;
        set => m_carriedBoostGaugeRate = Mathf.Clamp01(value);
    }

    /// <summary>ブースト中・中断中の残量を取得・設定します。</summary>
    public float SuspendedBoostGaugeRate
    {
        get => m_suspendedBoostGaugeRate;
        set => m_suspendedBoostGaugeRate = Mathf.Clamp01(value);
    }

    /// <summary>ゲージの最大値を設定します。</summary>
    /// <param name="maxGauge">最大値。1未満は1として扱います。</param>
    public void SetMaxGauge(int maxGauge) => MaxGauge = maxGauge;

    /// <summary>現在のチャージ量を取得します。</summary>
    /// <returns>0から最大値までのチャージ量。</returns>
    public int GetGauge() => Gauge;

    /// <summary>現在のチャージ量を設定します。</summary>
    /// <param name="value">設定するチャージ量。</param>
    public void SetGauge(int value) => Gauge = value;

    /// <summary>現在のチャージ割合を設定します。</summary>
    /// <param name="rate">0から1までの割合。範囲外は制限します。</param>
    public void SetGaugeRate(float rate) => GaugeRate = rate;

    /// <summary>現在のチャージ割合を取得します。</summary>
    /// <returns>0から1までのチャージ割合。</returns>
    public float GetGaugeRate() => GaugeRate;

    /// <summary>チャージ量を増減します。</summary>
    /// <param name="amount">増減量。</param>
    public void AddGauge(int amount) => Gauge += amount;

    /// <summary>ブースト中に1秒当たり消費する割合を設定します。</summary>
    /// <param name="ratePerSecond">1秒当たりの消費割合。</param>
    public void SetBoostGaugeDepletionRate(float ratePerSecond)
    {
        m_boostGaugeDepletionRatePerSecond = Mathf.Max(0.0f, ratePerSecond);
    }

    /// <summary>ブースト残量を消費し、現在のゲージ値へ反映します。</summary>
    /// <param name="deltaTime">消費を進める秒数。</param>
    public void ConsumeBoostGauge(float deltaTime)
    {
        SuspendedBoostGaugeRate -=
            m_boostGaugeDepletionRatePerSecond * Mathf.Max(0.0f, deltaTime);
        GaugeRate = SuspendedBoostGaugeRate;
    }

    /// <summary>ゲージの実行時データを初期化します。最大値の設定は維持します。</summary>
    public void ResetGauge()
    {
        Gauge = 0;
        CarriedBoostGaugeRate = 0.0f;
        SuspendedBoostGaugeRate = 0.0f;
        m_boostGaugeDepletionRatePerSecond = 0.0f;
    }

    /// <summary>Inspectorから入力された値を有効範囲へ制限します。</summary>
    private void OnValidate()
    {
        MaxGauge = m_maxGauge;
    }
}