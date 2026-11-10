using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AudioSequentialAssetの再生を受け付ける共通コンポーネントです。
/// プレイヤー状態とアニメーションの両方から利用します。
/// </summary>
[DisallowMultipleComponent]
public sealed class SequentialAudioPlayer : MonoBehaviour
{
    [SerializeField, Header("既定の音声発生位置（未指定時は自身のTransform）")]
    private Transform m_defaultEmissionPoint;

    // このコンポーネントが開始した再生セットを保持します。
    private readonly List<AudioSequentialHandle> m_activeHandles = new List<AudioSequentialHandle>();

    /// <summary>
    /// 音声セットを再生します。異なるセットの同時再生も可能です。
    /// </summary>
    /// <param name="asset">再生する音声セット。</param>
    /// <param name="emissionPoint">音声発生位置。省略時は既定の位置を使用します。</param>
    /// <returns>停止に使用するハンドル。失敗した場合はnull。</returns>
    public AudioSequentialHandle Play(AudioSequentialAsset asset, Transform emissionPoint = null)
    {
        if (asset == null)
        {
            Debug.LogWarning("[SequentialAudioPlayer] AudioSequentialAssetが未設定です。", this);
            return null;
        }

        Transform source = emissionPoint != null ? emissionPoint : GetDefaultEmissionPoint();
        AudioSequentialHandle handle = new AudioSequentialHandle(asset, source);

        // 再生終了時のハンドルを自動で一覧から除外します。
        handle.Finished += OnPlaybackFinished;
        m_activeHandles.Add(handle);

        if (!handle.Begin())
        {
            handle.Finished -= OnPlaybackFinished;
            m_activeHandles.Remove(handle);
            return null;
        }

        return handle;
    }

    /// <summary>
    /// 指定した音声セットを終了音へ遷移させます。
    /// </summary>
    public void Stop(AudioSequentialHandle handle)
    {
        if (handle != null && m_activeHandles.Contains(handle))
        {
            handle.Stop();
        }
    }

    /// <summary>
    /// このコンポーネントで再生したすべての音声セットを即座に停止します。
    /// </summary>
    public void StopAllImmediate()
    {
        AudioSequentialHandle[] handles = m_activeHandles.ToArray();
        m_activeHandles.Clear();

        foreach (AudioSequentialHandle handle in handles)
        {
            handle.Finished -= OnPlaybackFinished;
            handle.StopImmediate();
        }
    }

    /// <summary>
    /// 音声発生位置を取得します。
    /// </summary>
    private Transform GetDefaultEmissionPoint()
    {
        return m_defaultEmissionPoint != null ? m_defaultEmissionPoint : transform;
    }

    /// <summary>
    /// 自然終了・強制終了した音声セットを管理対象から除外します。
    /// </summary>
    private void OnPlaybackFinished(AudioSequentialHandle handle)
    {
        handle.Finished -= OnPlaybackFinished;
        m_activeHandles.Remove(handle);
    }

    private void OnDisable()
    {
        StopAllImmediate();
    }
}
