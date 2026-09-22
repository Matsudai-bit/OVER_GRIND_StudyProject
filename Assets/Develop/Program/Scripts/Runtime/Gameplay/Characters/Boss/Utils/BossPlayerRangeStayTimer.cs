using UnityEngine;

/// <summary>
/// Playerが指定範囲内に連続して滞在した時間を計測します。
/// </summary>
[DisallowMultipleComponent]
public sealed class BossPlayerRangeStayTimer : MonoBehaviour
{
    // フェーズ共通参照
    [SerializeField, Header("参照")]
    private BossPhaseReferences m_references;

    // 行動選択パラメータ
    [SerializeField]
    private S1P1BossDecisionParameterAsset m_decisionParameter;

    // 現在の連続滞在時間
    [SerializeField, Header("デバッグ")]
    private float m_timeCounter = 0.0f;

    // Gizmo表示色
    [SerializeField]
    private Color m_gizmoColor = Color.yellow;

    // PlayerのTransform
    private Transform m_playerTransform;

    // ボスの判定基準位置
    private Transform m_bossTransform;

    /// <summary>
    /// Playerの連続滞在時間を取得します。
    /// </summary>
    public float StayTime => m_timeCounter;

    // Playerとの距離判定に使用する半径
    private float RangeDistance =>
        m_decisionParameter.Stomp.Distance;

    private void Awake()
    {
        if (!TryInitialize())
        {
            enabled = false;
        }
    }

    private void Update()
    {
        UpdateStayTime();
    }

    /// <summary>
    /// 必要な参照を初期化します。
    /// </summary>
    /// <returns>
    /// true：初期化できました。
    /// false：初期化できませんでした。
    /// </returns>
    private bool TryInitialize()
    {
        if (m_references == null)
        {
            Debug.LogError(
                $"{nameof(BossPlayerRangeStayTimer)}: " +
                $"{nameof(m_references)} が設定されていません。",
                this);

            return false;
        }

        if (m_decisionParameter == null)
        {
            Debug.LogError(
                $"{nameof(BossPlayerRangeStayTimer)}: " +
                $"{nameof(m_decisionParameter)} が設定されていません。",
                this);

            return false;
        }

        m_playerTransform = m_references.PlayerTransform;
        m_bossTransform = m_references.Origin;

        if (m_playerTransform == null ||
            m_bossTransform == null)
        {
            Debug.LogError(
                $"{nameof(BossPlayerRangeStayTimer)}: " +
                "必要なTransformが設定されていません。",
                this);

            return false;
        }

        m_timeCounter = 0.0f;
        return true;
    }

    /// <summary>
    /// Playerの連続滞在時間を更新します。
    /// </summary>
    private void UpdateStayTime()
    {
        Vector3 difference =
            m_playerTransform.position - m_bossTransform.position;

        // 足元判定なので高さを無視する
        difference.y = 0.0f;

        float sqrDistance = difference.sqrMagnitude;
        float sqrRangeDistance = RangeDistance * RangeDistance;

        if (sqrDistance <= sqrRangeDistance)
        {
            m_timeCounter += Time.deltaTime;
            return;
        }

        m_timeCounter = 0.0f;
    }

    private void OnDrawGizmosSelected()
    {
        if (m_references == null ||
            m_decisionParameter == null ||
            m_references.Origin == null)
        {
            return;
        }

        Gizmos.color = m_gizmoColor;
        Gizmos.DrawWireSphere(
            m_references.Origin.position,
            m_decisionParameter.Stomp.Distance);
    }
}
