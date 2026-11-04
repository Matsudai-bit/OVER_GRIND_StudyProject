using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// シーン遷移の演出インターフェース
/// </summary>
public interface ISceneTransitionEffect
{
    /// <summary>
    /// シーン遷移の開始
    /// </summary>
    /// <param name="onComplete">シーン遷移が完了した際に呼び出されるコールバック</param>
    public void StartTransition(System.Action onComplete);

    // トランジション中かどうか
    public bool IsTransitioning();

}

/// <summary>
/// シーン遷移を管理するマネージャークラス
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    private static SceneTransitionManager m_instance;
    public static SceneTransitionManager GetInstance
    {
        get
        {
            if (m_instance == null)
            {
                GameObject obj = new GameObject("SceneTransitionManager");
                m_instance = obj.AddComponent<SceneTransitionManager>();
                DontDestroyOnLoad(obj);
            }
            return m_instance;
        }
    }

    // 遷移中かどうか(多重遷移の防止)
    public bool IsTransitioning { get; private set; }

    private void Awake()
    {
        if (m_instance == null)
        {
            m_instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (m_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// シーン遷移を実行する(Out再生 → シーン遷移 → In再生)
    /// </summary>
    /// <param name="sceneName">遷移先のシーン名</param>
    /// <param name="outEffectPrefab">画面を覆うエフェクトのPrefab</param>
    /// <param name="inEffectPrefab">画面を開くエフェクトのPrefab</param>
    /// <param name="inLoadingScene">ロードシーンを使うかどうか</param>
    public void TransitionToScene(
        string sceneName, 
        SceneTransitionEffect sceneTransitionInEffect,
        SceneTransitionEffect sceneTransitionOutEffect,
        bool inLoadingScene = false)
    {
        // 遷移中は受け付けない
        if (IsTransitioning) return;

        if (sceneTransitionInEffect == null || sceneTransitionOutEffect == null)
        {
            Debug.LogWarning("Out/Inエフェクトが設定されていません。");
            return;
        }

        IsTransitioning = true;
        StartCoroutine(
            Run(
                sceneName,
                sceneTransitionOutEffect,
                sceneTransitionInEffect, 
                inLoadingScene
            )
        );
    }

    private IEnumerator Run(
    string sceneName,
    SceneTransitionEffect outPrefab,
    SceneTransitionEffect inPrefab,
    bool inLoadingScene)
    {
        // Outの実体を生成して永続化する
        SceneTransitionEffect outEffect = Instantiate(outPrefab);
        DontDestroyOnLoad(outEffect.gameObject);

        // 画面を覆う
        Debug.Log("[Run] Out開始");
        yield return WaitEffect(outEffect);
        Debug.Log("[Run] Out完了 → ロード開始");

        // シーン遷移を行う
        if (inLoadingScene)
        {
            LoadingSceneRequest.GetInstance.RequestLoadingScene(sceneName);
        }
        else
        {
            yield return SceneManager.LoadSceneAsync(sceneName);
        }
        yield return null;

        // Inを生成し、同じフレームでOutを破棄する
        Debug.Log("[Run] In生成");
        SceneTransitionEffect inEffect = Instantiate(inPrefab);
        DontDestroyOnLoad(inEffect.gameObject);
        Destroy(outEffect.gameObject);

        // 画面を開く
        yield return WaitEffect(inEffect);
        Debug.Log("[Run] In完了");

        Destroy(inEffect.gameObject);
        IsTransitioning = false;
    }

    // エフェクトの再生が終わるまで待つ
    private IEnumerator WaitEffect(SceneTransitionEffect effect)
    {
        bool done = false;
        effect.StartTransition(() => done = true);
        yield return new WaitUntil(() => done);
    }
}
