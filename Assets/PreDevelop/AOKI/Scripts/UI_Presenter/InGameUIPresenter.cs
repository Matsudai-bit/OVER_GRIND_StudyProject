using UnityEngine;

/// <summary>
/// インゲームの UI 全体を統括し、Model と View を連携させる Presenter です。
/// </summary>
public class InGameUiPresenter : MonoBehaviour
{
    [Header("Models")]

    /// <summary>HP のデータを管理するモデル。</summary>
    [SerializeField] private HpUiModel m_hpModel;

    /// <summary>タイマーのデータを管理するモデル。</summary>
    [SerializeField] private TimerUiModel m_timerModel;

    /// <summary>車両のデータを管理するモデル。</summary>
    [SerializeField] private VehicleUiModel m_vehicleModel;

    [Header("Views")]

    /// <summary>HP を表示するビュー。</summary>
    [SerializeField] private HPDisplayView m_hpView;

    /// <summary>タイマーを表示するビュー。</summary>
    [SerializeField] private TimerDisplayView m_timerView;

    /// <summary>車両の速度とゲージを表示するビュー。</summary>
    [SerializeField] private SpeedDisplayView m_vehicleView;

    /// <summary>
    /// 初期化処理を行い、View に初期の最大 HP を設定します。
    /// </summary>
    private void Start()
    {
        if (m_hpModel != null && m_hpView != null)
        {
            m_hpView.Initialize(m_hpModel.MaxHp);
        }
    }

    /// <summary>
    /// オブジェクトが有効になった際、各 Model のイベントを購読します。
    /// </summary>
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

    /// <summary>
    /// オブジェクトが無効になった際、各 Model のイベント購読を解除します。
    /// </summary>
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

    /// <summary>
    /// HP の変更を受け取り、View の表示を更新します。
    /// </summary>
    /// <param name="currentHp">現在の HP。</param>
    /// <param name="maxHp">HP の最大値。</param>
    private void HandleHpChanged(float currentHp, float maxHp)
    {
        if (m_hpView != null) m_hpView.UpdateHPDisplay(currentHp, maxHp);
    }

    /// <summary>
    /// 経過時間の変更を受け取り、View の表示を更新します。
    /// </summary>
    /// <param name="time">現在の経過時間（秒）。</param>
    private void HandleTimeChanged(float time)
    {
        if (m_timerView == null) return;

        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int milliseconds = Mathf.FloorToInt((time * 100f) % 100f);
        minutes = Mathf.Clamp(minutes, 0, 99);

        m_timerView.SetTime(minutes, seconds, milliseconds);
    }

    /// <summary>
    /// 速度の変更を受け取り、View の表示を更新します。
    /// </summary>
    /// <param name="currentSpeed">現在の速度。</param>
    private void HandleSpeedChanged(float currentSpeed)
    {
        if (m_vehicleView != null) m_vehicleView.UpdateSpeed(currentSpeed);
    }

    /// <summary>
    /// ゲージの変更を受け取り、View の表示を更新します。
    /// </summary>
    /// <param name="currentGauge">現在のゲージ量（0.0～1.0）。</param>
    private void HandleGaugeChanged(float currentGauge)
    {
        if (m_vehicleView != null) m_vehicleView.UpdateGauge(currentGauge);
    }
}