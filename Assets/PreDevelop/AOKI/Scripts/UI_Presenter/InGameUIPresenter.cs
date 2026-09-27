using UnityEngine;

public class InGameUIPresenter : MonoBehaviour
{
    [Header("Models")]
    [SerializeField] private HP_UI_Model m_hpModel;
    [SerializeField] private Timer_UI_Model m_timerModel;
    [SerializeField] private Veicle_UI_Model m_vehicleModel;

    [Header("Views")]
    [SerializeField] private HPDisplayView m_hpView;
    [SerializeField] private TimerDisplayView m_timerView;
    [SerializeField] private SpeedDisplayView m_vehicleView;

    private void Start()
    {
        if (m_hpModel != null && m_hpView != null)
        {
            m_hpView.Initialize(m_hpModel.MaxHp);
        }
    }

    private void OnEnable()
    {
        if (m_hpModel != null) m_hpModel.OnHpChanged += HandleHpChanged;
        if (m_timerModel != null) m_timerModel.OnTimeChanged += HandleTimeChanged;
        if (m_vehicleModel != null)
        {
            m_vehicleModel.OnSpeedChanged += HandleSpeedChanged;
            m_vehicleModel.OnGaugeChanged += HandleGaugeChanged;
        }
    }

    private void OnDisable()
    {
        if (m_hpModel != null) m_hpModel.OnHpChanged -= HandleHpChanged;
        if (m_timerModel != null) m_timerModel.OnTimeChanged -= HandleTimeChanged;
        if (m_vehicleModel != null)
        {
            m_vehicleModel.OnSpeedChanged -= HandleSpeedChanged;
            m_vehicleModel.OnGaugeChanged -= HandleGaugeChanged;
        }
    }

    private void HandleHpChanged(float currentHp, float maxHp)
    {
        if (m_hpView != null) m_hpView.UpdateHpDisplay(currentHp, maxHp);
    }

    private void HandleTimeChanged(float time)
    {
        if (m_timerView == null) return;

        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int milliseconds = Mathf.FloorToInt((time * 100f) % 100f);
        minutes = Mathf.Clamp(minutes, 0, 99);

        m_timerView.SetTime(minutes, seconds, milliseconds);
    }

    private void HandleSpeedChanged(float currentSpeed)
    {
        if (m_vehicleView != null) m_vehicleView.UpdateSpeed(currentSpeed);
    }

    private void HandleGaugeChanged(float currentGauge)
    {
        if (m_vehicleView != null) m_vehicleView.UpdateGauge(currentGauge);
    }
}