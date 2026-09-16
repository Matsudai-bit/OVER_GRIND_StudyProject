using UnityEngine;

/// <summary>
/// HPとHPゲージを接続します。
/// </summary>
[DisallowMultipleComponent]
public sealed class HealthGaugePresenter : MonoBehaviour
{
    // 表示対象のHP
    [SerializeField]
    private Health m_health;

    // HPゲージの表示
    [SerializeField]
    private HealthGaugeView m_healthGaugeView;

    private void OnEnable()
    {
        if (!ValidateReferences())
        {
            return;
        }

        m_health.HealthChanged += HandleHealthChanged;

        if (m_health.IsInitialized)
        {
            HandleHealthChanged(
                m_health.CurrentHealth,
                m_health.MaxHealth);
        }
    }

    private void OnDisable()
    {
        if (m_health == null)
        {
            return;
        }

        m_health.HealthChanged -= HandleHealthChanged;
    }

    /// <summary>
    /// HP変更をゲージに反映します。
    /// </summary>
    /// <param name="currentHealth">現在のHP。</param>
    /// <param name="maxHealth">最大HP。</param>
    private void HandleHealthChanged(
        int currentHealth,
        int maxHealth)
    {
        m_healthGaugeView.SetHealthGauge(
            currentHealth,
            maxHealth);
    }

    /// <summary>
    /// 必要な参照を確認します。
    /// </summary>
    /// <returns>
    /// true：必要な参照が設定されています。
    /// false：必要な参照が不足しています。
    /// </returns>
    private bool ValidateReferences()
    {
        if (m_health == null)
        {
            Debug.LogError(
                $"{nameof(HealthGaugePresenter)}: Healthが設定されていません。",
                this);

            return false;
        }

        if (m_healthGaugeView == null)
        {
            Debug.LogError(
                $"{nameof(HealthGaugePresenter)}: HealthGaugeViewが設定されていません。",
                this);

            return false;
        }

        return true;
    }
}