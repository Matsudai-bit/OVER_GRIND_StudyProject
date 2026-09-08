using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// プレイヤーのカメラ制御を行います。
/// 通常時はCinemachineOrbitalFollowが
/// プレイヤー操作（マウス・スティック）による
/// フリールック回転を行いますが、
/// ブーストチャージ中などは外部（Stateなど）から
/// 「進行方向オーバーライド」を有効にすることで、
/// OrbitalFollowの水平角度を、
/// 「オーバーライド開始時の角度」と
/// 「プレイヤーの向き」の間を一定割合で見るように
/// 向かせることができます。
/// また、ブーストダッシュ開始時などに、
/// 一度だけ進行方向へ視点をリセットすることもできます。
/// </summary>
public class PlayerCamera : MonoBehaviour
{
    [SerializeField, Min(0.1f)]
    private float sensitivity = 1.0f;

    // カメラの軌道・向きを制御しているCinemachineコンポーネント
    [SerializeField]
    private CinemachineCamera m_cinemachineCamera;

    // 通常のフリールック入力を担うコンポーネント
    // 進行方向オーバーライド中はこれを無効化し、
    // 通常操作との回転の競合を防ぎます
    [SerializeField]
    private CinemachineInputAxisController m_inputAxisController;

    // CinemachineCameraから取得したOrbitalFollowへの参照
    // （水平角度を直接操作する対象）
    private CinemachineOrbitalFollow m_orbitalFollow;

    // 進行方向オーバーライドが有効か
    private bool m_isDriftOverrideActive;

    // オーバーライド開始時点でのカメラの水平角度（度数）
    // ここを基準点として、プレイヤーの向きとのブレンドに使用します
    private float m_overrideStartAngle;

    /// <summary>
    /// 進行方向オーバーライドが有効かどうかを取得します。
    /// </summary>
    public bool IsDriftOverrideActive =>
        m_isDriftOverrideActive;

    /// <summary>
    /// OrbitalFollowへの参照を解決します。
    /// </summary>
    private void Awake()
    {
        ResolveOrbitalFollow();
    }

    /// <summary>
    /// CinemachineCameraからCinemachineOrbitalFollowを取得します。
    /// </summary>
    private void ResolveOrbitalFollow()
    {
        if (m_cinemachineCamera == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerCamera)}] " +
                $"{nameof(CinemachineCamera)}が設定されていません。",
                this);

            return;
        }

        m_orbitalFollow =
            m_cinemachineCamera
                .GetComponent<CinemachineOrbitalFollow>();

        if (m_orbitalFollow == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerCamera)}] " +
                $"{nameof(CinemachineOrbitalFollow)}が見つかりません。",
                this);
        }
    }

    /// <summary>
    /// 進行方向を向かせるオーバーライドを開始します。
    /// 通常のフリールック入力を一時的に無効化し、
    /// 開始時点の角度をブレンドの基準点として記憶します。
    /// </summary>
    public void BeginDriftLookOverride()
    {
        m_isDriftOverrideActive = true;

        if (m_inputAxisController != null)
        {
            m_inputAxisController.enabled = false;
        }

        if (m_orbitalFollow != null)
        {
            m_overrideStartAngle =
                m_orbitalFollow.HorizontalAxis.Value;
        }
    }

    /// <summary>
    /// 進行方向オーバーライドを終了し、
    /// 通常のフリールック入力へ戻します。
    /// </summary>
    public void EndDriftLookOverride()
    {
        m_isDriftOverrideActive = false;

        if (m_inputAxisController != null)
        {
            m_inputAxisController.enabled = true;
        }
    }

    /// <summary>
    /// カメラの水平角度（OrbitalFollowのHorizontal Axis）を、
    /// 「オーバーライド開始時点の角度」と
    /// 「指定したワールド方向」の間をblendRateの割合で見た角度へ、
    /// 指定した速度で近づけます。
    /// BeginDriftLookOverride()呼び出し後、
    /// 毎フレーム（チャージ中など）呼び出してください。
    /// </summary>
    /// <param name="worldDirection">
    /// プレイヤーの向きなど、ブレンド先となるワールド方向
    /// （水平成分のみ使用します）。
    /// </param>
    /// <param name="blendRate">
    /// ブレンド割合（0～1）。
    /// 0で開始時の角度のまま、1で完全にworldDirectionを向く。
    /// 0.5でその中間を見る。
    /// </param>
    /// <param name="turnSpeedDegreesPerSecond">
    /// 1秒間の最大回転角度。
    /// </param>
    /// <param name="deltaTime">経過時間。</param>
    public void UpdateDriftLookDirection(
        Vector3 worldDirection,
        float blendRate,
        float turnSpeedDegreesPerSecond,
        float deltaTime)
    {
        if (!m_isDriftOverrideActive ||
            m_orbitalFollow == null)
        {
            return;
        }

        worldDirection.y = 0.0f;

        if (worldDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        worldDirection.Normalize();

        float directionAngle =
            Mathf.Atan2(
                worldDirection.x,
                worldDirection.z) *
            Mathf.Rad2Deg;

        float clampedBlendRate =
            Mathf.Clamp01(blendRate);

        float targetAngle =
            Mathf.MoveTowardsAngle(
                m_overrideStartAngle,
                directionAngle,
                Mathf.Abs(
                    Mathf.DeltaAngle(
                        m_overrideStartAngle,
                        directionAngle)) *
                    clampedBlendRate);

        float currentAngle =
            m_orbitalFollow.HorizontalAxis.Value;

        float nextAngle =
            Mathf.MoveTowardsAngle(
                currentAngle,
                targetAngle,
                Mathf.Max(turnSpeedDegreesPerSecond, 0.0f) *
                deltaTime);

        m_orbitalFollow.HorizontalAxis.Value =
            nextAngle;
    }

    /// <summary>
    /// カメラの水平角度を、指定したワールド方向へ
    /// 一度だけ即座にリセットします。
    /// 通常のフリールック操作（CinemachineInputAxisController）は
    /// 無効化せず、そのまま維持します。
    /// ブーストダッシュ開始時など、視点をリセットしたい
    /// 一瞬だけ呼び出してください。
    /// </summary>
    /// <param name="worldDirection">
    /// 向かせたいワールド方向（水平成分のみ使用します）。
    /// </param>
    public void SnapLookDirectionOnce(
        Vector3 worldDirection)
    {
        if (m_orbitalFollow == null)
        {
            return;
        }

        worldDirection.y = 0.0f;

        if (worldDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        worldDirection.Normalize();

        float targetAngle =
            Mathf.Atan2(
                worldDirection.x,
                worldDirection.z) *
            Mathf.Rad2Deg;

        m_orbitalFollow.HorizontalAxis.Value =
            targetAngle;
    }
}