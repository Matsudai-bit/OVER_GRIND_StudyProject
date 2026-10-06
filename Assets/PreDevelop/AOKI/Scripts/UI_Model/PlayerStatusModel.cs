using System;
using UnityEngine;

/// <summary>
/// 速度(Speed)とVゲージ(VGauge)の状態を一括管理するMVPのModelです。
/// UIやStateを参照せず、物理演算・状態更新は呼び出し元が決定します。
/// </summary>
public class PlayerStatusModel : MonoBehaviour
{
    // ==========================================
    // Events (Presenterが購読するイベント)
    // ==========================================

    /// <summary>速度が変更された時に呼ばれます（引数: 現在の速度）</summary>
    public event Action<float> OnSpeedChanged;

    /// <summary>ゲージ量が変更された時に呼ばれます（引数: 0.0?1.0の割合）</summary>
    public event Action<float> OnGaugeChanged;

    // ==========================================
    // Speed Fields
    // ==========================================
    [Header("Speed Settings")]
    private const float MAX_SPEED = 99.9f;

    [SerializeField, Range(0.0f, MAX_SPEED)]
    private float m_currentSpeed;
    private float? m_speedDisplayOverride;

    [SerializeField]
    SpeedPlaceModel m_placeModel;

    [SerializeField]
    VGaugePlaceModel m_gaugePlaceModel;

    // ==========================================
    // V-Gauge Fields
    // ==========================================
    [Header("V-Gauge Settings")]
    [SerializeField, Min(1)]
    private int m_maxGauge = 100;

    [SerializeField, Min(0)]
    private int m_currentGauge;

    private float m_carriedBoostGaugeRate;
    private float m_suspendedBoostGaugeRate;
    private float m_boostGaugeDepletionRatePerSecond;

    // ==========================================
    // Speed Properties
    // ==========================================
    public float Speed
    {
        get => m_currentSpeed;
        set
        {
            float newValue = Mathf.Clamp(value, 0.0f, MAX_SPEED);
            if (!Mathf.Approximately(m_currentSpeed, newValue))
            {
                m_currentSpeed = newValue;
                OnSpeedChanged?.Invoke(m_currentSpeed); // 新しい速度を通知
            }
        }
    }

    // ==========================================
    // V-Gauge Properties
    // ==========================================
    public int MaxGauge
    {
        get => Mathf.Max(1, m_maxGauge);
        set
        {
            int newValue = Mathf.Max(1, value);
            if (m_maxGauge != newValue)
            {
                m_maxGauge = newValue;
                Gauge = m_currentGauge; // 最大値が変わったら現在値のクランプとイベント発火を行う
            }
        }
    }

    public int Gauge
    {
        get => m_currentGauge;
        set
        {
            int newValue = Mathf.Clamp(value, 0, MaxGauge);
            if (m_currentGauge != newValue)
            {
                m_currentGauge = newValue;
                // Presenterの要求に合わせて、0.0?1.0の「割合」を計算して通知
                OnGaugeChanged?.Invoke(GaugeRate);
            }
        }
    }

    public float GaugeRate
    {
        get => Gauge / (float)MaxGauge;
        set => Gauge = Mathf.RoundToInt(Mathf.Clamp01(value) * MaxGauge);
    }

    public float CarriedBoostGaugeRate
    {
        get => m_carriedBoostGaugeRate;
        set => m_carriedBoostGaugeRate = Mathf.Clamp01(value);
    }

    public float SuspendedBoostGaugeRate
    {
        get => m_suspendedBoostGaugeRate;
        set => m_suspendedBoostGaugeRate = Mathf.Clamp01(value);
    }

    // ==========================================
    // Speed Methods
    // ==========================================
    public void UpdateSpeed(float horizontalSpeed)
    {
        Speed = m_speedDisplayOverride ?? horizontalSpeed;
    }

    public void SetSpeedDisplayOverride(float displaySpeed)
    {
        m_speedDisplayOverride = Mathf.Max(0.0f, displaySpeed);
        Speed = m_speedDisplayOverride.Value;
    }

    public void ClearSpeedDisplayOverride()
    {
        m_speedDisplayOverride = null;
    }

    public void ResetSpeed()
    {
        ClearSpeedDisplayOverride();
        Speed = 0.0f;
    }

    // ==========================================
    // V-Gauge Methods
    // ==========================================
    public void AddGauge(int amount)
    {
        Gauge += amount;
    }

    public void SetBoostGaugeDepletionRate(float ratePerSecond)
    {
        m_boostGaugeDepletionRatePerSecond = Mathf.Max(0.0f, ratePerSecond);
    }

    public void ConsumeBoostGauge(float deltaTime)
    {
        SuspendedBoostGaugeRate -= m_boostGaugeDepletionRatePerSecond * Mathf.Max(0.0f, deltaTime);
        GaugeRate = SuspendedBoostGaugeRate; // これによりGaugeが変わり、自動でEventが飛ぶ
    }

    public void ResetGauge()
    {
        Gauge = 0;
        CarriedBoostGaugeRate = 0.0f;
        SuspendedBoostGaugeRate = 0.0f;
        m_boostGaugeDepletionRatePerSecond = 0.0f;
    }

    // ==========================================
    // Unity Lifecycle
    // ==========================================
    private void OnValidate()
    {
        // Inspectorで値が変更された時も安全な値に補正する
        m_currentSpeed = Mathf.Clamp(m_currentSpeed, 0.0f, MAX_SPEED);
        m_maxGauge = Mathf.Max(1, m_maxGauge);
        m_currentGauge = Mathf.Clamp(m_currentGauge, 0, m_maxGauge);
    }

    private void Awake()
    {
        m_placeModel.OnSpeedChanged +=(a,b)=> { Speed = a; };
    }
}