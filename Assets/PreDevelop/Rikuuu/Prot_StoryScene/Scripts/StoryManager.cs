using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public struct StoryPage
{
    // ストーリーテクスチャ
    public Sprite storyTexture;

    // ストーリーテキストテクスチャ
    public Sprite storyText;
}

public class StoryManager : MonoBehaviour
{
    [Header("定数")]
    // 矢印が出現するまでの時間
    [SerializeField]
    private float APPEARANCE_ARROW_TIME = 5.0f;

    [Header("コンポーネント類")]
    // イメージコンポーネント
    [SerializeField]
    private UnityEngine.UI.Image m_storyImage;
    // テキストイメージコンポーネント
    [SerializeField]
    private UnityEngine.UI.Image m_storyText;
    // 矢印イメージコンポーネント
    [SerializeField]
    private UnityEngine.UI.Image m_arrowImage;

    [Header("ストーリーページ")]
    // ストーリーページ
    [SerializeField]
    private StoryPage[] m_storyPages;

    [Header("入力関連")]
    // 決定キー
    [SerializeField]
    private InputActionReference m_enterActionRef;
    // スキップキー
    [SerializeField]
    private InputActionReference m_skipActionRef;

    [Header("ワイプアニメーション")]
    // アニメーション
    [SerializeField] 
    private Animator m_wipeAnimator;
    // 画面を暗くするアニメーション名
    [SerializeField]
    private string m_animationStateName = "WipeOut";
    // 画面を明るくするアニメーション名
    [SerializeField]
    private string m_animationInStateName = "WipeIn";

    // 現在表示しているページ
    private int m_currentPage = 0;
    // ページが表示されてからの経過時間
    private float m_elapsedTime = 0.0f;
    // ワイプ演出中かどうか
    private bool m_isTransitioning = false;

    private void Awake()
    {
        // ストーリーページが設定されていない場合
        if(m_storyPages == null || m_storyPages.Length == 0)
        {
            // ログを出す
            Debug.LogWarning("ストーリーページが設定されていません");
            // この後の関数を実行しない
            enabled = false;
            return;
        }

        // コンポーネントのテクスチャを入れ替える
        m_storyImage.sprite = m_storyPages[m_currentPage].storyTexture;
        m_storyText.sprite = m_storyPages[m_currentPage].storyText;

        // イメージコンポーネントのサイズ調整
        m_storyImage.SetNativeSize();
        m_storyText.SetNativeSize();

        // 経過時間の初期化
        m_elapsedTime = 0.0f;
    }

    private void Update()
    {
        // ワイプ演出中の場合
        if (m_isTransitioning)
        {
            // なにもしない
            return;
        }

        // 矢印が表示されていない場合
        if (!m_arrowImage.enabled)
        {
            // 経過時間の加算
            m_elapsedTime += Time.deltaTime;
        }

        // 決定キーが押された場合
        if (m_enterActionRef != null && m_enterActionRef.action.WasPressedThisFrame())
        {
            // コルーチン（時間差処理）を開始する
            StartCoroutine(PageTransitionCoroutine());
        }
    }

    private IEnumerator PageTransitionCoroutine()
    {
        // 連打防止のためにフラグをONにする
        m_isTransitioning = true;

        // 1. アニメーション（画面を黒くする）を再生
        m_wipeAnimator.Play(m_animationStateName);

        // 2. アニメーションが開始して現在のステートに切り替わるまで1フレーム待つ
        yield return null;

        // 3. アニメーションが終了（真っ黒になる）するまで待機
        AnimatorStateInfo stateInfo = m_wipeAnimator.GetCurrentAnimatorStateInfo(0);
        while (stateInfo.normalizedTime < 1.0f)
        {
            stateInfo = m_wipeAnimator.GetCurrentAnimatorStateInfo(0);
            yield return null;
        }

        // --- ここから画面が真っ黒な状態の処理 ---

        // 最後のページだった場合は、次のシーンへ行くなどの処理を書く
        if (m_currentPage >= m_storyPages.Length - 1)
        {
            // 例: タイトルやゲーム本編へ遷移させる場合（必要に応じてシーン名を変えてください）
            // SceneManager.LoadScene("NextSceneName");

            // 今回はとりあえず最初のページに戻るか、何もしないようにしておきます
            Debug.Log("すべてのストーリーページが終了しました");
            m_isTransitioning = false;
            yield break;
        }

        // 次のページへ進める
        m_currentPage++;

        // コンポーネントのテクスチャを入れ替える
        m_storyImage.sprite = m_storyPages[m_currentPage].storyTexture;
        m_storyText.sprite = m_storyPages[m_currentPage].storyText;

        // イメージコンポーネントのサイズ調整
        m_storyImage.SetNativeSize();
        m_storyText.SetNativeSize();

        // 経過時間の初期化
        m_elapsedTime = 0.0f;
        m_arrowImage.enabled = false;

        // --- ここから画面を戻す処理 ---

        // 4. もし「画面を戻すアニメーション（WipeIn）」を作っている場合はここで再生
        if (!string.IsNullOrEmpty(m_animationInStateName))
        {
            m_wipeAnimator.Play(m_animationInStateName);
            yield return null;

            // 元に戻るアニメーションが終わるまで待つ
            stateInfo = m_wipeAnimator.GetCurrentAnimatorStateInfo(0);
            while (stateInfo.normalizedTime < 1.0f)
            {
                stateInfo = m_wipeAnimator.GetCurrentAnimatorStateInfo(0);
                yield return null;
            }
        }

        // 演出終了、再度操作可能にする
        m_isTransitioning = false;
    }

    private void LateUpdate()
    {
        // ワイプ演出中は矢印の計算を行わない
        if (m_isTransitioning) 
        {
            return;
        }

        // 経過時間に達している場合
        if (m_elapsedTime >= APPEARANCE_ARROW_TIME && !m_arrowImage.enabled)
        {
            // 矢印を表示する
            m_arrowImage.enabled = true;
        }
        // 経過時間に達していない場合
        else if(m_elapsedTime < APPEARANCE_ARROW_TIME && m_arrowImage.enabled)
        {
            // 矢印を非表示にする
            m_arrowImage.enabled = false;
        }
    }
}
