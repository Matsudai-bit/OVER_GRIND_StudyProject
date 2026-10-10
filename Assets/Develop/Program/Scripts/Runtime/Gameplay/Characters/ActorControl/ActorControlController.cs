using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Actorの制御状態を管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class ActorControlController : MonoBehaviour
{
    // シーケンス状態変更イベント
    [SerializeField, Header("シーケンスイベント")]
    private GameplaySequenceChangedEvent m_sequenceChangedEvent;

    // ポーズ状態変更イベント
    [SerializeField, Header("ポーズイベント")]
    private PauseChangedEvent m_pauseChangedEvent;

    // Actor制御を停止するシーケンスモード
    [SerializeField, Header("Actor制御停止対象モード")]
    private List<GameplayModeType> m_controlLockSequenceModes = new();
    // 制御対象ActorのRoot
    [SerializeField, Header("制御対象Actor")]
    private List<GameObject> m_actorObjects = new();
    // 制御対象Actor
    //[SerializeField, Header("制御対象Actor")]
    //private List<MonoBehaviour> m_actorComponents = new();

    // Actorの制御停止理由
    private readonly HashSet<ActorControlLockReason> m_lockReasons = new();

    // 制御可能なActor
    private readonly List<IActorControllable> m_actorControls = new();

    // Actorの制御が有効か
    private bool m_isControlEnabled = true;

    /// <summary>
    /// Actorの制御が有効か取得します。
    /// </summary>
    public bool IsControlEnabled => m_isControlEnabled;

    private void Awake()
    {
        CacheActorControls();
    }

    private void OnEnable()
    {
        RegisterEvents();
    }

    private void OnDisable()
    {
        UnregisterEvents();
    }

    /// <summary>
    /// Actorの制御停止理由を追加します。
    /// </summary>
    /// <param name="lockReason">追加する停止理由。</param>
    public void AddControlLock(ActorControlLockReason lockReason)
    {
        if (!m_lockReasons.Add(lockReason))
        {
            return;
        }

        RefreshActorControl();
    }

    /// <summary>
    /// Actorの制御停止理由を削除します。
    /// </summary>
    /// <param name="lockReason">削除する停止理由。</param>
    public void RemoveControlLock(ActorControlLockReason lockReason)
    {
        if (!m_lockReasons.Remove(lockReason))
        {
            return;
        }

        RefreshActorControl();
    }

    /// <summary>
    /// 指定した停止理由が登録されているか確認します。
    /// </summary>
    /// <param name="lockReason">確認する停止理由。</param>
    /// <returns>
    /// true：登録されています。
    /// false：登録されていません。
    /// </returns>
    public bool HasControlLock(ActorControlLockReason lockReason)
    {
        return m_lockReasons.Contains(lockReason);
    }

    /// <summary>
    /// イベントを登録します。
    /// </summary>
    private void RegisterEvents()
    {
        if (m_sequenceChangedEvent != null)
        {
            m_sequenceChangedEvent.RegisterListener(
                OnSequenceChanged);
        }
        else
        {
            Debug.LogWarning(
                $"{nameof(GameplaySequenceChangedEvent)} が設定されていません。",
                this);
        }

        if (m_pauseChangedEvent != null)
        {
            m_pauseChangedEvent.RegisterListener(
                OnPauseChanged);
        }
        else
        {
            Debug.LogWarning(
                $"{nameof(PauseChangedEvent)} が設定されていません。",
                this);
        }
    }

    /// <summary>
    /// イベント登録を解除します。
    /// </summary>
    private void UnregisterEvents()
    {
        if (m_sequenceChangedEvent != null)
        {
            m_sequenceChangedEvent.UnregisterListener(
                OnSequenceChanged);
        }

        if (m_pauseChangedEvent != null)
        {
            m_pauseChangedEvent.UnregisterListener(
                OnPauseChanged);
        }
    }

  

    /// <summary>
    /// 制御対象Actorを取得します。
    /// </summary>
    private void CacheActorControls()
    {
        m_actorControls.Clear();

        foreach (GameObject actorObject in m_actorObjects)
        {
            if (actorObject == null)
            {
                continue;
            }

            MonoBehaviour[] components =
                actorObject.GetComponentsInChildren<MonoBehaviour>(true);

            foreach (MonoBehaviour component in components)
            {
                if (component is not IActorControllable actorControl)
                {
                    continue;
                }

                if (m_actorControls.Contains(actorControl))
                {
                    continue;
                }

                m_actorControls.Add(actorControl);
            }
        }
    }

    /// <summary>
    /// シーケンス状態変更を受け取ります。
    /// </summary>
    /// <param name="eventData">シーケンス状態変更情報。</param>
    private void OnSequenceChanged(
        GameplaySequenceChangedEventData eventData)
    {
        if (eventData.Sequence == null)
        {
            return;
        }

        GameplayModeType requiredMode =
            eventData.Sequence.RequiredMode;

        if (!m_controlLockSequenceModes.Contains(requiredMode))
        {
            return;
        }

        switch (eventData.EventType)
        {
            case GameplaySequenceEventType.STARTED:
                AddControlLock(
                    ActorControlLockReason.CUTSCENE);
                break;

            case GameplaySequenceEventType.FINISHED:
            case GameplaySequenceEventType.CANCELED:
                RemoveControlLock(
                    ActorControlLockReason.CUTSCENE);
                break;
        }
    }

    /// <summary>
    /// ポーズ状態変更を受け取ります。
    /// </summary>
    /// <param name="eventData">ポーズ状態変更情報。</param>
    private void OnPauseChanged(
        PauseChangedEventData eventData)
    {
        if (eventData.IsPaused)
        {
            AddControlLock(
                ActorControlLockReason.PAUSE);

            return;
        }

        RemoveControlLock(
            ActorControlLockReason.PAUSE);
    }

    /// <summary>
    /// 現在の停止理由からActor制御状態を更新します。
    /// </summary>
    private void RefreshActorControl()
    {
        bool isControlEnabled = m_lockReasons.Count == 0;

        if (m_isControlEnabled == isControlEnabled)
        {
            return;
        }

        m_isControlEnabled = isControlEnabled;

        foreach (IActorControllable actorControl in m_actorControls)
        {
            if (actorControl is Object unityObject
                && unityObject == null)
            {
                continue;
            }

            actorControl.SetControlEnabled(
                m_isControlEnabled);
        }
    }
}
