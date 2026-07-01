using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(SplineContainer))]
public class SplineRailInfo : MonoBehaviour
{
    public SplineContainer Container { get; private set; }

    // ソニック風にするための拡張プロパティ例
    [Header("Rail Settings")]
    [SerializeField] private float railSpeedMultiplier = 1.0f; // レール固有の速度倍率
    [SerializeField] private bool isBoostRail = false;         // 加速パネル付きレールか

    public float SpeedMultiplier => railSpeedMultiplier;
    public bool IsBoostRail => isBoostRail;

    private void Awake()
    {
        Container = GetComponent<SplineContainer>();
    }
}