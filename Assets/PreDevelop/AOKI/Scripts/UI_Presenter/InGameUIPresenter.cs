using UnityEngine;

/// <summary>
/// インゲームのUI全体を統括し、ModelとViewを連携させるクラスです。
/// </summary>
public class InGameUiPresenter : MonoBehaviour
{
    [Header("Models")]

    /// <summary>HPのデータを管理するモデル</summary>
    [SerializeField] private Health m_healthModel;

    /// <summary>タイマーのデータを管理するモデル</summary>
    [SerializeField] private TimerUiModel m_timerModel;

    /// <summary>速度のデータを管理するモデル</summary>
    [SerializeField] private SpeedPlaceModel m_speedModel;

    /// <summary>Vゲージのデータを管理するモデル</summary>
    [SerializeField] private VGaugePlaceModel m_vGaugeModel;

    [Header("Views")]

    /// <summary>HPを表示するビュー</summary>
    [SerializeField] private HpDisplayView m_hpView;

    /// <summary>タイマーを表示するビュー</summary>
    [SerializeField] private TimerDisplayView m_timerView;

    /// <summary>速度とゲージを表示するビュー</summary>
    [SerializeField] private SpeedDisplayView m_vehicleView;

    /// <summary>
    /// 初期化処理を行い、Viewに初期のHPや車両ステータスを設定します。
    /// </summary>
    private void Start()
    {
        // Healthモデルの初期状態をViewに反映
        if (m_healthModel != null && m_hpView != null)
        {
            // ※HpDisplayView側のメソッド名変更に合わせて InitializeHp に修正
            m_hpView.Initialize(m_healthModel.MaxHealth);
            m_hpView.UpdateHpDisplay(m_healthModel.CurrentHealth, m_healthModel.MaxHealth);
        }

        // 車両モデルの初期状態をViewに反映
        if (m_vehicleView != null)
        {
            if (m_speedModel != null) m_vehicleView.UpdateSpeed(m_speedModel.Speed);
            if (m_vGaugeModel != null) m_vehicleView.UpdateGauge(m_vGaugeModel.GaugeRate);
        }
    }

    /// <summary>
    /// オブジェクトが有効になった際、各Modelのイベントを購読します。
    /// </summary>
    private void OnEnable()
    {
        if (m_healthModel != null) m_healthModel.HealthChanged += HandleHpChanged;
        if (m_timerModel != null) m_timerModel.OnTimeChanged += HandleTimeChanged;
        if (m_speedModel != null) m_speedModel.OnSpeedChanged += HandleSpeedChanged;
        if (m_vGaugeModel != null) m_vGaugeModel.OnGaugeRateChanged += HandleGaugeChanged;
    }

    /// <summary>
    /// オブジェクトが無効になった際、各Modelのイベント購読を解除します。
    /// </summary>
    private void OnDisable()
    {
        if (m_healthModel != null) m_healthModel.HealthChanged -= HandleHpChanged;
        if (m_timerModel != null) m_timerModel.OnTimeChanged -= HandleTimeChanged;
        if (m_speedModel != null) m_speedModel.OnSpeedChanged -= HandleSpeedChanged;
        if (m_vGaugeModel != null) m_vGaugeModel.OnGaugeRateChanged -= HandleGaugeChanged;
    }

    /// <summary>
    /// HPの変更を受け取り、Viewの表示を更新します。
    /// </summary>
    /// <param name="currentHp">現在のHP。</param>
    /// <param name="maxHp">HPの最大値。</param>
    private void HandleHpChanged(int currentHp, int maxHp)
    {
        if (m_hpView != null) m_hpView.UpdateHpDisplay((float)currentHp, (float)maxHp);
    }

    /// <summary>
    /// 経過時間の変更を受け取り、Viewの表示を更新します。
    /// </summary>
    /// <param name="time">現在の経過時間（秒）。</param>
    private void HandleTimeChanged(float time)
    {
        if (m_timerView == null) return;

        // 時間計算
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int milliseconds = Mathf.FloorToInt((time * 100f) % 100f);
        minutes = Mathf.Clamp(minutes, 0, 99);

        m_timerView.SetTime(minutes, seconds, milliseconds);
    }

    /// <summary>
    /// 速度の変更を受け取り、Viewの表示を更新します。
    /// </summary>
    /// <param name="oldSpeed">変更前の速度。</param>
    /// <param name="newSpeed">変更後の速度。</param>
    private void HandleSpeedChanged(float oldSpeed, float newSpeed)
    {
        if (m_vehicleView != null) m_vehicleView.UpdateSpeed(newSpeed);
    }

    /// <summary>
    /// ゲージの変更を受け取り、Viewの表示を更新します。
    /// </summary>
    /// <param name="currentGauge">現在のゲージ量（0.0～1.0）。</param>
    private void HandleGaugeChanged(float currentGauge)
    {
        if (m_vehicleView != null) m_vehicleView.UpdateGauge(currentGauge);
    }
}