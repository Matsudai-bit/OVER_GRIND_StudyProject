using System;
using UnityEngine;

// Vゲージのモデル
public class protVGauge
{
    private int m_currentVGauge;

    public event Action<int> OnVGaugeChanged;

    //コンストラクタ
    public protVGauge(int initialVGauge)
    {
        m_currentVGauge = initialVGauge;
    }

    /// モデルのゲージを設定
    /// <param name="value"></param>
    public void SetVGauge(int value)
    {
        if (m_currentVGauge != value)
        {
            m_currentVGauge = value;
            OnVGaugeChanged?.Invoke(m_currentVGauge);
        }
    }

    //取得する
    public int GetVGauge() => m_currentVGauge;
}