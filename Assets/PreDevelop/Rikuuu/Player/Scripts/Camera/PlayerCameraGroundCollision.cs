using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Cinemachineが算出したカメラ位置をGroundLayerの手前へ補正し、地面への貫通を防ぎます。
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerCameraGroundCollision : CinemachineExtension
{
    private const float MIN_CAST_DISTANCE = 0.0001f;

    [SerializeField]
    [Tooltip("カメラを貫通させない地面のLayer。")]
    private LayerMask m_groundLayerMask = 1 << 3;

    [SerializeField, Min(0.01f)]
    [Tooltip("地面との判定に使用するカメラ周囲の半径。")]
    private float m_collisionRadius = 0.3f;

    [SerializeField, Min(0.0f)]
    [Tooltip("地面との接触位置から追加で確保する余白。")]
    private float m_collisionPadding = 0.05f;

    [SerializeField, Min(0.0f)]
    [Tooltip("追従対象の位置から地面判定を開始する高さ。")]
    private float m_originHeight = 1.0f;

    /// <summary>
    /// 地面衝突判定に使用する値を設定します。
    /// </summary>
    /// <param name="groundLayerMask">地面として判定するLayer。</param>
    /// <param name="collisionRadius">カメラ周囲の衝突判定半径。</param>
    /// <param name="collisionPadding">接触位置から確保する追加の余白。</param>
    /// <param name="originHeight">追従対象から判定開始位置までの高さ。</param>
    public void Configure(
        LayerMask groundLayerMask,
        float collisionRadius,
        float collisionPadding,
        float originHeight)
    {
        m_groundLayerMask = groundLayerMask;
        m_collisionRadius = Mathf.Max(0.01f, collisionRadius);
        m_collisionPadding = Mathf.Max(0.0f, collisionPadding);
        m_originHeight = Mathf.Max(0.0f, originHeight);
    }

    /// <summary>
    /// 最終カメラ位置まで球体を飛ばし、地面に接触する場合は接触直前へ位置を戻します。
    /// </summary>
    /// <param name="vcam">補正対象のCinemachineカメラ。</param>
    /// <param name="stage">現在のCinemachineパイプライン段階。</param>
    /// <param name="state">補正対象のカメラ状態。</param>
    /// <param name="deltaTime">今回の更新時間。</param>
    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Finalize ||
            vcam != ComponentOwner ||
            m_groundLayerMask.value == 0 ||
            !(vcam is CinemachineCamera camera) ||
            camera.Target.TrackingTarget == null)
        {
            return;
        }

        Vector3 origin = camera.Target.TrackingTarget.position + Vector3.up * m_originHeight;
        Vector3 desiredPosition = state.GetFinalPosition();
        Vector3 castVector = desiredPosition - origin;
        float castDistance = castVector.magnitude;

        if (castDistance <= MIN_CAST_DISTANCE)
        {
            return;
        }

        Vector3 castDirection = castVector / castDistance;
        if (!Physics.SphereCast(
                origin,
                m_collisionRadius,
                castDirection,
                out RaycastHit hit,
                castDistance,
                m_groundLayerMask,
                QueryTriggerInteraction.Ignore))
        {
            return;
        }

        // SphereCastの中心位置を接触点から少し戻し、Near Clipによる地面の透過も防ぎます。
        float correctedDistance = Mathf.Max(0.0f, hit.distance - m_collisionPadding);
        Vector3 correctedPosition = origin + castDirection * correctedDistance;
        state.PositionCorrection += correctedPosition - desiredPosition;
    }
}
