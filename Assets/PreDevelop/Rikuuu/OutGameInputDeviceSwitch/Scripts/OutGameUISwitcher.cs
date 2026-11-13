using UnityEngine;
using UnityEngine.UI;

public class OutGameUISwitcher : MonoBehaviour
{
    [SerializeField, Header("入力画像Manager")]
    private InputCommandSpriteManager m_spriteManager;

    [SerializeField, Header("入力デバイス切替")]
    private InputDeviceTypeSwitcher m_switcher;

    [SerializeField, Header("対応キー名")]
    private string m_keyName;

    private Image m_image;

    /// <summary>
    /// Imageを取得し、入力デバイス変更を購読して現在の画像を反映します。
    /// </summary>
    private void OnEnable()
    {
        if (m_image == null)
        {
            m_image = GetComponent<Image>();
        }

        if (m_switcher == null)
        {
            Debug.LogWarning(
                $"{nameof(InputDeviceTypeSwitcher)}が設定されていません。",
                this);
            return;
        }

        m_switcher.InputDeviceTypeChanged += OnInputDeviceTypeChanged;
        ApplySprite(m_switcher.CurrentInputDeviceType);
    }

    /// <summary>
    /// 入力デバイス変更の購読を解除します。
    /// </summary>
    private void OnDisable()
    {
        if (m_switcher != null)
        {
            m_switcher.InputDeviceTypeChanged -= OnInputDeviceTypeChanged;
        }
    }

    /// <summary>
    /// 入力デバイス変更時に画像を更新します。
    /// </summary>
    /// <param name="inputDeviceType">変更後の入力デバイス。</param>
    private void OnInputDeviceTypeChanged(InputDeviceType inputDeviceType)
    {
        ApplySprite(inputDeviceType);
    }

    /// <summary>
    /// 指定デバイス用のスプライトをImageへ設定します。
    /// </summary>
    /// <param name="inputDeviceType">表示対象の入力デバイス。</param>
    private void ApplySprite(InputDeviceType inputDeviceType)
    {
        if (m_spriteManager == null || m_image == null)
        {
            return;
        }

        // Managerの共有状態に依存せず、デバイスを明示して取得します。
        Sprite sprite = m_spriteManager.GetSprite(m_keyName, inputDeviceType);

        m_image.sprite = sprite;
        m_image.preserveAspect = true;
        m_image.enabled = sprite != null;

        if (sprite != null)
        {
            m_image.SetNativeSize();
        }
    }
}
