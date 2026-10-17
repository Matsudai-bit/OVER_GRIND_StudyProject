using UnityEngine;

/// <summary>
/// ゲームプレイ中に実行するシーケンス情報です。
/// </summary>
[CreateAssetMenu(
    fileName = "GameplaySequence",
    menuName = "Game/Gameplay/Gameplay Sequence")]
public sealed class GameplaySequenceAsset : ScriptableObject
{
    // シーケンス中に必要なゲームプレイモード
    [SerializeField, Header("ゲームプレイモード")]
    private GameplayModeType m_requiredMode = GameplayModeType.CINEMATIC;

    /// <summary>
    /// シーケンス中に必要なゲームプレイモードを取得します。
    /// </summary>
    public GameplayModeType RequiredMode => m_requiredMode;
}