using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// QTEのボタンの押し方の種類。
/// </summary>
public enum QTEType
{
    NONE,    // なし
    ONCE,    // 単押し
    MASHING  // 連打
}

/// <summary>
/// QTE（クイックタイムイベント）の進行を管理するシングルトンクラス。
/// </summary>
public class QTEEventManager : SingletonMonoBehaviour<QTEEventManager>
{
    // ゲージの最大値
    private const float MAX_GAUGE = 1.0f;
    // 単押し成功時のゲージ増加量
    private const float GAUGE_INCREMENT_ONCE = 1.0f;
    // 連打成功時のゲージ増加量
    private const float GAUGE_INCREMENT_MASHING = 0.25f;
    // デバッグログを出力する間隔（秒）
    private const float DEBUG_LOG_INTERVAL = 1.0f;

    /// <summary>
    /// QTEイベント1つ分のデータ。
    /// </summary>
    [System.Serializable]
    public class EventData
    {
        [SerializeField] private InputActionReference m_action;   // 押すべきキー
        [SerializeField] private QTEType m_pushType;              // ボタンの押し方
        [SerializeField] private float m_limitTime;               // 制限時間（秒）
        [SerializeField] private UnityEvent m_successEvent;       // 成功したときに呼ばれるイベント
        [SerializeField] private UnityEvent m_failureEvent;       // 失敗したときに呼ばれるイベント

        /// <summary>
        /// 押すべきキーのアクション。
        /// </summary>
        public InputActionReference Action => m_action;

        /// <summary>
        /// ボタンの押し方の種類。
        /// </summary>
        public QTEType PushType => m_pushType;

        /// <summary>
        /// 制限時間（秒）。
        /// </summary>
        public float LimitTime => m_limitTime;

        /// <summary>
        /// 成功したときに呼ばれるイベント。
        /// </summary>
        public UnityEvent SuccessEvent => m_successEvent;

        /// <summary>
        /// 失敗したときに呼ばれるイベント。
        /// </summary>
        public UnityEvent FailureEvent => m_failureEvent;
    }

    // ゲージ表示用のスライダー
    [SerializeField] private Slider m_slider;
    // QTEイベントデータ配列
    [SerializeField] private EventData[] m_qteEvents;

    // 再生中のQTEイベントデータ
    private EventData m_currentQteEvent;
    // 経過時間
    private float m_elapsedTime;
    // デバッグログ用の経過時間
    private float m_debugElapsedTime;
    // ゲージの溜まっている量
    private float m_gauge;

    // ----------------------------------------------------------------------------------------

    /// <summary>
    /// シングルトンの初期化処理。
    /// </summary>
    protected override void Init()
    {
        // イベントが発生していない状態にする
        ResetCurrentQTEEvent();

        // 経過時間の初期化
        m_elapsedTime = 0.0f;
        m_debugElapsedTime = 0.0f;

        // ゲージの初期化
        m_gauge = 0.0f;

        // スライダーの初期化
        if (m_slider != null)
        {
            m_slider.value = 0.0f;
        }
    }

    /// <summary>
    /// QTE進行中の入力判定とゲージ更新を行います。
    /// </summary>
    private void Update()
    {
        // イベントが発生していなかった場合
        if (m_currentQteEvent == null)
        {
            return;
        }

        UpdateGauge();

        // スライダーの値の更新
        if (m_slider != null)
        {
            m_slider.value = m_gauge;
        }

        // ゲージが満タンになったら成功として終了
        if (m_gauge >= MAX_GAUGE)
        {
            CompleteQTEEvent(isSuccess: true);
            return;
        }

        UpdateElapsedTime();

        // 制限時間を超えていたら失敗として終了
        if (m_elapsedTime >= m_currentQteEvent.LimitTime)
        {
            CompleteQTEEvent(isSuccess: false);
        }
    }

    // ----------------------------------------------------------------------------------------

    /// <summary>
    /// 現在のQTEイベントの入力に応じてゲージを増加させます。
    /// </summary>
    private void UpdateGauge()
    {
        // アクションが設定されていない場合は何もしない
        if (m_currentQteEvent.Action == null || m_currentQteEvent.Action.action == null)
        {
            return;
        }

        // アクションキーが押されていない場合は何もしない
        if (!m_currentQteEvent.Action.action.WasPerformedThisFrame())
        {
            return;
        }

        switch (m_currentQteEvent.PushType)
        {
            // 単押し
            case QTEType.ONCE:
                m_gauge += GAUGE_INCREMENT_ONCE;
                Debug.Log("Gauge：" + m_gauge);
                break;

            // 連打
            case QTEType.MASHING:
                m_gauge += GAUGE_INCREMENT_MASHING;
                Debug.Log("Gauge：" + m_gauge);
                break;

            case QTEType.NONE:
            default:
                break;
        }
    }

    /// <summary>
    /// 経過時間を更新し、一定間隔でデバッグログを出力します。
    /// </summary>
    private void UpdateElapsedTime()
    {
        m_elapsedTime += Time.deltaTime;
        m_debugElapsedTime += Time.deltaTime;

        // 一定間隔でログを出力する
        if (m_debugElapsedTime > DEBUG_LOG_INTERVAL)
        {
            Debug.Log("elapsedTime：" + m_elapsedTime);
            m_debugElapsedTime = 0.0f;
        }
    }

    /// <summary>
    /// QTEイベントを完了させ、成功・失敗イベントを実行します。
    /// </summary>
    /// <param name="isSuccess">
    /// true：成功として終了します。
    /// false：失敗として終了します。
    /// </param>
    private void CompleteQTEEvent(bool isSuccess)
    {
        if (isSuccess)
        {
            m_currentQteEvent.SuccessEvent?.Invoke();
        }
        else
        {
            m_currentQteEvent.FailureEvent?.Invoke();
        }

        ResetCurrentQTEEvent();
    }

    // ----------------------------------------------------------------------------------------

    /// <summary>
    /// 指定したアクション名のQTEイベントを開始します。
    /// </summary>
    /// <param name="actionName">開始するアクションの名前。</param>
    public void BeginQTEEvent(string actionName)
    {
        // QTEイベントが行われていなかったら
        if (m_currentQteEvent == null)
        {
            // QTEイベントを発生させる
            m_currentQteEvent = FindEventByAction(actionName);

            // 経過時間とゲージをリセットする
            m_elapsedTime = 0.0f;
            m_debugElapsedTime = 0.0f;
            m_gauge = 0.0f;
        }
    }

    /// <summary>
    /// 現在のQTEイベントをリセットし、未実行状態にします。
    /// </summary>
    public void ResetCurrentQTEEvent()
    {
        m_currentQteEvent = null;
    }

    // ----------------------------------------------------------------------------------------

    /// <summary>
    /// アクション名からQTEイベントデータを検索します。
    /// </summary>
    /// <param name="actionName">検索するアクションの名前。</param>
    /// <returns>見つかったQTEイベントデータ。見つからない場合はnull。</returns>
    public EventData FindEventByAction(string actionName)
    {
        // 配列が設定されていない場合は見つからない
        if (m_qteEvents == null)
        {
            return null;
        }

        // 名前の変更
        string fullActionName = "QTE/" + actionName;

        // 配列にイベントが存在しているか
        foreach (var qteEvent in m_qteEvents)
        {
            // アクションが設定されていない場合はスキップ
            if (qteEvent.Action == null)
            {
                continue;
            }

            // イベントが存在していたら
            if (qteEvent.Action.name == fullActionName)
            {
                return qteEvent;
            }
        }

        return null;
    }

    // ----------------------------------------------------------------------------------------

    /// <summary>
    /// デバッグ用の成功時ログを出力します。
    /// </summary>
    public void SuccessEvent()
    {
        Debug.Log("Success");
    }

    /// <summary>
    /// デバッグ用の失敗時ログを出力します。
    /// </summary>
    public void FailureEvent()
    {
        Debug.Log("Failure");
    }
}