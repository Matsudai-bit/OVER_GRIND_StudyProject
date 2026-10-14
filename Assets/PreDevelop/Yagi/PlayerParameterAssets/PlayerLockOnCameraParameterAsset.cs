using UnityEngine;

/// <summary>PlayerLockOnCameraの構図設定を保持します。</summary>
[CreateAssetMenu(fileName = "PlayerLockOnCameraParameter", menuName = "Game/Parameters/Player/Lock On Camera")]
public sealed class PlayerLockOnCameraParameterAsset : ScriptableObject
{
    [SerializeField, Range(0.05f, 0.3f)] private float m_screenMargin = 0.12f;
    [SerializeField, Range(0.0f, 20.0f)] private float m_pitch = 8.0f;
    [SerializeField, Min(0.0f)] private float m_bodyPadding = 0.2f;
    [SerializeField, Min(0.1f)] private float m_minimumDistance = 5.0f;

    public float ScreenMargin => Mathf.Clamp(m_screenMargin, 0.05f, 0.3f);
    public float Pitch => Mathf.Clamp(m_pitch, 0.0f, 20.0f);
    public float BodyPadding => Mathf.Max(0.0f, m_bodyPadding);
    public float MinimumDistance => Mathf.Max(0.1f, m_minimumDistance);
}
