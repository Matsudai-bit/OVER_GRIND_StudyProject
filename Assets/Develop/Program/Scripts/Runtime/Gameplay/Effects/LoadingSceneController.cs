using System.Collections;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ローディングシーンのコントローラー
/// </summary>
public class LoadingSceneController : MonoBehaviour
{
    [Header("シーン遷移時の最小ロード時間")]
    [SerializeField] private float m_minLoadTime = 0.5f;

    [SerializeField] private GameObject m_loadingUI;
    [SerializeField] private Slider m_slider;

    [SerializeField] public string sceneName ;

    [SerializeField] private GameObject m_text;

    private float m_angle;
    // ロード開始時間を記録する変数
    private float m_loadStartTime;

    void Start()
    {
        m_angle = 0.0f;
        m_loadingUI.SetActive(true);


        // 次のシーン名を取得
        sceneName = LoadingSceneRequest.GetInstance.NextSceneName;
        StartCoroutine(StartLoadingSequence());
    }

    void Update()
    {
   

        m_angle += 100.0f * Time.deltaTime;

        if (m_angle >= 360.0f)
        {
            m_angle = m_angle - 360.0f;
        }

        // サイン波でテキストを上下に動かす
        m_text.GetComponent<RectTransform>().anchoredPosition = new Vector2(
            m_text.GetComponent<RectTransform>().anchoredPosition.x,
            10.0f * Mathf.Sin(Mathf.Deg2Rad * m_angle));

    }

    public void LoadNextScene()
    {
        // ロード開始時間を記録
        m_loadStartTime = Time.time;
        StartCoroutine(LoadScene());
    }

    IEnumerator StartLoadingSequence()
    {
        // UI描画を確実に反映
        yield return new WaitForEndOfFrame();

        // ★★★ 描画を確実に行うために一フレーム待機 ★★★
        // これにより、LoadingUIが画面に描画されることが保証される
        yield return null;

        // ロード開始時間を記録
        m_loadStartTime = Time.time;
        StartCoroutine(LoadScene());
    }
    IEnumerator LoadScene()
    {
        float minDuration = m_minLoadTime; // 最小待機時間
        float loadStartTime = Time.time;

        // ロードを開始し、自動アクティベーションを停止
        AsyncOperation async = SceneManager.LoadSceneAsync(sceneName);
        async.allowSceneActivation = false;


        // ロード完了（90%）または最小待機時間（2秒）が経過するまでループ
        while (async.progress < 0.9f || Time.time < loadStartTime + minDuration)
        {
            float elapsed = Time.time - loadStartTime;

            // 1. ロード進捗 (0.0 ～ 0.9 の正規化)
            float loadProgress = Mathf.Clamp01(async.progress / 0.9f);

            // 2. 時間進捗 (0.0 ～ 1.0 の正規化)
            // 経過時間を最小待機時間で割ることで、時間経過による進捗を計算
            float timeProgress = Mathf.Clamp01(elapsed / minDuration);

            // 3. スライダー値の決定
            // 最小待機時間内は、時間進捗を優先してスムーズに動かす
            // ロードが遅れている場合は、ロード進捗を最低限の値とする
            float finalProgress = Mathf.Min(loadProgress, timeProgress);

            m_slider.value = finalProgress;

            yield return null;
        }

        // ループ終了後、確実に 1.0f に設定
        m_slider.value = 1.0f;

        // ----------------------------------------------------
        // 4. シーン遷移を許可
        // ----------------------------------------------------
        async.allowSceneActivation = true;

        while (!async.isDone)
        {
            yield return null;
        }

    }

}
