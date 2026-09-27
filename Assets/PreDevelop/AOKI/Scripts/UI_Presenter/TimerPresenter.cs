using UnityEngine;

public class TimerPresenter : MonoBehaviour
{
    [Header("Model & View")]
    [SerializeField] private Timer_UI_Model timerModel;
    [SerializeField] private TimerDisplayView timerView; 

    void OnEnable()
    {
        if (timerModel != null) timerModel.OnTimeChanged += HandleTimeChanged;
    }

    void OnDisable()
    {
        if (timerModel != null) timerModel.OnTimeChanged -= HandleTimeChanged;
    }

    private void HandleTimeChanged(float time)
    {
        if (timerView == null) return;

        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int milliseconds = Mathf.FloorToInt((time * 100f) % 100f);
        minutes = Mathf.Clamp(minutes, 0, 99);

        timerView.SetTime(minutes, seconds, milliseconds);
    }
}