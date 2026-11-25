using UnityEngine;
using DG.Tweening;

public class CursorRotator : MonoBehaviour
{
    private const string ROTATE_ID = "CursorRotate";

    [Header("回転させるカーソル")]
    [SerializeField]
    private RectTransform m_cursor;

    [Header("回転の設定")]
    [SerializeField]
    private float m_angle = 90.0f;
    [SerializeField]
    private float m_duration = 0.2f;
    [SerializeField]
    private Ease m_ease = Ease.OutCubic;

    // 現在の表示角度（Z）
    private float m_currentAngle;
    // 目標角度（Z）
    private float m_targetAngle;

    private void Awake()
    {
        // 0〜360 ではなく -180〜180 に直して保持する
        m_currentAngle = Mathf.DeltaAngle(0.0f, m_cursor.localEulerAngles.z);
        m_targetAngle = m_currentAngle;
    }

    // true で時計回り(-90)、false で反時計回り(+90)
    public void Rotate(bool clockwise)
    {
        m_targetAngle += clockwise ? -m_angle : m_angle;
        PlayRotate();
    }

    // 指定した角度へ直接回す
    public void RotateTo(float angle)
    {
        m_targetAngle = angle;
        PlayRotate();
    }

    private void PlayRotate()
    {
        // 回転のTweenだけ止める（揺れは止めない）
        DOTween.Kill(ROTATE_ID);

        DOTween.To(() => m_currentAngle, value =>
        {
            m_currentAngle = value;
            m_cursor.localRotation = Quaternion.Euler(0.0f, 0.0f, value);
        }, m_targetAngle, m_duration)
            .SetEase(m_ease)
            .SetId(ROTATE_ID)
            .SetUpdate(true)
            .SetLink(gameObject);
    }
}
