using UnityEngine;

/// <summary>
/// ステージSceneへの遷移に必要な静的情報を保持します。
/// </summary>
[CreateAssetMenu(
    fileName = "StageDefinition",
    menuName = "Game/Stage/Stage Definition")]
public sealed class StageDefinitionAsset : ScriptableObject
{
    private const int MIN_STAGE_NUMBER = 1;

    // ステージ番号
    [SerializeField, Header("ステージ")]
    [Min(MIN_STAGE_NUMBER)]
    private int m_stageNumber = MIN_STAGE_NUMBER;

    // ロードするScene名
    [SerializeField]
    private string m_sceneName;

    // ステージ固有設定
    [SerializeField]
    private StageSettingsAsset m_stageSettings;

    /// <summary>
    /// ステージ番号を取得します。
    /// </summary>
    public int StageNumber =>
        m_stageNumber;

    /// <summary>
    /// ロードするScene名を取得します。
    /// </summary>
    public string SceneName =>
        m_sceneName;

    /// <summary>
    /// ステージ固有設定を取得します。
    /// </summary>
    public StageSettingsAsset StageSettings =>
        m_stageSettings;

    /// <summary>
    /// ステージ表示名を取得します。
    /// </summary>
    public string StageName =>
        m_stageSettings != null
            ? m_stageSettings.StageName
            : string.Empty;

#if UNITY_EDITOR

    private void OnValidate()
    {
        m_stageNumber =
            Mathf.Max(
                MIN_STAGE_NUMBER,
                m_stageNumber);
    }

#endif
}
