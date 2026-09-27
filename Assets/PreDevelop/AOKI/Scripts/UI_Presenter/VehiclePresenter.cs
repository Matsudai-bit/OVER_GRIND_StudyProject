using UnityEngine;
using UnityEngine.UI;

public class VehiclePresenter : MonoBehaviour
{
    [Header("Model & View")]
    [SerializeField] private Veicle_UI_Model vehicleModel;
    [SerializeField] private SpeedDisplayView speedView; 

    [SerializeField] private Image gaugeFillImage;

    void OnEnable()
    {
        if (vehicleModel != null)
        {
            vehicleModel.OnSpeedChanged += HandleSpeedChanged;
            vehicleModel.OnGaugeChanged += HandleGaugeChanged;
        }
    }

    void OnDisable()
    {
        if (vehicleModel != null)
        {
            vehicleModel.OnSpeedChanged -= HandleSpeedChanged;
            vehicleModel.OnGaugeChanged -= HandleGaugeChanged;
        }
    }

    private void HandleSpeedChanged(float currentSpeed)
    {
        if (speedView != null) speedView.SetSpeed(currentSpeed);
    }

    private void HandleGaugeChanged(float currentGauge)
    {
        if (gaugeFillImage != null) gaugeFillImage.fillAmount = currentGauge;
    }
}