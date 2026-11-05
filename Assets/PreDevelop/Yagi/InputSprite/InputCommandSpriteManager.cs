using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 入力コマンド名に対応する入力画像Dataを管理します。
/// コマンド名には参照先ScriptableObjectのアセット名を使用します。
/// </summary>
[CreateAssetMenu(
    fileName = "InputCommandSpriteManager",
    menuName = "Game/UI/Input Command Sprite Manager")]
public sealed class InputCommandSpriteManager : ScriptableObject
{
    [SerializeField, Header("入力画像Data一覧")]
    private List<InputCommandSpriteData> m_commandDataList = new();

    private InputDeviceType m_currentInputDeviceType;

    /// <summary>
    /// 現在選択されている入力デバイスを取得します。
    /// </summary>
    public InputDeviceType CurrentInputDeviceType => m_currentInputDeviceType;

    /// <summary>
    /// Managerが使用する入力デバイスを切り替えます。
    /// </summary>
    /// <param name="inputDeviceType">切り替え先の入力デバイス。</param>
    public void SetInputDeviceType(InputDeviceType inputDeviceType)
    {
        m_currentInputDeviceType = inputDeviceType;
    }

    /// <summary>
    /// 現在選択されている入力デバイスに対応する画像を取得します。
    /// </summary>
    /// <param name="commandAssetName">検索するコマンドDataアセット名。</param>
    /// <returns>対応する画像。見つからない場合はnull。</returns>
    public Sprite GetSprite(string commandAssetName)
    {
        return GetSprite(commandAssetName, m_currentInputDeviceType);
    }

    /// <summary>
    /// 指定したアセット名と入力デバイスに対応する画像を取得します。
    /// </summary>
    /// <param name="commandAssetName">
    /// コマンドDataアセット名。例：Move、Jump、Attack。
    /// </param>
    /// <param name="inputDeviceType">画像を取得する入力デバイス。</param>
    /// <returns>対応する画像。見つからない場合はnull。</returns>
    public Sprite GetSprite(
        string commandAssetName,
        InputDeviceType inputDeviceType)
    {
        if (!TryGetData(commandAssetName, out InputCommandSpriteData commandData))
        {
            return null;
        }

        return commandData.GetSprite(inputDeviceType);
    }

    /// <summary>
    /// 指定したアセット名に対応する入力画像Dataを取得します。
    /// </summary>
    /// <param name="commandAssetName">検索するコマンドDataアセット名。</param>
    /// <param name="commandData">見つかった入力画像Data。</param>
    /// <returns>true：取得成功。false：引数不正またはData未登録。</returns>
    public bool TryGetData(
        string commandAssetName,
        out InputCommandSpriteData commandData)
    {
        commandData = null;

        if (string.IsNullOrWhiteSpace(commandAssetName))
        {
            return false;
        }

        foreach (InputCommandSpriteData data in m_commandDataList)
        {
            if (data == null || data.name != commandAssetName)
            {
                continue;
            }

            commandData = data;
            return true;
        }

        return false;
    }
}
