using UnityEngine;

/// <summary>
/// 1つの入力コマンドに対応するキーボード用・コントローラー用の画像を保持します。
/// コマンド名はこのScriptableObjectのアセット名で判定します。
/// </summary>
[CreateAssetMenu(
    fileName = "InputCommandSpriteData",
    menuName = "Game/UI/Input Command Sprite Data")]
public sealed class InputCommandSpriteData : ScriptableObject
{
    [SerializeField, Header("キーボード用画像")]
    private Sprite m_keyboardSprite;

    [SerializeField, Header("コントローラー用画像")]
    private Sprite m_controllerSprite;

    /// <summary>
    /// 指定した入力デバイスに対応する画像を取得します。
    /// </summary>
    /// <param name="inputDeviceType">画像を取得する入力デバイス。</param>
    /// <returns>対応する画像。未設定の場合はnull。</returns>
    public Sprite GetSprite(InputDeviceType inputDeviceType)
    {
        return inputDeviceType switch
        {
            InputDeviceType.KEYBOARD => m_keyboardSprite,
            InputDeviceType.CONTROLLER => m_controllerSprite,
            _ => null
        };
    }
}

/// <summary>
/// 入力画像を切り替える入力デバイスの種類です。
/// </summary>
public enum InputDeviceType
{
    KEYBOARD,
    CONTROLLER
}
