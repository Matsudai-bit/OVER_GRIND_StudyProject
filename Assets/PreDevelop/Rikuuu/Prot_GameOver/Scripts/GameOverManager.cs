using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
struct GameOverReason
{
    public string reason;
    public Sprite reasonTexture;
}

public class GameOverManager : MonoBehaviour
{
    [Header("監視対象")]
    [Header("自機の体力を取得")]
    [SerializeField]
    private Health m_playerHealth;

    [Header("時間を取得")]
    [SerializeField]
    private StageTimeLimitController m_stageTimeLimit;

    [Header("イメージコンポーネント")]
    [SerializeField]
    private UnityEngine.UI.Image m_reasonImage;

    [Header("ゲームオーバー原因")]
    [SerializeField]
    private List<GameOverReason> m_reasonList = new List<GameOverReason>();

    private Dictionary<string, Sprite> m_reasonDictionary = new Dictionary<string, Sprite>();
    private bool m_isGameOver = false;

    private void Awake()
    {
        m_reasonDictionary = new Dictionary<string, Sprite>();
        foreach (var r in m_reasonList)
        {
            if (!string.IsNullOrEmpty(r.reason) && !m_reasonDictionary.ContainsKey(r.reason))
            {
                m_reasonDictionary.Add(r.reason, r.reasonTexture);
            }
        }
    }

    private void OnEnable()
    {
        m_isGameOver = false;
        Time.timeScale = 0.0f;
        ApplyGameOverReason();
    }

    private void Start()
    {
        // 初回起動時、他のスクリプトのOnEnableが終わった後に確実に反映させる
        ApplyGameOverReason();
    }

    private void OnDisable()
    {
        Time.timeScale = 1.0f;
    }

    // 現在の状態から原因を判定して画像を適用する
    private void ApplyGameOverReason()
    {
        if (m_isGameOver) return;

        // 制限時間切れか確認
        if (m_stageTimeLimit != null && m_stageTimeLimit.IsExpired)
        {
            TimeUp();
            return;
        }

        // プレイヤーのHPが0か確認
        if (m_playerHealth != null && m_playerHealth.IsDead)
        {
            HitpointZero();
            return;
        }
    }

    public void HitpointZero()
    {
        m_isGameOver = true;
        if (m_reasonDictionary.TryGetValue("HPZERO", out Sprite sprite))
        {
            m_reasonImage.sprite = sprite;
        }
    }

    public void TimeUp()
    {
        m_isGameOver = true;
        if (m_reasonDictionary.TryGetValue("TIMEUP", out Sprite sprite))
        {
            m_reasonImage.sprite = sprite;
        }
    }
}