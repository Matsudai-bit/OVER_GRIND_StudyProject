using UnityEngine;

[CreateAssetMenu(fileName = "NewGamepadRumbleAsset", menuName = "Game/Rumble/Gamepad Rumble Asset")]
public class GamepadRumbleAsset : ScriptableObject
{
    [SerializeField, Header("振動持続時間 (秒)")]
    [Min(0f)] private float m_duration = 0.2f;

    [SerializeField, Header("低周波モーター最大強度 (0.0 ~ 1.0)")]
    [Range(0f, 1f)] private float m_lowFrequencyStrength = 0.5f;

    [SerializeField, Header("高周波モーター最大強度 (0.0 ~ 1.0)")]
    [Range(0f, 1f)] private float m_highFrequencyStrength = 0.5f;

    [SerializeField, Header("時間経過に伴う振動強度の評価カーブ")]
    private AnimationCurve m_dampingCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    [SerializeField, Header("固定優先度 (PRIORITYモード用)")]
    private int m_priority = 0;

    // GamepadRumbleController から値を読み取るための公開プロパティ
    public float Duration => m_duration;
    public float LowFrequencyStrength => m_lowFrequencyStrength;
    public float HighFrequencyStrength => m_highFrequencyStrength;
    public AnimationCurve DampingCurve => m_dampingCurve;
    public int Priority => m_priority;
}