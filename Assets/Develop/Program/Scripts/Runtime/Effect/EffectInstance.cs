using System;
using System.Collections;
using UnityEngine;

/// <summary>ルートParticleSystemの再生・寿命・再利用を制御します。</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(ParticleSystem))]
public sealed class EffectInstance : MonoBehaviour
{
    [SerializeField, Header("ルートパーティクル（未設定なら自身）")]
    private ParticleSystem m_rootParticle;

    private EffectAttachmentController m_attachmentController;
    private EffectAnchorRegistry m_boundRegistry;
    private Action<EffectInstance> m_finishedCallback;
    private Coroutine m_monitorCoroutine;
    private bool m_isPrepared;
    private bool m_isActive;
    private bool m_isCompleting;
    private EffectPlaybackMode m_playbackMode;
    private int m_generation;

    /// <summary>現在の再生世代。</summary>
    public int Generation => m_generation;
    /// <summary>再生中または残存粒子待ちであるか。</summary>
    public bool IsPlaying => m_isActive;

    /// <summary>終了通知と必要なコンポーネントを取得します。</summary>
    /// <param name="finishedCallback">再生終了時の通知先。</param>
    /// <returns>true：初期化成功。false：ルートParticleSystemがありません。</returns>
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

    /// <summary>子を含むParticleSystemを再生します。</summary>
    /// <param name="request">座標・アンカーの設定。</param>
    /// <param name="playbackMode">自動返却するか継続再生するか。</param>
    /// <returns>true：再生成功。false：設定不足です。</returns>
    public bool Play(EffectSpawnRequest request, EffectPlaybackMode playbackMode)
    {
        if (!m_isPrepared || m_isActive || m_rootParticle == null)
        {
            return false;
        }
        // 非アクティブの状態で位置を確定させ、Constraint参照を設定します。
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

        // 繰り返し再生のフラグはPrefab側のParticleSystem設定を尊重します。
        m_playbackMode = playbackMode;
        m_boundRegistry = request.Anchors;
        if (m_boundRegistry != null)
        {
            m_boundRegistry.BindingsChanged += OnAnchorBindingsChanged;
        }
        m_isActive = true;
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

    /// <summary>指定世代が再生中か確認します。</summary>
    public bool IsGenerationActive(int generation)
    {
        return m_isActive && m_generation == generation;
    }

    /// <summary>指定世代のみ停止します。</summary>
    /// <param name="generation">停止対象の再生世代。</param>
    /// <param name="stopMode">停止方法。</param>
    /// <returns>true：停止要求を受理しました。false：別世代です。</returns>
    public bool StopIfGenerationMatches(int generation, EffectStopMode stopMode)
    {
        if (!IsGenerationActive(generation))
        {
            return false;
        }
        Stop(stopMode);
        return true;
    }

    /// <summary>指定方式で停止します。</summary>
    public void Stop(EffectStopMode stopMode)
    {
        if (!m_isActive)
        {
            return;
        }
        if (m_monitorCoroutine != null)
        {
            StopCoroutine(m_monitorCoroutine);
            m_monitorCoroutine = null;
        }
        if (stopMode == EffectStopMode.IMMEDIATE)
        {
            m_rootParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Complete();
            return;
        }
        // 全子ParticleSystemの放出のみ止め、残った粒子が消えるのを待ちます。
        m_rootParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        m_monitorCoroutine = StartCoroutine(MonitorCompletion());
    }

    /// <summary>強制的に停止し、再利用可能な状態に戻します。</summary>
    public void StopImmediately()
    {
        Stop(EffectStopMode.IMMEDIATE);
    }

    /// <summary>再使用に向けて粒子・追従参照を初期化します。</summary>
    public void ResetForReuse()
    {
        m_isActive = false;
        if (m_monitorCoroutine != null)
        {
            StopCoroutine(m_monitorCoroutine);
            m_monitorCoroutine = null;
        }
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
    }

    /// <summary>モデルが更新されたら再生中のConstraint参照を張り直します。</summary>
    private void OnAnchorBindingsChanged()
    {
        if (m_isActive && m_boundRegistry != null &&
            !m_attachmentController.TryBind(m_boundRegistry))
        {
            StopImmediately();
        }
    }

    /// <summary>粒子がすべて消滅したら返却通知します。</summary>
    private IEnumerator MonitorCompletion()
    {
        // 開始直後の未生成状態を完了と誤判定しないように1フレーム待ちます。
        yield return null;
        while (m_isActive && m_rootParticle != null && m_rootParticle.IsAlive(true))
        {
            yield return null;
        }
        m_monitorCoroutine = null;
        if (m_isActive)
        {
            Complete();
        }
    }

    /// <summary>同一再生を二重返却しないように通知します。</summary>
    private void Complete()
    {
        if (!m_isActive || m_isCompleting)
        {
            return;
        }
        m_isCompleting = true;
        m_isActive = false;
        m_finishedCallback?.Invoke(this);
        m_isCompleting = false;
    }

    /// <summary>外部から無効化された場合も所有者へ返却を通知します。</summary>
    private void OnDisable()
    {
        if (m_isActive && !m_isCompleting)
        {
            Stop(EffectStopMode.IMMEDIATE);
        }
    }
}
