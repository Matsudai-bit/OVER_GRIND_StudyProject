using UnityEngine;

/// <summary>
/// ゲームプレイ中に発生するCue情報です。
/// </summary>
[CreateAssetMenu(
    fileName = "GameplayCue",
    menuName = "Game/Gameplay/Gameplay Cue")]
public class GameplayCueAsset : ScriptableObject
{
    // Cueの種類
    [SerializeField, Header("Cue設定")]
    private GameplayCueType m_cueType = GameplayCueType.NOTIFICATION;

    // Cueの継続時間
    [SerializeField, Min(0.0f)]
    private float m_duration = 0.0f;

    /// <summary>
    /// Cueの種類を取得します。
    /// </summary>
    public GameplayCueType CueType => m_cueType;

    /// <summary>
    /// Cueの継続時間を取得します。
    /// </summary>
    public float Duration => m_duration;

    /// <summary>
    /// 自動終了するCueか取得します。
    /// </summary>
    public bool HasDuration => m_duration > 0.0f;
}