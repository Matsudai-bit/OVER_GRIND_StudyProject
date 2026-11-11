using UnityEngine;

/// <summary>
/// 開くページの指定を、シーンをまたいで受け渡します。
/// </summary>
public static class UserSupportOpenRequest
{
    // 何も指定がない場合に開くページ
    public const MenuOpenTargetType DEFAULT_TARGET = MenuOpenTargetType.OPTIONS;

    // 次に開くページ
    public static MenuOpenTargetType Target = DEFAULT_TARGET;

    // 指定されたページを取り出し、既定の状態に戻す
    public static MenuOpenTargetType Consume()
    {
        MenuOpenTargetType target = Target;
        Target = DEFAULT_TARGET;
        return target;
    }
}

/// <summary>
/// ポーズメニューのボタンから、開くページを指定して UserSupportScene を開きます。
/// </summary>
public class UserSupportOpener : MonoBehaviour
{
    [SerializeField]
    private WindowNavigator m_navigator;

    [SerializeField]
    private string m_sceneName = "UserSupportScene";

    // マニュアルページを開く(ManualButton の On Click Action に登録)
    public void OpenManual()
    {
        Open(MenuOpenTargetType.MANUAL);
    }

    // オプションページを開く(OptionButton の On Click Action に登録)
    public void OpenOption()
    {
        Open(MenuOpenTargetType.OPTIONS);
    }

    private void Open(MenuOpenTargetType target)
    {
        if (m_navigator == null)
        {
            Debug.LogError($"{gameObject.name}: WindowNavigator が設定されていません。", this);
            return;
        }

        // 開くページを先に記録してから、シーンを開く
        UserSupportOpenRequest.Target = target;
        m_navigator.OpenWindow(m_sceneName);
    }
}