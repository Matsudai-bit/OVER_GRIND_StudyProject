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
        return StartSequence(new AudioSequentialHandle(asset, source));
    }

    /// <summary>
    /// 指定ワールド座標でシーケンスを固定位置3D再生します。
    /// AssetのSpatialModeはこの再生に限り上書きします。
    /// </summary>
    /// <param name="asset">連続音声の設定。</param>
    /// <param name="position">発生するワールド座標。</param>
    /// <returns>再生ハンドル。失敗時はnull。</returns>
    public AudioSequentialHandle PlayAt(AudioSequentialAsset asset, Vector3 position)
    {
        if (asset == null) return null;
        return StartSequence(new AudioSequentialHandle(asset, position));
    }

    /// <summary>
    /// 指定Transformに追従させて3Dシーケンスを再生します。
    /// </summary>
    /// <param name="asset">連続音声の設定。</param>
    /// <param name="target">追従先。</param>
    /// <returns>再生ハンドル。失敗時はnull。</returns>
    public AudioSequentialHandle PlayFollowing(AudioSequentialAsset asset, Transform target)
    {
        if (asset == null || target == null)
        {
            Debug.LogWarning("[SequentialAudioPlayer] Assetまたは追従先がありません。", this);
            return null;
        }

        return StartSequence(new AudioSequentialHandle(asset,
            AudioSequentialSpatialMode.THREE_DIMENSIONAL_FOLLOWING, target, target.position));
    }

    /// <summary>
    /// シーケンスを2D再生します。
    /// </summary>
    /// <param name="asset">連続音声の設定。</param>
    /// <returns>再生ハンドル。失敗時はnull。</returns>
    public AudioSequentialHandle Play2D(AudioSequentialAsset asset)
    {
        if (asset == null) return null;
        return StartSequence(new AudioSequentialHandle(asset,
            AudioSequentialSpatialMode.TWO_DIMENSIONAL, null, Vector3.zero));
    }

    /// <summary>
    /// 再生ハンドルを登録し、一連の再生を開始します。
    /// </summary>
    private AudioSequentialHandle StartSequence(AudioSequentialHandle handle)
    {
        if (handle == null) return null;

        // 再生終了時に管理対象から自動的に除外します。
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
    /// 単発音声を再生します。アニメーション終了時にも自動停止しません。
    /// </summary>
    /// <param name="asset">単発音声の設定。</param>
    /// <param name="emissionPoint">音声発生位置。省略時は既定値。</param>
    /// <returns>個別停止用ハンドル。失敗した場合はnull。</returns>
    public AudioPlayHandle PlayOneShot(AudioOneShotAsset asset, Transform emissionPoint = null)
    {
        if (asset == null || AudioManager.Instance == null)
        {
            Debug.LogWarning("[SequentialAudioPlayer] 単発音声AssetまたはAudioManagerがありません。", this);
            return null;
        }

        AudioManager manager = AudioManager.Instance;
        Transform source = emissionPoint != null ? emissionPoint : GetDefaultEmissionPoint();

        switch (asset.SpatialMode)
        {
            case AudioSequentialSpatialMode.THREE_DIMENSIONAL_AT_POSITION:
                return manager.Play3DAtPosition(asset.AudioID, source.position, false);

            case AudioSequentialSpatialMode.THREE_DIMENSIONAL_FOLLOWING:
                return manager.Play3DFollowing(asset.AudioID, source, false);

            default:
                return manager.Play2D(asset.AudioID, false);
        }
    }

    /// <summary>
    /// 単発SEを固定ワールド座標から再生します。
    /// </summary>
    /// <param name="asset">単発音声の設定。</param>
    /// <param name="position">発生するワールド座標。</param>
    /// <returns>再生ハンドル。失敗時はnull。</returns>
    public AudioPlayHandle PlayOneShotAt(AudioOneShotAsset asset, Vector3 position)
    {
        if (asset == null || AudioManager.Instance == null) return null;
        return AudioManager.Instance.Play3DAtPosition(asset.AudioID, position, false);
    }

    /// <summary>
    /// 単発SEを指定Transformに追従して再生します。
    /// </summary>
    /// <param name="asset">単発音声の設定。</param>
    /// <param name="target">追従先。</param>
    /// <returns>再生ハンドル。失敗時はnull。</returns>
    public AudioPlayHandle PlayOneShotFollowing(AudioOneShotAsset asset, Transform target)
    {
        if (asset == null || AudioManager.Instance == null || target == null) return null;
        return AudioManager.Instance.Play3DFollowing(asset.AudioID, target, false);
    }

    /// <summary>
    /// 単発SEを2D再生します。
    /// </summary>
    /// <param name="asset">単発音声の設定。</param>
    /// <returns>再生ハンドル。失敗時はnull。</returns>
    public AudioPlayHandle PlayOneShot2D(AudioOneShotAsset asset)
    {
        if (asset == null || AudioManager.Instance == null) return null;
        return AudioManager.Instance.Play2D(asset.AudioID, false);
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
