using DG.Tweening;
using UnityEngine;

/// <summary>
/// ResultRankModel からランク情報を取得し、ResultRankView への表示・演出を制御する Presenter です。
/// </summary>
public class ResultRankPresenter : MonoBehaviour
{
    [Header("View と Model の参照")]
    [SerializeField] private ResultRankView m_view;
    [SerializeField] private ResultRankModel m_model;

    [Header("表示演出設定")]
    [Tooltip("チェックを入れると開始時に自動でランクを表示します")]
    [SerializeField] private bool m_playOnStart = true;

    [Tooltip("演出が始まるまでの待ち時間（秒）")]
    [SerializeField] private float m_delay = 0.5f;

    [Tooltip("表示時にポップアニメーション（拡大バウンド）再生するか")]
    [SerializeField] private bool m_usePopAnimation = true;

    private Sequence m_delaySequence;

    private void Awake()
    {
        EnsureReferences();
    }

    private void EnsureReferences()
    {
        if (m_view == null) m_view = GetComponent<ResultRankView>();
        if (m_model == null) m_model = FindFirstObjectByType<ResultRankModel>();
    }

    private void OnEnable()
    {
        EnsureReferences();
        if (m_model != null)
        {
            m_model.OnRankChanged += OnModelRankChanged;
        }
    }

    private void OnDisable()
    {
        if (m_model != null)
        {
            m_model.OnRankChanged -= OnModelRankChanged;
        }
        m_delaySequence?.Kill();
    }

    private void Start()
    {
        EnsureReferences();

        if (m_playOnStart && m_model != null)
        {
            RefreshRank();
        }
    }

    private void OnModelRankChanged(RankType newRank)
    {
        RefreshRankDirect();
    }

    /// <summary>
    /// 即座に現在のランクを表示します（遅延・アニメーションなし）。
    /// </summary>
    public void RefreshRankDirect()
    {
        m_delaySequence?.Kill();
        EnsureReferences();

        if (m_view != null && m_model != null)
        {
            m_view.DisplayRank(m_model.CurrentRank);
        }
    }

    /// <summary>
    /// 設定された m_delay 秒を待ってからランクを表示し、演出を開始します。
    /// </summary>
    public void RefreshRank()
    {
        EnsureReferences();
        if (m_view == null || m_model == null) return;

        m_delaySequence?.Kill();

        RankType targetRank = m_model.CurrentRank;

        if (m_delay <= 0f)
        {
            m_view.DisplayRank(targetRank);
            if (m_usePopAnimation) m_view.PlayPopAnimation();
            return;
        }

        m_delaySequence = DOTween.Sequence();
        m_delaySequence.SetUpdate(true); // timeScale = 0 対応

        m_delaySequence.AppendInterval(m_delay);
        m_delaySequence.OnComplete(() =>
        {
            if (m_view != null)
            {
                m_view.DisplayRank(targetRank);
                if (m_usePopAnimation)
                {
                    m_view.PlayPopAnimation();
                }
            }
        });
    }
}