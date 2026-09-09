using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

/// <summary>
/// Timelineを使用したQTEの試作コントローラです。
/// </summary>
[DisallowMultipleComponent]
public sealed class QTEPrototypeController : MonoBehaviour
{
    // Timelineを制御するPlayableDirector
    [SerializeField, Header("Timeline")]
    private PlayableDirector m_playableDirector;

    // QTEで使用する入力
    [SerializeField, Header("QTE入力")]
    private InputActionReference m_qteActionReference;

    // QTEの制限時間
    [SerializeField, Header("QTE設定"), Min(0.1f)]
    private float m_timeLimit = 2.0f;

    // QTE成功時のTimeline再開位置
    [SerializeField, Header("Timeline分岐位置"), Min(0.0f)]
    private double m_successTime = 6.0;

    // QTE失敗時のTimeline再開位置
    [SerializeField, Min(0.0f)]
    private double m_failureTime = 9.0;

    [SerializeField]
    private GameObject m_qteDrawer;

    [SerializeField, Header("QTEループ設定"), Min(0.0f)]
    private double m_loopStartTime = 2.0;

    // QTEが実行中か
    private bool m_isQteActive;

    // QTEの残り時間
    private float m_remainingTime;

    // QTE開始前のTimeline更新方式
    private DirectorUpdateMode m_previousUpdateMode;

    // QTE停止位置
    private double m_pausedTimelineTime;

    private void OnEnable()
    {
        if (m_qteActionReference == null ||
            m_qteActionReference.action == null)
        {
            return;
        }

        m_qteActionReference.action.performed += OnQtePerformed;
    }

    private void OnDisable()
    {
        if (m_qteActionReference == null ||
            m_qteActionReference.action == null)
        {
            return;
        }

        m_qteActionReference.action.performed -= OnQtePerformed;

        if (m_qteActionReference.action.enabled)
        {
            m_qteActionReference.action.Disable();
        }
    }

    private void Update()
    {
        if (!m_isQteActive)
        {
            return;
        }

        // Timeline停止中でもQTE時間を進める
        m_remainingTime -= Time.unscaledDeltaTime;

        if (m_remainingTime > 0.0f)
        {
            return;
        }

        FinishQTE(false);
    }

    /// <summary>
    /// QTEを開始します。
    /// </summary>
    public void StartQTE()
    {
        if (m_isQteActive)
        {
            return;
        }

        if (m_playableDirector == null)
        {
            Debug.LogError("PlayableDirectorが設定されていません。", this);
            return;
        }

        if (m_qteActionReference == null ||
            m_qteActionReference.action == null)
        {
            Debug.LogError("QTE用InputActionが設定されていません。", this);
            return;
        }

        m_qteDrawer.SetActive(true);
        m_isQteActive = true;
        m_remainingTime = m_timeLimit;

        // QTE入力を有効化
        m_qteActionReference.action.Enable();

        // Timelineを停止
        // Timelineの評価を維持したまま時間だけ停止する
        m_playableDirector.playableGraph
            .GetRootPlayable(0)
            .SetSpeed(0.0);

        Debug.Log("QTE開始");
    }

    /// <summary>
    /// QTE入力を受け取ります。
    /// </summary>
    /// <param name="context">入力情報。</param>
    private void OnQtePerformed(InputAction.CallbackContext context)
    {
        if (!m_isQteActive)
        {
            return;
        }

        FinishQTE(true);
    }

    /// <summary>
    /// QTEを終了してTimelineを再開します。
    /// </summary>
    /// <param name="isSuccess">QTEに成功したか。</param>
    private void FinishQTE(bool isSuccess)
    {
        m_isQteActive = false;

        if (m_qteActionReference.action.enabled)
        {
            m_qteActionReference.action.Disable();
        }

        double targetTime = isSuccess
            ? m_playableDirector.time + 0.2f
            : m_failureTime;

        // 分岐先へTimelineを移動
        m_playableDirector.time = targetTime;
        m_playableDirector.Evaluate();

        // Timeline再開
        // 通常速度へ戻す
        m_playableDirector.playableGraph
            .GetRootPlayable(0)
            .SetSpeed(1.0);
        m_qteDrawer.SetActive(false);

        Debug.Log(isSuccess
            ? "QTE成功"
            : "QTE失敗");
    }


    /// <summary>
    /// QTE演出の先頭へTimelineを戻します。
    /// </summary>
    public void LoopQTESequence()
    {
        if (m_playableDirector == null)
        {
            return;
        }

        m_playableDirector.time = m_loopStartTime;
        m_playableDirector.Evaluate();
    }
}