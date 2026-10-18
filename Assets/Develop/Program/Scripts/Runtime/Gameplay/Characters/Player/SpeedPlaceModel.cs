using System;
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

    // Presenterが購読するイベント（引数: 古い速度, 新しい速度）
    public event Action<float, float> OnSpeedChanged;

    /// <summary>現在の速度を0から99.9の範囲で取得・設定します。</summary>
    public float Speed
    {
        get => m_currentSpeed;
        set
        {
            float newValue = Mathf.Clamp(value, 0.0f, MAX_SPEED);

            // 値が実際に変化した時のみ通知を飛ばす
            if (!Mathf.Approximately(m_currentSpeed, newValue))
            {
                float oldValue = m_currentSpeed;
                m_currentSpeed = newValue;
                OnSpeedChanged?.Invoke(oldValue, m_currentSpeed);
            }
        }
    }

    /// <summary>現在の水平速度と表示上書きから、このフレームの速度を保存します。</summary>
    public void UpdateSpeed(float horizontalSpeed)
    {
        Speed = m_speedDisplayOverride ?? horizontalSpeed;
    }

    /// <summary>物理速度の代わりに表示する速度を設定します。</summary>
    public void SetSpeedDisplayOverride(float displaySpeed)
    {
        m_speedDisplayOverride = Mathf.Max(0.0f, displaySpeed);
        // 上書き設定された値でSpeedを更新（これによりEventが発火する）
        Speed = m_speedDisplayOverride.Value;
    }

    /// <summary>表示速度の上書きを解除します。</summary>
    public void ClearSpeedDisplayOverride()
    {
        m_speedDisplayOverride = null;
        // 上書きが解除されたので、必要であればこの後 UpdateSpeed(...) を呼んで正しい速度に戻す運用になります
    }

    /// <summary>速度と表示上書きを初期化します。</summary>
    public void ResetSpeed()
    {
        ClearSpeedDisplayOverride();
        Speed = 0.0f;
    }

    private void OnValidate()
    {
        // Inspectorで値を直接書き換えた場合もプロパティを経由させる（Playモード中ならイベントが飛ぶ）
        Speed = m_currentSpeed;
    }
}
