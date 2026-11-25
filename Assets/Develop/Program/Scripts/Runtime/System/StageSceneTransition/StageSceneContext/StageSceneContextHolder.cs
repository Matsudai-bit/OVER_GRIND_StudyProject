using UnityEngine;

/// <summary>
/// StageSceneContext の参照を保持し、
/// Scene遷移中にランタイムデータが失われることを防ぎます。
/// </summary>
[DisallowMultipleComponent]
public sealed class StageSceneContextHolder : MonoBehaviour
{
    // シングルトンインスタンス
    private static StageSceneContextHolder s_instance;

    // Scene間で保持するステージContext
    [SerializeField, Header("Context")]
    private StageSceneContext m_stageSceneContext;

    /// <summary>
    /// StageSceneContext を取得します。
    /// </summary>
    public StageSceneContext Context =>
        m_stageSceneContext;

    private void Awake()
    {
        // 既に別インスタンスが存在する場合は重複を破棄する
        if (s_instance != null
            && s_instance != this)
        {
            Destroy(gameObject);

            return;
        }

        s_instance =
            this;

        // Scene遷移時に破棄されないよう永続化する
        DontDestroyOnLoad(
            gameObject);

        ValidateContext();
    }

    private void OnDestroy()
    {
        // 自身が現在のインスタンスの場合のみ参照を解除する
        if (s_instance == this)
        {
            s_instance =
                null;
        }
    }

    /// <summary>
    /// StageSceneContext が設定されているか確認します。
    /// </summary>
    private void ValidateContext()
    {
        if (m_stageSceneContext != null)
        {
            return;
        }

        Debug.LogError(
            $"{nameof(StageSceneContext)} が設定されていません。",
            this);
    }
}