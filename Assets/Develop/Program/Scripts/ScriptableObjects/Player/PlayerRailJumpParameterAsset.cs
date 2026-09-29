using UnityEngine;

/// <summary>レールジャンプの初速・角度・入力判定を共有データとして保持します。</summary>
[CreateAssetMenu(fileName = "PlayerRailJumpParameter", menuName = "Game/Parameters/Player/Rail Jump Parameter")]
public sealed class PlayerRailJumpParameterAsset : ScriptableObject
{
    [SerializeField]
    private PlayerRailJumpParameters m_parameters = new PlayerRailJumpParameters();

    /// <summary>ジャンプの調整値を取得します。</summary>
    public PlayerRailJumpParameters Parameters =>
        m_parameters ?? (m_parameters = new PlayerRailJumpParameters());
}
