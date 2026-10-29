using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// ストーリー1ページ分のデータです。
/// </summary>
[System.Serializable]
public struct StoryPage
{
    /// <summary>
    /// ストーリー画像。
    /// </summary>
    [FormerlySerializedAs("storyTexture")]
    [SerializeField]
    private Sprite m_storyTexture;

    /// <summary>
    /// ストーリーテキスト。
    /// </summary>
    [FormerlySerializedAs("storyText")]
    [SerializeField]
    [TextArea(2, 5)]
    private string m_storyText;

    /// <summary>
    /// ストーリー動画。設定されている場合は、画像の代わりに再生されます。
    /// </summary>
    [SerializeField]
    private VideoClip m_storyVideo;

    /// <summary>
    /// ストーリー画像を取得します。
    /// </summary>
    public Sprite StoryTexture => m_storyTexture;

    /// <summary>
    /// ストーリーテキストを取得します。
    /// </summary>
    public string StoryText => m_storyText;

    /// <summary>
    /// ストーリー動画を取得します。設定されていない場合は null です。
    /// </summary>
    public VideoClip StoryVideo => m_storyVideo;
}

/// <summary>
/// ストーリー画面のページ送りとワイプ演出を管理します。
/// </summary>
public class StoryManager : MonoBehaviour
{
    // ---------- 定数 ----------

    /// <summary>
    /// テキストがすべて表示されてから、矢印が出現するまでの初期時間（秒）。
    /// </summary>
    private const float DEFAULT_ARROW_APPEAR_TIME = 1.0f;

    /// <summary>
    /// ワイプアウトアニメーションの初期ステート名。
    /// </summary>
    private const string DEFAULT_WIPE_OUT_STATE_NAME = "WipeOut";

    /// <summary>
    /// ワイプインアニメーションの初期ステート名。
    /// </summary>
    private const string DEFAULT_WIPE_IN_STATE_NAME = "WipeIn";

    /// <summary>
    /// ワイプアニメーションを再生する Animator のレイヤー番号。
    /// </summary>
    private const int WIPE_ANIMATOR_LAYER_INDEX = 0;

    /// <summary>
    /// アニメーション終了とみなす正規化時間。
    /// </summary>
    private const float ANIMATION_END_NORMALIZED_TIME = 1.0f;

    /// <summary>
    /// 矢印の点滅速度の初期値（1秒あたりの点滅回数）。
    /// </summary>
    private const float DEFAULT_ARROW_BLINK_SPEED = 1.0f;

    /// <summary>
    /// 矢印の点滅時の最小アルファ値の初期値。
    /// </summary>
    private const float DEFAULT_ARROW_BLINK_MIN_ALPHA = 0.0f;

    /// <summary>
    /// 矢印が完全に見えているときのアルファ値。
    /// </summary>
    private const float ARROW_MAX_ALPHA = 1.0f;

    /// <summary>
    /// 動画の0秒目が描かれるまで待つ最大時間（秒）。これを過ぎたら待たずに進めます。
    /// </summary>
    private const float VIDEO_READY_TIMEOUT = 5.0f;

    /// <summary>
    /// 末尾に表示するカーソル文字。
    /// </summary>
    private const string CURSOR_CHARACTER = "|";

    /// <summary>
    /// カーソルの点滅間隔の初期値（秒）。
    /// </summary>
    private const float DEFAULT_CURSOR_BLINK_INTERVAL = 0.5f;

    /// <summary>
    /// Inspector で入力された「\n」（バックスラッシュと n の2文字）。
    /// </summary>
    private const string ESCAPED_NEW_LINE = "\\n";

    /// <summary>
    /// 本物の改行文字。
    /// </summary>
    private const string NEW_LINE = "\n";

    /// <summary>
    /// 透明にするためのリッチテキストタグ。
    /// </summary>
    private const string TRANSPARENT_TAG = "<alpha=#00>";

    /// <summary>
    /// 透明にしたあと、不透明へ戻すためのリッチテキストタグ。
    /// Vertex Color のアルファを 255 未満にしている場合は、その値に合わせてください。
    /// </summary>
    private const string OPAQUE_TAG = "<alpha=#FF>";

    /// <summary>
    /// 字形の右側に空白がある、末尾の句読点。
    /// </summary>
    private const string TRAILING_PUNCTUATION = "。、";

    // ---------- Inspector 設定値 ----------

    [Header("文字の表示速度")]
    /// <summary>
    /// 1文字あたりの表示時間（秒）。
    /// </summary>
    [SerializeField]
    private float m_interval = 0.05f;

    [Header("カーソル「|」の設定")]
    /// <summary>
    /// カーソルの点滅間隔（秒）。表示と非表示が切り替わるまでの時間です。
    /// 0.5 なら、1回の点滅（表示→非表示→表示）に 1 秒かかります。
    /// </summary>
    [Min(0.05f)]
    [SerializeField]
    private float m_cursorBlinkInterval = DEFAULT_CURSOR_BLINK_INTERVAL;

    [Header("矢印設定")]
    /// <summary>
    /// テキストがすべて表示されてから、矢印が出現するまでの時間（秒）。
    /// </summary>
    [FormerlySerializedAs("APPEARANCE_ARROW_TIME")]
    [SerializeField]
    private float m_appearanceArrowTime = DEFAULT_ARROW_APPEAR_TIME;

    [Header("コンポーネント類")]
    /// <summary>
    /// ストーリー画像を表示する Image。
    /// </summary>
    [SerializeField]
    private Image m_storyImage;

    /// <summary>
    /// ストーリーテキストを表示する TextMeshProUGUI。
    /// Alignment の水平方向は Center にしてください。
    /// </summary>
    [SerializeField]
    private TextMeshProUGUI m_storyText;

    /// <summary>
    /// 矢印を表示する Image。
    /// </summary>
    [SerializeField]
    private Image m_arrowImage;

    [Header("動画")]
    /// <summary>
    /// 動画を再生する VideoPlayer（Play On Awake はオフにしてください）。
    /// </summary>
    [SerializeField]
    private VideoPlayer m_videoPlayer;

    /// <summary>
    /// 動画を表示する RawImage（Texture 欄は空のままで構いません）。
    /// </summary>
    [SerializeField]
    private RawImage m_videoImage;

    /// <summary>
    /// 動画をループ再生するかどうか。
    /// </summary>
    [SerializeField]
    private bool m_loopVideo = true;

    [Header("ストーリーページ")]
    /// <summary>
    /// ストーリーのページ一覧。
    /// </summary>
    [SerializeField]
    private StoryPage[] m_storyPages;

    [Header("入力関連")]
    /// <summary>
    /// 決定キーの入力アクション。
    /// </summary>
    [SerializeField]
    private InputActionReference m_enterActionRef;

    /// <summary>
    /// スキップキーの入力アクション（現在は未使用）。
    /// </summary>
    [SerializeField]
    private InputActionReference m_skipActionRef;

    [Header("ワイプアニメーション")]
    /// <summary>
    /// ワイプ演出用の Animator。
    /// </summary>
    [SerializeField]
    private Animator m_wipeAnimator;

    /// <summary>
    /// 画面を暗くするアニメーションのステート名。
    /// </summary>
    [SerializeField]
    private string m_animationOutStateName = DEFAULT_WIPE_OUT_STATE_NAME;

    /// <summary>
    /// 画面を明るくするアニメーションのステート名。
    /// 空にすると、起動時とページ切り替え時のフェードインを行いません。
    /// </summary>
    [SerializeField]
    private string m_animationInStateName = DEFAULT_WIPE_IN_STATE_NAME;

    /// <summary>
    /// 矢印の点滅速度（1秒あたりの点滅回数）。
    /// </summary>
    [SerializeField]
    private float m_arrowBlinkSpeed = DEFAULT_ARROW_BLINK_SPEED;

    /// <summary>
    /// 矢印が点滅するときの最小アルファ値。
    /// </summary>
    [Range(0.0f, 1.0f)]
    [SerializeField]
    private float m_arrowBlinkMinAlpha = DEFAULT_ARROW_BLINK_MIN_ALPHA;

    // ---------- 内部状態 ----------

    /// <summary>
    /// 現在表示しているページ番号。
    /// </summary>
    private int m_currentPage = 0;

    /// <summary>
    /// ページ表示後の経過時間（秒）。
    /// </summary>
    private float m_elapsedTime = 0.0f;

    /// <summary>
    /// ワイプ演出中かどうか。
    /// </summary>
    private bool m_isTransitioning = false;

    /// <summary>
    /// 矢印が表示されてからの点滅用の経過時間（秒）。
    /// </summary>
    private float m_arrowBlinkTimer = 0.0f;

    /// <summary>
    /// 実行中のタイプライター表示コルーチン。
    /// </summary>
    private Coroutine m_typewriterCoroutine;

    /// <summary>
    /// 表示中のメッセージ全文（カーソルを含まない）。
    /// </summary>
    private string m_currentMessage = string.Empty;

    /// <summary>
    /// テキストがすべて表示されたかどうか。
    /// </summary>
    private bool m_isTextComplete = false;

    /// <summary>
    /// 動画の映像を受け取る RenderTexture（実行時に作成します）。
    /// </summary>
    private RenderTexture m_videoTexture;

    /// <summary>
    /// 動画の再生を開始済みかどうか（false の間は、0秒目で止めて待機します）。
    /// </summary>
    private bool m_isVideoStarted = false;

    /// <summary>
    /// 動画の0秒目が描かれるのを待っている最中かどうか。
    /// </summary>
    private bool m_isVideoPending = false;

    // ---------- Unity イベント ----------

    /// <summary>
    /// 必須設定を確認し、最初のページの画像と文章を準備します。
    /// </summary>
    private void Awake()
    {
        // 必須設定が足りない場合
        if (!ValidateSettings())
        {
            // 以降の処理を止める
            enabled = false;
            return;
        }

        // 最初のページを準備する（文字の表示はフェードイン後に始める）
        ApplyPage(m_currentPage);

        // 矢印の待ち時間を初期化する
        ResetArrowTimer();

        // 入力を有効にする
        m_enterActionRef?.action.Enable();
        m_skipActionRef?.action.Enable();
    }

    /// <summary>
    /// 最初のページのフェードインを待ってから、文字の表示を始めます。
    /// </summary>
    private void Start()
    {
        // Awake で無効化されている場合は何もしない
        if (!enabled)
        {
            return;
        }

        StartCoroutine(IntroCoroutine());
    }

    /// <summary>
    /// 動画用の RenderTexture を解放します。
    /// </summary>
    private void OnDestroy()
    {
        // 動画のイベント登録を解除する
        if (m_videoPlayer != null)
        {
            m_videoPlayer.prepareCompleted -= OnVideoPrepared;
            m_videoPlayer.frameReady -= OnFirstFrameReady;
        }

        ReleaseVideoTexture();
    }

    /// <summary>
    /// 矢印の待ち時間の計測と決定キー入力を処理します。
    /// </summary>
    private void Update()
    {
        // ワイプ演出中は何もしない
        if (m_isTransitioning)
        {
            return;
        }

        // 経過時間を加算する
        CountElapsedTime();

        // 決定キーが押された場合
        if (IsEnterPressed())
        {
            // テキストが表示されている途中の場合
            if (!m_isTextComplete)
            {
                // 全文を表示する（矢印は、この1秒後に出現する）
                ShowAllText();
                return;
            }

            // 矢印が表示されている場合はページ切り替え演出を開始する
            if (m_arrowImage.enabled)
            {
                StartCoroutine(TransitPageCoroutine());
                return;
            }

            // 矢印が出る前は、ページを送らない
        }

        // スキップキーが押された場合
        if (IsSkipPressed())
        {
            return;
        }

        // 矢印の表示状態を更新する
        UpdateArrowVisibility();
    }

    // ---------- 初期化・ページ表示 ----------

    /// <summary>
    /// 必須の設定が揃っているか確認します。
    /// </summary>
    /// <returns>
    /// true：設定が揃っています。
    /// false：設定が不足しています。
    /// </returns>
    private bool ValidateSettings()
    {
        // ストーリーページが未設定の場合
        if (m_storyPages == null || m_storyPages.Length == 0)
        {
            Debug.LogWarning("ストーリーページが設定されていません");
            return false;
        }

        // 各コンポーネントが未設定の場合
        if (m_storyImage == null || m_storyText == null ||
            m_arrowImage == null || m_wipeAnimator == null)
        {
            Debug.LogWarning("必要なコンポーネントが設定されていません");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 指定ページの画像を入れ替え、文章を準備します。
    /// 文字の表示は StartTypewriter を呼ぶまで始まりません。
    /// </summary>
    /// <param name="pageIndex">表示するページ番号。</param>
    private void ApplyPage(int pageIndex)
    {
        // テクスチャを入れ替える
        m_storyImage.sprite = m_storyPages[pageIndex].StoryTexture;

        // 動画があるページなら、画像の代わりに動画を表示する
        bool hasVideo = PrepareVideo(m_storyPages[pageIndex].StoryVideo);
        m_storyImage.enabled = !hasVideo;
        if (m_videoImage != null)
        {
            m_videoImage.enabled = hasVideo;
        }

        // 前のタイプライター表示が動いていたら止める
        StopTypewriter();
        m_isTextComplete = false;

        // Inspector で入力された「\n」（2文字）を本物の改行に変換して保持する
        string message = m_storyPages[pageIndex].StoryText ?? string.Empty;
        m_currentMessage = message.Replace(ESCAPED_NEW_LINE, NEW_LINE);

        // フェードイン中は何も表示しない
        m_storyText.text = string.Empty;
    }

    /// <summary>
    /// 動画の再生を準備します。前のページの動画は止めます。
    /// </summary>
    /// <param name="clip">再生する動画。null の場合は動画を使いません。</param>
    /// <returns>
    /// true：動画を使います。
    /// false：動画を使いません。
    /// </returns>
    private bool PrepareVideo(VideoClip clip)
    {
        // 前のページの動画を止める
        if (m_videoPlayer != null)
        {
            m_videoPlayer.frameReady -= OnFirstFrameReady;
            m_videoPlayer.sendFrameReadyEvents = false;
            m_videoPlayer.Stop();
        }

        m_isVideoStarted = false;
        m_isVideoPending = false;

        // このページに動画がない場合
        if (clip == null)
        {
            return false;
        }

        // 動画用のコンポーネントが未設定の場合
        if (m_videoPlayer == null || m_videoImage == null)
        {
            Debug.LogWarning("動画用の VideoPlayer と RawImage が設定されていません");
            return false;
        }

        // 動画のサイズに合わせた RenderTexture を用意する
        int width = (int)clip.width;
        int height = (int)clip.height;
        if (m_videoTexture == null || m_videoTexture.width != width || m_videoTexture.height != height)
        {
            ReleaseVideoTexture();
            m_videoTexture = new RenderTexture(width, height, 0);
        }

        m_videoPlayer.playOnAwake = false;
        m_videoPlayer.source = VideoSource.VideoClip;
        m_videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        m_videoPlayer.targetTexture = m_videoTexture;
        m_videoPlayer.clip = clip;
        m_videoPlayer.isLooping = m_loopVideo;

        m_videoImage.texture = m_videoTexture;

        // 0秒目の映像が描かれるまでは、動画を透明にしておく
        SetVideoAlpha(0.0f);

        // 暗転している間に読み込んでおく（読み込めたら、0秒目を描いて待機する）
        m_videoPlayer.prepareCompleted -= OnVideoPrepared;
        m_videoPlayer.prepareCompleted += OnVideoPrepared;
        m_isVideoPending = true;
        m_videoPlayer.Prepare();

        return true;
    }

    /// <summary>
    /// 動画のあるページなら、動画の再生を始めます。
    /// </summary>
    private void StartVideo()
    {
        if (m_videoPlayer != null && m_videoImage != null && m_videoImage.enabled)
        {
            m_isVideoStarted = true;
            SetVideoAlpha(1.0f);

            // 0秒目で待機している場合は、そこから再生が続く
            m_videoPlayer.Play();
        }
    }

    /// <summary>
    /// 動画の読み込みが終わったら、0秒目の映像を描くために再生を始めます。
    /// </summary>
    /// <param name="source">読み込みが終わった VideoPlayer。</param>
    private void OnVideoPrepared(VideoPlayer source)
    {
        // すでに再生が始まっている場合は、何もしない
        if (m_isVideoStarted)
        {
            return;
        }

        // 最初の映像が描かれたら止めるため、通知を受け取る
        source.sendFrameReadyEvents = true;
        source.frameReady -= OnFirstFrameReady;
        source.frameReady += OnFirstFrameReady;
        source.Play();
    }

    /// <summary>
    /// 最初の映像が描かれたら、再生を止めて 0 秒目の映像を見せます。
    /// </summary>
    /// <param name="source">VideoPlayer。</param>
    /// <param name="frameIdx">描かれたフレーム番号。</param>
    private void OnFirstFrameReady(VideoPlayer source, long frameIdx)
    {
        source.frameReady -= OnFirstFrameReady;
        source.sendFrameReadyEvents = false;

        // 再生がすでに始まっている場合は止めない
        if (!m_isVideoStarted)
        {
            source.Pause();
        }

        // 0秒目の映像が描かれたので、動画を見えるようにする
        m_isVideoPending = false;
        SetVideoAlpha(1.0f);
    }

    /// <summary>
    /// 動画の0秒目が描かれるまで待ちます。動画がないページでは、すぐに終わります。
    /// 画面が暗い間に呼び、フェードインで後ろ側が見えるのを防ぎます。
    /// </summary>
    private IEnumerator WaitForVideoReadyCoroutine()
    {
        float elapsed = 0.0f;

        while (m_isVideoPending && elapsed < VIDEO_READY_TIMEOUT)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 時間内に描かれなかった場合は、待たずに進める
        if (m_isVideoPending)
        {
            Debug.LogWarning("動画の0秒目が描かれるのを待ちきれませんでした");
            m_isVideoPending = false;
            SetVideoAlpha(1.0f);
        }
    }

    /// <summary>
    /// 動画を表示する RawImage のアルファ値だけを変更します。
    /// </summary>
    /// <param name="alpha">設定するアルファ値（0〜1）。</param>
    private void SetVideoAlpha(float alpha)
    {
        if (m_videoImage == null)
        {
            return;
        }

        Color color = m_videoImage.color;
        color.a = alpha;
        m_videoImage.color = color;
    }

    /// <summary>
    /// 動画用の RenderTexture を解放します。
    /// </summary>
    private void ReleaseVideoTexture()
    {
        if (m_videoTexture != null)
        {
            m_videoTexture.Release();
            Destroy(m_videoTexture);
            m_videoTexture = null;
        }
    }

    /// <summary>
    /// 起動時のフェードインを待ってから、最初のページの文字表示を始めます。
    /// </summary>
    private IEnumerator IntroCoroutine()
    {
        // フェードイン中は入力と矢印の計測を止める
        m_isTransitioning = true;

        // 動画の0秒目が描かれるまで、暗いまま待つ
        yield return WaitForVideoReadyCoroutine();

        // ワイプインが設定されている場合
        if (!string.IsNullOrEmpty(m_animationInStateName))
        {
            // ワイプインを再生して待つ
            yield return StartCoroutine(PlayWipeAnimationCoroutine(m_animationInStateName));
        }

        // フェードインが終わってから動画と文字の表示を始める
        StartVideo();
        StartTypewriter();

        // 演出終了
        m_isTransitioning = false;
    }

    // ---------- 矢印 ----------

    /// <summary>
    /// 矢印の経過時間を 0 に戻し、矢印を非表示にします。
    /// </summary>
    private void ResetArrowTimer()
    {
        m_elapsedTime = 0.0f;
        m_arrowImage.enabled = false;

        // 次に表示されるときのために点滅状態を初期化する
        m_arrowBlinkTimer = 0.0f;
        SetArrowAlpha(ARROW_MAX_ALPHA);
    }

    /// <summary>
    /// テキストがすべて表示されていて、矢印が非表示の間、経過時間を加算します。
    /// </summary>
    private void CountElapsedTime()
    {
        // テキストが表示し終わっていて、矢印がまだ出ていない場合のみ加算する
        if (m_isTextComplete && !m_arrowImage.enabled)
        {
            m_elapsedTime += Time.deltaTime;
        }
    }

    /// <summary>
    /// 経過時間に応じて矢印の表示・非表示を切り替えます。
    /// </summary>
    private void UpdateArrowVisibility()
    {
        // 表示すべき状態かどうか
        bool shouldShow = m_elapsedTime >= m_appearanceArrowTime;

        // 現在の状態と異なる場合のみ切り替える
        if (m_arrowImage.enabled != shouldShow)
        {
            m_arrowImage.enabled = shouldShow;

            // 表示された瞬間は透明から始め、徐々に出現させる
            m_arrowBlinkTimer = 0.0f;
            SetArrowAlpha(m_arrowBlinkMinAlpha);
        }

        // 矢印が表示されている間は点滅させる
        if (m_arrowImage.enabled)
        {
            UpdateArrowBlink();
        }
    }

    /// <summary>
    /// 矢印のアルファ値を周期的に変化させて点滅させます。
    /// </summary>
    private void UpdateArrowBlink()
    {
        m_arrowBlinkTimer += Time.deltaTime;

        // 0〜1 を滑らかに往復する値（表示直後は 0 から始まるので、徐々に出現する）
        float wave = (1.0f - Mathf.Cos(m_arrowBlinkTimer * Mathf.PI * 2.0f * m_arrowBlinkSpeed)) * 0.5f;

        // 最小アルファ〜1 の範囲に変換して反映する
        SetArrowAlpha(Mathf.Lerp(m_arrowBlinkMinAlpha, ARROW_MAX_ALPHA, wave));
    }

    /// <summary>
    /// 矢印のアルファ値だけを変更します。
    /// </summary>
    /// <param name="alpha">設定するアルファ値（0〜1）。</param>
    private void SetArrowAlpha(float alpha)
    {
        Color color = m_arrowImage.color;
        color.a = alpha;
        m_arrowImage.color = color;
    }

    // ---------- 入力判定 ----------

    /// <summary>
    /// 決定キーが押されたかを判定します。
    /// </summary>
    /// <returns>
    /// true：このフレームで押されました。
    /// false：押されていません。
    /// </returns>
    private bool IsEnterPressed()
    {
        return m_enterActionRef != null && m_enterActionRef.action.WasPressedThisFrame();
    }

    /// <summary>
    /// スキップキーが押されたかを判定します。
    /// </summary>
    /// <returns>
    /// true：このフレームで押されました。
    /// false：押されていません。
    /// </returns>
    private bool IsSkipPressed()
    {
        return m_skipActionRef != null && m_skipActionRef.action.WasPressedThisFrame();
    }

    /// <summary>
    /// 現在のページが最後のページか判定します。
    /// </summary>
    /// <returns>
    /// true：最後のページです。
    /// false：次のページがあります。
    /// </returns>
    private bool IsLastPage()
    {
        return m_currentPage >= m_storyPages.Length - 1;
    }

    // ---------- タイプライター表示 ----------

    /// <summary>
    /// 準備済みの文章のタイプライター表示を最初から開始します。
    /// </summary>
    private void StartTypewriter()
    {
        StopTypewriter();
        m_typewriterCoroutine = StartCoroutine(Play());
    }

    /// <summary>
    /// テキストを順番に表示し、表示し終わったらカーソルを点滅させます。
    /// </summary>
    private IEnumerator Play()
    {
        // 0文字（カーソルだけ）から1文字ずつ表示する
        for (int i = 0; i < m_currentMessage.Length; i++)
        {
            // 直前に出した文字が改行なら、待たずに次の文字へ進む
            if (i > 0 && m_currentMessage[i - 1] == '\n')
            {
                continue;
            }

            ApplyText(i, true);
            yield return new WaitForSeconds(m_interval);
        }

        // 全文を表示して、カーソルの点滅へ移る
        m_isTextComplete = true;
        ApplyText(m_currentMessage.Length, true);
        yield return BlinkCursorCoroutine();
    }

    /// <summary>
    /// カーソルを点滅させ続けます。
    /// </summary>
    private IEnumerator BlinkCursorCoroutine()
    {
        bool isVisible = true;

        while (true)
        {
            yield return new WaitForSeconds(m_cursorBlinkInterval);
            isVisible = !isVisible;
            ApplyText(m_currentMessage.Length, isVisible);
        }
    }

    /// <summary>
    /// 指定文字数ぶんを表示し、その直後にカーソルを付けたテキストを設定します。
    /// 未表示の文字は入れないため、中央揃えにすると中央から広がって表示されます。
    /// 最後の行の先頭に透明な文字を足して、見た目が中央に来るよう幅を揃えます。
    /// 次の行以降は透明で入れておき、行数が増えても Y 座標が動かないようにします。
    /// </summary>
    /// <param name="visibleCount">表示する文字数。</param>
    /// <param name="isCursorVisible">カーソルを見せるかどうか。</param>
    private void ApplyText(int visibleCount, bool isCursorVisible)
    {
        string shown = m_currentMessage.Substring(0, visibleCount);

        // 点滅で幅が変わらないよう、消すときも透明で残す
        string cursor = isCursorVisible
            ? CURSOR_CHARACTER
            : TRANSPARENT_TAG + CURSOR_CHARACTER;

        // 最後の行の先頭に、カーソルと同じ幅の透明な文字を置く
        string padding = TRANSPARENT_TAG + CURSOR_CHARACTER + OPAQUE_TAG;

        // 末尾が句読点なら、その字形の空白ぶんも左側に足す
        if (shown.Length > 0 &&
            TRAILING_PUNCTUATION.IndexOf(shown[shown.Length - 1]) >= 0)
        {
            padding += TRANSPARENT_TAG + shown[shown.Length - 1] + OPAQUE_TAG;
        }

        // カーソルがあるのは最後の行だけなので、その行の先頭にだけ足す
        int lastLineStart = shown.LastIndexOf('\n') + 1;
        string body = shown.Insert(lastLineStart, padding);

        // まだ表示していない「次の行以降」を透明で入れて、行数（高さ）を固定する。
        // 現在の行の残りは入れないので、横方向の中央揃えには影響しない。
        int nextNewLine = m_currentMessage.IndexOf('\n', visibleCount);
        string reservedLines = nextNewLine >= 0
            ? TRANSPARENT_TAG + m_currentMessage.Substring(nextNewLine)
            : string.Empty;

        m_storyText.text = body + cursor + reservedLines;
    }

    /// <summary>
    /// タイプライター表示を止めて全文を表示し、カーソルを点滅させます。
    /// </summary>
    private void ShowAllText()
    {
        StopTypewriter();
        m_isTextComplete = true;
        ApplyText(m_currentMessage.Length, true);
        m_typewriterCoroutine = StartCoroutine(BlinkCursorCoroutine());
    }

    /// <summary>
    /// 実行中のタイプライター表示を止めます。
    /// </summary>
    private void StopTypewriter()
    {
        if (m_typewriterCoroutine != null)
        {
            StopCoroutine(m_typewriterCoroutine);
            m_typewriterCoroutine = null;
        }
    }

    // ---------- ページ切り替え・ワイプ演出 ----------

    /// <summary>
    /// ワイプ演出を挟んで次のページへ切り替えます。
    /// </summary>
    private IEnumerator TransitPageCoroutine()
    {
        // 連打防止のためにフラグを ON にする
        m_isTransitioning = true;

        // 次のページへ向かう間は、矢印を完全に見える状態にする
        SetArrowAlpha(ARROW_MAX_ALPHA);

        // ワイプアウト（画面を暗くする）を再生して待つ
        yield return StartCoroutine(PlayWipeAnimationCoroutine(m_animationOutStateName));

        // 最後のページだった場合
        if (IsLastPage())
        {
            // 何もせず演出を終了する
            Debug.Log("すべてのストーリーページが終了しました");
            m_isTransitioning = false;
            yield break;
        }

        // 次のページへ進めて表示を更新する（文字はまだ表示しない）
        m_currentPage++;
        ApplyPage(m_currentPage);

        // 矢印の待ち時間を初期化する
        ResetArrowTimer();

        // 動画の0秒目が描かれるまで、暗いまま待つ
        yield return WaitForVideoReadyCoroutine();

        // ワイプイン（画面を明るくする）が設定されている場合
        if (!string.IsNullOrEmpty(m_animationInStateName))
        {
            // ワイプインを再生して待つ
            yield return StartCoroutine(PlayWipeAnimationCoroutine(m_animationInStateName));
        }

        // フェードインが終わってから動画と文字の表示を始める
        StartVideo();
        StartTypewriter();

        // 演出終了
        m_isTransitioning = false;
    }

    /// <summary>
    /// ワイプアニメーションを再生し、終了まで待ちます。
    /// </summary>
    /// <param name="stateName">再生するステート名。</param>
    private IEnumerator PlayWipeAnimationCoroutine(string stateName)
    {
        // アニメーションを再生する
        m_wipeAnimator.Play(stateName);

        // ステートが切り替わるまで 1 フレーム待つ
        yield return null;

        // アニメーションが終了するまで待機する
        AnimatorStateInfo stateInfo = m_wipeAnimator.GetCurrentAnimatorStateInfo(WIPE_ANIMATOR_LAYER_INDEX);
        while (stateInfo.normalizedTime < ANIMATION_END_NORMALIZED_TIME)
        {
            stateInfo = m_wipeAnimator.GetCurrentAnimatorStateInfo(WIPE_ANIMATOR_LAYER_INDEX);
            yield return null;
        }
    }
}