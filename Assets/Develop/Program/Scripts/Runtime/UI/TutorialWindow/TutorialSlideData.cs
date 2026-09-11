using UnityEngine;

[CreateAssetMenu(fileName = "TutorialSlideData", menuName = "Tutorial/Slide Data")]
public class TutorialSlideData : ScriptableObject
{
    [Header("GIFファイル")]
    [Tooltip("チュートリアルで再生するGIFファイル。\n" +
             "元の .gif ファイルの拡張子を「.gif.bytes」にリネームしてから\n" +
             "プロジェクトに追加すると、Unityが中身をそのままTextAssetとして扱ってくれます。")]
    [SerializeField]
    private TextAsset gifFile;

    [Header("挿絵")]
    [Tooltip("GIFと一緒に左上へ表示する挿絵。未設定なら非表示になります")]
    [SerializeField]
    private Sprite overlayImage;

    /// <summary>
    /// GIFファイルの生バイト列を取得します。
    /// </summary>
    public byte[] GifBytes => gifFile != null ? gifFile.bytes : null;

    /// <summary>
    /// GIFと一緒に表示する挿絵を取得します。
    /// </summary>
    public Sprite OverlayImage => overlayImage;
}