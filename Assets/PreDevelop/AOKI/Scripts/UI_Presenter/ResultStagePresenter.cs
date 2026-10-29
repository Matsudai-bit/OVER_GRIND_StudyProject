using DG.Tweening;
using UnityEngine;

/// <summary>
/// ステージ情報 Model（データ）と View（画面表示・スクランブル演出）の更新を制御する Presenter クラスです。
/// </summary>
public class ResultStagePresenter : MonoBehaviour
{
    private const string DEFAULT_STAGE_NUMBER_FORMAT = "Stage{0:D2}";

    [Header("MVP 参照設定")]
    [Tooltip("ステージデータを保持する Model コンポーネント（仮Model または 本実装Model）")]
    [SerializeField] private ResultStageModel m_stageModel;

    [Tooltip("作成済みのステージ表示用 View コンポーネント")]
    [SerializeField] private ResultStageView m_stageView;

    [Header("フォーマット設定")]
    [Tooltip("ステージ番号の表示フォーマット（例: \"Stage{0:D2}\" で \"Stage00\" になります）")]
    [SerializeField] private string m_stageNumberFormat = DEFAULT_STAGE_NUMBER_FORMAT;

    [Header("演出設定")]
    [Tooltip("チェックを入れると文字がシャッフル（Scramble）表示されます")]
    [SerializeField] private bool m_useScramble = true;

    [Tooltip("演出が始まるまでの待ち時間（秒）")]
    [SerializeField] private float m_delay = 0.3f;

    private Tween m_delayTween;

    private void Awake()
    {
        InitializeReferences();
    }

    private void OnEnable()
    {
        InitializeReferences();

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

        m_delayTween?.Kill();
    }

    /// <summary>
    /// 未設定のコンポーネント参照を自動取得します。
    /// </summary>
    private void InitializeReferences()
    {
        if (m_stageModel == null)
        {
            m_stageModel = GetComponent<ResultStageModel>();
            if (m_stageModel == null)
            {
                m_stageModel = FindFirstObjectByType<ResultStageModel>();
            }
        }

        if (m_stageView == null)
        {
            m_stageView = GetComponent<ResultStageView>();
            if (m_stageView == null)
            {
                m_stageView = FindFirstObjectByType<ResultStageView>();
            }
        }
    }

    /// <summary>
    /// Model からデータ変更通知を受け取り、フォーマット整形して View に演出指示を送ります。
    /// </summary>
    private void HandleStageChanged(int stageIndex, string stageName)
    {
        if (m_stageView == null) return;

        m_delayTween?.Kill();

        // ステージ番号を "Stage00" 形式に整形
        string formattedStageNumber = string.IsNullOrEmpty(m_stageNumberFormat)
            ? stageIndex.ToString()
            : string.Format(m_stageNumberFormat, stageIndex);

        // スクランブル演出を使わない場合
        if (!m_useScramble)
        {
            m_stageView.SetStageInfo(formattedStageNumber, stageName, stageIndex);
            return;
        }

        // スクランブル演出を行う場合
        if (m_delay <= 0f)
        {
            m_stageView.PlayStageInfoScramble(formattedStageNumber, stageName, stageIndex);
        }
        else
        {
            m_delayTween = DOVirtual.DelayedCall(m_delay, () =>
            {
                if (m_stageView != null)
                {
                    m_stageView.PlayStageInfoScramble(formattedStageNumber, stageName, stageIndex);
                }
            }).SetUpdate(true); // timeScale = 0対応
        }
    }
}