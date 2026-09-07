using UnityEngine;

public class CountManager : MonoBehaviour, IHitEvent
{
    [SerializeField, Header("カウント")]
    static int m_hitCount;

    [SerializeField, Header("ヒットカウントHUD")]
    HUD m_hitCountHUD;

    public static  int HitCount()
    {
        return m_hitCount;
    }

    private void Start()
    {
        HitEventManager.Instance.AddObserver(this);
    }

    /// <summary>
    /// ヒットイベントを受け取る
    /// </summary>
    /// <param name="eventType"></param>

    public void OnHitEvent(HitEventType eventType)
    {
        switch (eventType)
        {
            case HitEventType.Hit:
                m_hitCount++;
                m_hitCountHUD.DrawText(m_hitCount);
                break;
        }
    }
}
