using UnityEngine;

/// <summary>
/// ボスの突進攻撃パラメータを設定します。
/// </summary>
[CreateAssetMenu(
    fileName = "S1BossChargeAttackParameter",
    menuName = "Game/Parameters/Boss/Attack/Charge Attack Parameter")]
public sealed class S1BossChargeAttackParameterAsset :
    ScriptableObject
{
    // 突進前の準備時間
    [SerializeField, Header("ゲーム調整")]
    [Tooltip("突進開始前にプレイヤー方向を追尾する時間です。")]
    [Range(0.0f, 10.0f)]
    private float m_preparationDuration = 1.0f;

    // 準備中の回転速度
    [SerializeField]
    [Tooltip("準備中にプレイヤー方向へ回転する速度です。")]
    [Range(0.0f, 360.0f)]
    private float m_rotationSpeed = 70.0f;

    // 突進速度
    [SerializeField]
    [Tooltip("突進中の移動速度です。")]
    [Range(0.0f, 100.0f)]
    private float m_chargeSpeed = 30.0f;

    // 最大突進距離
    [SerializeField]
    [Tooltip("1回の突進で移動できる最大距離です。")]
    [Range(0.0f, 100.0f)]
    private float m_maxChargeDistance = 30.0f;

    // 停止余白
    [SerializeField]
    [Tooltip("障害物や移動可能範囲の手前で停止する距離です。")]
    [Range(0.0f, 5.0f)]
    private float m_stopMargin = 0.5f;

    // 突進終了後の硬直時間
    [SerializeField]
    [Tooltip("突進終了後に次の行動へ移るまでの時間です。")]
    [Range(0.0f, 10.0f)]
    private float m_endDuration = 1.0f;

    // 準備アニメーション名
    [SerializeField, Header("システム設定")]
    [Tooltip("突進準備で使用するAnimator Bool名です。")]
    private string m_preparationAnimationBoolName;

    // 突進アニメーション名
    [SerializeField]
    [Tooltip("突進中に使用するAnimator Bool名です。")]
    private string m_chargeAnimationBoolName;

    // 終了アニメーション名
    [SerializeField]
    [Tooltip("突進終了時に使用するAnimator Bool名です。")]
    private string m_endAnimationBoolName;

    // 攻撃識別子
    [SerializeField]
    [Tooltip("突進中に有効化する攻撃判定の識別子です。")]
    private AttackIdentifier m_attackIdentifier;

    /// <summary>
    /// 実行時に使用する突進攻撃パラメータを生成します。
    /// </summary>
    public S1BossChargeAttackParameters CreateParameters()
    {
        return new S1BossChargeAttackParameters(
            m_preparationDuration,
            m_rotationSpeed,
            m_chargeSpeed,
            m_maxChargeDistance,
            m_stopMargin,
            m_endDuration,
            m_preparationAnimationBoolName,
            m_chargeAnimationBoolName,
            m_endAnimationBoolName,
            m_attackIdentifier);
    }
}