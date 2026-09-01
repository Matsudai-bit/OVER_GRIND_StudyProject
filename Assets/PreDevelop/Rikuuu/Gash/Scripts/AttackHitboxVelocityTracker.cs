using UnityEngine;

/// <summary>
/// アタッチしたオブジェクトの移動速度を毎フレーム計測します。
/// 攻撃判定のON/OFFに影響されない、常にアクティブな親オブジェクトに付与してください。
/// </summary>
public class AttackHitboxVelocityTracker : MonoBehaviour
{
    // 直前フレームのワールド座標です。
    private Vector3 m_previousPosition;

    /// <summary>
    /// 現在の移動速度（ワールド空間、m/秒）を取得します。
    /// </summary>
    public Vector3 Velocity { get; private set; }

    /// <summary>
    /// 初回フレームで速度が異常値にならないよう、初期位置を記録します。
    /// </summary>
    private void Start()
    {
        // 現在位置を前回位置として保存します。
        m_previousPosition = transform.position;
    }

    /// <summary>
    /// 毎フレーム、前回位置との差分から移動速度を計算します。
    /// </summary>
    private void Update()
    {
        // deltaTimeが0以下だと除算エラーになるため、正の値のときのみ速度を更新します。
        if (Time.deltaTime > 0f)
        {
            // 位置の差分を経過時間で割って速度を求めます。
            Velocity = (transform.position - m_previousPosition) / Time.deltaTime;
        }

        // 次フレームの計算に使うため、現在位置を前回位置として保存します。
        m_previousPosition = transform.position;
    }
}