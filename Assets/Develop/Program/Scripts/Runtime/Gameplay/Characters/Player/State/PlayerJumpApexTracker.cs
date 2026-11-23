using UnityEngine;
/// <summary>ジャンプごとの実測最高点と下降開始を追跡します。</summary>
public sealed class PlayerJumpApexTracker
{
    private float m_previousHeight;
    /// <summary>今回のジャンプで観測した最高高度。</summary>
    public float HighestHeight { get; private set; }
    /// <summary>開始高度を保存します。</summary>
    public PlayerJumpApexTracker(float height) { HighestHeight = height; m_previousHeight = height; }
    /// <summary>最高点を更新し、下降開始を判定します。</summary>
    /// <returns>trueは下降中、falseは上昇中または頂点での停止。</returns>
    public bool UpdateApex(float height, float verticalVelocity)
    {
        HighestHeight = Mathf.Max(HighestHeight, height);
        bool descending = verticalVelocity < 0.0f && height <= m_previousHeight;
        m_previousHeight = height;
        return descending;
    }
}
