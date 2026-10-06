using UnityEngine;

/// <summary>
/// UIで扱う速度と、攻撃中などの表示速度の上書きを保持します。
/// 物理的な移動速度はPlayerMotorが管理します。
/// </summary>
[DisallowMultipleComponent]
public class SpeedPlaceModel : MonoBehaviour
{
    private const float MAX_SPEED = 99.9f;

    [SerializeField, Range(0.0f, MAX_SPEED)]
    private float m_currentSpeed;

    private float? m_speedDisplayOverride;

    /// <summary>現在の速度を0から99.9の範囲で取得・設定します。</summary>
    public float Speed
    {
        get => m_currentSpeed;
        set => m_currentSpeed = Mathf.Clamp(value, 0.0f, MAX_SPEED);
    }

    /// <summary>現在の速度を取得します。</summary>
    /// <returns>0から99.9までの速度。</returns>
    public float GetSpeed() => Speed;

    /// <summary>現在の速度を設定します。</summary>
    /// <param name="speed">設定する速度。</param>
    public void SetSpeed(float speed) => Speed = speed;

    /// <summary>現在の水平速度と表示上書きから、このフレームの速度を保存します。</summary>
    /// <param name="horizontalSpeed">PlayerMotorから取得した水平速度。</param>
    public void UpdateSpeed(float horizontalSpeed)
    {
        Speed = m_speedDisplayOverride ?? horizontalSpeed;
    }

    /// <summary>物理速度の代わりに表示する速度を設定します。</summary>
    /// <param name="displaySpeed">攻撃中などに表示する速度。</param>
    public void SetSpeedDisplayOverride(float displaySpeed)
    {
        m_speedDisplayOverride = Mathf.Max(0.0f, displaySpeed);
    }

    /// <summary>表示速度の上書きを解除します。</summary>
    public void ClearSpeedDisplayOverride()
    {
        m_speedDisplayOverride = null;
    }

    /// <summary>速度と表示上書きを初期化します。</summary>
    public void ResetSpeed()
    {
        Speed = 0.0f;
        ClearSpeedDisplayOverride();
    }

    /// <summary>Inspectorから入力された速度を有効範囲へ制限します。</summary>
    private void OnValidate()
    {
        Speed = m_currentSpeed;
    }
}