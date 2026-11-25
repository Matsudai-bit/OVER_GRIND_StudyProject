using UnityEngine;

/// <summary>
/// S1P3で確定遷移するStateの要求を管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class S1P3BossStateTransitionRequest : MonoBehaviour
{
    // スタン状態への遷移要求
    [SerializeField, Header("デバッグ表示")]
    private bool m_isStunnedRequested;

    /// <summary>
    /// スタン状態への遷移要求があるか取得します。
    /// </summary>
    public bool IsStunnedRequested =>
        m_isStunnedRequested;

    /// <summary>
    /// スタン状態への遷移を要求します。
    /// </summary>
    public void RequestStunned()
    {
        m_isStunnedRequested = true;
    }

    /// <summary>
    /// スタン状態への遷移要求を消費します。
    /// </summary>
    public void ConsumeStunnedRequest()
    {
        m_isStunnedRequested = false;
    }

    /// <summary>
    /// すべての遷移要求を解除します。
    /// </summary>
    public void ResetRequests()
    {
        m_isStunnedRequested = false;
    }
}
