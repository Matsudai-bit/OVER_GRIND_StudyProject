using UnityEngine;

/// <summary>
/// 速度UIで扱う速度を保持します。
/// </summary>
public class SpeedPlaceModel
{
    private const float MAX_SPEED = 99.9f;

    private float m_currentSpeed;

    /// <summary>
    /// 現在の速度を取得します。
    /// </summary>
    /// <returns>0から99.9までの速度。</returns>
    public float GetSpeed()
    {
        return m_currentSpeed;
    }

    /// <summary>
    /// 速度を従来の表示範囲に制限して設定します。
    /// </summary>
    /// <param name="speed">設定する速度。</param>
    public void SetSpeed(float speed)
    {
        m_currentSpeed = Mathf.Clamp(speed, 0f, MAX_SPEED);
    }
}
