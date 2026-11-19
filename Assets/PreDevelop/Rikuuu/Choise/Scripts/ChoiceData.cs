using UnityEngine;

[CreateAssetMenu(fileName = "ChoiceData", menuName = "Game/UI/Choice Data")]
public class ChoiceData : ScriptableObject
{
    [Header("表示テキスト")]
    // タイトル（英語）
    [SerializeField] 
    private string m_titleEnglish = "EXIT GAME";
    // タイトル（日本語）
    [SerializeField] 
    private string m_titleJapanese = "ゲームを終了しますか？";
    // 詳細説明テキスト
    [SerializeField, TextArea(1, 3)] 
    private string m_detail = "保存されていない進行状況は失われます。";

    public string TitleEnglish => m_titleEnglish;
    public string TitleJapanese => m_titleJapanese;
    public string Detail => m_detail;
}