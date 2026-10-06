using UnityEngine;

/// <summary>
/// S1P3の突進選択条件で使用する行動選択回数を管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class S1P3BossActionSelectionTracker : MonoBehaviour
{
    // 前回の突進以降に選択された別行動の回数
    [SerializeField, Header("デバッグ表示"), Min(0)]
    private int m_otherActionCount;

    /// <summary>
    /// 前回の突進以降に選択された別行動の回数を取得します。
    /// </summary>
    public int OtherActionCount => m_otherActionCount;

    /// <summary>
    /// 突進が選択されたことを通知します。
    /// </summary>
    public void NotifyChargeSelected()
    {
        m_otherActionCount = 0;
    }

    /// <summary>
    /// 突進以外の行動が選択されたことを通知します。
    /// </summary>
    public void NotifyOtherActionSelected()
    {
        m_otherActionCount++;
    }

    /// <summary>
    /// 必要回数だけ別行動が選択されているか確認します。
    /// </summary>
    /// <param name="requiredCount">必要な選択回数。</param>
    /// <returns>
    /// true：必要回数に到達しています。
    /// false：必要回数に到達していません。
    /// </returns>
    public bool HasReachedRequiredCount(
        int requiredCount)
    {
        return m_otherActionCount >=
               Mathf.Max(0, requiredCount);
    }

    /// <summary>
    /// 行動選択回数をリセットします。
    /// </summary>
    public void ResetCount()
    {
        m_otherActionCount = 0;
    }
}
