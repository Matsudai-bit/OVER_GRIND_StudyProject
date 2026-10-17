using UnityEngine;

/// <summary>
/// 速度に応じてUIを3D的に奥へ傾ける（Y軸・X軸・Z軸回転）演出スクリプトです。
/// </summary>
public class AirRideUiTilter : MonoBehaviour
{
    [Header("対象UI")]
    [SerializeField] private RectTransform m_targetRect;

    [Header("速度0のとき（ベタ貼り）")]
    [SerializeField] private Vector3 m_zeroSpeedRotation = Vector3.zero;

    [Header("最高速のときの3D角度 (X, Y, Z)")]
    [Tooltip("Y軸回転で左右の奥行き、X軸で上下の倒れ込み、Z軸で斜め傾きを作ります")]
    [SerializeField] private Vector3 m_maxSpeedRotation = new Vector3(10f, -25f, -12f);

    [Header("ハンドル操作（G）によるZ軸傾き")]
    [SerializeField] private float m_maxTurnAngleZ = 15f;

    [Header("追従の滑らかさ")]
    [SerializeField] private float m_smoothSpeed = 8f;

    private Vector3 m_currentRotation;
    private Vector3 m_targetRotation;

    private void Awake()
    {
        if (m_targetRect == null)
        {
            m_targetRect = GetComponent<RectTransform>();
        }
        m_currentRotation = m_zeroSpeedRotation;
    }

    private void Update()
    {
        if (m_targetRect == null) return;

        // 現在の回転から目標の回転へスムーズに遷移
        m_currentRotation = Vector3.Lerp(
            m_currentRotation,
            m_targetRotation,
            Time.deltaTime * m_smoothSpeed
        );

        // 3D回転（Quaternion）を適用
        m_targetRect.localRotation = Quaternion.Euler(m_currentRotation);
    }

    /// <summary>
    /// 速度と操作入力に応じて傾き目標を計算します
    /// </summary>
    /// <param name="speed">現在の速度 (km/h)</param>
    /// <param name="maxSpeed">最高速度 (km/h)</param>
    /// <param name="turnInput">左右入力 (-1.0 ～ +1.0)</param>
    public void SetSpeedAndTurn(float speed, float maxSpeed, float turnInput = 0f)
    {
        // 速度割合 (0.0 ～ 1.0)
        float speedRatio = Mathf.Clamp01(speed / maxSpeed);

        // 速度による3D回転（0の時は Vector3.zero）
        Vector3 speedRot = Vector3.Lerp(m_zeroSpeedRotation, m_maxSpeedRotation, speedRatio);

        // ハンドル（G）による左右の傾きを追加
        float turnZ = Mathf.Clamp(turnInput, -1f, 1f) * m_maxTurnAngleZ;
        speedRot.z += turnZ;

        m_targetRotation = speedRot;
    }
}