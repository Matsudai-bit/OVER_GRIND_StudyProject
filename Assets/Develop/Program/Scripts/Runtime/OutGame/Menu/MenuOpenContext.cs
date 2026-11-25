using UnityEngine;

/// <summary>
/// メニューシーンを開く際の共有情報を保持します。
/// </summary>
[CreateAssetMenu(
    fileName = "MenuOpenContext",
    menuName = "Game/Menu/Menu Open Context")]
public sealed class MenuOpenContext : ScriptableObject
{
    // メニューシーン開始時に開く画面
    [SerializeField]
    private MenuOpenTargetType m_openTarget =
        MenuOpenTargetType.NONE;

    /// <summary>
    /// 現在指定されている開始画面を取得します。
    /// </summary>
    public MenuOpenTargetType OpenTarget =>
        m_openTarget;

    /// <summary>
    /// メニューシーン開始時に開く画面を設定します。
    /// </summary>
    /// <param name="openTarget">開く画面。</param>
    public void SetOpenTarget(
        MenuOpenTargetType openTarget)
    {
        m_openTarget = openTarget;
    }

    /// <summary>
    /// 開始画面を取得して、指定状態をクリアします。
    /// </summary>
    /// <returns>
    /// 指定されていた開始画面。
    /// </returns>
    public MenuOpenTargetType ConsumeOpenTarget()
    {
        MenuOpenTargetType openTarget =
            m_openTarget;

        m_openTarget =
            MenuOpenTargetType.NONE;

        return openTarget;
    }

    /// <summary>
    /// 開始画面の指定をクリアします。
    /// </summary>
    public void Clear()
    {
        m_openTarget =
            MenuOpenTargetType.NONE;
    }
}