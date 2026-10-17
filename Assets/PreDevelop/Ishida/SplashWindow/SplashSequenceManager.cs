using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using DG.Tweening;

/// <summary>
/// スプラッシュ画面の表示とシーン遷移を管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class SplashSequenceManager : MonoBehaviour
{
    /// <summary>
    /// ロゴの表示情報です。
    /// </summary>
    [Serializable]
    public struct LogoData
    {
        // ロゴオブジェクト
        [SerializeField]
        private GameObject m_sprite;

        // ロゴの表示時間
        [SerializeField, Min(0.0f)]
        private float m_displayTime;

        /// <summary>
        /// ロゴオブジェクトを取得します。
        /// </summary>
        public GameObject Sprite => m_sprite;

        /// <summary>
        /// 表示時間を取得します。
        /// </summary>
        public float DisplayTime => m_displayTime;
    }

    [Header("コンポーネント設定")]

    // フェード用の黒色Image
    [SerializeField]
    private Image m_fadeImage;

    [Header("フェード設定")]

    // フェードイン時間
    [SerializeField, Min(0.0f)]
    private float m_fadeInDuration = 1.0f;

    // フェードインのイージング
    [SerializeField]
    private Ease m_fadeInEase = Ease.InOutSine;

    // フェードアウト時間
    [SerializeField, Min(0.0f)]
    private float m_fadeOutDuration = 1.0f;

    // フェードアウトのイージング
    [SerializeField]
    private Ease m_fadeOutEase = Ease.InOutSine;

    // ロゴ間の待機時間
    [SerializeField, Min(0.0f)]
    private float m_intervalDuration = 1.0f;

    [Header("ロゴと表示順")]

    // 表示するロゴ一覧
    [SerializeField]
    private LogoData[] m_logoSequence;

    [Header("遷移先のタイトルシーン")]

    // 遷移先のシーン名
    [SerializeField]
    private string m_titleSceneName = "TitleScene";

    // 実行中のフェードTween
    private Tween m_fadeTween;

    // スキップ要求フラグ
    private bool m_isSkipRequested = false;

    private void Awake()
    {
        // 必須コンポーネントの確認
        if (m_fadeImage == null)
        {
            Debug.LogError("フェード用Imageが設定されていません。", this);
            enabled = false;
            return;
        }

        // フェード画像を黒色・不透明に初期化
        m_fadeImage.color = Color.black;
        m_fadeImage.raycastTarget = false;

        // すべてのロゴを非表示にする
        if (m_logoSequence != null)
        {
            foreach (LogoData logo in m_logoSequence)
            {
                if (logo.Sprite != null)
                {
                    logo.Sprite.SetActive(false);
                }
            }
        }
    }

    private void Start()
    {
        StartCoroutine(PlayLogoSequence());
    }

    private void OnDisable()
    {
        // 実行中の処理を停止
        StopAllCoroutines();

        if (m_fadeTween != null && m_fadeTween.IsActive())
        {
            m_fadeTween.Kill();
        }

        m_fadeTween = null;
    }

    /// <summary>
    /// スキップ入力を受け取ります。
    /// </summary>
    /// <param name="context">入力情報。</param>
    public void OnSubmit(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        m_isSkipRequested = true;

        // 実行中のフェードを停止
        if (m_fadeTween != null && m_fadeTween.IsActive())
        {
            m_fadeTween.Kill();
        }
    }

    /// <summary>
    /// ロゴを順番に表示します。
    /// </summary>
    private IEnumerator PlayLogoSequence()
    {
        if (m_logoSequence != null)
        {
            foreach (LogoData logo in m_logoSequence)
            {
                if (logo.Sprite == null)
                {
                    Debug.LogWarning("ロゴが設定されていません。", this);
                    continue;
                }

                // ロゴを表示
                logo.Sprite.SetActive(true);

                // フェードイン
                yield return FadeAlpha(
                    0.0f,
                    m_fadeInDuration,
                    m_fadeInEase
                );

                // スキップされていなければ表示時間を待機
                if (!m_isSkipRequested)
                {
                    yield return WaitDisplayTime(logo.DisplayTime);
                }

                // スキップされていなければフェードアウト
                if (!m_isSkipRequested)
                {
                    yield return FadeAlpha(
                        1.0f,
                        m_fadeOutDuration,
                        m_fadeOutEase
                    );
                }

                // スキップ時も確実に黒画面にする
                SetFadeAlpha(1.0f);

                // ロゴを非表示
                logo.Sprite.SetActive(false);

                // スキップ要求をリセット
                m_isSkipRequested = false;

                // ロゴ間の待機
                yield return WaitDisplayTime(m_intervalDuration);

                // 次のロゴに備えて入力をリセット
                m_isSkipRequested = false;
            }
        }

        // タイトルシーンへ遷移
        SceneManager.LoadScene(m_titleSceneName);
    }

    /// <summary>
    /// DOTweenを使用してフェード画像の透明度を変更します。
    /// </summary>
    /// <param name="targetAlpha">目標透明度。</param>
    /// <param name="duration">フェード時間。</param>
    /// <param name="ease">イージングの種類。</param>
    private IEnumerator FadeAlpha(
        float targetAlpha,
        float duration,
        Ease ease)
    {
        // フェード時間が0なら即座に変更
        if (duration <= 0.0f)
        {
            SetFadeAlpha(targetAlpha);
            yield break;
        }

        // フェードTweenを生成
        m_fadeTween = m_fadeImage
            .DOFade(targetAlpha, duration)
            .SetEase(ease)
            .SetUpdate(true)
            .SetAutoKill(true);

        // フェード完了または中断まで待機
        yield return m_fadeTween.WaitForCompletion();

        m_fadeTween = null;

        // スキップされていなければ目標透明度に設定
        if (!m_isSkipRequested)
        {
            SetFadeAlpha(targetAlpha);
        }
    }

    /// <summary>
    /// 指定時間待機します。スキップ入力で終了します。
    /// </summary>
    /// <param name="displayTime">待機時間。</param>
    private IEnumerator WaitDisplayTime(float displayTime)
    {
        float timer = 0.0f;

        while (timer < displayTime && !m_isSkipRequested)
        {
            timer += Time.unscaledDeltaTime;

            yield return null;
        }
    }

    /// <summary>
    /// フェード画像の透明度を設定します。
    /// </summary>
    /// <param name="alpha">透明度。</param>
    private void SetFadeAlpha(float alpha)
    {
        Color color = m_fadeImage.color;

        color.a = Mathf.Clamp01(alpha);

        m_fadeImage.color = color;
    }
}