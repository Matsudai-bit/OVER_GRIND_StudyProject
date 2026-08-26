using System;
using UnityEngine;

/// <summary>
/// 直線突進の実行設定を保持します。
/// </summary>
[Serializable]
public sealed class StraightChargeSettings
{
    // 予備動作時間
    [SerializeField, Header("予備動作")]
    [Min(0.0f)]
    private float m_preparationDuration = 1.0f;

    // 予備動作中の回転速度
    [SerializeField, Min(0.0f)]
    private float m_rotationSpeed = 70.0f;

    // 突進速度
    [SerializeField, Header("突進移動")]
    [Min(0.0f)]
    private float m_chargeSpeed = 100.0f;

    // 最大突進距離
    [SerializeField, Min(0.0f)]
    private float m_maxChargeDistance = 100.0f;

    // NavMesh境界との停止余白
    [SerializeField, Min(0.0f)]
    private float m_stopMargin = 0.5f;

    // 終了アニメーション待機時間
    [SerializeField, Header("終了")]
    [Min(0.0f)]
    private float m_endDuration = 0.0f;

    // 開始アニメーションTrigger名
    [SerializeField, Header("アニメーション")]
    private string m_startAnimationTriggerName;

    // 突進中アニメーションTrigger名
    [SerializeField]
    private string m_chargeAnimationTriggerName;

    // 終了アニメーションTrigger名
    [SerializeField]
    private string m_endAnimationTriggerName;

    // 突進中に有効化する攻撃ID
    [SerializeField, Header("攻撃判定")]
    private AttackIdentifier m_attackIdentifier;

    /// <summary>
    /// 予備動作時間を取得します。
    /// </summary>
    public float PreparationDuration => m_preparationDuration;

    /// <summary>
    /// 回転速度を取得します。
    /// </summary>
    public float RotationSpeed => m_rotationSpeed;

    /// <summary>
    /// 突進速度を取得します。
    /// </summary>
    public float ChargeSpeed => m_chargeSpeed;

    /// <summary>
    /// 最大突進距離を取得します。
    /// </summary>
    public float MaxChargeDistance => m_maxChargeDistance;

    /// <summary>
    /// 停止余白を取得します。
    /// </summary>
    public float StopMargin => m_stopMargin;

    /// <summary>
    /// 終了待機時間を取得します。
    /// </summary>
    public float EndDuration => m_endDuration;

    /// <summary>
    /// 開始アニメーションTrigger名を取得します。
    /// </summary>
    public string StartAnimationTriggerName => m_startAnimationTriggerName;

    /// <summary>
    /// 突進中アニメーションTrigger名を取得します。
    /// </summary>
    public string ChargeAnimationTriggerName => m_chargeAnimationTriggerName;

    /// <summary>
    /// 終了アニメーションTrigger名を取得します。
    /// </summary>
    public string EndAnimationTriggerName => m_endAnimationTriggerName;

    /// <summary>
    /// 突進中に使用する攻撃IDを取得します。
    /// </summary>
    public AttackIdentifier AttackIdentifier => m_attackIdentifier;
}
