using System;
using UnityEngine;

/// <summary>
/// リザルト画面におけるステージ情報のデータモデルインターフェースです。
/// </summary>
public interface IResultStageModel
{
    /// <summary>ステージ情報が変更された際に発生するイベントです。</summary>
    event Action<int, string> OnStageChanged;

    /// <summary>現在のステージ番号を取得します。</summary>
    int StageIndex { get; }

    /// <summary>現在のステージ名を取得します。</summary>
    string StageName { get; }
}

/// <summary>
/// リザルト画面におけるステージ情報データを保持・管理する仮 Model クラスです。
/// 友人が本実装の Model を作成するまでのテスト用として使用できます。
/// </summary>
public class ResultStageModel : MonoBehaviour, IResultStageModel
{
    private const int DEFAULT_STAGE_INDEX = 0;
    private const string DEFAULT_STAGE_NAME = "Tutorial";

    /// <summary>
    /// ステージ情報が変更された際に発生するイベントです。
    /// 第一引数: ステージ番号 (int), 第二引数: ステージ名 (string)
    /// </summary>
    public event Action<int, string> OnStageChanged;

    [Header("仮データ設定（インスペクターテスト用）")]
    [Tooltip("テスト用の初期ステージ番号（例: 0）")]
    [SerializeField] private int m_initialStageIndex = DEFAULT_STAGE_INDEX;

    [Tooltip("テスト用の初期ステージ名（例: \"Tutorial\"）")]
    [SerializeField] private string m_initialStageName = DEFAULT_STAGE_NAME;

    private int m_stageIndex;
    private string m_stageName;

    /// <summary>
    /// 現在のステージ番号を取得します。
    /// </summary>
    public int StageIndex => m_stageIndex;

    /// <summary>
    /// 現在のステージ名を取得します。
    /// </summary>
    public string StageName => m_stageName;

    private void Awake()
    {
        SetStageData(m_initialStageIndex, m_initialStageName);
    }

    /// <summary>
    /// ステージ情報を設定し、変更イベントを発火します。
    /// </summary>
    /// <param name="stageIndex">ステージ番号（例: 0）。</param>
    /// <param name="stageName">ステージ名（例: "Tutorial"）。</param>
    public void SetStageData(int stageIndex, string stageName)
    {
        m_stageIndex = stageIndex;
        m_stageName = stageName;

        OnStageChanged?.Invoke(m_stageIndex, m_stageName);
    }
}