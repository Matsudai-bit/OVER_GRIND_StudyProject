using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

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
    /// ストーリーテキスト画像。
    /// </summary>
    [FormerlySerializedAs("storyText")]
    [SerializeField]
    private Sprite m_storyText;

    /// <summary>
    /// ストーリー画像を取得します。
    /// </summary>
    public Sprite StoryTexture => m_storyTexture;

    /// <summary>
    /// ストーリーテキスト画像を取得します。
    /// </summary>
    public Sprite StoryText => m_storyText;
}

/// <summary>
/// ストーリー画面のページ送りとワイプ演出を管理します。
/// </summary>
public class StoryManager : MonoBehaviour
{
    // ---------- 定数 ----------

    /// <summary>
    /// 矢印が出現するまでの初期時間（秒）。
    /// </summary>
    private const float DEFAULT_ARROW_APPEAR_TIME = 5.0f;

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

    // ---------- Inspector 設定値 ----------

    [Header("矢印設定")]
    /// <summary>
    /// 矢印が出現するまでの時間（秒）。
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
    /// ストーリーテキストを表示する Image。
    /// </summary>
    [SerializeField]
    private Image m_storyText;

    /// <summary>
    /// 矢印を表示する Image。
    /// </summary>
    [SerializeField]
    private Image m_arrowImage;

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
    /// </summary>
    [SerializeField]
    private string m_animationInStateName = DEFAULT_WIPE_IN_STATE_NAME;

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
    /// 必須設定を確認し、最初のページを表示します。
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

        // 最初のページを表示する
        ApplyPage(m_currentPage);

        // 矢印の待ち時間を初期化する
        ResetArrowTimer();
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
            // ページ切り替え演出を開始する
            StartCoroutine(TransitPageCoroutine());
            return;
        }

        // 矢印の表示状態を更新する
        UpdateArrowVisibility();
    }

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
    /// 指定ページの画像とテキストを表示します。
    /// </summary>
    /// <param name="pageIndex">表示するページ番号。</param>
    private void ApplyPage(int pageIndex)
    {
        // スプライトを入れ替える
        m_storyImage.sprite = m_storyPages[pageIndex].StoryTexture;
        m_storyText.sprite = m_storyPages[pageIndex].StoryText;

        // イメージのサイズを画像に合わせる
        m_storyImage.SetNativeSize();
        m_storyText.SetNativeSize();
    }

    /// <summary>
    /// 矢印の経過時間を 0 に戻し、矢印を非表示にします。
    /// </summary>
    private void ResetArrowTimer()
    {
        m_elapsedTime = 0.0f;
        m_arrowImage.enabled = false;
    }

    /// <summary>
    /// 矢印が非表示の間、経過時間を加算します。
    /// </summary>
    private void CountElapsedTime()
    {
        // 矢印が表示されていない場合のみ加算する
        if (!m_arrowImage.enabled)
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
        }
    }

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

    /// <summary>
    /// ワイプ演出を挟んで次のページへ切り替えます。
    /// </summary>
    private IEnumerator TransitPageCoroutine()
    {
        // 連打防止のためにフラグを ON にする
        m_isTransitioning = true;

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

        // 次のページへ進めて表示を更新する
        m_currentPage++;
        ApplyPage(m_currentPage);

        // 矢印の待ち時間を初期化する
        ResetArrowTimer();

        // ワイプイン（画面を明るくする）が設定されている場合
        if (!string.IsNullOrEmpty(m_animationInStateName))
        {
            // ワイプインを再生して待つ
            yield return StartCoroutine(PlayWipeAnimationCoroutine(m_animationInStateName));
        }

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