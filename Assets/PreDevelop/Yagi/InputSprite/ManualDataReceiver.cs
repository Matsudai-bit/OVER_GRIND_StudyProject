using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ManualDataAssetから選択したManual内容を子UIへ適用します。
/// 子UIにはCommandImageとBodyTextを配置してください。
/// </summary>
[DisallowMultipleComponent]
public sealed class ManualDataReceiver : MonoBehaviour
{
    private const string COMMAND_IMAGE_OBJECT_NAME = "CommandImage";
    private const string BODY_TEXT_OBJECT_NAME = "BodyText";
    private const string SECOND_COMMAND_IMAGE_OBJECT_NAME = "CommandImage_2";
    private const string SECOND_BODY_TEXT_OBJECT_NAME = "BodyText_2";

    [SerializeField, Header("Manualデータ")]
    private ManualDataAsset m_manualData;

    [SerializeField, Min(0), Header("Manual番号")]
    private int m_manualIndex;

    [SerializeField, Header("入力画像Manager")]
    private InputCommandSpriteManager m_spriteManager;

    [SerializeField, Header("入力デバイス切替")]
    private InputDeviceTypeSwitcher m_deviceTypeSwitcher;

    private Image m_commandImage;
    private TMP_Text m_bodyText;
    private Image m_secondCommandImage;
    private TMP_Text m_secondBodyText;

    /// <summary>
    /// 子UIを取得してManual内容を適用します。
    /// </summary>
    private void OnEnable()
    {
        CacheChildUi();

        if (m_deviceTypeSwitcher != null)
        {
            m_deviceTypeSwitcher.InputDeviceTypeChanged += OnInputDeviceTypeChanged;
        }

        ApplyManual();
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

#if UNITY_EDITOR
    /// <summary>
    /// Inspector変更時にManual内容をプレビューへ反映します。
    /// </summary>
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            CacheChildUi();
            ApplyManual();
        }
    }
#endif

    /// <summary>
    /// 選択したManualの本文とコマンド画像を子UIへ設定します。
    /// </summary>
    public void ApplyManual()
    {
        if (m_manualData == null)
        {
            Debug.LogWarning(
                $"{nameof(ManualDataAsset)}が設定されていません。",
                this);
            return;
        }

        if (!m_manualData.TryGetManual(
                m_manualIndex,
                out string commandAssetName,
                out string body,
                out string secondCommandAssetName,
                out string secondBody))
        {
            Debug.LogWarning(
                $"Manual番号が範囲外、またはData未設定です。 Index: {m_manualIndex}",
                this);
            return;
        }

        if (m_bodyText != null)
        {
            m_bodyText.text = body;
        }

        ApplyCommandSprite(commandAssetName);
        ApplySecondRow(secondCommandAssetName, secondBody);
    }

    /// <summary>
    /// 入力デバイス変更時にコマンド画像だけを更新します。
    /// </summary>
    /// <param name="inputDeviceType">変更後の入力デバイス。</param>
    private void OnInputDeviceTypeChanged(InputDeviceType inputDeviceType)
    {
        if (m_manualData == null || !m_manualData.TryGetManual(
                m_manualIndex,
                out string commandAssetName,
                out _,
                out string secondCommandAssetName,
                out _))
        {
            return;
        }

        ApplyCommandSprite(commandAssetName);
        ApplyCommandSprite(m_secondCommandImage, secondCommandAssetName);
    }

    /// <summary>
    /// 指定コマンドに対応する画像をCommandImageへ設定します。
    /// </summary>
    /// <param name="commandAssetName">表示するコマンドDataアセット名。</param>
    private void ApplyCommandSprite(string commandAssetName)
    {
        ApplyCommandSprite(m_commandImage, commandAssetName);
    }

    /// <summary>
    /// 指定したImageへコマンド画像を設定します。
    /// </summary>
    /// <param name="targetImage">画像を設定するImage。</param>
    /// <param name="commandAssetName">表示するコマンドDataアセット名。</param>
    private void ApplyCommandSprite(Image targetImage, string commandAssetName)
    {
        if (targetImage == null)
        {
            return;
        }

        Sprite sprite = m_spriteManager == null
            ? null
            : m_spriteManager.GetSprite(commandAssetName);

        targetImage.sprite = sprite;
        targetImage.preserveAspect = true;
        targetImage.enabled = sprite != null;
    }

    /// <summary>
    /// 2行目の本文とコマンド画像を設定します。
    /// </summary>
    /// <param name="commandAssetName">2行目のコマンドDataアセット名。</param>
    /// <param name="body">2行目の本文。</param>
    private void ApplySecondRow(string commandAssetName, string body)
    {
        if (m_secondBodyText != null)
        {
            m_secondBodyText.text = body;
        }

        ApplyCommandSprite(m_secondCommandImage, commandAssetName);
    }

    /// <summary>
    /// 規定名の子UIから表示先コンポーネントを取得します。
    /// </summary>
    private void CacheChildUi()
    {
        Transform commandImageTransform = transform.Find(COMMAND_IMAGE_OBJECT_NAME);
        Transform bodyTextTransform = transform.Find(BODY_TEXT_OBJECT_NAME);

        m_commandImage = commandImageTransform == null
            ? null
            : commandImageTransform.GetComponent<Image>();

        m_bodyText = bodyTextTransform == null
            ? null
            : bodyTextTransform.GetComponent<TMP_Text>();

        Transform secondCommandImageTransform =
            transform.Find(SECOND_COMMAND_IMAGE_OBJECT_NAME);
        Transform secondBodyTextTransform =
            transform.Find(SECOND_BODY_TEXT_OBJECT_NAME);

        m_secondCommandImage = secondCommandImageTransform == null
            ? null
            : secondCommandImageTransform.GetComponent<Image>();

        m_secondBodyText = secondBodyTextTransform == null
            ? null
            : secondBodyTextTransform.GetComponent<TMP_Text>();

        if (m_commandImage == null)
        {
            Debug.LogWarning(
                $"子オブジェクト'{COMMAND_IMAGE_OBJECT_NAME}'にImageがありません。",
                this);
        }

        if (m_bodyText == null)
        {
            Debug.LogWarning(
                $"子オブジェクト'{BODY_TEXT_OBJECT_NAME}'にTMP_Textがありません。",
                this);
        }
    }
}
