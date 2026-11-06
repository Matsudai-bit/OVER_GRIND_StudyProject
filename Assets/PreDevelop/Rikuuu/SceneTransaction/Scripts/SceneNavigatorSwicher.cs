using UnityEngine;

/// <summary>
/// 初回か2回目以降かで、SceneNavigator に渡す引数を切り替えます。
/// SceneNavigator 自体は変更しません。
/// </summary>
public class SceneNavigatorSwitcher : MonoBehaviour
{
    /// <summary>
    /// デバッグ用:初回判定の上書きモード。
    /// </summary>
    public enum DebugVisitMode
    {
        // 実際の保存データで判定する(通常)
        USE_SAVED_DATA = 0,
        // 常に初回として扱う
        FORCE_FIRST_TIME,
        // 常に2回目以降として扱う
        FORCE_AFTER_FIRST_TIME
    }

    [System.Serializable]
    public struct TransitionSet
    {
        public string SceneName;
        public SceneTransitionEffect InEffect;
        public SceneTransitionEffect OutEffect;
    }

    [Header("対象")]
    [SerializeField] private SceneNavigator m_sceneNavigator;

    [Header("セーブデータ")]
    [SerializeField]  SaveData m_saveData;

    [Header("初回判定のキー(用途ごとに変える)")]
    [SerializeField] private string m_visitKey = "Story";

    [Header("初回の遷移設定")]
    [SerializeField] private TransitionSet m_firstTime;

    [Header("2回目以降の遷移設定")]
    [SerializeField] private TransitionSet m_afterFirstTime;

    [Header("訪問済みにするタイミング")]
    [Tooltip("ON: Execute を呼んだ時点で記録 / OFF: MarkVisited を呼ぶまで記録しない")]
    [SerializeField] private bool m_markOnExecute = true;

    [Header("デバッグ用")]
    [Tooltip("初回判定を強制します。通常は USE_SAVED_DATA のままにしてください。")]
    [SerializeField] private DebugVisitMode m_debugVisitMode = DebugVisitMode.USE_SAVED_DATA;

    [Tooltip("ON の間は、強制モードでも訪問済みの記録を書き込みません。")]
    [SerializeField] private bool m_debugSkipSave = true;

    /// <summary>
    /// 初回かどうかで引数を切り替えて、シーン遷移を実行します。
    /// </summary>
    public void Execute()
    {
        if (m_sceneNavigator == null)
        {
            Debug.LogWarning("SceneNavigator が設定されていません。");
            return;
        }

        bool visited = IsVisited();
        TransitionSet set = visited ? m_afterFirstTime : m_firstTime;

        // 既存のメソッドを、UnityEvent と同じ順番で呼ぶ
        m_sceneNavigator.ChangeTransitionInEffect(set.InEffect);
        m_sceneNavigator.ChangeTransitionOutEffect(set.OutEffect);
        m_sceneNavigator.ChangeScene(set.SceneName);

        if (m_markOnExecute)
        {
            SaveVisited();
        }
    }

    /// <summary>
    /// 訪問済みとして記録します(ストーリー終了時などに UnityEvent から呼びます)。
    /// </summary>
    public void MarkVisited()
    {
        SaveVisited();
    }

    /// <summary>
    /// デバッグ設定を考慮して、訪問済みかどうかを返します。
    /// </summary>
    private bool IsVisited()
    {
        if (m_debugVisitMode != DebugVisitMode.USE_SAVED_DATA)
        {

            switch (m_debugVisitMode)
            {
                case DebugVisitMode.FORCE_FIRST_TIME:
                    return false;

                case DebugVisitMode.FORCE_AFTER_FIRST_TIME:
                    return true;

                default:
                    return true;
                    //return FirstVisitFlag.HasVisited(m_visitKey);
            }            
        }

        return m_saveData.Progress.HasWatchedOpening;
    }

    /// <summary>
    /// 訪問済みの記録を保存します。デバッグ強制中は設定に応じて保存を飛ばします。
    /// </summary>
    private void SaveVisited()
    {
        if (m_debugVisitMode != DebugVisitMode.USE_SAVED_DATA)
        {
            bool isForced = m_debugVisitMode != DebugVisitMode.USE_SAVED_DATA;
            if (isForced && m_debugSkipSave)
            {
                return;
            }
        }
        m_saveData.Progress.MarkOpeningWatched();
        
        //FirstVisitFlag.MarkVisited(m_visitKey);
    }
}