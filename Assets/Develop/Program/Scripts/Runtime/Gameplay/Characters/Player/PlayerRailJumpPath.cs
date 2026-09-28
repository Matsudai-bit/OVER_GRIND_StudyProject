using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

/// <summary>無入力ジャンプ中の、移動・湾曲するレールに沿った軌道を計算します。</summary>
public sealed class PlayerRailJumpPath
{
    private const float MIN_LENGTH = 0.001f;
    private readonly SplineRailInfo m_rail;
    private readonly float m_speed;
    private float m_positionT;
    private float m_heightOffset;
    private int m_direction = 1;

    /// <summary>追従先と引き継ぐ滑走速度を保持します。</summary>
    /// <param name="rail">ジャンプ元のレール。</param>
    /// <param name="speed">レールに沿う移動速度。</param>
    public PlayerRailJumpPath(SplineRailInfo rail, float speed)
    {
        m_rail = rail;
        m_speed = Mathf.Max(0.0f, speed);
    }

    /// <summary>現在位置から追従開始位置・高さ・進行方向を決定します。</summary>
    /// <param name="position">プレイヤーの物理位置。</param>
    /// <param name="forward">ジャンプ直前の進行方向。</param>
    /// <returns>true：追従可能。false：レールが無効。</returns>
    public bool TryInitialize(Vector3 position, Vector3 forward)
    {
        if (!IsRailValid()) return false;

        using (var spline = new NativeSpline(m_rail.Container.Splines[0],
            m_rail.Container.transform.localToWorldMatrix))
        {
            if (spline.GetLength() <= MIN_LENGTH) return false;
            // 搭乗位置はレールの真上です。点からの最近傍では、坂で開始位置が前方へずれます。
            SplineUtility.GetNearestPoint(spline, new Ray(position, Vector3.down), out _, out m_positionT);
            float3 nearestPoint = spline.EvaluatePosition(m_positionT);
            Vector3 tangent = spline.EvaluateTangent(m_positionT);
            m_direction = Vector3.Dot(tangent, forward) >= 0.0f ? 1 : -1;
            // 既存の搭乗高さを測定し、レール側の固定値には依存しません。
            m_heightOffset = position.y - nearestPoint.y;
        }
        return true;
    }

    /// <summary>最新のレール形状から次の物理位置と接線方向を求めます。</summary>
    /// <param name="deltaTime">物理更新時間。</param>
    /// <param name="jumpHeight">搭乗位置を基準にしたジャンプ高度。</param>
    /// <param name="position">次の目標位置。</param>
    /// <param name="forward">レールに沿う進行方向。</param>
    /// <returns>true：追従継続。false：終端到達、削除、無効化。</returns>
    public bool TryAdvance(float deltaTime, float jumpHeight, out Vector3 position, out Vector3 forward)
    {
        position = Vector3.zero;
        forward = Vector3.forward;
        if (!IsRailValid()) return false;

        // ワールド空間のスプラインを毎回評価し、ボスの移動・変形にも追従します。
        using (var spline = new NativeSpline(m_rail.Container.Splines[0],
            m_rail.Container.transform.localToWorldMatrix))
        {
            if (spline.GetLength() <= MIN_LENGTH) return false;
            Vector3 railPosition = spline.GetPointAtLinearDistance(
                m_positionT, m_speed * m_direction * deltaTime, out float nextT);
            if ((m_direction > 0 && nextT >= 1.0f) || (m_direction < 0 && nextT <= 0.0f))
                return false;

            m_positionT = nextT;
            position = railPosition + Vector3.up * (m_heightOffset + jumpHeight);
            forward = ((Vector3)spline.EvaluateTangent(m_positionT)).normalized * m_direction;
        }
        return true;
    }

    /// <summary>追従対象のレールとスプラインが利用可能か判定します。</summary>
    /// <returns>true：利用可能。false：利用不可。</returns>
    private bool IsRailValid()
    {
        return m_rail != null && m_rail.isActiveAndEnabled &&
            m_rail.Container != null && m_rail.Container.Splines.Count > 0 &&
            m_rail.Container.Splines[0].Count >= 2;
    }
}
