using System.Collections.Generic;

/// <summary>
/// カウントイベントの通知を管理するマネージャー（Subject）
/// Observerの登録・解除・通知を担う
/// </summary>
public class HitEventManager
{
    // -------------------------------------------------------------------
    // Singleton
    // -------------------------------------------------------------------
    private static HitEventManager _instance;
    public static HitEventManager Instance => _instance ??= new HitEventManager();

    // -------------------------------------------------------------------
    // Fields
    // -------------------------------------------------------------------
    private readonly List<IHitEvent> _observers = new();

    // -------------------------------------------------------------------
    // Observer 管理
    // -------------------------------------------------------------------

    /// <summary>Observerを登録する</summary>
    public void AddObserver(IHitEvent observer)
    {
        if (!_observers.Contains(observer))
            _observers.Add(observer);
    }

    /// <summary>Observerを解除する</summary>
    public void RemoveObserver(IHitEvent observer)
    {
        _observers.Remove(observer);
    }

    // -------------------------------------------------------------------
    // 通知
    // -------------------------------------------------------------------

    /// <summary>
    /// 登録済みの全Observerにイベントを通知する
    /// </summary>
    /// <param name="eventType">通知する種類</param>
    public void Notify(HitEventType eventType)
    {
        // リスト走査中の変更に備えてコピーを使用
        foreach (var observer in new List<IHitEvent>(_observers))
            observer.OnHitEvent(eventType);
    }
}
