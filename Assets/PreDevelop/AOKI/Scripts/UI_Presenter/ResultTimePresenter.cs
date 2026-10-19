using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// ResultTimeModel から「今回のクリアタイム（ClearTime）」を取得し、
/// View への表示・カウントアップ演出、および新記録（IsNewRecord）時の赤点滅演出を制御する Presenter です。
/// </summary>
public class ResultBestTimePresenter : MonoBehaviour
{
    [Header("View と Model の参照")]
    [SerializeField] private ResultTimeView m_view;
    [SerializeField] private ResultTimeModel m_model;

    [Header("表示演出設定")]
    [Tooltip("チェックを入れると開始時に自動でタイムを表示します")]
    [SerializeField] private bool m_playOnStart = true;

    [Tooltip("チェックを入れると数字回転（カウントアップ）演出を行います（オフなら即時表示）")]
    [SerializeField] private bool m_useCountUp = true;

    [Tooltip("カウントアップ演出時間（秒）")]
    [SerializeField] private float m_countUpDuration = 1.2f;

    [Tooltip("演出が始まるまでの待ち時間（秒）")]
    [SerializeField] private float m_delay = 0.3f;

    [Tooltip("アニメーションの加減速カーブ")]
    [SerializeField] private Ease m_countUpEase = Ease.OutQuint;

    [Header("赤点滅（New Best 演出）設定")]
    [Tooltip("新記録（IsNewRecord）時に赤点滅させる対象の Image")]
    [SerializeField] private Image m_blinkTargetImage;

    [Tooltip("点滅時の赤色の濃度・色合い")]
    [SerializeField] private Color m_blinkRedColor = new Color(1f, 0.2f, 0.2f, 1f);

    [Tooltip("点滅1周の速度（秒）")]
    [SerializeField] private float m_blinkDuration = 0.4f;

    private Tween m_countUpTween;
    private Tween m_blinkTween;
    private Color m_originalColor = Color.white;

    private void Awake()
    {
        EnsureReferences();

        if (m_blinkTargetImage != null)
        {
            m_originalColor = m_blinkTargetImage.color;
        }
    }

    private void EnsureReferences()
    {
        if (m_view == null) m_view = GetComponent<ResultTimeView>();
        if (m_model == null) m_model = FindFirstObjectByType<ResultTimeModel>();
    }

    private void OnEnable()
    {
        EnsureReferences();
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
        StopAllTweens();
    }

    private void Start()
    {
        EnsureReferences();

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
    /// ResultTimeModel から「今回のクリアタイム（新しい数字）」を直接取得します。
    /// </summary>
    public float GetClearTime()
    {
        if (m_model == null) return 0f;
        return m_model.ClearTime; // 「今回のクリアタイム」をそのまま返す
    }

    /// <summary>
    /// 即座に今回のクリアタイムを表示します（アニメーションなし）
    /// </summary>
    public void RefreshViewDirect()
    {
        StopAllTweens();
        EnsureReferences();

        float clearTime = GetClearTime();

        if (m_view != null)
        {
            m_view.DisplayTime(clearTime);
        }

        // Model の IsNewRecord を見て新記録なら赤点滅
        if (m_model != null && m_model.IsNewRecord)
        {
            StartBlinking();
        }
        else
        {
            StopBlinking();
        }
    }

    /// <summary>
    /// 今回のクリアタイムを表示（カウントアップ演出付き）し、新記録（m_model.IsNewRecord）なら赤点滅を開始します。
    /// </summary>
    public void RefreshBestTime()
    {
        EnsureReferences();
        if (m_view == null || m_model == null) return;

        StopAllTweens();

        float clearTime = GetClearTime();
        bool isNewRecord = m_model.IsNewRecord; // Model から新記録判定を取得

        if (!m_useCountUp)
        {
            m_view.DisplayTime(clearTime);
            if (isNewRecord) StartBlinking();
            return;
        }

        m_view.DisplayTime(0f);

        Sequence seq = DOTween.Sequence();

        if (m_delay > 0f)
        {
            seq.AppendInterval(m_delay);
        }

        seq.Append(
            DOVirtual.Float(0f, clearTime, m_countUpDuration, value =>
            {
                if (m_view != null) m_view.DisplayTime(value);
            })
            .SetEase(m_countUpEase)
        );

        seq.SetUpdate(true);

        seq.OnComplete(() =>
        {
            if (m_view != null) m_view.DisplayTime(clearTime);

            // カウント完了後、IsNewRecord が true なら赤点滅開始
            if (isNewRecord)
            {
                StartBlinking();
            }
            else
            {
                StopBlinking();
            }
        });

        m_countUpTween = seq;
    }

    private void StartBlinking()
    {
        StopBlinking();

        if (m_blinkTargetImage == null) return;

        m_blinkTween = m_blinkTargetImage.DOColor(m_blinkRedColor, m_blinkDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    private void StopBlinking()
    {
        m_blinkTween?.Kill();
        if (m_blinkTargetImage != null)
        {
            m_blinkTargetImage.color = m_originalColor;
        }
    }

    private void StopAllTweens()
    {
        m_countUpTween?.Kill();
        StopBlinking();
    }
}