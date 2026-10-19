using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// ロックオン中の構図を一つの計算で決定します。
/// 最終構図を先に求め、現在位置から目的位置へ直線的に補間します。
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerLockOnCamera : CinemachineExtension
{
    private const float MIN_DISTANCE = 0.01f;

    [SerializeField, Range(0.05f, 0.3f)] private float m_screenMargin = 0.12f;
    [SerializeField, Range(0.0f, 20.0f)] private float m_pitch = 8.0f;
    [SerializeField, Min(0.0f)] private float m_bodyPadding = 0.2f;
    [SerializeField, Min(0.1f)] private float m_minimumDistance = 5.0f;

    private Transform m_target;
    private Transform m_player;
    private Renderer[] m_renderers;
    private bool m_active;
    private bool m_initialize;
    private bool m_releasing;
    private float m_duration;
    private float m_transitionTime;
    private float m_releaseTime;
    private Vector3 m_pivot;
    private Vector3 m_transitionOffset;
    private Quaternion m_transitionRotation;
    private Vector3 m_positionVelocity;
    private Vector3 m_previousPlayerPosition;
    private Vector3 m_position;
    private Quaternion m_rotation;
    private Vector3 m_releaseOffset;
    private Quaternion m_releaseRotation;
    private bool m_hasOutput;

    /// <summary>ロックオンまたは通常カメラへの復帰が進行中かを取得します。</summary>
    public bool IsControlling => m_active || m_releasing;

    /// <summary>現在のカメラ状態を維持してロックオンを開始します。</summary>
    public void BeginFocus(Transform target, float duration)
    {
        if (target == null)
            return;
        // 呼び出し側の設定値を、そのままロックオン開始から到達までの時間として使用します。
        m_duration = Mathf.Max(0.05f, duration);
        m_initialize = !m_active || m_target != target;
        m_target = target;
        m_active = true;
        m_releasing = false;
    }

    /// <summary>対象変更時は現在の出力位置を次の直線遷移の始点にします。</summary>
    public void SetTarget(Transform target)
    {
        if (!m_active)
            return;
        if (m_target != target)
            m_initialize = true;
        m_target = target;
        if (target == null)
            EndFocus();
    }

    /// <summary>最後のロックオン位置から通常カメラへ復帰します。</summary>
    public void EndFocus()
    {
        if (!m_active)
            return;
        m_active = false;
        m_releasing = m_hasOutput;
        m_releaseTime = 0.0f;
        m_releaseOffset = m_player != null ? m_position - m_player.position : m_position;
        m_releaseRotation = m_rotation;
        // 通常軌道の内部角度も現在の姿勢へ合わせ、解除時の大きな逆旋回を防ぎます。
        if (m_hasOutput && ComponentOwner != null)
            ComponentOwner.ForceCameraPosition(m_position, m_rotation);
    }

    /// <summary>Cinemachineの最終段階で、一貫した位置と向きを出力します。</summary>
    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage,
        ref CameraState state, float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Finalize || vcam != ComponentOwner)
            return;
        if (m_active && m_target == null)
            EndFocus();
        if (!IsControlling)
        {
            m_position = state.GetFinalPosition();
            m_rotation = state.GetFinalOrientation();
            m_hasOutput = true;
            return;
        }

        float dt = Mathf.Max(0.0f, deltaTime);
        if (m_releasing)
        {
            m_releaseTime += dt;
            float progress = Mathf.SmoothStep(0.0f, 1.0f,
                Mathf.Clamp01(m_releaseTime / Mathf.Max(0.1f, m_duration)));
            Vector3 start = m_player != null ? m_player.position + m_releaseOffset : m_releaseOffset;
            m_position = Vector3.Lerp(start, state.GetFinalPosition(), progress);
            m_rotation = Quaternion.Slerp(m_releaseRotation, state.GetFinalOrientation(), progress);
            if (progress >= 1.0f)
                m_releasing = false;
            ApplyState(ref state);
            return;
        }

        if (!(vcam is CinemachineCamera camera) || camera.Target.TrackingTarget == null)
        {
            EndFocus();
            return;
        }
        if (m_player != camera.Target.TrackingTarget)
        {
            m_player = camera.Target.TrackingTarget;
            m_renderers = m_player.GetComponentsInChildren<Renderer>(true);
        }

        Bounds body = GetBodyBounds();
        if (m_initialize)
        {
            // 前回の最終出力を使い、解除途中の再ロックでも位置を飛ばしません。
            if (!m_hasOutput)
            {
                m_position = camera.State.GetFinalPosition();
                m_rotation = camera.State.GetFinalOrientation();
            }
            m_transitionOffset = m_position - m_player.position;
            m_transitionRotation = m_rotation;
            m_transitionTime = 0.0f;
            m_positionVelocity = Vector3.zero;
            m_previousPlayerPosition = m_player.position;
            m_initialize = false;
            m_hasOutput = true;
            ApplyState(ref state);
            return;
        }

        // Playerの並進は追従させ、目的位置への移動だけを補間します。
        m_position += m_player.position - m_previousPlayerPosition;
        m_previousPlayerPosition = m_player.position;
        Vector3 direction = m_target.position - body.center;
        direction.y = 0.0f;
        float desiredYaw = direction.sqrMagnitude > MIN_DISTANCE
            ? Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg : m_rotation.eulerAngles.y;
        Quaternion desiredRotation = Quaternion.Euler(m_pitch, desiredYaw, 0.0f);
        m_pivot = Vector3.Lerp(body.center, m_target.position, 0.2f);

        float tanVertical = Mathf.Tan(state.Lens.FieldOfView * Mathf.Deg2Rad * 0.5f);
        float tanHorizontal = tanVertical * Mathf.Max(0.1f, state.Lens.Aspect);
        float safeArea = 1.0f - 2.0f * m_screenMargin;
        Quaternion inverse = Quaternion.Inverse(desiredRotation);
        float bodyDistance = GetRequiredDistance(body, inverse, tanHorizontal * safeArea,
            tanVertical * safeArea, state.Lens.NearClipPlane);
        float targetDistance = GetRequiredDistance(new Bounds(m_target.position, Vector3.one * 0.6f),
            inverse, tanHorizontal * safeArea, tanVertical * safeArea, state.Lens.NearClipPlane);
        float required = Mathf.Max(m_minimumDistance, Mathf.Max(bodyDistance, targetDistance));
        Vector3 desiredPosition = m_pivot - desiredRotation * Vector3.forward * required;
        if (m_transitionTime < m_duration)
        {
            m_transitionTime = Mathf.Min(m_duration, m_transitionTime + dt);
            float progress = Mathf.SmoothStep(0.0f, 1.0f, m_transitionTime / m_duration);
            // 角度から途中位置を逆算せず、始点と完成した構図を直接つなぎます。
            Vector3 startPosition = m_player.position + m_transitionOffset;
            m_position = Vector3.Lerp(startPosition, desiredPosition, progress);
            m_rotation = Quaternion.Slerp(m_transitionRotation, desiredRotation, progress);
        }
        else
        {
            m_position = Vector3.SmoothDamp(m_position, desiredPosition, ref m_positionVelocity,
                m_duration, Mathf.Infinity, dt);
            m_rotation = Quaternion.Slerp(m_rotation, desiredRotation,
                1.0f - Mathf.Exp(-dt / m_duration));
        }
        ApplyState(ref state);
    }

    /// <summary>体の描画境界を取得します。エフェクトの大きさは構図へ含めません。</summary>
    private Bounds GetBodyBounds()
    {
        Bounds bounds = new Bounds(m_player.position + Vector3.up, new Vector3(1.0f, 2.0f, 1.0f));
        bool found = false;
        foreach (Renderer renderer in m_renderers)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy ||
                !(renderer is SkinnedMeshRenderer || renderer is MeshRenderer))
                continue;
            if (!found)
                bounds = renderer.bounds;
            else
                bounds.Encapsulate(renderer.bounds);
            found = true;
        }
        bounds.Expand(m_bodyPadding * 2.0f);
        return bounds;
    }

    /// <summary>境界の8頂点が画面の安全領域に入る最小距離を計算します。</summary>
    private float GetRequiredDistance(Bounds bounds, Quaternion inverse,
        float tanHorizontal, float tanVertical, float nearClip)
    {
        float distance = MIN_DISTANCE;
        for (int index = 0; index < 8; index++)
        {
            Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3(
                (index & 1) == 0 ? -1 : 1, (index & 2) == 0 ? -1 : 1, (index & 4) == 0 ? -1 : 1));
            Vector3 local = inverse * (corner - m_pivot);
            distance = Mathf.Max(distance, Mathf.Abs(local.x) / Mathf.Max(MIN_DISTANCE, tanHorizontal) - local.z);
            distance = Mathf.Max(distance, Mathf.Abs(local.y) / Mathf.Max(MIN_DISTANCE, tanVertical) - local.z);
            distance = Mathf.Max(distance, nearClip + m_bodyPadding - local.z);
        }
        return distance;
    }

    /// <summary>他の制御の補正を重ねず、確定した構図をCinemachineへ返します。</summary>
    private void ApplyState(ref CameraState state)
    {
        state.RawPosition = m_position;
        state.RawOrientation = m_rotation;
        state.PositionCorrection = Vector3.zero;
        state.OrientationCorrection = Quaternion.identity;
        state.Lens.Dutch = 0.0f;
    }
}
