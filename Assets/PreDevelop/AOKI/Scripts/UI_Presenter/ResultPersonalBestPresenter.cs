using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 今回のタイムと前回の自己ベストタイムを比較し、
/// ベストタイムの表示および新記録（Best更新）時の赤点滅演出を制御する Presenter です。
/// </summary>
public class ResultPersonalBestPresenter : MonoBehaviour
{
    // アニメーションおよび計算用の定数
    private const float DEFAULT_START_TIME = 0f;
    private const int INFINITE_LOOP = -1;

    [Header(" View と Model の参照")]
    [Tooltip("タイムを表示する View オブジェクト")]
    [SerializeField] private ResultTimeView m_view; // View の参照

    [Tooltip("タイムデータを管理する Model オブジェクト")]
    [SerializeField] private ResultTimeModel m_model; // Model の参照

    [Header("表示演出設定")]
    [Tooltip("チェックを入れると開始時に自動でタイムを表示します")]
    [SerializeField] private bool m_playOnStart = true; // 開始時自動実行フラグ

    [Tooltip("チェックを入れると数字回転（カウントアップ）演出を行います（オフなら即時表示）")]
    [SerializeField] private bool m_useCountUp = true; // カウントアップ演出使用フラグ

    [Tooltip("カウントアップ演出時間（秒）")]
    [SerializeField] private float m_countUpDuration = 1.2f; // カウントアップにかかる時間

    [Tooltip("演出が始まるまでの待ち時間（秒）")]
    [SerializeField] private float m_delay = 0.3f; // 演出開始までのディレイ時間

    [Tooltip("アニメーションの加減速カーブ")]
    [SerializeField] private Ease m_countUpEase = Ease.OutQuint; // カウントアップのイージング

    [Header(" 赤点滅（New Best 演出）設定")]
    [Tooltip("新記録（今の方が良い）時に赤点滅させる対象の Image")]
    [SerializeField] private Image m_blinkTargetImage; // 点滅対象の Image

    [Tooltip("点滅時の赤色の濃度・色合い")]
    [SerializeField] private Color m_blinkRedColor = new Color(1f, 0.2f, 0.2f, 1f); // 点滅時の目標カラー

    [Tooltip("点滅1周の速度（秒）")]
    [SerializeField] private float m_blinkDuration = 0.4f; // 点滅の周期時間

    private Tween m_countUpTween; // カウントアップ用 Tween
    private Tween m_blinkTween; // 赤点滅用 Tween
    private Color m_originalColor = Color.white; // 点滅対象の初期カラー

    /// <summary>
    /// コンポーネントの初期化処理を行います。
    /// </summary>
    private void Awake()
    {
        EnsureReferences();

        // 初期カラーの保持
        if (m_blinkTargetImage != null)
        {
            m_originalColor = m_blinkTargetImage.color;
        }
    }

    /// <summary>
    /// オブジェクトが有効化された際にイベント購読を開始します。
    /// </summary>
    private void OnEnable()
    {
        EnsureReferences();

        if (m_model != null)
        {
            m_model.OnTimeChanged += OnModelTimeChanged;
        }
    }

    /// <summary>
    /// オブジェクトが無効化された際にイベント解除および Tweens の停止を行います。
    /// </summary>
    private void OnDisable()
    {
        if (m_model != null)
        {
            m_model.OnTimeChanged -= OnModelTimeChanged;
        }

        StopAllTweens();
    }

    /// <summary>
    /// 開始処理を行います。
    /// </summary>
    private void Start()
    {
        EnsureReferences();

        if (m_playOnStart && m_model != null)
        {
            RefreshBestTime();
        }
    }

    /// <summary>
    /// Model のタイムが変更された際のコールバックです。
    /// </summary>
    /// <param name="newTime">変更後のタイム秒数。</param>
    private void OnModelTimeChanged(float newTime)
    {
        // カウントアップ再生中でなければ即時表示更新を行います
        if (m_countUpTween == null || !m_countUpTween.IsActive())
        {
            RefreshViewDirect();
        }
    }

    /// <summary>
    /// 参照コンポーネントが未設定の場合に自動取得を試みます。
    /// </summary>
    private void EnsureReferences()
    {
        if (m_view == null)
        {
            m_view = GetComponent<ResultTimeView>();
        }

        if (m_model == null)
        {
            m_model = FindFirstObjectByType<ResultTimeModel>();
        }
    }

    /// <summary>
    /// 今回と過去のタイムを評価し、表示対象となるベストタイムを取得します。
    /// </summary>
    /// <param name="isNewRecord">今回のタイムが新記録かどうかを出力します。</param>
    /// <returns>表示すべきベストタイム（秒）。</returns>
    public float GetTargetBestTime(out bool isNewRecord)
    {
        if (m_model == null)
        {
            isNewRecord = false;
            return DEFAULT_START_TIME;
        }

        // Model の新記録判定を取得
        isNewRecord = m_model.IsNewRecord;

        // 新記録の場合は今回のタイム、そうでない場合は過去の自己ベストタイムを返します
        if (isNewRecord)
        {
            return m_model.ClearTime;
        }
        else
        {
            return m_model.PersonalBestTime > DEFAULT_START_TIME ? m_model.PersonalBestTime : m_model.ClearTime;
        }
    }

    /// <summary>
    /// 即座に適切なベストタイムを表示します（アニメーションなし）。
    /// </summary>
    public void RefreshViewDirect()
    {
        StopAllTweens();
        EnsureReferences();

        float targetTime = GetTargetBestTime(out bool isNewRecord);

        // View の表示更新
        if (m_view != null)
        {
            m_view.DisplayTime(targetTime);
        }

        // 新記録時のみ赤点滅を開始
        if (isNewRecord)
        {
            StartBlinking();
        }
        else
        {
            StopBlinking();
        }
    }

    /// <summary>
    /// 設定に応じてカウントアップ演出を行い、ベストタイムを表示します。
    /// </summary>
    public void RefreshBestTime()
    {
        EnsureReferences();

        if (m_view == null || m_model == null)
        {
            return;
        }

        StopAllTweens();

        float targetTime = GetTargetBestTime(out bool isNewRecord);

        // カウントアップ不使用時は即時更新
        if (!m_useCountUp)
        {
            m_view.DisplayTime(targetTime);

            if (isNewRecord)
            {
                StartBlinking();
            }

            return;
        }

        // 0秒からカウントアップ演出を開始
        m_view.DisplayTime(DEFAULT_START_TIME);

        Sequence sequence = DOTween.Sequence();

        // 開始前ディレイの追加
        if (m_delay > DEFAULT_START_TIME)
        {
            sequence.AppendInterval(m_delay);
        }

        // カウントアップ数値のアニメーション
        sequence.Append(
            DOVirtual.Float(DEFAULT_START_TIME, targetTime, m_countUpDuration, value =>
            {
                if (m_view != null)
                {
                    m_view.DisplayTime(value);
                }
            })
            .SetEase(m_countUpEase)
        );

        sequence.SetUpdate(true);

        // カウント完了時の処理
        sequence.OnComplete(() =>
        {
            if (m_view != null)
            {
                m_view.DisplayTime(targetTime);
            }

            // 新記録達成時のみ赤点滅アニメーションを開始
            if (isNewRecord)
            {
                StartBlinking();
            }
            else
            {
                StopBlinking();
            }
        });

        m_countUpTween = sequence;
    }

    /// <summary>
    /// 赤点滅アニメーションを開始します。
    /// </summary>
    private void StartBlinking()
    {
        StopBlinking();

        if (m_blinkTargetImage == null)
        {
            return;
        }

        // 元の色と指定した赤色を交互にループ（Yoyo）させて点滅します
        m_blinkTween = m_blinkTargetImage.DOColor(m_blinkRedColor, m_blinkDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(INFINITE_LOOP, LoopType.Yoyo)
            .SetUpdate(true);
    }

    /// <summary>
    /// 赤点滅アニメーションを停止し、元の色に戻します。
    /// </summary>
    private void StopBlinking()
    {
        m_blinkTween?.Kill();

        if (m_blinkTargetImage != null)
        {
            m_blinkTargetImage.color = m_originalColor;
        }
    }

    /// <summary>
    /// 実行中のすべての Tween アニメーションを停止します。
    /// </summary>
    private void StopAllTweens()
    {
        m_countUpTween?.Kill();
        StopBlinking();
    }
}