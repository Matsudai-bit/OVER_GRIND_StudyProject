using UnityEngine;

/// <summary>
/// ボス破壊可能部位の被弾演出パラメータを保持します。
/// </summary>
[CreateAssetMenu(
    fileName = "BossBreakablePartFeedbackParameter",
    menuName = "Game/Boss/Breakable Part Feedback Parameter")]
public sealed class BossBreakablePartFeedbackParameterAsset :
    ScriptableObject
{
    // Emissiveの加算値
    [SerializeField, Header("Emissive")]
    private float m_emissiveIncrease = 50.0f;

    // Emissiveを上げる時間
    [SerializeField, Header("上昇"), Min(0.0f)]
    private float m_riseDuration = 0.2f;

    // Emissive上昇時のイージング
    [SerializeField]
    private AnimationCurve m_riseCurve =
        AnimationCurve.EaseInOut(
            0.0f,
            0.0f,
            1.0f,
            1.0f);

    // Emissiveを下げる時間
    [SerializeField, Header("下降"), Min(0.0f)]
    private float m_fallDuration = 0.3f;

    // Emissive下降時のイージング
    [SerializeField]
    private AnimationCurve m_fallCurve =
        AnimationCurve.EaseInOut(
            0.0f,
            0.0f,
            1.0f,
            1.0f);

    /// <summary>
    /// Emissiveの加算値を取得します。
    /// </summary>
    public float EmissiveIncrease =>
        m_emissiveIncrease;

    /// <summary>
    /// Emissive上昇時間を取得します。
    /// </summary>
    public float RiseDuration =>
        m_riseDuration;

    /// <summary>
    /// Emissive上昇カーブを取得します。
    /// </summary>
    public AnimationCurve RiseCurve =>
        m_riseCurve;

    /// <summary>
    /// Emissive下降時間を取得します。
    /// </summary>
    public float FallDuration =>
        m_fallDuration;

    /// <summary>
    /// Emissive下降カーブを取得します。
    /// </summary>
    public AnimationCurve FallCurve =>
        m_fallCurve;
}