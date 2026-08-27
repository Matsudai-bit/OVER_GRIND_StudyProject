// Unityの基本機能を使用するために読み込みます。
using UnityEngine;

/// <summary>
/// アタッチしたオブジェクトの移動速度を毎フレーム計測し、
/// 振り方向の推定などに利用できるようにするコンポーネントです。
/// </summary>
public class AttackHitboxVelocityTracker : MonoBehaviour
{
    // 直前のフレームで記録したワールド座標を保持します。
    private Vector3 m_previousPosition;

    // 現在の移動速度を保持します。
    public Vector3 Velocity { get; private set; }

    /// <summary>
    /// オブジェクトが有効になったときに速度計測の基準位置を初期化します。
    /// </summary>
    private void OnEnable()
    {
        // 現在のワールド座標を基準位置として記録します。
        m_previousPosition = transform.position;

        // 有効化直後は移動速度がまだ計算されていないため、速度をゼロにします。
        Velocity = Vector3.zero;
    }

    /// <summary>
    /// 毎フレーム、オブジェクトの移動速度を計算します。
    /// </summary>
    private void Update()
    {
        // フレーム経過時間が0より大きい場合だけ速度を計算します。
        if (Time.deltaTime > 0.0f)
        {
            // 現在位置と前回位置の差をフレーム経過時間で割り、速度を計算します。
            Velocity = (transform.position - m_previousPosition) / Time.deltaTime;
        }

        // 次のフレームで使用するため、現在の位置を前回位置として保存します。
        m_previousPosition = transform.position;
    }
}