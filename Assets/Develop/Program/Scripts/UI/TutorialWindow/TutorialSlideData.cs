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

    /// <summary>
    /// GIFファイルの生バイト列を取得します。
    /// </summary>
    public byte[] GifBytes => gifFile != null ? gifFile.bytes : null;
}