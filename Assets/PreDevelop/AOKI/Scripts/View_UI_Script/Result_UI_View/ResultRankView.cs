using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// イージングの指定方法を定義する列挙型です。
/// </summary>
public enum EaseTypeMode
{
    STANDARD_EASE, // ドロップダウン（InExpo等）で選択
    CUSTOM_CURVE   // 緑色のグラフ（AnimationCurve）で手動作成
}

/// <summary>
/// クリアランクの画像表示および「溜めからの縮小 ＆ Alpha（透明度）フェード」演出を担当する View クラスです。
/// </summary>
public class ResultRankView : MonoBehaviour
{
    [Header("UI参照")]
    /// <summary>ランク画像表示用の Image オブジェクト。</summary>
    [Tooltip("ランク画像表示用の Image オブジェクト")]
    [SerializeField] private Image m_rankImage;

    [Header("ランク別スプライト設定")]
    /// <summary>S ランク表示時に使用する画像。</summary>
    [Tooltip("S ランク表示時に使用する画像")]
    [SerializeField] private Sprite m_spriteS;

    /// <summary>A ランク表示時に使用する画像。</summary>
    [Tooltip("A ランク表示時に使用する画像")]
    [SerializeField] private Sprite m_spriteA;

    /// <summary>B ランク表示時に使用する画像。</summary>
    [Tooltip("B ランク表示時に使用する画像")]
    [SerializeField] private Sprite m_spriteB;

    /// <summary>C ランク表示時に使用する画像。</summary>
    [Tooltip("C ランク表示時に使用する画像")]
    [SerializeField] private Sprite m_spriteC;

    [Header("演出設定（スケール）")]
    /// <summary>アニメーションさせるターゲット Transform。</summary>
    [Tooltip("アニメーションさせるターゲット Transform")]
    [SerializeField] private Transform m_popTarget;

    /// <summary>出現時の初期の大きさ倍率。</summary>
    [Tooltip("出現時の初期の大きさ")]
    [SerializeField] private float m_startScaleMultiplier = 1.5f;

    /// <summary>元のサイズに戻るまでの時間（秒）。</summary>
    [Tooltip("元のサイズに戻るまでの時間（秒）")]
    [SerializeField] private float m_popDuration = 1.5f;

    [Header("イージング設定（溜め演出）")]
    /// <summary>イージングの指定方法。</summary>
    [Tooltip("イージングの指定方法（STANDARD_EASE: プリセット選択 / CUSTOM_CURVE: 手動グラフ描画）")]
    [SerializeField] private EaseTypeMode m_easeMode = EaseTypeMode.STANDARD_EASE;

    /// <summary>プリセットイージング（InExpo や InQuint を推奨）。</summary>
    [Tooltip("★【溜めて出す場合】InExpo や InQuint を選択してください")]
    [SerializeField] private Ease m_popEase = Ease.InExpo;

    /// <summary>手動カーブイージング。</summary>
    [Tooltip("★【手動カーブ用】Inspectorで「溜めて一気に出す」カーブを自由に描けます")]
    [SerializeField]
    private AnimationCurve m_customCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 0f),      // スタート（水平に溜める）
        new Keyframe(1f, 1f, 3f, 0f)       // 一気に立ち上がって着地
    );

    [Header("演出設定（Alpha / 透明度）")]
    /// <summary>縮小演出と同時に Alpha 値を 0 から 1 へフェードインさせるか。</summary>
    [Tooltip("縮小演出と同時に Alpha 値を 0 から 1へフェードインさせるか")]
    [SerializeField] private bool m_useFadeIn = true;

    /// <summary>フェードインにかかる時間（秒）。</summary>
    [Tooltip("フェードインにかかる時間（秒）")]
    [SerializeField] private float m_fadeDuration = 1.5f;

    /// <summary>フェードのイージング。</summary>
    [Tooltip("フェードのイージング（InQuint等にすると後半に一気に不透明になります）")]
    [SerializeField] private Ease m_fadeEase = Ease.InQuint;

    /// <summary>初期スケール記憶用変数。</summary>
    private Vector3 m_originalScale = Vector3.one;

    /// <summary>初期 Alpha 値記憶用変数。</summary>
    private float m_originalAlpha = 1f;

    /// <summary>初期化完了フラグ。</summary>
    private bool m_isInitialized = false;

    /// <summary>スケールアニメーション用 Tween 参照。</summary>
    private Tween m_popTween;

    /// <summary>フェードアニメーション用 Tween 参照。</summary>
    private Tween m_fadeTween;

    private void Awake()
    {
        InitializeComponents();
        Hide();
    }

    private void OnDisable()
    {
        KillTweens();
    }

    private void OnDestroy()
    {
        KillTweens();
    }

    /// <summary>
    /// 各コンポーネントの参照取得と初期設定を行います。
    /// </summary>
    private void InitializeComponents()
    {
        if (m_isInitialized) return;

        if (m_rankImage == null)
        {
            m_rankImage = GetComponentInChildren<Image>();
        }

        if (m_popTarget == null)
        {
            m_popTarget = (m_rankImage != null) ? m_rankImage.transform : transform;
        }

        if (m_popTarget != null && m_popTarget.localScale != Vector3.zero)
        {
            m_originalScale = m_popTarget.localScale;
        }

        if (m_rankImage != null)
        {
            m_originalAlpha = m_rankImage.color.a;
        }

        m_isInitialized = true;
    }

    /// <summary>
    /// ランク表示 UI を非表示（Alpha 0、Scale 0）にします。
    /// </summary>
    public void Hide()
    {
        InitializeComponents();
        KillTweens();

        if (m_rankImage != null)
        {
            Color c = m_rankImage.color;
            c.a = 0f;
            m_rankImage.color = c;
            m_rankImage.gameObject.SetActive(false);
        }

        if (m_popTarget != null)
        {
            m_popTarget.localScale = Vector3.zero;
            m_popTarget.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// アニメーション（ポップアップ＆フェードイン）を再生してクリアランク画像を表示します。
    /// </summary>
    /// <param name="rank">表示するランク（RankType）。</param>
    public void DisplayRankWithAnimation(RankType rank)
    {
        InitializeComponents();
        KillTweens();

        if (m_rankImage != null)
        {
            Sprite targetSprite = GetSpriteByRank(rank);
            if (targetSprite != null)
            {
                m_rankImage.sprite = targetSprite;
            }
            m_rankImage.gameObject.SetActive(true);
        }

        // 1. スケールアニメーション
        if (m_popTarget != null)
        {
            m_popTarget.gameObject.SetActive(true);

            if (m_originalScale == Vector3.zero)
            {
                m_originalScale = Vector3.one;
            }

            m_popTarget.localScale = m_originalScale * m_startScaleMultiplier;

            m_popTween = m_popTarget.DOScale(m_originalScale, m_popDuration).SetUpdate(true);

            // モードに応じてイージングを適用
            if (m_easeMode == EaseTypeMode.STANDARD_EASE)
            {
                m_popTween.SetEase(m_popEase);
            }
            else
            {
                m_popTween.SetEase(m_customCurve);
            }
        }

        // 2. フェードインアニメーション
        if (m_rankImage != null)
        {
            if (m_useFadeIn)
            {
                Color c = m_rankImage.color;
                c.a = 0f;
                m_rankImage.color = c;

                float duration = (m_fadeDuration > 0f) ? m_fadeDuration : m_popDuration;

                m_fadeTween = m_rankImage.DOFade(m_originalAlpha, duration).SetUpdate(true);

                if (m_easeMode == EaseTypeMode.STANDARD_EASE)
                {
                    m_fadeTween.SetEase(m_fadeEase);
                }
                else
                {
                    m_fadeTween.SetEase(m_customCurve);
                }
            }
            else
            {
                Color c = m_rankImage.color;
                c.a = m_originalAlpha;
                m_rankImage.color = c;
            }
        }
    }

    /// <summary>
    /// アニメーションを行わず、即座にクリアランク画像を表示します。
    /// </summary>
    /// <param name="rank">表示するランク（RankType）。</param>
    public void DisplayRankDirect(RankType rank)
    {
        InitializeComponents();
        KillTweens();

        if (m_rankImage != null)
        {
            Sprite targetSprite = GetSpriteByRank(rank);
            if (targetSprite != null)
            {
                m_rankImage.sprite = targetSprite;
            }

            Color c = m_rankImage.color;
            c.a = m_originalAlpha;
            m_rankImage.color = c;
            m_rankImage.gameObject.SetActive(true);
        }

        if (m_popTarget != null)
        {
            m_popTarget.localScale = m_originalScale;
            m_popTarget.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// ランクの種類に応じたスプライトを取得します。
    /// </summary>
    /// <param name="rank">対象のランク。</param>
    /// <returns>対応する Sprite。</returns>
    private Sprite GetSpriteByRank(RankType rank)
    {
        switch (rank)
        {
            case RankType.S:
                return m_spriteS;

            case RankType.A:
                return m_spriteA;

            case RankType.B:
                return m_spriteB;

            case RankType.C:
                return m_spriteC;

            default:
                return null;
        }
    }

    /// <summary>
    /// 実行中の Tween アニメーションを停止・破棄します。
    /// </summary>
    private void KillTweens()
    {
        if (m_popTween != null)
        {
            m_popTween.Kill();
            m_popTween = null;
        }

        if (m_fadeTween != null)
        {
            m_fadeTween.Kill();
            m_fadeTween = null;
        }
    }
}