using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// ルートParticleSystemの再生・停止・寿命を制御します。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(ParticleSystem))]
public sealed class EffectInstance : MonoBehaviour
{
    [SerializeField, Header("ルートパーティクル（未設定なら自身）")]
    private ParticleSystem m_rootParticle;

    // 追従設定と再生終了時の返却通知
    private EffectAttachmentController m_attachmentController;
    private EffectAnchorRegistry m_boundRegistry;
    private Action<EffectInstance> m_finishedCallback;

    // 再生状態と終了監視
    private Coroutine m_monitorCoroutine;
    private bool m_isPrepared;
    private bool m_isActive;
    private bool m_isCompleting;
    private bool m_isStopping;
    private EffectPlaybackMode m_playbackMode;
    private int m_generation;

    // 同一インスタンスの再利用後も残る、再生単位の終了通知
    private EffectCompletionTicket m_completionTicket;

    /// <summary>
    /// 現在の再生世代を取得します。
    /// </summary>
    public int Generation => m_generation;

    /// <summary>
    /// 再生中または残存粒子待ちか取得します。
    /// </summary>
    public bool IsPlaying => m_isActive;

    /// <summary>
    /// 現在の再生に対応する終了通知チケットを取得します。
    /// </summary>
    internal EffectCompletionTicket CompletionTicket => m_completionTicket;

    /// <summary>
    /// 再生終了時の返却先とコンポーネントを設定します。
    /// </summary>
    /// <param name="finishedCallback">再生終了時の返却処理。</param>
    /// <returns>true：初期化成功。false：設定が不正です。</returns>
    public bool Prepare(Action<EffectInstance> finishedCallback)
    {
        if (m_isPrepared)
        {
            return true;
        }
        if (m_rootParticle == null)
        {
            m_rootParticle = GetComponent<ParticleSystem>();
        }
        if (m_rootParticle == null || m_rootParticle.gameObject != gameObject)
        {
            Debug.LogError("[EffectInstance] ルートと同じオブジェクトにParticleSystemを設定してください。", this);
            return false;
        }

        m_attachmentController = new EffectAttachmentController(
            GetComponentsInChildren<EffectAttachmentSlot>(true));
        m_finishedCallback = finishedCallback;
        m_isPrepared = true;
        return true;
    }

    /// <summary>
    /// 子を含むParticleSystemを再生します。
    /// </summary>
    /// <param name="request">発生位置と追従設定。</param>
    /// <param name="playbackMode">再生方式。</param>
    /// <returns>true：再生成功。false：設定不足です。</returns>
    public bool Play(EffectSpawnRequest request, EffectPlaybackMode playbackMode)
    {
        if (!m_isPrepared || m_isActive || m_rootParticle == null)
        {
            return false;
        }

        // 生成位置とConstraintの参照先を確定します。
        if (request.UseWorldPose)
        {
            transform.SetPositionAndRotation(request.Position, request.Rotation);
        }
        if (!m_attachmentController.TryBind(request.Anchors))
        {
            return false;
        }

        gameObject.SetActive(true);
        if (!gameObject.activeInHierarchy)
        {
            m_attachmentController.ClearBindings();
            Debug.LogWarning("[EffectInstance] 親が非アクティブのため再生できません。", this);
            return false;
        }

        m_playbackMode = playbackMode;
        m_boundRegistry = request.Anchors;
        if (m_boundRegistry != null)
        {
            m_boundRegistry.BindingsChanged += OnAnchorBindingsChanged;
        }

        // 再生するたびにチケットを新規作成し、過去の購読を持ち越しません。
        m_isActive = true;
        m_isStopping = false;
        m_completionTicket = new EffectCompletionTicket();
        unchecked { m_generation++; }

        m_rootParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        m_rootParticle.Clear(true);
        m_rootParticle.Play(true);

        if (m_playbackMode == EffectPlaybackMode.ONE_SHOT)
        {
            m_monitorCoroutine = StartCoroutine(MonitorCompletion());
        }
        return true;
    }

    /// <summary>
    /// 指定世代の再生が現在も有効か取得します。
    /// </summary>
    public bool IsGenerationActive(int generation)
    {
        return m_isActive && m_generation == generation;
    }

    /// <summary>
    /// 指定世代の再生だけを停止します。
    /// </summary>
    /// <param name="generation">停止対象の世代。</param>
    /// <param name="stopMode">停止方式。</param>
    /// <returns>true：停止要求成功。false：対象外です。</returns>
    public bool StopIfGenerationMatches(int generation, EffectStopMode stopMode)
    {
        if (!IsGenerationActive(generation))
        {
            return false;
        }
        Stop(stopMode);
        return true;
    }

    /// <summary>
    /// 指定方式で停止します。
    /// </summary>
    public void Stop(EffectStopMode stopMode)
    {
        if (!m_isActive)
        {
            return;
        }

        // 即時停止は自然停止待機中でも割り込めます。
        if (stopMode == EffectStopMode.IMMEDIATE)
        {
            if (m_monitorCoroutine != null)
            {
                StopCoroutine(m_monitorCoroutine);
                m_monitorCoroutine = null;
            }

            m_rootParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Complete(EffectCompletionReason.CANCELLED);
            return;
        }

        if (m_isStopping)
        {
            return;
        }
        m_isStopping = true;

        if (m_monitorCoroutine != null)
        {
            StopCoroutine(m_monitorCoroutine);
            m_monitorCoroutine = null;
        }

        // 放出を止め、子を含む既存粒子が消えるまで待機します。
        m_rootParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        m_monitorCoroutine = StartCoroutine(MonitorCompletion());
    }

    /// <summary>
    /// エフェクトを即時停止します。
    /// </summary>
    public void StopImmediately()
    {
        Stop(EffectStopMode.IMMEDIATE);
    }

    /// <summary>
    /// 再利用に向けて粒子と追従参照を初期化します。
    /// </summary>
    public void ResetForReuse()
    {
        // 通常の終了を経ずに破棄する場合は、後から強制終了を通知します。
        EffectCompletionTicket cancelledTicket =
            m_isActive && !m_isCompleting ? m_completionTicket : null;
        m_isActive = false;

        if (m_monitorCoroutine != null)
        {
            StopCoroutine(m_monitorCoroutine);
            m_monitorCoroutine = null;
        }
        m_isActive = false;
        m_isStopping = false;

        if (m_rootParticle != null)
        {
            m_rootParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        if (m_boundRegistry != null)
        {
            m_boundRegistry.BindingsChanged -= OnAnchorBindingsChanged;
            m_boundRegistry = null;
        }
        m_attachmentController?.ClearBindings();

        // 再利用用の初期化を終えてから外部処理に通知します。
        cancelledTicket?.Complete(EffectCompletionReason.CANCELLED);
    }

    /// <summary>
    /// モデル変更時にConstraintを再設定します。
    /// </summary>
    private void OnAnchorBindingsChanged()
    {
        if (m_isActive && m_boundRegistry != null &&
            !m_attachmentController.TryBind(m_boundRegistry))
        {
            StopImmediately();
        }
    }

    /// <summary>
    /// 粒子がすべて消滅したら終了通知します。
    /// </summary>
    private IEnumerator MonitorCompletion()
    {
        // 再生直後の未生成フレームは終了判定しません。
        yield return null;
        while (m_isActive && m_rootParticle != null && m_rootParticle.IsAlive(true))
        {
            yield return null;
        }
        m_monitorCoroutine = null;
        if (m_isActive)
        {
            EffectCompletionReason reason = m_isStopping
                ? EffectCompletionReason.STOPPED
                : EffectCompletionReason.COMPLETED;
            Complete(reason);
        }
    }

    /// <summary>
    /// 返却前に終了を1度だけ通知します。
    /// </summary>
    private void Complete(EffectCompletionReason reason)
    {
        if (!m_isActive || m_isCompleting)
        {
            return;
        }

        m_isCompleting = true;
        m_isActive = false;

        // 所有者の返却処理を先に終え、再利用可能になってから通知します。
        // 世代別チケットを退避するので、再利用後も古い購読先は変わりません。
        EffectCompletionTicket completedTicket = m_completionTicket;
        try
        {
            m_finishedCallback?.Invoke(this);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
        finally
        {
            m_isCompleting = false;
            completedTicket?.Complete(reason);
        }
    }

    /// <summary>
    /// 外部から無効化された場合も終了通知します。
    /// </summary>
    private void OnDisable()
    {
        if (m_isActive && !m_isCompleting)
        {
            Stop(EffectStopMode.IMMEDIATE);
        }
    }
}
