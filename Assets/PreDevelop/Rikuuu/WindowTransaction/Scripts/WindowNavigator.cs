using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// トランジション演出付きでオーバーレイウィンドウを開閉します。
/// </summary>
[DisallowMultipleComponent]
public sealed class WindowNavigator : MonoBehaviour
{
    [SerializeField, Header("ウィンドウを開く機構")]
    private UIOverlaySceneManager m_sceneManager;

    [SerializeField, Header("画面を開くエフェクト(In)")]
    private SceneTransitionEffect m_sceneTransitionInEffect;

    [SerializeField, Header("画面を覆うエフェクト(Out)")]
    private SceneTransitionEffect m_sceneTransitionOutEffect;

    [SerializeField, Header("開閉完了を待つ最大秒数")]
    private float m_timeoutSeconds = 10.0f;

    [SerializeField, Header("演出イベント")]
    private UnityEvent m_onTransitionStarted = new();   // 開く・閉じる演出の開始時
    [SerializeField]
    private UnityEvent m_onCloseFinished = new();       // 閉じる演出の完了時

    // 開閉処理中かどうかです。
    private bool m_isBusy = false;

    // ウィンドウを閉じれるかどうか
    private Func<bool> m_canCloseCondition;

    public void ChangeTransitionInEffect(SceneTransitionEffect sceneTransitionInEffect)
    {
        m_sceneTransitionInEffect = sceneTransitionInEffect;
    }

    public void ChangeTransitionOutEffect(SceneTransitionEffect sceneTransitionOutEffect)
    {
        m_sceneTransitionOutEffect = sceneTransitionOutEffect;
    }

    /// <summary>
    /// ウィンドウ(Additiveシーン)を演出付きで開きます。
    /// </summary>
    /// <param name="sceneName">開くシーン名。</param>
    public void OpenWindow(string sceneName)
    {
        if (!CanStart(sceneName))
        {
            return;
        }

        if (SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            Debug.LogWarning($"シーンは既に読み込まれています: {sceneName}", this);
            return;
        }

        m_isBusy = true;
        StartCoroutine(TransitionRoutine(
            () => m_sceneManager.OpenOverlayScene(sceneName),
            () => SceneManager.GetSceneByName(sceneName).isLoaded));
    }

    /// <summary>
    /// ウィンドウ(Additiveシーン)を演出付きで閉じます。
    /// </summary>
    /// <param name="sceneName">閉じるシーン名。</param>
    public void CloseWindow(string sceneName)
    {
        if (m_canCloseCondition != null && !m_canCloseCondition())
        {
            return;
        }

        if (!CanStart(sceneName))
        {
            return;
        }

        if (!SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            return;
        }

        m_isBusy = true;
        StartCoroutine(TransitionRoutine(
            () => m_sceneManager.CloseOverlayScene(),
            () => !SceneManager.GetSceneByName(sceneName).isLoaded,
            m_onCloseFinished));
    }

    /// <summary>
    /// 開閉を開始できる状態か確認します。
    /// </summary>
    private bool CanStart(string sceneName)
    {
        if (m_isBusy)
        {
            return false;
        }

        if (m_sceneManager == null)
        {
            Debug.LogError("UIOverlaySceneManagerが設定されていません。", this);
            return false;
        }

        if (m_sceneTransitionInEffect == null || m_sceneTransitionOutEffect == null)
        {
            Debug.LogWarning("TransitionEffectが設定されていません。", this);
            return false;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("シーン名が指定されていません。", this);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Out再生、開閉、完了待ち、In再生の順に実行します。
    /// </summary>
    /// <param name="request">開閉を要求する処理。</param>
    /// <param name="isCompleted">開閉が完了したかの判定。</param>
    private IEnumerator TransitionRoutine(Action request, Func<bool> isCompleted, UnityEvent onFinished = null)
    {
        m_onTransitionStarted?.Invoke();

        // 画面を覆います。
        SceneTransitionEffect outEffect = Instantiate(m_sceneTransitionOutEffect);
        yield return WaitEffect(outEffect);

        // 覆った状態で開閉します。
        request();

        float startTime = Time.unscaledTime;
        while (!isCompleted())
        {
            if (Time.unscaledTime - startTime > m_timeoutSeconds)
            {
                Debug.LogError("ウィンドウの開閉が完了しませんでした。", this);
                break;
            }

            yield return null;
        }

        // 表示処理が終わるまで1フレーム待ちます。
        yield return null;

        // 画面を開きます。
        SceneTransitionEffect inEffect = Instantiate(m_sceneTransitionInEffect);
        Destroy(outEffect.gameObject);
        yield return WaitEffect(inEffect);

        Destroy(inEffect.gameObject);
        m_isBusy = false;

        onFinished?.Invoke();
    }

    /// <summary>
    /// エフェクトの再生が終わるまで待ちます。
    /// </summary>
    private IEnumerator WaitEffect(SceneTransitionEffect effect)
    {
        bool isDone = false;
        effect.StartTransition(() => isDone = true);
        yield return new WaitUntil(() => isDone);
    }

    public void SetCloseCondition(Func<bool> condition)
    {
        m_canCloseCondition = condition;
    }
}