using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
struct GameOverReason
{
    // ゲームオーバー原因
    public string reason;
    // イメージ
    public Sprite reasonTexture;
}

public class GameOverManager : MonoBehaviour
{
    [Header("イメージコンポーネント")]
    // イメージコンポーネント
    [SerializeField]
    private UnityEngine.UI.Image m_reasonImage;

    [Header("ゲームオーバー原因")]
    // 登録用ペア
    [SerializeField]
    private List<GameOverReason> m_reasonList = new List<GameOverReason>();

    // ゲームオーバー原因とそのイメージ
    private Dictionary<string, Sprite> m_reasonDictionary = new Dictionary<string, Sprite>();

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

    private void Start()
    {
        TimeUp();
    }

    public void HitpointZero()
    {
        m_reasonImage.sprite = m_reasonDictionary["HPZERO"];
    }

    public void TimeUp()
    {
        m_reasonImage.sprite = m_reasonDictionary["TIMEUP"];
    }

}
