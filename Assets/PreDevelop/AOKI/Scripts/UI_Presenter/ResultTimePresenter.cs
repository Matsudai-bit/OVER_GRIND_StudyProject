using UnityEngine;
using DG.Tweening;

/// <summary>
/// 今回のタイムと過去の自己ベストタイムを見比べ、
/// より良い（速い）方のタイムを自動選出して View（PERSONAL BEST表示欄等）へ送る Presenter です。
/// </summary>
public class ResultBestTimePresenter : MonoBehaviour
{
    [Header("★ View と Model の参照")]
    [SerializeField] private ResultTimeView m_view;
    [SerializeField] private ResultTimeModel m_model;

    [Header("★ 表示演出設定")]
    [Tooltip("チェックを入れると開始時に自動でベストタイムを表示します")]
    [SerializeField] private bool m_playOnStart = true;

    [Tooltip("チェックを入れるとベストタイムも数字回転（カウントアップ）演出を行います（オフなら即時表示）")]
    [SerializeField] private bool m_useCountUp = false;

    [Tooltip("カウントアップ演出時間（秒）")]
    [SerializeField] private float m_countUpDuration = 1.0f;

    [Tooltip("演出が始まるまでの待ち時間（秒）")]
    [SerializeField] private float m_delay = 0.3f;

    [Tooltip("アニメーションの加減速カーブ")]
    [SerializeField] private Ease m_countUpEase = Ease.OutCubic;

    private Tween m_countUpTween;

    private void Awake()
    {
        if (m_view == null) m_view = GetComponent<ResultTimeView>();
        if (m_model == null) m_model = FindFirstObjectByType<ResultTimeModel>();
    }

    private void OnEnable()
    {
        if (m_model != null)
        {
            m_model.OnTimeChanged += OnModelTimeChanged;
        }
    }

    private void OnDisable()
    {
        if (m_model != null)
        {
            m_model.OnTimeChanged -= OnModelTimeChanged;
        }
        m_countUpTween?.Kill();
    }

    private void Start()
    {
        if (m_playOnStart && m_model != null)
        {
            RefreshBestTime();
        }
    }

    private void OnModelTimeChanged(float newTime)
    {
        if (m_countUpTween == null || !m_countUpTween.IsActive())
        {
            RefreshViewDirect();
        }
    }

    /// <summary>
    /// 今回のタイムと前の記録を見比べ、速い（小さな）方のタイムを算出します。
    /// </summary>
    public float GetBestTime()
    {
        if (m_model == null) return 0f;

        float clear = m_model.ClearTime;
        float pb = m_model.PersonalBestTime;

        // クリアタイムが無効（0以下）の場合は自己ベストを返す
        if (clear <= 0f) return Mathf.Max(0f, pb);

        // 自己ベストが無効（0以下）の場合は今回のクリアタイムを返す
        if (pb <= 0f) return clear;

        // タイムアタックなので、値が小さい（速い）方をベストとして採用
        return Mathf.Min(clear, pb);
    }

    /// <summary>
    /// 即座にベストタイムを View に表示します（アニメーションなし）
    /// </summary>
    public void RefreshViewDirect()
    {
        if (m_view == null || m_model == null) return;
        m_countUpTween?.Kill();
        m_view.DisplayTime(GetBestTime());
    }

    /// <summary>
    /// 設定に応じて、即時表示またはカウントアップ演出でベストタイムを表示します。
    /// </summary>
    public void RefreshBestTime()
    {
        if (m_view == null || m_model == null) return;

        m_countUpTween?.Kill();
        float bestTime = GetBestTime();

        // アニメーションオフの場合は即時表示
        if (!m_useCountUp)
        {
            m_view.DisplayTime(bestTime);
            return;
        }

        // アニメーションオンの場合：0秒からベストタイムまでカウントアップ
        m_view.DisplayTime(0f);

        Sequence seq = DOTween.Sequence();

        if (m_delay > 0f)
        {
            seq.AppendInterval(m_delay);
        }

        seq.Append(
            DOVirtual.Float(0f, bestTime, m_countUpDuration, value =>
            {
                m_view.DisplayTime(value);
            })
            .SetEase(m_countUpEase)
        );

        seq.SetUpdate(true);

        seq.OnComplete(() =>
        {
            m_view.DisplayTime(bestTime);
        });

        m_countUpTween = seq;
    }
}