using UnityEngine;

/// <summary>
/// BGM再生クラス
/// </summary>
public class BgmPlayer : MonoBehaviour
{
    [Header("即座に再生するかどうか")]
    [SerializeField]
    private bool m_playingImmediately; // 即座に再生するかどうかのフラグ

    [Header("再生するBGMのID")]
    [SerializeField]
    public SoundID m_soundID;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (m_playingImmediately)
        {
            // 即座にBGM再生
            PlayBGM();
        }

    }

    /// <summary>
    /// BGM再生
    /// </summary>
    public void PlayBGM()
    {
        SoundManager.GetInstance.PlayBGM(m_soundID);
    }

    /// <summary>
    /// BGM停止
    /// </summary>
    void StopBGM()
    {
        SoundManager.GetInstance.StopBGM();
    }
}
