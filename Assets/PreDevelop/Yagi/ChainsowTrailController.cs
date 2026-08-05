using UnityEngine;

/// <summary>
/// アーマチュアのボーンを基準にチェーンソーの軌跡を制御します。
/// </summary>
[RequireComponent(typeof(TrailRenderer))]
public class ChainsawTrailController : MonoBehaviour
{
    /// <summary>
    /// 軌跡の種類。
    /// </summary>
    public enum TrailType
    {
        PERSISTENT,
        FADE
    }

    [Header("ボーン設定")]

    /// <summary>
    /// 軌跡の基準となるボーン。
    /// </summary>
    [SerializeField]
    private Transform m_targetBone;

    /// <summary>
    /// 1つ目の接地判定位置。
    /// </summary>
    [SerializeField]
    private Vector3 m_positionOffset;

    /// <summary>
    /// 2つ目の接地判定位置。
    /// </summary>
    [SerializeField]
    private Vector3 m_secondPositionOffset;

    [Header("接地判定")]

    /// <summary>
    /// 地面として判定するレイヤー。
    /// </summary>
    [SerializeField]
    private LayerMask m_groundLayer;

    /// <summary>
    /// 地面判定を行う距離。
    /// </summary>
    [SerializeField]
    private float m_groundCheckDistance = 0.15f;

    /// <summary>
    /// 地面判定開始位置のオフセット。
    /// </summary>
    [SerializeField]
    private float m_groundCheckOffset = 0.02f;

    [Header("軌跡設定")]

    /// <summary>
    /// 軌跡の種類。
    /// </summary>
    [SerializeField]
    private TrailType m_trailType = TrailType.FADE;

    /// <summary>
    /// 軌跡の幅。
    /// </summary>
    [SerializeField]
    private float m_trailWidth = 0.03f;

    /// <summary>
    /// フェードするまでの時間。
    /// </summary>
    [SerializeField]
    private float m_fadeTime = 1.0f;

    /// <summary>
    /// 残り続ける軌跡の保持時間。
    /// </summary>
    [SerializeField]
    private float m_persistentTrailTime = 99999.0f;

    [Header("デバッグ")]

    /// <summary>
    /// Sceneビューに接地判定用のRayを表示するか。
    /// </summary>
    [SerializeField]
    private bool m_showGroundCheck = true;

    /// <summary>
    /// 使用するTrailRenderer。
    /// </summary>
    private TrailRenderer m_trailRenderer;

    /// <summary>
    /// 初期化処理を行います。
    /// </summary>
    private void Awake()
    {
        m_trailRenderer = GetComponent<TrailRenderer>();

        if (m_trailRenderer == null)
        {
            return;
        }

        SetupTrail();
    }

    /// <summary>
    /// アニメーション後にボーン位置を追従します。
    /// </summary>
    private void LateUpdate()
    {
        if (m_trailRenderer == null || m_targetBone == null)
        {
            return;
        }

        UpdateTrail();
    }

    /// <summary>
    /// 接地状態に応じて軌跡を更新します。
    /// </summary>
    private void UpdateTrail()
    {
        Vector3 firstPosition = GetOffsetPosition(m_positionOffset);
        Vector3 secondPosition = GetOffsetPosition(m_secondPositionOffset);

        bool firstHit = CheckGround(firstPosition, out RaycastHit firstHitInfo);
        bool secondHit = CheckGround(secondPosition, out RaycastHit secondHitInfo);

        if (!firstHit && !secondHit)
        {
            m_trailRenderer.emitting = false;
            return;
        }

        Vector3 trailPosition = GetTrailPosition(
            firstHit,
            firstHitInfo,
            secondHit,
            secondHitInfo
        );

        transform.position = trailPosition;
        m_trailRenderer.emitting = true;
    }

    /// <summary>
    /// ボーンを基準に指定したオフセット位置を取得します。
    /// </summary>
    /// <param name="positionOffset">ボーンからの位置オフセット。</param>
    /// <returns>オフセット適用後のワールド座標。</returns>
    private Vector3 GetOffsetPosition(Vector3 positionOffset)
    {
        return m_targetBone.position +
               m_targetBone.rotation * positionOffset;
    }

    /// <summary>
    /// 指定位置から地面との接触を確認します。
    /// </summary>
    /// <param name="position">地面判定を行う位置。</param>
    /// <param name="hit">地面に接触した情報。</param>
    /// <returns>
    /// true：地面に接しています。
    /// false：地面に接していません。
    /// </returns>
    private bool CheckGround(Vector3 position, out RaycastHit hit)
    {
        Vector3 rayStartPosition =
            position +
            Vector3.up * m_groundCheckOffset;

        return Physics.Raycast(
            rayStartPosition,
            Vector3.down,
            out hit,
            m_groundCheckDistance,
            m_groundLayer
        );
    }

    /// <summary>
    /// 接地している位置からTrailの発生位置を決定します。
    /// </summary>
    /// <param name="firstHit">1つ目の接地判定結果。</param>
    /// <param name="firstHitInfo">1つ目のRaycast結果。</param>
    /// <param name="secondHit">2つ目の接地判定結果。</param>
    /// <param name="secondHitInfo">2つ目のRaycast結果。</param>
    /// <returns>Trailを発生させる位置。</returns>
    private Vector3 GetTrailPosition(
        bool firstHit,
        RaycastHit firstHitInfo,
        bool secondHit,
        RaycastHit secondHitInfo)
    {
        if (firstHit && secondHit)
        {
            return (firstHitInfo.point + secondHitInfo.point) * 0.5f;
        }

        if (firstHit)
        {
            return firstHitInfo.point;
        }

        return secondHitInfo.point;
    }

    /// <summary>
    /// TrailRendererの設定を行います。
    /// </summary>
    private void SetupTrail()
    {
        m_trailRenderer.startWidth = m_trailWidth;
        m_trailRenderer.endWidth = m_trailWidth;

        switch (m_trailType)
        {
            case TrailType.PERSISTENT:
                m_trailRenderer.time = m_persistentTrailTime;
                break;

            case TrailType.FADE:
                m_trailRenderer.time = m_fadeTime;
                break;
        }
    }

    /// <summary>
    /// Sceneビューに接地判定用のRayを表示します。
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!m_showGroundCheck || m_targetBone == null)
        {
            return;
        }

        Vector3 firstPosition = GetOffsetPosition(m_positionOffset);
        Vector3 secondPosition = GetOffsetPosition(m_secondPositionOffset);

        DrawGroundCheckGizmo(firstPosition);
        DrawGroundCheckGizmo(secondPosition);
    }

    /// <summary>
    /// 接地判定用のGizmoを表示します。
    /// </summary>
    /// <param name="position">接地判定位置。</param>
    private void DrawGroundCheckGizmo(Vector3 position)
    {
        Vector3 rayStartPosition =
            position +
            Vector3.up * m_groundCheckOffset;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(position, 0.02f);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(
            rayStartPosition,
            rayStartPosition + Vector3.down * m_groundCheckDistance
        );
    }
}