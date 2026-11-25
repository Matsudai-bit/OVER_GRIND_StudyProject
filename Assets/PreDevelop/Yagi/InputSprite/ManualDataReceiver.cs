using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ManualDataAssetから選択したManual内容を子UIへ適用します。
/// 表示先はInspector参照を使用し、未設定の場合は規定名の子UIを取得します。
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

    [SerializeField, Header("画像の左側のText（任意）")]
    private TMP_Text m_leftText;

    [SerializeField, Header("本文Textの上書き先（任意）")]
    private TMP_Text m_bodyTextOverride;

    [SerializeField, Header("1行目のコマンド画像")]
    private Image m_commandImage;

    [SerializeField, Header("1行目の本文")]
    private TMP_Text m_bodyText;

    [SerializeField, Header("2行目のコマンド画像（任意）")]
    private Image m_secondCommandImage;

    [SerializeField, Header("2行目の本文（任意）")]
    private TMP_Text m_secondBodyText;

    [SerializeField, Header("配置基準の背景")]
    private RectTransform m_backgroundImage;

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

        ManualTextData data = m_manualData.GetManual(m_manualIndex);
        if (m_leftText != null)
        {
            m_leftText.richText = true;
            m_leftText.text = HighlightedTextFormatter.Format(
                data.LeftBody, data.HighlightColor);
        }

        if (m_bodyText != null)
        {
            m_bodyText.richText = true;
            m_bodyText.text = HighlightedTextFormatter.Format(
                body, data.HighlightColor);
        }

        ApplyCommandSprite(commandAssetName);
        ApplySecondRow(secondCommandAssetName, secondBody);
        ApplyAdditionalCommands();
        AlignContent();
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
        ApplyAdditionalCommands();
        AlignContent();
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
            m_secondBodyText.richText = true;
            m_secondBodyText.text = HighlightedTextFormatter.Format(
                body, m_manualData.GetManual(m_manualIndex).HighlightColor);
        }

        ApplyCommandSprite(m_secondCommandImage, commandAssetName);
    }

    /// <summary>
    /// 追加コマンドの子UIを更新します。
    /// </summary>
    private void ApplyAdditionalCommands()
    {
        ManualTextData data = m_manualData.GetManual(m_manualIndex);
        if (data == null) return;
        ApplyAdditionalCommand("AdditionalCommandImage", "PlusSign", data.AdditionalCommandAssetName);
        ApplyAdditionalCommand("AdditionalCommandImage_2", "PlusSign_2", data.SecondAdditionalCommandAssetName);
    }

    /// <summary>指定行の追加画像と＋を更新します。未設定なら両方を非表示にします。</summary>
    private void ApplyAdditionalCommand(string imageName, string plusName, string commandName)
    {
        Transform imageTransform = transform.Find(imageName);
        Transform plusTransform = transform.Find(plusName);
        Image image = imageTransform == null ? null : imageTransform.GetComponent<Image>();
        ApplyCommandSprite(image, commandName);
        bool visible = image != null && image.sprite != null;
        if (imageTransform != null) imageTransform.gameObject.SetActive(visible);
        if (plusTransform != null) plusTransform.gameObject.SetActive(visible);
    }

    /// <summary>背景の中心に文字とコマンド画像を配置します。</summary>
    private void AlignContent()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && !gameObject.activeInHierarchy)
        {
            return;
        }
#endif

        ManualContentLayout.Align(
            m_backgroundImage,
            m_leftText,
            m_commandImage,
            m_bodyText,
            m_secondCommandImage,
            m_secondBodyText);
    }

    /// <summary>未設定の表示先を規定名の子UIから取得します。</summary>
    private void CacheChildUi()
    {
        if (m_backgroundImage == null)
        {
            Transform backgroundTransform = transform.Find("BackgroundImage");
            m_backgroundImage = backgroundTransform as RectTransform;
        }

        Transform commandImageTransform = transform.Find(COMMAND_IMAGE_OBJECT_NAME);
        Transform bodyTextTransform = transform.Find(BODY_TEXT_OBJECT_NAME);

        if (m_commandImage == null)
        {
            m_commandImage = commandImageTransform == null
                ? null
                : commandImageTransform.GetComponent<Image>();
        }

        if (m_bodyTextOverride != null)
        {
            m_bodyText = m_bodyTextOverride;
        }
        else if (m_bodyText == null)
        {
            m_bodyText = bodyTextTransform == null
                ? null
                : bodyTextTransform.GetComponent<TMP_Text>();
        }

        Transform secondCommandImageTransform =
            transform.Find(SECOND_COMMAND_IMAGE_OBJECT_NAME);
        Transform secondBodyTextTransform =
            transform.Find(SECOND_BODY_TEXT_OBJECT_NAME);

        if (m_secondCommandImage == null)
        {
            m_secondCommandImage = secondCommandImageTransform == null
                ? null
                : secondCommandImageTransform.GetComponent<Image>();
        }

        if (m_secondBodyText == null)
        {
            m_secondBodyText = secondBodyTextTransform == null
                ? null
                : secondBodyTextTransform.GetComponent<TMP_Text>();
        }

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
