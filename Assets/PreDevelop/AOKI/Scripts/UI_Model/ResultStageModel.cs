using System;
using UnityEngine;

/// <summary>
/// リザルト画面におけるステージ情報のデータモデルインターフェースです。
/// </summary>
public interface IResultStageModel
{
    /// <summary>
    /// ステージ情報が変更された際に発生するイベントです。
    /// </summary>
    event Action<int, string> OnStageChanged;

    /// <summary>
    /// 現在のステージ番号を取得します。
    /// </summary>
    int StageIndex { get; }

    /// <summary>
    /// 現在のステージ名を取得します。
    /// </summary>
    string StageName { get; }
}

/// <summary>
/// リザルト画面におけるステージ情報を保持・管理します。
/// </summary>
public class ResultStageModel :
    MonoBehaviour,
    IResultStageModel
{
    private const int DEFAULT_STAGE_INDEX = 0;
    private const string DEFAULT_STAGE_NAME = "Tutorial";

    /// <summary>
    /// ステージ情報が変更された際に発生するイベントです。
    /// 第一引数：ステージ番号、第二引数：ステージ名。
    /// </summary>
    public event Action<int, string> OnStageChanged;

    [Header("仮データ設定（インスペクターテスト用）")]
    [Tooltip("テスト用の初期ステージ番号")]
    [SerializeField]
    private int m_initialStageIndex =
        DEFAULT_STAGE_INDEX;

    [Tooltip("テスト用の初期ステージ名")]
    [SerializeField]
    private string m_initialStageName =
        DEFAULT_STAGE_NAME;

    // 現在のステージ番号
    private int m_stageIndex;

    // 現在のステージ名
    private string m_stageName;

    /// <summary>
    /// 現在のステージ番号を取得します。
    /// </summary>
    public int StageIndex =>
        m_stageIndex;

    /// <summary>
    /// 現在のステージ名を取得します。
    /// </summary>
    public string StageName =>
        m_stageName;

    private void Awake()
    {
        SetStageData(
            m_initialStageIndex,
            m_initialStageName);
    }

    /// <summary>
    /// StageSceneContextからステージ名を取得して設定します。
    /// </summary>
    /// <param name="stageIndex">
    /// 現在のステージ番号。
    /// </param>
    /// <param name="stageSceneContext">
    /// ステージ名を保持しているContext。
    /// </param>
    /// <returns>
    /// true：設定しました。
    /// false：Contextが設定されていません。
    /// </returns>
    public bool SetStageData(
        int stageIndex,
        StageSceneContext stageSceneContext)
    {
        if (stageSceneContext == null)
        {
            return false;
        }

        SetStageData(
            stageIndex,
            stageSceneContext.StageName);

        return true;
    }

    /// <summary>
    /// ステージ情報を設定し、変更イベントを発行します。
    /// </summary>
    /// <param name="stageIndex">ステージ番号。</param>
    /// <param name="stageName">ステージ名。</param>
    public void SetStageData(
        int stageIndex,
        string stageName)
    {
        m_stageIndex =
            Mathf.Max(
                DEFAULT_STAGE_INDEX,
                stageIndex);

        m_stageName =
            stageName ?? string.Empty;

        OnStageChanged?.Invoke(
            m_stageIndex,
            m_stageName);
    }
}
