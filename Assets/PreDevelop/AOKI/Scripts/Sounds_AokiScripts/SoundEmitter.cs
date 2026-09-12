using UnityEngine;

public class SoundEmitter_Aoki : MonoBehaviour
{
    [SerializeField] private SoundID_Aoki m_soundID;
    [SerializeField] private Transform m_spawnPoint;

    private int m_currentHandle = -1;

    public void PlaySound()
    {
        Vector3 pos = m_spawnPoint != null ? m_spawnPoint.position : transform.position;
        m_currentHandle = SoundManager_Aoki.Instance.Play(m_soundID, pos);
    }

    public void StopSound()
    {
        if (m_currentHandle != -1)
        {
            SoundManager_Aoki.Instance.Stop(m_currentHandle);
            m_currentHandle = -1;
        }
    }
}