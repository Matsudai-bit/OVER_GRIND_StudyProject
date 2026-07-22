using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// ボタンの押し方の種類
public enum QTEType
{
    NONE,   // なし

    ONCE,   // 単押し
    MASHING // 連打
}

public class QTEEventManager : SingletonMonoBehaviour<QTEEventManager>
{
    const float MAX_GAUGE = 1.0f;
    const float GAUGE_INCREMENT = 0.25f;

    // QTEイベントデータ構造体
    [System.Serializable]
    public class EventData
    {
        public InputActionReference action;    // 押すべきキー
        public QTEType pushType;               // ボタンの押し方
        public float limitTime;                // 制限時間（秒）
        public UnityEvent successEvent;        // 成功したときに呼ばれる関数
        public UnityEvent failureEvent;        // 失敗したときに呼ばれる関数
    }

    // スライダー
    [SerializeField] private Slider m_slider;

    // QTEイベントデータ配列
    [SerializeField] private EventData[] m_QTEEvents;
    // 再生中のQTEイベントデータ
    private EventData m_currentQTEEvent;
    // 経過時間
    private float m_elapsedTime;
    // デバッグ用経過時間
    private float m_debugElapsedTime;

    // ゲージの溜まっている量
    float m_gauge;

    // ----------------------------------------------------------------------------------------

    protected override void Init()
    {
        // イベントが発生していない状態にする
        ResetCurrentQTEEvent();

        // 経過時間の初期化
        m_elapsedTime = 0.0f;

        // ゲージの初期化
        m_gauge = 0.0f;

        // スライダーの初期化
        m_slider.value = 0.0f;
    }

    public void Update()
    {
        // イベントが発生していなかった場合
        if (m_currentQTEEvent == null)
        {
            return;
        }

        switch(m_currentQTEEvent.pushType)
        {
            case QTEType.NONE:
                break;

            // 単押し
            case QTEType.ONCE:
                // アクションキーが押されたかどうか
                if (m_currentQTEEvent.action.action.WasPerformedThisFrame())
                {
                    // ゲージ量の増加
                    m_gauge += MAX_GAUGE;

                    // ゲージ量を表示
                    Debug.Log("Gauge：" + m_gauge);
                }
                break;

            // 連打
            case QTEType.MASHING:
                // アクションキーが押されたかどうか
                if (m_currentQTEEvent.action.action.WasPerformedThisFrame())
                {
                    // ゲージ量の増加
                    m_gauge += GAUGE_INCREMENT;

                    // ゲージ量を表示
                    Debug.Log("Gauge：" + m_gauge);
                }
                break;

            default:
                break;
        }

        // スライダーの値の更新
        m_slider.value = m_gauge;

        // ゲージが満タンになったら
        if (m_gauge >= MAX_GAUGE)
        {
            // 成功イベントを実行する
            m_currentQTEEvent.successEvent.Invoke();

            // イベントを完了させる
            ResetCurrentQTEEvent();
            return;
        }

        // 経過時間の計算
        m_elapsedTime += Time.deltaTime;
        m_debugElapsedTime += Time.deltaTime;
        // １秒経過したら
        if (m_debugElapsedTime > 1.0f)
        {
            Debug.Log("elapsedTime：" + m_elapsedTime);
            m_debugElapsedTime = 0.0f;
        }
        // 制限時間を超えていたら
        if(m_elapsedTime >= m_currentQTEEvent.limitTime)
        {
            // 失敗イベントを実行する
            m_currentQTEEvent.failureEvent.Invoke();

            // イベントを完了させる
            ResetCurrentQTEEvent();
            return;
        }
    }

    // ----------------------------------------------------------------------------------------

    public void BeginQTEEvent(string actionName)
    {
        // QTEイベントが行われていなかったら
        if(m_currentQTEEvent == null)
        {
            // QTEイベントを発生させる
            m_currentQTEEvent = FindEventByAction(actionName);
        }
    }

    public void ResetCurrentQTEEvent()
    {
        m_currentQTEEvent = null;
    }

    // ----------------------------------------------------------------------------------------

    public EventData FindEventByAction(string actionName)
    {
        // 名前の変更
        actionName = "QTE/" + actionName;
        // 配列にイベントが存在しているか
        foreach (var QTEEvent in m_QTEEvents)
        {
            // イベントが存在していたら
            if (QTEEvent.action.name == actionName)
            {
                return QTEEvent;
            }
        }
        return null;
    }

    // ----------------------------------------------------------------------------------------

    public void SuccessEvent()
    {
        Debug.Log("Success");
    }

    public void FailureEvent()
    {
        Debug.Log("Failure");
    }

}
