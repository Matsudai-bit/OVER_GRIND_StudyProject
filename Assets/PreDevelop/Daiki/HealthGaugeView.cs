using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HPゲージを表示します。
/// </summary>
[DisallowMultipleComponent]
public sealed class HealthGaugeView : MonoBehaviour
{
    // HPを表示するSlider
    [SerializeField]
    private Slider m_healthSlider;

    private float elapsedTime = 0.0f;

    private void Start()
    {
        elapsedTime = 3.0f;
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;

        if (!gameObject.CompareTag("Player") &&  elapsedTime >= 3.0f)
        {
            m_healthSlider.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// HPゲージを更新します。
    /// </summary>
    /// <param name="currentHealth">現在のHP。</param>
    /// <param name="maxHealth">最大HP。</param>
    public void SetHealthGauge(
        int currentHealth,
        int maxHealth)
    {
        elapsedTime = 0.0f;
        if (m_healthSlider == null)
        {
            Debug.LogError(
                $"{nameof(HealthGaugeView)}: Sliderが設定されていません。",
                this);

            return;
        }

        int safeMaxHealth = Mathf.Max(1, maxHealth);
        int safeCurrentHealth = Mathf.Clamp(
            currentHealth,
            0,
            safeMaxHealth);

        m_healthSlider.wholeNumbers = true;
        m_healthSlider.minValue = 0;
        m_healthSlider.maxValue = safeMaxHealth;


        m_healthSlider.gameObject.SetActive(true);


        m_healthSlider.SetValueWithoutNotify(safeCurrentHealth);
    }
}