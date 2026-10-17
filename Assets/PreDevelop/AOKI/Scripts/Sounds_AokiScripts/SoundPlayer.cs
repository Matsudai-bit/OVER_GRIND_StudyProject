using UnityEngine;
using UnityEngine.InputSystem; // 新しいInput Systemを使用

public class SoundPlayer : MonoBehaviour
{
    [Header("鳴らしたいサウンドID")]
    [SerializeField] private SoundID_Aoki m_testSoundID;

    private int m_currentHandle = -1;

    private void Update()
    {
        // キーボードが接続されていない場合は処理しない
        if (Keyboard.current == null) return;

        // スペースキーを押したら再生
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log($"[SoundTest] {m_testSoundID} を再生");
            m_currentHandle = SoundManager_Aoki.Instance.Play(m_testSoundID);
        }

        // Sキーを押したら停止
        if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            if (m_currentHandle != -1)
            {
                Debug.Log($"[SoundTest] ハンドルID: {m_currentHandle} を停止");
                SoundManager_Aoki.Instance.Stop(m_currentHandle);
                m_currentHandle = -1;
            }
        }
    }
}