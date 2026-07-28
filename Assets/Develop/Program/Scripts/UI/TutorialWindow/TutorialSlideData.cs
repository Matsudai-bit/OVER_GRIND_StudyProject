using UnityEngine;

[CreateAssetMenu(fileName = "TutorialSlideData", menuName = "Tutorial/Slide Data")]
public class TutorialSlideData : ScriptableObject
{
    [Tooltip("表示するスライド画像。配列の順番がそのままスライド順になります（16:9推奨）")]
    public Sprite[] slideImages;
}