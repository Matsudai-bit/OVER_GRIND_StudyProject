/// <summary>
/// カウントイベントを受け取るObserverインターフェース
/// </summary>
public interface IHitEvent
{
    /// <summary>
    /// イベント通知を受け取る
    /// </summary>
    /// <param name="eventType">通知の種類</param>
    void OnHitEvent(HitEventType eventType);
}
