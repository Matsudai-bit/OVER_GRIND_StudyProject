using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UnityEventから音声IDを指定した音声を再生・停止します。
/// </summary>
[DisallowMultipleComponent]
public sealed class AudioEventPlayer : MonoBehaviour
{
    /// <summary>
    /// 音声の発生方式を指定します。
    /// </summary>
    public enum PlaybackMode
    {
        TWO_DIMENSIONAL,
        THREE_DIMENSIONAL_AT_POSITION,
        THREE_DIMENSIONAL_FOLLOWING
    }

    /// <summary>
    /// SoundDatabaseのループ設定を上書きする方法です。
    /// </summary>
    public enum LoopMode
    {
        USE_DATABASE,
        FORCE_ONCE,
        FORCE_LOOP
    }

    [SerializeField, Header("再生する音声ID")]
    private AudioID m_audioId;

    [SerializeField, Header("再生方式")]
    private PlaybackMode m_playbackMode = PlaybackMode.TWO_DIMENSIONAL;

    [SerializeField, Header("3D音声の発生地点（未設定時は自身）")]
    private Transform m_emissionPoint;

    [SerializeField, Header("ループ設定")]
    private LoopMode m_loopMode = LoopMode.USE_DATABASE;

    [SerializeField, Header("再生中の重複再生を許可する")]
    private bool m_allowOverlapping = true;

    [SerializeField, Header("無効化時に発生した音声を停止する")]
    private bool m_stopOnDisable = true;

    // このコンポーネントが開始した個別再生を管理します。
    private readonly List<AudioPlayHandle> m_activeHandles = new List<AudioPlayHandle>();

    /// <summary>
    /// このコンポーネントが開始した音声を再生中か取得します。
    /// </summary>
    public bool IsActive => m_activeHandles.Count > 0;

    /// <summary>
    /// Inspectorで指定した方式で音声を再生します。
    /// </summary>
    public void Play()
    {
        PlayWithMode(m_playbackMode);
    }

    /// <summary>
    /// 2D音声を再生します。
    /// </summary>
    public void Play2D()
    {
        PlayWithMode(PlaybackMode.TWO_DIMENSIONAL);
    }

    /// <summary>
    /// 指定した地点から3D音声を再生します。
    /// </summary>
    public void Play3DAtPosition()
    {
        PlayWithMode(PlaybackMode.THREE_DIMENSIONAL_AT_POSITION);
    }

    /// <summary>
    /// 指定したTransformに追従する3D音声を再生します。
    /// </summary>
    public void Play3DFollowing()
    {
        PlayWithMode(PlaybackMode.THREE_DIMENSIONAL_FOLLOWING);
    }

    /// <summary>
    /// このコンポーネントが開始した音声をすべて停止します。
    /// </summary>
    public void Stop()
    {
        // 停止中のイベントでリストが変更されないよう、先に除外します。
        while (m_activeHandles.Count > 0)
        {
            int lastIndex = m_activeHandles.Count - 1;
            AudioPlayHandle handle = m_activeHandles[lastIndex];
            m_activeHandles.RemoveAt(lastIndex);
            if (handle == null)
            {
                continue;
            }

            handle.Finished -= OnPlaybackFinished;
            handle.Stop();
        }
    }

    /// <summary>
    /// このコンポーネントが開始した音声を一時停止します。
    /// </summary>
    public void Pause()
    {
        foreach (AudioPlayHandle handle in m_activeHandles)
        {
            if (handle != null && handle.IsActive)
            {
                handle.Pause();
            }
        }
    }

    /// <summary>
    /// 一時停止した音声を再開します。
    /// </summary>
    public void Resume()
    {
        foreach (AudioPlayHandle handle in m_activeHandles)
        {
            if (handle != null && handle.IsActive)
            {
                handle.Resume();
            }
        }
    }

    /// <summary>
    /// 再生方式に従って音声を再生し、ハンドルを登録します。
    /// </summary>
    private void PlayWithMode(PlaybackMode mode)
    {
        AudioManager manager = AudioManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("[AudioEventPlayer] AudioManagerが見つかりません。", this);
            return;
        }

        RemoveInactiveHandles();
        if (!m_allowOverlapping && m_activeHandles.Count > 0)
        {
            return;
        }

        // SoundDatabaseの設定を標準とし、必要な場合だけループを上書きします。
        bool? loopOverride = null;
        switch (m_loopMode)
        {
            case LoopMode.FORCE_ONCE:
                loopOverride = false;
                break;
            case LoopMode.FORCE_LOOP:
                loopOverride = true;
                break;
        }

        Transform emissionPoint = m_emissionPoint != null ? m_emissionPoint : transform;
        AudioPlayHandle handle;
        switch (mode)
        {
            case PlaybackMode.TWO_DIMENSIONAL:
                handle = manager.Play2D(m_audioId, loopOverride);
                break;

            case PlaybackMode.THREE_DIMENSIONAL_AT_POSITION:
                handle = manager.Play3DAtPosition(m_audioId, emissionPoint.position, loopOverride);
                break;

            case PlaybackMode.THREE_DIMENSIONAL_FOLLOWING:
                handle = manager.Play3DFollowing(m_audioId, emissionPoint, loopOverride);
                break;

            default:
                Debug.LogWarning("[AudioEventPlayer] 不明な再生方式です。", this);
                return;
        }

        if (handle == null || !handle.IsActive)
        {
            return;
        }

        m_activeHandles.Add(handle);
        handle.Finished += OnPlaybackFinished;
    }

    /// <summary>
    /// 終了済みの再生ハンドルを管理対象から除外します。
    /// </summary>
    private void OnPlaybackFinished(AudioPlayHandle handle, bool completedNaturally)
    {
        if (handle == null)
        {
            return;
        }

        handle.Finished -= OnPlaybackFinished;
        m_activeHandles.Remove(handle);
    }

    /// <summary>
    /// ハンドルの無効化を考慮して、終了済みの再生情報を整理します。
    /// </summary>
    private void RemoveInactiveHandles()
    {
        for (int i = m_activeHandles.Count - 1; i >= 0; i--)
        {
            AudioPlayHandle handle = m_activeHandles[i];
            if (handle != null && handle.IsActive)
            {
                continue;
            }

            if (handle != null)
            {
                handle.Finished -= OnPlaybackFinished;
            }

            m_activeHandles.RemoveAt(i);
        }
    }

    /// <summary>
    /// 無効化時に必要なら音声を停止し、終了通知の購読を解除します。
    /// </summary>
    private void OnDisable()
    {
        if (m_stopOnDisable)
        {
            Stop();
            return;
        }

        // 音声を残しても、このコンポーネントが再生を監視し続けないようにします。
        foreach (AudioPlayHandle handle in m_activeHandles)
        {
            if (handle != null)
            {
                handle.Finished -= OnPlaybackFinished;
            }
        }
        m_activeHandles.Clear();
    }
}
