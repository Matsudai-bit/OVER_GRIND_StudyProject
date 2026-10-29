using UnityEngine;

/// <summary>
/// 再生中の音声を外部から操作・制御するためのハンドルクラスです。
/// </summary>
public class AudioPlayHandle
{
    /// <summary>
    /// 制御対象の音源ノードです。
    /// </summary>
    private AudioSourceNode m_node;

    /// <summary>
    /// 音声が再生中かどうかを取得します。
    /// </summary>
    /// <returns>
    /// true：再生中です。
    /// false：停止中またはノードが存在しません。
    /// </returns>
    public bool IsPlaying => m_node != null && m_node.IsPlaying;

    /// <summary>
    /// AudioPlayHandle の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="node">制御対象の音源ノード。</param>
    public AudioPlayHandle(AudioSourceNode node)
    {
        m_node = node;
    }

    /// <summary>
    /// 再生中の音声を停止し、プールへ返却します。
    /// </summary>
    public void Stop()
    {
        if (m_node != null && m_node.IsPlaying)
        {
            m_node.StopAndReturnToPool();
        }
    }

    /// <summary>
    /// 3D 音声の追従対象を途中で変更します。
    /// </summary>
    /// <param name="newTarget">新しい追従対象の Transform。</param>
    public void ChangeFollowTarget(Transform newTarget)
    {
        if (m_node != null)
        {
            m_node.SetFollowTarget(newTarget);
        }
    }
}