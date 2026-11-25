using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// チュートリアルUIに固定コマンドの入力画像を表示します。
/// UIが表示された時点の入力デバイスに応じて画像を切り替えます。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class TutorialCommandSpritePresenter : MonoBehaviour
{
    [SerializeField, Header("入力画像Manager")]
    private InputCommandSpriteManager m_spriteManager;

    [SerializeField, Header("入力デバイス切替")]
    private InputDeviceTypeSwitcher m_deviceTypeSwitcher;

    [SerializeField, Header("固定コマンドDataアセット名")]
    private string m_commandAssetName;

    private Image m_targetImage;

    /// <summary>
    /// 表示先Imageを取得します。
    /// </summary>
    private void Awake()
    {
        m_targetImage = GetComponent<Image>();
    }

    /// <summary>
    /// 入力デバイス変更を購読し、初期画像を表示します。
    /// </summary>
    private void OnEnable()
    {
        if (m_deviceTypeSwitcher != null)
        {
            m_deviceTypeSwitcher.InputDeviceTypeChanged += OnInputDeviceTypeChanged;
        }

        RefreshSprite();
    }

    /// <summary>
    /// 入力デバイス変更の購読を解除します。
    /// </summary>
    private void OnDisable()
    {
        if (m_deviceTypeSwitcher != null)
        {
            m_deviceTypeSwitcher.InputDeviceTypeChanged -= OnInputDeviceTypeChanged;
        }
    }

    /// <summary>
    /// 入力デバイス変更時に表示画像を更新します。
    /// </summary>
    /// <param name="inputDeviceType">変更後の入力デバイス。</param>
    private void OnInputDeviceTypeChanged(InputDeviceType inputDeviceType)
    {
        RefreshSprite();
    }

    /// <summary>
    /// 固定コマンドと現在の入力デバイスに対応する画像を表示します。
    /// </summary>
    private void RefreshSprite()
    {
        if (m_targetImage == null)
        {
            return;
        }

        if (m_spriteManager == null || string.IsNullOrWhiteSpace(m_commandAssetName))
        {
            m_targetImage.enabled = false;
            return;
        }

        Sprite sprite = m_spriteManager.GetSprite(m_commandAssetName);
        m_targetImage.sprite = sprite;
        m_targetImage.preserveAspect = true;
        m_targetImage.enabled = sprite != null;
    }
}
