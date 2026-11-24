/// <summary>チャージ終了直後の固定方向ブーストダッシュを管理します。</summary>
public sealed class PlayerBoostDashingState : PlayerBoostRunningStateBase
{
    private PlayerBoostDash m_dash;

    /// <summary>ダッシュ中は移動入力の有無で停止しません。</summary>
    protected override bool StopsWithoutMoveInput => false;

    /// <summary>チャージ残量とダッシュ方向を引き継いで開始します。</summary>
    protected override void OnStartState()
    {
        Owner.SuspendedBoostGaugeRate = Owner.CarriedBoostGaugeRate;
        m_dash = new PlayerBoostDash(Owner);
        StartBoost();
    }

    /// <summary>ダッシュを更新し、終了時に通常ブースト移動へ遷移します。</summary>
    protected override void UpdateBoostMovement()
    {
        if (m_dash.UpdateDash(UnityEngine.Time.fixedDeltaTime))
            HandOffToStableBoost();
    }
}
