/// @ using :: 使用エンジン
using UnityEngine;

/// @ className :: インスペクターから手軽にサウンドを鳴らす発射台
/// @ name :: Aoki Hayate
/// @ date :: 2026/09/12
public class SoundEmitter_Aoki : MonoBehaviour
{
    [SerializeField] private SoundID_Aoki m_soundID; // 発生させるサウンド（メンバー変数規約適用）
    [SerializeField] private Transform m_spawnPoint; // 再生位置の基準点

    private int m_currentHandle = -1; // 再生中の個別ハンドル保持用

    // UnityEvent等から呼び出してサウンドを再生する
    public void PlaySound()
    {
        Vector3 pos = m_spawnPoint != null ? m_spawnPoint.position : transform.position;
        m_currentHandle = SoundManager_Aoki.Instance.Play(m_soundID, pos);
    }

    // 直近で再生したサウンドを個別に停止する
    public void StopSound()
    {
        if (m_currentHandle != -1)
        {
            SoundManager_Aoki.Instance.Stop(m_currentHandle);
            m_currentHandle = -1;
        }
    }
}