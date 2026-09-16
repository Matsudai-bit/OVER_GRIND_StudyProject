using UnityEngine;

/// <summary>
/// 任意のオブジェクトにアタッチして、インスペクターからエフェクトを呼び出す汎用クラス
/// </summary>
public class VFXEmitter : MonoBehaviour
{
    [Header("発生させるエフェクトを選択")]
    [SerializeField] private EffectID m_effectID;

    [Header("生成位置（空欄ならこのオブジェクトの位置）")]
    [SerializeField] private Transform m_spawnPoint;

    private int m_currentHandle = -1;

    /// <summary>
    /// エフェクトを再生する（アニメーションイベントやUnityEvent、他スクリプトから呼ぶ）
    /// </summary>
    public void PlayEffect()
    {
        Vector3 pos = m_spawnPoint != null ? m_spawnPoint.position : transform.position;

        // 再生しつつ、個別に止めるための管理番号（ハンドル）を記憶しておく
        m_currentHandle = VFXManager.Instance.Play(m_effectID, pos);
    }

    /// <summary>
    /// 再生したエフェクトを個別に停止する（ループエフェクト用）
    /// </summary>
    public void StopEffect()
    {
        if (m_currentHandle != -1)
        {
            // 記憶した番号のエフェクトだけをピンポイントで消す
            VFXManager.Instance.Stop(m_currentHandle);
            m_currentHandle = -1;
        }
    }
}