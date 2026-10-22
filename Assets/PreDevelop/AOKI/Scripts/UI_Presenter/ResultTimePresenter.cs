using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// カウントアップ演出時間の計算方式を定義する列挙型です。
/// </summary>
public enum CountUpMode
{
    [Tooltip("タイムの長さに関わらず、アニメーション時間を常に一定（例: 2.5秒）に統一します")]
    COUNT_UP_MODE_FIXED_DURATION,

    [Tooltip("1秒間に進むカウント速度を一定にし、タイムの長さに比例してアニメーション時間を可変にします")]
    COUNT_UP_MODE_FIXED_SPEED
}

/// <summary>
/// ResultTimeModel から「クリアタイム（ClearTime）」を取得し、
/// 上の桁（分・秒の10位）を最初の瞬間に完全固定し、最後の「ミリ秒?1秒分」のみを超減速回転させる Presenter クラスです。
/// </summary>
public class ResultBestTimePresenter : MonoBehaviour
{
    private const float DEFAULT_COUNT_UP_DURATION = 2.5f;
    private const float DEFAULT_SPEED_PER_SECOND = 10f;
    private const float DEFAULT_MIN_DURATION = 0.8f;
    private const float DEFAULT_MAX_DURATION = 5.0f;
    private const float DEFAULT_DELAY = 0.3f;
    private const float DEFAULT_FAST_PHASE_DURATION = 0.15f;
    private const float DEFAULT_SLOW_PHASE_REMAINING_SECONDS = 1.5f;
    private const float DEFAULT_PUNCH_STRENGTH = 0.12f;
    private const float DEFAULT_PUNCH_DURATION = 0.35f;

    [Header("View と Model の参照")]
    /// <summary>タイム表示用 View コンポーネント。</summary>
    [SerializeField] private ResultTimeViewNow m_view;

    /// <summary>タイムデータ保持用 Model コンポーネント。</summary>
    [SerializeField] private ResultTimeModel m_model;

    [Header("表示演出設定")]
    /// <summary>有効化時に自動で再生するかどうか。</summary>
    [Tooltip("チェックを入れると開始（有効化）時に自動でタイムを表示・カウントアップします")]
    [SerializeField] private bool m_playOnStart = true;

    /// <summary>数字演出（カウントアップ）を行うかどうか。</summary>
    [Tooltip("チェックを入れると数字演出を行います（オフなら即時表示）")]
    [SerializeField] private bool m_useCountUp = true;

    /// <summary>アニメーション時間の計算方式。</summary>
    [Tooltip("アニメーション時間の計算方式を選択")]
    [SerializeField] private CountUpMode m_countUpMode = CountUpMode.COUNT_UP_MODE_FIXED_DURATION;

    /// <summary>一定統一時のアニメーション時間（秒）。</summary>
    [Tooltip("【FixedDuration用】アニメーション時間（秒）。クリアタイムに関わらずこの時間で一定に統一されます")]
    [SerializeField] private float m_countUpDuration = DEFAULT_COUNT_UP_DURATION;

    /// <summary>速度一定統一時の1秒あたりの加算量。</summary>
    [Tooltip("【FixedSpeed用】1秒あたりに増えるタイム量（例: 10 なら 1秒間で10秒分進む）")]
    [SerializeField] private float m_speedPerSecond = DEFAULT_SPEED_PER_SECOND;

    /// <summary>速度一定統一時の最小アニメーション時間（秒）。</summary>
    [Tooltip("【FixedSpeed用】アニメーション時間の最小値（秒）")]
    [SerializeField] private float m_minDuration = DEFAULT_MIN_DURATION;

    /// <summary>速度一定統一時の最大アニメーション時間（秒）。</summary>
    [Tooltip("【FixedSpeed用】アニメーション時間の最大値（秒）")]
    [SerializeField] private float m_maxDuration = DEFAULT_MAX_DURATION;

    /// <summary>演出開始までの待ち時間（秒）。</summary>
    [Tooltip("演出が始まるまでの待ち時間（秒）")]
    [SerializeField] private float m_delay = DEFAULT_DELAY;

    [Header("下桁粘り演出設定（絶対時間制御）")]
    /// <summary>最初の爆速フェーズに使う時間（秒）。</summary>
    [Tooltip("★【上の桁の即時固定】開始からこの秒数（例: 0.15秒）で上の桁（分や10秒の位）を速効で到達・固定します")]
    [SerializeField] private float m_fastPhaseDuration = DEFAULT_FAST_PHASE_DURATION;

    /// <summary>余韻フェーズで描画・回転させる目標タイム手前の秒数（秒）。</summary>
    [Tooltip("★【ミリ秒粘り】目標タイムの「残り何秒手前」から超減速を始めるか（例: 1.5 なら最後の1.5秒分だけを長く回転させます）")]
    [SerializeField] private float m_slowPhaseRemainingSeconds = DEFAULT_SLOW_PHASE_REMAINING_SECONDS;

    /// <summary>後半の余韻フェーズに適用する減速イージング。</summary>
    [Tooltip("後半（ミリ秒がじわ～っと粘るフェーズ）の減速イージング（OutExpo 推奨）")]
    [SerializeField] private Ease m_slowPhaseEase = Ease.OutExpo;

    [Header("着地確定インパクト（余韻演出）")]
    /// <summary>カウント完了時に View を少しバウンド（パンチ演出）させて着地感を強調するかどうか。</summary>
    [Tooltip("チェックを入れるとカウント完了時にタイム文字がポンと着地インパクトを起こします")]
    [SerializeField] private bool m_usePunchOnComplete = true;

    /// <summary>パンチ演出の強さ。</summary>
    [Tooltip("完了時の拡大パンチの強さ（例: 0.12）")]
    [SerializeField] private float m_punchStrength = DEFAULT_PUNCH_STRENGTH;

    /// <summary>パンチ演出の時間（秒）。</summary>
    [Tooltip("パンチ演出の持続時間（秒）")]
    [SerializeField] private float m_punchDuration = DEFAULT_PUNCH_DURATION;

    /// <summary>アニメーション制御用 Tween。</summary>
    private Tween m_countUpTween;

    /// <summary>パンチ演出用 Tween。</summary>
    private Tween m_punchTween;

    private void Awake()
    {
        EnsureReferences();
    }

    private void OnEnable()
    {
        EnsureReferences();

        if (m_model != null)
        {
            m_model.OnTimeChanged += OnModelTimeChanged;
        }

        if (m_playOnStart && m_model != null)
        {
            RefreshBestTime();
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

    /// <summary>
    /// 未設定の参照を自動取得します。
    /// </summary>
    private void EnsureReferences()
    {
        if (m_view == null)
        {
            m_view = GetComponent<ResultTimeViewNow>();
        }

        if (m_model == null)
        {
            m_model = FindFirstObjectByType<ResultTimeModel>();
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
    /// ResultTimeModel からクリアタイムを取得します。
    /// </summary>
    /// <returns>クリアタイム（秒）。</returns>
    public float GetClearTime()
    {
        if (m_model == null) return 0f;
        return m_model.ClearTime;
    }

    /// <summary>
    /// 即座にクリアタイムを表示します（アニメーションなし）。
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
    }

    /// <summary>
    /// クリアタイムを表示します（上の桁を一瞬で固定し、ミリ秒・下桁のみを長く粘らせる余韻アニメーション付き）。
    /// </summary>
    public void RefreshBestTime()
    {
        EnsureReferences();
        if (m_view == null || m_model == null) return;

        StopAllTweens();

        float clearTime = GetClearTime();

        if (!m_useCountUp)
        {
            m_view.DisplayTime(clearTime);
            return;
        }

        float totalDuration = CalculateDuration(clearTime);

        Sequence seq = DOTween.Sequence();

        if (m_delay > 0f)
        {
            seq.AppendInterval(m_delay);
        }

        // 高速フェーズの秒数と低速フェーズの秒数を算出
        float fastDuration = Mathf.Min(m_fastPhaseDuration, totalDuration * 0.3f);
        float slowDuration = Mathf.Max(0.1f, totalDuration - fastDuration);

        // 高速フェーズで到達するタイム（例: 12:50.19 の場合、1.5秒手前の 12:48.69 まで一気に持っていく）
        float slowStartThreshold = Mathf.Max(0f, clearTime - m_slowPhaseRemainingSeconds);

        // フェーズ1: 最初の 0.15秒 で目標直前（ClearTime - 1.5秒）まで一気に到達して「分」等を即座に固定！
        seq.Append(
            DOVirtual.Float(0f, slowStartThreshold, fastDuration, currentDisplayTime =>
            {
                if (m_view != null)
                {
                    m_view.DisplayTime(currentDisplayTime);
                }
            })
            .SetEase(Ease.OutQuad)
        );

        // フェーズ2: 残りの約 2.35秒間 を贅沢に使い、最後の「1.5秒分（＝ミリ秒桁のみ）」を OutExpo で超減速回転！
        seq.Append(
            DOVirtual.Float(slowStartThreshold, clearTime, slowDuration, currentDisplayTime =>
            {
                if (m_view != null)
                {
                    m_view.DisplayTime(currentDisplayTime);
                }
            })
            .SetEase(m_slowPhaseEase)
        );

        seq.SetUpdate(true); // timeScale = 0（ポーズ中）対応

        seq.OnComplete(() =>
        {
            if (m_view != null)
            {
                m_view.DisplayTime(clearTime);

                // 着地確定インパクト（DOPunchScale）で目に見える余韻を生み出す
                if (m_usePunchOnComplete)
                {
                    m_punchTween?.Kill();
                    m_view.transform.localScale = Vector3.one;
                    m_punchTween = m_view.transform
                        .DOPunchScale(Vector3.one * m_punchStrength, m_punchDuration, 2, 0.5f)
                        .SetUpdate(true);
                }
            }
        });

        m_countUpTween = seq;
    }

    /// <summary>
    /// 設定モードに応じてアニメーション時間（秒）を算出します。
    /// </summary>
    /// <param name="clearTime">クリアタイム（秒）。</param>
    /// <returns>計算されたアニメーション秒数。</returns>
    private float CalculateDuration(float clearTime)
    {
        if (m_countUpMode == CountUpMode.COUNT_UP_MODE_FIXED_DURATION)
        {
            return Mathf.Max(0.1f, m_countUpDuration);
        }
        else
        {
            if (m_speedPerSecond <= 0f) return m_countUpDuration;
            float calculated = clearTime / m_speedPerSecond;
            return Mathf.Clamp(calculated, m_minDuration, m_maxDuration);
        }
    }

    /// <summary>
    /// 実行中の全 Tween アニメーションを停止・破棄します。
    /// </summary>
    private void StopAllTweens()
    {
        if (m_countUpTween != null)
        {
            m_countUpTween.Kill();
            m_countUpTween = null;
        }

        if (m_punchTween != null)
        {
            m_punchTween.Kill();
            m_punchTween = null;

            if (m_view != null)
            {
                m_view.transform.localScale = Vector3.one;
            }
        }
    }
}