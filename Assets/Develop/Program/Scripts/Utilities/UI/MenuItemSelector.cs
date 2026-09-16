using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// メニューのボタン選択を管理するクラス
/// タイトル・リザルト・ポーズ画面など
/// 共通で利用することを想定している。
/// </summary>
public class MenuItemSelector : MonoBehaviour
{
    [Header("Default Select")]
    // 初期選択するボタン
    [SerializeField]
    private Button defaultButton;

    /// <summary>
    /// メニュー入力が有効か
    /// </summary>
    private bool enableInput = true;

    /// <summary>
    /// メニュー入力の有効/無効を切り替える
    /// </summary>
    public void EnableInput(bool enable)
    {
        enableInput = enable;

        if (!enable)
        {
            // 入力無効時は選択状態を解除
            EventSystem.current.SetSelectedGameObject(null);
        }
        else
        {
            // 入力再開時は初期ボタンを選択
            SelectDefault();
        }
    }

    /// <summary>
    /// 初期ボタンを選択する
    /// </summary>
    public void SelectDefault()
    {
        EventSystem.current.SetSelectedGameObject(defaultButton.gameObject);
    }

    /// <summary>
    /// 指定したボタンを選択する
    /// </summary>
    public void Select(Button button)
    {
        EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    /// <summary>
    /// 選択状態の監視
    /// </summary>
    private void Update()
    {
        // 入力無効中は監視しない
        if (!enableInput)
            return;

        // ボタン選択が解除された場合は初期ボタンへ戻す
        if (EventSystem.current.currentSelectedGameObject == null)
        {
            SelectDefault();
        }
    }
}