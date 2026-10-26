using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; // Input System 用の参照を追加

/// <summary>
/// Spaceキーを押すことで、リザルトUI全体の演出を一括でリセット＆再再生するデバッグ用スクリプトです（New Input System対応）。
/// </summary>
public class ResultUIDebugController : MonoBehaviour
{
    [Header("再生対象のルートUI")]
    [Tooltip("Hierarchy上にある ResultUI や View_Root などをドラッグ＆ドロップしてください")]
    [SerializeField] private GameObject m_resultRootUI;

    private void Update()
    {
        // Input System での Spaceキー入力判定
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            RestartAllSequences();
        }
    }

    /// <summary>
    /// 全UI演出を一括リセットして最初から再再生します。
    /// （Inspectorのコンポーネント右クリックメニュー「一括再再生」からも実行可能です）
    /// </summary>
    [ContextMenu("一括再再生")]
    public void RestartAllSequences()
    {
        if (m_resultRootUI == null)
        {
            Debug.LogWarning("[ResultUIDebugController] Root UI がアタッチされていません！Inspectorを確認してください。");
            return;
        }

        StopAllCoroutines();
        StartCoroutine(RestartCoroutine());
    }

    private IEnumerator RestartCoroutine()
    {
        // ルートオブジェクトを一度OFFにして演出を初期化
        m_resultRootUI.SetActive(false);

        // 1フレーム待機（内部状態を確実にクリア）
        yield return null;

        // 再びONにして演出を一括再再生！
        m_resultRootUI.SetActive(true);

        Debug.Log("?? [ResultUIDebug] リザルト演出を一括再再生しました！");
    }
}