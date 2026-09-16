using UnityEngine;

[CreateAssetMenu(
    fileName = "TutorialData",
    menuName = "Tutorial/Tutorial Data"
)]
public class TutorialData : ScriptableObject
{
    [Header("チュートリアルスライド")]
    [Tooltip("登録順がそのままチュートリアルのページ順になります")]
    [SerializeField]
    private TutorialSlideData[] slides;

    /// <summary>
    /// チュートリアルスライド一覧を取得します。
    /// </summary>
    public TutorialSlideData[] Slides => slides;
}