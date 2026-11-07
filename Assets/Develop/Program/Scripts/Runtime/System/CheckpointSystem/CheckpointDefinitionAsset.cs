using UnityEngine;

/// <summary>
/// チェックポイントの静的な定義を保持します。
/// </summary>
[CreateAssetMenu(
    fileName = "CheckpointDefinition",
    menuName = "Game/Checkpoint/Checkpoint Definition")]
public sealed class CheckpointDefinitionAsset : ScriptableObject
{
    // チェックポイント識別子
    [SerializeField, Header("チェックポイント")]
    private string m_checkpointId = "Checkpoint";

    // チェックポイントに対応するボスフェーズ
    [SerializeField]
    private BossPhaseID m_bossPhase =
        BossPhaseID.PHASE_1;

    /// <summary>
    /// チェックポイント識別子を取得します。
    /// </summary>
    public string CheckpointId =>
        m_checkpointId;

    /// <summary>
    /// 対応するボスフェーズを取得します。
    /// </summary>
    public BossPhaseID BossPhase =>
        m_bossPhase;
}
