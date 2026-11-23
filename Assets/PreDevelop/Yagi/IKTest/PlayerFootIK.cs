using UnityEngine;

/// <summary>Generic リグの脚をアニメーション評価後に地形に合わせます。</summary>
[DefaultExecutionOrder(500)]
[DisallowMultipleComponent]
public sealed class PlayerFootIK : MonoBehaviour
{
    [SerializeField] private LayerMask m_groundLayerMask = 1 << 3;
    [SerializeField, Min(0f)] private float m_rayStartHeight = .45f;
    [SerializeField, Min(0f)] private float m_rayDistance = 1.1f;
    [SerializeField, Min(0f)] private float m_footSoleOffset = .08f;
    [SerializeField, Min(.001f)] private float m_maxCorrection = .3f;
    [SerializeField, Min(.001f)] private float m_contactHeight = .18f;
    [SerializeField, Min(.01f)] private float m_blendSpeed = 12f;
    [SerializeField, Min(0f)] private float m_heelLength = .07f;
    [SerializeField, Min(0f)] private float m_soleHalfWidth = .04f;
    [SerializeField, Range(30f, 150f), Tooltip("伸びた膝を0度とする、IK後の膝の最大曲げ角度。")]
    private float m_maxKneeBend = 120f;
    [SerializeField, Range(5f, 60f), Tooltip("アニメーション姿勢から許容する足首の追加回転角度。")]
    private float m_maxAnkleCorrection = 35f;
    [SerializeField, Range(5f, 90f), Tooltip("アニメーション姿勢から許容する股関節の追加回転角度。")]
    private float m_maxHipCorrection = 45f;
    [SerializeField, Range(1f, 30f), Tooltip("各角度上限に達する手前で補正を弱める範囲。")]
    private float m_angleFadeRange = 15f;
    private Transform m_pelvis;
    private Vector3 m_pelvisPose;
    private bool m_pelvisApplied;
    private float m_pelvisOffset;
    private CapsuleCollider m_capsule;
    private PlayerMonitor m_monitor;
    private PlayerMotor m_motor;
    private float m_lastStepHeight;
    private Leg m_left, m_right;

    /// <summary>モデルの脚チェーンを起動時に解決します。</summary>
    private void Awake()
    {
        m_capsule = GetComponent<CapsuleCollider>();
        m_monitor = GetComponent<PlayerMonitor>();
        m_motor = GetComponent<PlayerMotor>();
        m_lastStepHeight = m_motor ? m_motor.TotalStepHeight : 0f;
        // 実際に描画されるメッシュにバインドされた骨から脚を特定する。
        foreach (var mesh in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            foreach (Transform bone in mesh.bones)
            {
                if (!bone) continue;
                if (bone.name == "Ankle_L") m_left = new Leg(bone, "L");
                if (bone.name == "Ankle_R") m_right = new Leg(bone, "R");
            }
            if (m_capsule && m_monitor && m_left?.IsValid == true && m_right?.IsValid == true)
            {
                m_pelvis = m_left.Hip.parent;
                if (m_pelvis && m_pelvis != transform && m_right.Hip.IsChildOf(m_pelvis)) return;
            }
        }
        Debug.LogWarning("PlayerFootIK: PlayerMonitor または脚ボーンが見つかりません。", this);
        enabled = false;
    }

    /// <summary>初期姿勢の刃の頂点から左右の接地点を取得します。</summary>
    private void Start()
    {
        foreach (var registry in GetComponentsInChildren<MeshReferenceRegistry>(true))
        {
            if (registry.TryGetRenderer("Chainsaw_Left", out var left) && left is SkinnedMeshRenderer leftMesh)
                m_left.CacheBlade(leftMesh, transform.forward);
            if (registry.TryGetRenderer("Chainsaw_Right", out var right) && right is SkinnedMeshRenderer rightMesh)
                m_right.CacheBlade(rightMesh, transform.forward);
        }
        if (m_left.BladePoints == null || m_right.BladePoints == null)
            Debug.LogWarning("PlayerFootIK: Blade の参照がない足は靴底で接地します。", this);
    }

    /// <summary>前回の補正をアニメーション評価前に解除します。</summary>
    private void Update()
    {
        RestorePelvis();
        m_left.Restore();
        m_right.Restore();
    }

    /// <summary>脚ごとに地形を検出して姿勢を更新します。</summary>
    private void LateUpdate()
    {
        float left = SampleFoot(m_left, out Vector3 leftNormal);
        float right = SampleFoot(m_right, out Vector3 rightNormal);
        float blend = 1f - Mathf.Exp(-m_blendSpeed * Time.deltaTime);
        float stepHeight = m_motor ? m_motor.TotalStepHeight : m_lastStepHeight;
        float stepDelta = stepHeight - m_lastStepHeight;
        m_lastStepHeight = stepHeight;
        // 本体が段差移動したフレームは古い足目標を残さず再計算する。
        float positionBlend = stepDelta > 0f ? 1f : blend;
        left = m_left.FilterOffset(left, positionBlend);
        right = m_right.FilterOffset(right, positionBlend);
        // 同じ補間済み目標から腰と両脚を解く。腰と脚で二重に遅延させない。
        m_pelvisOffset = Mathf.Min(0f, Mathf.Min(left, right));
        m_pelvisPose = m_pelvis.localPosition;
        m_pelvis.position += Vector3.up * m_pelvisOffset;
        m_pelvisApplied = true;
        m_left.Solve(left - m_pelvisOffset, leftNormal, blend, transform.forward);
        m_right.Solve(right - m_pelvisOffset, rightNormal, blend, transform.forward);
        // 両脚と腰に同じ重みを適用し、片脚だけ解除して腰が沈むことを防ぐ。
        float safeWeight = Mathf.Min(
            m_left.GetSafeWeight(m_maxKneeBend, m_maxAnkleCorrection, m_maxHipCorrection, m_angleFadeRange),
            m_right.GetSafeWeight(m_maxKneeBend, m_maxAnkleCorrection, m_maxHipCorrection, m_angleFadeRange));
        m_pelvis.localPosition = Vector3.Lerp(m_pelvisPose, m_pelvis.localPosition, safeWeight);
        m_left.ApplyWeight(safeWeight);
        m_right.ApplyWeight(safeWeight);
    }

    /// <summary>停止時に元の姿勢を戻します。</summary>
    private void OnDisable()
    {
        RestorePelvis();
        m_left?.Restore();
        m_right?.Restore();
        m_pelvisOffset = 0f;
    }

    /// <summary>無効期間の段差移動を再有効化時に持ち越さないようにします。</summary>
    private void OnEnable()
    {
        if (m_motor) m_lastStepHeight = m_motor.TotalStepHeight;
    }

    /// <summary>足の持ち上げ量を残しながら地面の高さと傾きを反映します。</summary>
    private float SampleFoot(Leg leg, out Vector3 normal)
    {
        normal = Vector3.up;
        if (!m_monitor.IsGrounded && !m_monitor.HasGroundContact) return 0f;
        Vector3 ankle = leg.Ankle.position;
        Vector3 toe = leg.Toes ? leg.Toes.position : ankle + transform.forward * m_heelLength;
        Vector3 direction = Vector3.ProjectOnPlane(toe - ankle, Vector3.up).normalized;
        Vector3 heel = ankle - direction * m_heelLength;
        Vector3 side = Vector3.Cross(Vector3.up, direction) * m_soleHalfWidth;
        float soleY = ankle.y - m_footSoleOffset;
        float requested = float.NegativeInfinity;
        for (int i = 0; i < 5; i++)
        {
            Vector3 point = i == 4 ? (heel + toe) * .5f : (i < 2 ? heel : toe) + ((i & 1) == 0 ? side : -side);
            point.y = soleY;
            if (leg.BladePoints != null)
                point = leg.Ankle.TransformPoint(leg.BladePoints[i]);
            if (TryProbe(point, out RaycastHit hit) && hit.point.y - point.y > requested)
            {
                requested = hit.point.y - point.y;
                normal = hit.normal;
            }
        }
        if (float.IsNegativeInfinity(requested)) return 0f;
        float offset = Mathf.Clamp(requested, -m_maxCorrection, m_maxCorrection);
        float swing = Mathf.InverseLerp(m_contactHeight, m_contactHeight + m_maxCorrection,
            soleY - m_capsule.bounds.min.y);
        return offset < 0f ? offset * (1f - swing) : offset;
    }

    /// <summary>前フレームで下げた腰の位置をアニメーション評価前に戻します。</summary>
    private void RestorePelvis()
    {
        if (!m_pelvisApplied) return;
        m_pelvis.localPosition = m_pelvisPose;
        m_pelvisApplied = false;
    }

    /// <summary>足裏のサンプル位置から Ground の上面を検出します。</summary>
    private bool TryProbe(Vector3 point, out RaycastHit hit)
    {
        return Physics.Raycast(point + Vector3.up * m_rayStartHeight, Vector3.down, out hit,
            m_rayStartHeight + m_rayDistance, m_groundLayerMask, QueryTriggerInteraction.Ignore);
    }

    /// <summary>片脚の骨参照と補正前の姿勢を保持します。</summary>
    private sealed class Leg
    {
        private readonly Transform m_hip, m_knee;
        public Vector3[] BladePoints { get; private set; }

        /// <summary>刃を前後に五分割し、各区間の最下点を足首座標に保存します。</summary>
        public void CacheBlade(SkinnedMeshRenderer renderer, Vector3 forward)
        {
            const int COUNT = 5;
            if (!renderer.sharedMesh || renderer.sharedMesh.vertexCount == 0) return;
            var mesh = new Mesh();
            renderer.BakeMesh(mesh, false);
            var vertices = mesh.vertices;
            Object.Destroy(mesh);
            float min = float.PositiveInfinity, max = float.NegativeInfinity;
            int lowest = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = renderer.transform.TransformPoint(vertices[i]);
                float distance = Vector3.Dot(vertices[i] - Ankle.position, forward);
                min = Mathf.Min(min, distance);
                max = Mathf.Max(max, distance);
                if (vertices[i].y < vertices[lowest].y) lowest = i;
            }
            var indices = new int[COUNT] { -1, -1, -1, -1, -1 };
            for (int i = 0; i < vertices.Length; i++)
            {
                float distance = Vector3.Dot(vertices[i] - Ankle.position, forward);
                int bin = Mathf.Min(COUNT - 1, (int)(COUNT * Mathf.InverseLerp(min, max, distance)));
                if (indices[bin] < 0 || vertices[i].y < vertices[indices[bin]].y) indices[bin] = i;
            }
            BladePoints = new Vector3[COUNT];
            for (int i = 0; i < COUNT; i++)
                BladePoints[i] = Ankle.InverseTransformPoint(vertices[indices[i] < 0 ? lowest : indices[i]]);
        }

        public Transform Hip => m_hip;
        public Transform Ankle { get; }
        public Transform Toes { get; }
        public bool IsValid => m_hip && m_knee && Ankle;
        private Quaternion m_hipPose, m_kneePose, m_anklePose;
        private bool m_applied;
        private float m_offset;
        private Vector3 m_normal = Vector3.up;

        public Leg(Transform ankle, string side)
        {
            Ankle = ankle;
            Toes = ankle.Find($"Toes_{side}");
            for (Transform parent = ankle.parent; parent; parent = parent.parent)
            {
                if (parent.name == $"Knee_{side}") m_knee = parent;
                if (parent.name == $"Hip_{side}") { m_hip = parent; break; }
            }
        }

        /// <summary>腰と脚で共通に使う足の高さ補正を一度だけ補間します。</summary>
        public float FilterOffset(float offset, float blend)
        {
            m_offset = Mathf.Lerp(m_offset, offset, blend);
            return m_offset;
        }

        /// <summary>脚の長さと現在の膝の曲がる方向から目標姿勢を求めます。</summary>
        public void Solve(float offset, Vector3 normal, float blend, Vector3 forward)
        {
            m_normal = Vector3.Slerp(m_normal, normal, blend);
            Vector3 target = Ankle.position + Vector3.up * offset;
            Vector3 upper = m_knee.position - m_hip.position;
            Vector3 lower = Ankle.position - m_knee.position;
            Vector3 axis = (target - m_hip.position).normalized;
            const float EPSILON = .0001f;
            if (axis == Vector3.zero || Mathf.Min(upper.magnitude, lower.magnitude) <= EPSILON) return;
            float reach = Mathf.Clamp(Vector3.Distance(target, m_hip.position),
                Mathf.Abs(upper.magnitude - lower.magnitude) + EPSILON, upper.magnitude + lower.magnitude - EPSILON);
            float along = (upper.sqrMagnitude - lower.sqrMagnitude + reach * reach) / (2f * reach);
            Vector3 bend = Vector3.ProjectOnPlane(upper, axis);
            if (bend.sqrMagnitude < EPSILON * EPSILON) bend = Vector3.ProjectOnPlane(forward, axis);
            Vector3 thigh = axis * along + bend.normalized * Mathf.Sqrt(Mathf.Max(0f, upper.sqrMagnitude - along * along));
            m_hipPose = m_hip.localRotation;
            m_kneePose = m_knee.localRotation;
            m_anklePose = Ankle.localRotation;
            Quaternion footRotation = Ankle.rotation;
            m_hip.rotation = Quaternion.FromToRotation(upper, thigh) * m_hip.rotation;
            m_knee.rotation = Quaternion.FromToRotation(Ankle.position - m_knee.position,
                m_hip.position + axis * reach - m_knee.position) * m_knee.rotation;
            Ankle.rotation = Quaternion.FromToRotation(Vector3.up, m_normal) * footRotation;
            m_applied = true;
        }

        /// <summary>前回書き込む直前のローカル回転を復元します。</summary>
        public void Restore()
        {
            if (!m_applied) return;
            m_hip.localRotation = m_hipPose;
            m_knee.localRotation = m_kneePose;
            Ankle.localRotation = m_anklePose;
            m_applied = false;
        }

        /// <summary>膝の曲げ角と股関節・足首の追加回転から安全な補正の重みを求めます。</summary>
        public float GetSafeWeight(float kneeLimit, float ankleLimit, float hipLimit, float fadeRange)
        {
            if (!m_applied) return 0f;
            float kneeBend = Vector3.Angle(m_knee.position - m_hip.position, Ankle.position - m_knee.position);
            float ankleChange = Quaternion.Angle(m_anklePose, Ankle.localRotation);
            float hipChange = Quaternion.Angle(m_hipPose, m_hip.localRotation);
            float kneeWeight = GetAngleWeight(kneeBend, kneeLimit, fadeRange);
            float ankleWeight = GetAngleWeight(ankleChange, ankleLimit, fadeRange);
            return Mathf.Min(kneeWeight, Mathf.Min(ankleWeight, GetAngleWeight(hipChange, hipLimit, fadeRange)));
        }

        /// <summary>上限付近では滑らかに補正を弱め、上限以上ではゼロを返します。</summary>
        private static float GetAngleWeight(float angle, float limit, float fadeRange)
        {
            float start = Mathf.Max(0f, limit - fadeRange);
            return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, limit, angle));
        }

        /// <summary>計算した姿勢をアニメーション姿勢へ重み付けして戻します。</summary>
        public void ApplyWeight(float weight)
        {
            if (!m_applied) return;
            m_hip.localRotation = Quaternion.Slerp(m_hipPose, m_hip.localRotation, weight);
            m_knee.localRotation = Quaternion.Slerp(m_kneePose, m_knee.localRotation, weight);
            Ankle.localRotation = Quaternion.Slerp(m_anklePose, Ankle.localRotation, weight);
        }
    }
}
