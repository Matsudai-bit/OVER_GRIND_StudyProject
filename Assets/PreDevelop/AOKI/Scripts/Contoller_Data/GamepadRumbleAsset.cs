using UnityEngine;

/// <summary>
/// 振動パターンの設定データを保持します。
/// </summary>
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

    [SerializeField, Header("ループ再生を行うかどうか")]
    private bool m_isLoop = false;

    [SerializeField, Header("優先度の高い振動に割り込まれた後、復帰再生させるか")]
    private bool m_canResumeAfterInterrupt = true;

    /// <summary>振動持続時間を取得します。</summary>
    public float Duration => m_duration;

    /// <summary>低周波モーター最大強度を取得します。</summary>
    public float LowFrequencyStrength => m_lowFrequencyStrength;

    /// <summary>高周波モーター最大強度を取得します。</summary>
    public float HighFrequencyStrength => m_highFrequencyStrength;

    /// <summary>時間経過に伴う振動強度の評価カーブを取得します。</summary>
    public AnimationCurve DampingCurve => m_dampingCurve;

    /// <summary>固定優先度を取得します。</summary>
    public int Priority => m_priority;

    /// <summary>ループ再生フラグを取得または設定します。</summary>
    public bool IsLoop
    {
        get => m_isLoop;
        set => m_isLoop = value;
    }

    /// <summary>割り込み後の復帰許可フラグを取得または設定します。</summary>
    public bool CanResumeAfterInterrupt
    {
        get => m_canResumeAfterInterrupt;
        set => m_canResumeAfterInterrupt = value;
    }
}