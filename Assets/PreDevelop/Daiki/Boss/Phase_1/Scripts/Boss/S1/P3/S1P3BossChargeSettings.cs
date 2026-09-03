using UnityEngine;

/// <summary>
/// ステージ1フェーズ3の連続突進設定を保持します。
/// </summary>
[DisallowMultipleComponent]
public sealed class S1P3BossChargeSettings : MonoBehaviour
{
    // 1回目の突進設定
    [SerializeField, Header("1回目")]
    private StraightChargeSettings m_firstChargeSettings = new();

    // 2回目の突進設定
    [SerializeField, Header("2回目")]
    private StraightChargeSettings m_secondChargeSettings = new();

    // 3回目の突進設定
    [SerializeField, Header("3回目")]
    private StraightChargeSettings m_thirdChargeSettings = new();

    /// <summary>
    /// 指定回数の突進設定を取得します。
    /// </summary>
    /// <param name="chargeIndex">0から始まる突進回数。</param>
    /// <param name="settings">取得した設定。</param>
    /// <returns>
    /// true：取得できました。
    /// false：指定回数の設定がありません。
    /// </returns>
    public bool TryGetChargeSettings(
        int chargeIndex,
        out StraightChargeSettings settings)
    {
        switch (chargeIndex)
        {
            case 0:
                settings = m_firstChargeSettings;
                return settings != null;

            case 1:
                settings = m_secondChargeSettings;
                return settings != null;

            case 2:
                settings = m_thirdChargeSettings;
                return settings != null;

            default:
                settings = null;
                return false;
        }
    }
}
