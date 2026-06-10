using UnityEngine;
using TMPro;
using UnityEditor.Build.Content;

public class ResultController : MonoBehaviour
{
    [SerializeField]
    TMP_Text scoreText;

    void Start()
    {
        scoreText.text = "Score: " + GetScore();
    }

    private int GetScore()
    {
        // スコアの計算ロジックをここに実装
        int score = 0;
        // スコアを管理しているスクリプトから取得する（今後追加）
        score = 100; // 仮のスコア値
        return score;
    }
}
