using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// オーバーレイ内のContainerControllerを取得し、WindowNavigatorへ条件を渡します。
/// </summary>
[DisallowMultipleComponent]
public sealed class GetContainerController : MonoBehaviour
{
    [SerializeField, Header("条件を渡す先")]
    private WindowNavigator m_windowNavigator;

    [SerializeField, Header("取得対象のシーン名")]
    private string m_sceneName = "UserSupportScene";

    [SerializeField, Header("取得を待つ最大秒数")]
    private float m_timeoutSeconds = 10.0f;

    // 取得待ちの処理です。
    private Coroutine m_routine;

    /// <summary>
    /// 設定ボタンから呼び、ロード完了後に取得を行います。
    /// </summary>
    public void BeginAcquire()
    {
        if (m_routine != null)
        {
            StopCoroutine(m_routine);
        }

        m_routine = StartCoroutine(AcquireRoutine());
    }

    private IEnumerator AcquireRoutine()
    {
        float startTime = Time.unscaledTime;
        ContainerController controller = null;

        while (controller == null)
        {
            controller = FindInScene(m_sceneName);

            if (controller != null)
            {
                Debug.LogWarning("取得完了", this);
                break;
            }

            if (Time.unscaledTime - startTime > m_timeoutSeconds)
            {
                Debug.LogError("ContainerControllerを取得できませんでした。", this);
                m_routine = null;
                yield break;
            }

            yield return null;
        }

        // 参照を条件として渡します。アンロード後は null 判定になり false を返します。
        m_windowNavigator.SetCloseCondition(
            () => controller != null && controller.IsLeft);

        m_routine = null;
    }

    private ContainerController FindInScene(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.isLoaded)
        {
            return null;
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            ContainerController controller =
                root.GetComponentInChildren<ContainerController>(true);

            if (controller != null)
            {
                return controller;
            }
        }

        return null;
    }
}