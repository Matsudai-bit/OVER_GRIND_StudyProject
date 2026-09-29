using UnityEngine;

public class S1P3EnergyCannonStateReferences : MonoBehaviour
{

    // エネルギー砲の回転基準
    [SerializeField, Header("参照")]
    private Transform m_cannonPivot;

    // エネルギー砲の発射地点
    [SerializeField]
    private Transform m_firePoint;

    /// <summary>
    /// エネルギー砲の回転基準を取得します。
    /// </summary>
    public Transform CannonPivot =>
        m_cannonPivot;

    /// <summary>
    /// エネルギー砲の発射地点を取得します。
    /// </summary>
    public Transform FirePoint =>
        m_firePoint;
}
