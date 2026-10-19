using UnityEngine;

/// <summary>
/// ステージ情報 Model（データ）と作成済み View（画面表示）の更新イベントを仲介する Presenter クラスです。
/// </summary>
public class ResultStagePresenter : MonoBehaviour
{
    private const string DEFAULT_STAGE_NUMBER_FORMAT = "Stage{0:D2}";

    [Header("★ MVP 参照設定")]
    [Tooltip("ステージデータを保持する Model コンポーネント（仮Model または 本実装Model）")]
    [SerializeField] private ResultStageModel m_stageModel;

    [Tooltip("作成済みのステージ表示用 View コンポーネント")]
    [SerializeField] private ResultStageView m_stageView; // ※ 既存のViewクラス名に合わせて変更してください

    [Header("★ フォーマット設定")]
    [Tooltip("ステージ番号の表示フォーマット（例: \"Stage{0:D2}\" で \"Stage00\" になります）")]
    [SerializeField] private string m_stageNumberFormat = DEFAULT_STAGE_NUMBER_FORMAT;

    private void Awake()
    {
        InitializeReferences();
    }

    private void OnEnable()
    {
        if (m_stageModel != null)
        {
            m_stageModel.OnStageChanged += HandleStageChanged;

            // 初回表示の同期
            HandleStageChanged(m_stageModel.StageIndex, m_stageModel.StageName);
        }
    }

    private void OnDisable()
    {
        if (m_stageModel != null)
        {
            m_stageModel.OnStageChanged -= HandleStageChanged;
        }
    }

    /// <summary>
    /// 未設定のコンポーネント参照を自動取得します。
    /// </summary>
    private void InitializeReferences()
    {
        if (m_stageModel == null)
        {
            m_stageModel = GetComponent<ResultStageModel>();
        }

        if (m_stageView == null)
        {
            m_stageView = GetComponent<ResultStageView>();
        }
    }

    /// <summary>
    /// Model からデータ変更通知を受け取り、フォーマット整形して View に表示指示を送ります。
    /// </summary>
    /// <param name="stageIndex">ステージ番号。</param>
    /// <param name="stageName">ステージ名。</param>
    private void HandleStageChanged(int stageIndex, string stageName)
    {
        if (m_stageView == null)
        {
            return;
        }

        // ステージ番号を "Stage00" 形式に整形
        string formattedStageNumber = string.IsNullOrEmpty(m_stageNumberFormat)
            ? stageIndex.ToString()
            : string.Format(m_stageNumberFormat, stageIndex);

        // ★ 作成済み View の表示更新メソッドを呼び出し
        // ※ 既存の View のメソッド名（例: DisplayStage, SetStageInfo 等）に合わせて適宜変更してください
        m_stageView.SetStageInfo(formattedStageNumber, stageName);
    }
}