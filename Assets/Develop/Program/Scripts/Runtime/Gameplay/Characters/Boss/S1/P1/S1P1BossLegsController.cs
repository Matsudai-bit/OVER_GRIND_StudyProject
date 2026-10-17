using UnityEngine;

/// <summary>
/// ステージ1フェーズ1のボスの脚破壊後処理を管理します。
/// </summary>
public sealed class S1P1BossLegsController : MonoBehaviour
{
    // 脚破壊時に再生するゲームプレイシーケンス
    [SerializeField]
    private GameplaySequenceRequestEvent m_gameplaySequenceRequestEvent;

    // 脚破壊Objective
    [SerializeField]
    private S1P1LegDestructionObjective m_destructionObjective;

    // 脚破壊後に再生するシーケンス
    [SerializeField]
    private GameplaySequenceAsset m_sequenceEventAsset;

    // モデルのRenderer参照管理
    [SerializeField]
    private MeshReferenceRegistry m_meshReferenceRegistry;

    // 左レールのMesh ID
    [SerializeField]
    private string[] m_leftRailBodyMeshIds;

    // 右レールのMesh ID
    [SerializeField]
    private string[] m_rightRailBodyMeshIds;

    // 開放後のレールRoot
    [SerializeField]
    private GameObject m_railRoot;

    // ゲームプレイシーケンスの状態変更通知
    [SerializeField]
    private GameplaySequenceChangedEvent m_gameplaySequenceChangedEvent;

    // レールが開放されているか
    private bool m_isOpen;

    private void Awake()
    {
        m_railRoot.SetActive(false);

        ApplyLegRailOpenState();
    }

    private void OnEnable()
    {
        m_destructionObjective.LegsDestroyed += HandleLegsDestroyed;

        m_gameplaySequenceChangedEvent.RegisterListener(
            HandleGameplaySequenceChanged);
    }

    private void OnDisable()
    {
        m_destructionObjective.LegsDestroyed -= HandleLegsDestroyed;

        m_gameplaySequenceChangedEvent.UnregisterListener(
            HandleGameplaySequenceChanged);
    }

    /// <summary>
    /// 両脚が破壊された際の処理を行います。
    /// </summary>
    /// <param name="objective">完了したObjective。</param>
    private void HandleLegsDestroyed(S1P1LegDestructionObjective objective)
    {
        if (m_gameplaySequenceRequestEvent == null ||
            m_sequenceEventAsset == null)
        {
            Debug.LogError(
                $"{nameof(S1P1BossLegsController)}: " +
                "ゲームプレイシーケンスの設定が不足しています。",
                this);

            return;
        }

        m_gameplaySequenceRequestEvent.Raise(m_sequenceEventAsset);
    }

    /// <summary>
    /// ゲームプレイシーケンスの状態変更を処理します。
    /// </summary>
    /// <param name="data">シーケンス変更情報。</param>
    private void HandleGameplaySequenceChanged(GameplaySequenceChangedEventData data)
    {
        if (m_isOpen)
        {
            return;
        }

        if (data.Sequence != m_sequenceEventAsset)
        {
            return;
        }

        if (data.EventType != GameplaySequenceEventType.FINISHED)
        {
            return;
        }

        OpenLegRail();
    }

    /// <summary>
    /// 脚部レールを開放状態にします。
    /// </summary>
    private void OpenLegRail()
    {
        m_isOpen = true;

        ApplyLegRailOpenState();

        if (m_railRoot != null)
        {
            m_railRoot.SetActive(true);
        }
    }

    /// <summary>
    /// 現在のレール開放状態をMeshへ反映します。
    /// </summary>
    private void ApplyLegRailOpenState()
    {
        ApplyRailMeshState(m_leftRailBodyMeshIds);
        ApplyRailMeshState(m_rightRailBodyMeshIds);
    }

    /// <summary>
    /// 指定したレールMeshへ現在の開放状態を反映します。
    /// </summary>
    /// <param name="meshIds">
    /// レールMesh ID。
    /// 0、1番目を閉鎖状態、2番目を開放状態として扱います。
    /// </param>
    private void ApplyRailMeshState(string[] meshIds)
    {
        if (meshIds == null || meshIds.Length < 3)
        {
            Debug.LogError(
                $"{nameof(S1P1BossLegsController)}: " +
                "レールMesh IDを3つ設定してください。",
                this);

            return;
        }

        SetMeshActive(meshIds[0], !m_isOpen);
        SetMeshActive(meshIds[1], !m_isOpen);
        SetMeshActive(meshIds[2], m_isOpen);
    }

    /// <summary>
    /// 指定したMeshの表示状態を変更します。
    /// </summary>
    /// <param name="meshId">Mesh ID。</param>
    /// <param name="isActive">表示する場合はtrue。</param>
    private void SetMeshActive(string meshId, bool isActive)
    {
        if (m_meshReferenceRegistry == null)
        {
            Debug.LogError(
                $"{nameof(S1P1BossLegsController)}: " +
                $"{nameof(MeshReferenceRegistry)}が設定されていません。",
                this);

            return;
        }

        Renderer renderer = m_meshReferenceRegistry.GetRenderer(meshId);

        if (renderer == null)
        {
            Debug.LogError(
                $"{nameof(S1P1BossLegsController)}: " +
                $"Mesh ID「{meshId}」のRendererを取得できませんでした。",
                this);

            return;
        }

        renderer.gameObject.SetActive(isActive);
    }
}