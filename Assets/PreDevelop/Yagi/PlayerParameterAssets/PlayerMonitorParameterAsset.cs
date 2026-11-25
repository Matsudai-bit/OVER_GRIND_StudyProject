using UnityEngine;

/// <summary>
/// PlayerMonitorの接地・接地面・レール検知設定を保持します。
/// </summary>
[CreateAssetMenu(
    fileName = "PlayerMonitorParameter",
    menuName = "Game/Parameters/Player/Monitor")]
public sealed class PlayerMonitorParameterAsset : ScriptableObject
{
    // 接地判定の半径
    [SerializeField, Header("接地判定")]
    [Tooltip("プレイヤーの足元に地面が存在するか確認する球判定の半径です。")]
    [Min(0.01f)]
    private float m_groundCheckRadius = 0.25f;

    // 接地対象のレイヤー
    [SerializeField]
    [Tooltip("地面として扱うLayerを指定します。")]
    private LayerMask m_groundLayerMask = ~0;

    // Ground Probeの半径
    [SerializeField, Header("接地面検出")]
    [Tooltip("現在プレイヤーを支えている接地面を調べるSphereCastの半径です。")]
    [Min(0.01f)]
    private float m_groundProbeRadius = 0.18f;

    // Ground Probe開始位置の上方向オフセット
    [SerializeField]
    [Tooltip("Ground Check OriginからSphereCast開始位置を上へずらす距離です。")]
    [Min(0.0f)]
    private float m_groundProbeStartOffset = 0.25f;

    // Ground Probeの下方向探索距離
    [SerializeField]
    [Tooltip("Ground Check Originより下方向へ接地面を探索する追加距離です。")]
    [Min(0.01f)]
    private float m_groundProbeDistance = 0.4f;

    // 最大登坂角度
    [SerializeField]
    [Tooltip("プレイヤーが地面として扱い、移動できる最大斜面角度です。")]
    [Range(0.0f, 89.0f)]
    private float m_maxClimbSlopeAngle = 50.0f;

    // 接地面法線の補間速度
    [SerializeField]
    [Tooltip("接地面が切り替わった際に、移動用法線が追従する最大角速度です。単位は度/秒です。")]
    [Min(0.0f)]
    private float m_groundNormalSmoothSpeed = 720.0f;

    [SerializeField, Header("段差乗り越え")]
    [Tooltip("自動で乗り越えられる最大段差高さです。")]
    [Min(0.0f)]
    private float m_maxStepHeight = 0.3f;

    [SerializeField]
    [Tooltip("段差判定を行う前方距離です。")]
    [Min(0.01f)]
    private float m_stepCheckDistance = 0.25f;

    [SerializeField]
    [Tooltip("段差上面を探索する際の余裕距離です。")]
    [Min(0.0f)]
    private float m_stepForwardOffset = 0.05f;

    // レール判定対象のレイヤー
    [SerializeField, Header("レール搭乗判定")]
    [Tooltip("レールとして検出するLayerを指定します。")]
    private LayerMask m_railLayerMask = ~0;

    // 地上・空中のレール搭乗判定設定
    [SerializeField]
    [Tooltip("地上・空中それぞれのレール搭乗判定半径を設定するアセットです。")]
    private PlayerRailDetectionParameterAsset m_railDetectionParameter;

    /// <summary>接地判定の半径を取得します。</summary>
    public float GroundCheckRadius =>
        Mathf.Max(0.01f, m_groundCheckRadius);

    /// <summary>接地対象のLayerMaskを取得します。</summary>
    public LayerMask GroundLayerMask => m_groundLayerMask;

    /// <summary>Ground Probeの半径を取得します。</summary>
    public float GroundProbeRadius =>
        Mathf.Max(0.01f, m_groundProbeRadius);

    /// <summary>Ground Probe開始位置の上方向オフセットを取得します。</summary>
    public float GroundProbeStartOffset =>
        Mathf.Max(0.0f, m_groundProbeStartOffset);

    /// <summary>Ground Probeの下方向探索距離を取得します。</summary>
    public float GroundProbeDistance =>
        Mathf.Max(0.01f, m_groundProbeDistance);

    /// <summary>最大登坂角度を取得します。</summary>
    public float MaxClimbSlopeAngle =>
        Mathf.Clamp(m_maxClimbSlopeAngle, 0.0f, 89.0f);

    /// <summary>接地面法線の補間速度を取得します。</summary>
    public float GroundNormalSmoothSpeed =>
        Mathf.Max(0.0f, m_groundNormalSmoothSpeed);

    /// <summary>レール判定対象のLayerMaskを取得します。</summary>
    public LayerMask RailLayerMask => m_railLayerMask;

    /// <summary>レール搭乗判定設定を取得します。</summary>
    public PlayerRailDetectionParameterAsset RailDetectionParameter =>
        m_railDetectionParameter;
}
