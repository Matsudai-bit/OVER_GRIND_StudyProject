using UnityEngine;

[CreateAssetMenu(fileName = "NewVibrationData", menuName = "Vibration/Vibration Data")]

/// コントローラーの振動や振動する秒数を仮として所持しデータを生成するクラス
public class VibrationData : ScriptableObject
{
    [Header("モーター強度設定")]
    [Tooltip("低周波（重い衝撃、爆発など） 0.0 ~ 1.0")]
    [Range(0f, 1f)]
    public float m_lowFrequency = 0.5f;

    [Tooltip("高周波（細かい振動、金属音など） 0.0 ~ 1.0")]
    [Range(0f, 1f)]
    public float m_highFrequency = 0.5f;

    [Header("時間と変化")]
    [Tooltip("振動が続く時間（秒）")]
    [Min(0f)]
    public float m_duration = 0.2f;

    [Tooltip("時間経過に伴う振動強度の減衰カーブ")]
    public AnimationCurve dampingCurve = AnimationCurve.Linear(0, 1, 1, 0);
}