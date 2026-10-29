using System;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 音声の再生およびプールへの自動返却を行う音源ノードクラスです。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioSourceNode : MonoBehaviour
{
    private const float DEFAULT_MIN_DISTANCE = 1.0f;
    private const float SPATIAL_BLEND_3D = 1.0f;
    private const float SPATIAL_BLEND_2D = 0.0f;

    /// <summary>
    /// 再生に使用する AudioSource コンポーネントです。
    /// </summary>
    private AudioSource m_audioSource;

    /// <summary>
    /// 3D 追従再生時の追従対象 Transform です。
    /// </summary>
    private Transform m_followTarget;

    /// <summary>
    /// 追従再生中かどうかを示すフラグです。
    /// </summary>
    private bool m_isFollowing = false;

    /// <summary>
    /// プール返却時に呼び出すコールバックアクションです。
    /// </summary>
    private Action<AudioSourceNode> m_onReturnPool;

    /// <summary>
    /// 安全に AudioSource コンポーネントを取得します。
    /// </summary>
    private AudioSource Source
    {
        get
        {
            if (m_audioSource == null)
            {
                m_audioSource = GetComponent<AudioSource>();
                if (m_audioSource == null)
                {
                    m_audioSource = gameObject.AddComponent<AudioSource>();
                }
                m_audioSource.playOnAwake = false;
            }
            return m_audioSource;
        }
    }

    /// <summary>
    /// 音声が再生中かどうかを取得します。
    /// </summary>
    /// <returns>
    /// true：再生中です。
    /// false：停止中です。
    /// </returns>
    public bool IsPlaying => Source != null && Source.isPlaying;

    /// <summary>
    /// 現在設定されているサウンドのカテゴリを取得します。
    /// </summary>
    public SoundCategory Category { get; private set; }

    /// <summary>
    /// サウンド設定およびプール返却アクションをセットアップします。
    /// </summary>
    /// <param name="data">再生するサウンドデータ。</param>
    /// <param name="mixerGroup">出力先の AudioMixerGroup。</param>
    /// <param name="returnAction">プール返却用アクション。</param>
    public void Setup(SoundDatabase.SoundData data, AudioMixerGroup mixerGroup, Action<AudioSourceNode> returnAction)
    {
        m_onReturnPool = returnAction;
        Category = data.m_category;

        Source.clip = data.m_clip;
        Source.volume = data.m_volume;
        Source.pitch = data.m_pitch;
        Source.loop = data.m_loop;
        Source.outputAudioMixerGroup = mixerGroup;

        if (data.m_is3D)
        {
            Source.spatialBlend = SPATIAL_BLEND_3D;
            Source.minDistance = DEFAULT_MIN_DISTANCE;
            Source.maxDistance = data.m_maxDistance;
            Source.rolloffMode = AudioRolloffMode.Logarithmic;
        }
        else
        {
            Source.spatialBlend = SPATIAL_BLEND_2D;
        }
    }

    /// <summary>
    /// 2D 音声として再生を開始します。
    /// </summary>
    public void Play2D()
    {
        ResetTarget();
        Source.spatialBlend = SPATIAL_BLEND_2D;
        Source.Play();
    }

    /// <summary>
    /// 指定されたワールド座標で 3D 音声を再生します。
    /// </summary>
    /// <param name="pos">再生位置のワールド座標。</param>
    public void Play3DAtPosition(Vector3 pos)
    {
        ResetTarget();
        Source.spatialBlend = SPATIAL_BLEND_3D;
        transform.position = pos;
        Source.Play();
    }

    /// <summary>
    /// 指定された Transform を追従しながら 3D 音声を再生します。
    /// </summary>
    /// <param name="target">追従対象の Transform。</param>
    public void Play3DFollowing(Transform target)
    {
        m_followTarget = target;
        m_isFollowing = true;
        Source.spatialBlend = SPATIAL_BLEND_3D;
        Source.Play();
    }

    /// <summary>
    /// 追従対象の Transform を新しいターゲットに変更します。
    /// </summary>
    /// <param name="newTarget">新しい追従対象の Transform。</param>
    public void SetFollowTarget(Transform newTarget)
    {
        m_followTarget = newTarget;
    }

    /// <summary>
    /// 追従状態およびターゲット参照をリセットします。
    /// </summary>
    private void ResetTarget()
    {
        m_followTarget = null;
        m_isFollowing = false;
    }

    private void Update()
    {
        if (!Source.isPlaying)
        {
            return;
        }

        if (m_isFollowing && m_followTarget != null)
        {
            transform.position = m_followTarget.position;
        }

        if (!Source.loop && !Source.isPlaying)
        {
            StopAndReturnToPool();
        }
    }

    /// <summary>
    /// 音声を停止し、オブジェクトを非アクティブ化してプールに戻します。
    /// </summary>
    public void StopAndReturnToPool()
    {
        if (Source.isPlaying)
        {
            Source.Stop();
        }

        ResetTarget();
        gameObject.SetActive(false);
        m_onReturnPool?.Invoke(this);
    }
}