using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ステージ1フェーズ1の脚オブジェクトを管理します。
/// </summary>
public sealed class S1P1BossLegsController : MonoBehaviour
{
    // 崩壊時に非表示にする脚オブジェクト
    [SerializeField, Header("脚オブジェクト")]
    private List<GameObject> m_legObjects = new();

    [SerializeField]
    private GameplaySequenceRequestEvent m_gameplaySequenceRequestEvent;

    [SerializeField]
    private S1P1LegDestructionObjective destructionObjective;

    [SerializeField]
    private GameplaySequenceAsset sequenceEventAsset;
    [SerializeField]
    private MeshReferenceRegistry m_meshReferenceRegistry;
    [SerializeField]
    private string[] m_LRailBodyMeshID;
    [SerializeField]
    private string[] m_RRailBodyMeshID;

    [SerializeField]
    private GameplaySequenceChangedEvent m_gameplaySequenceChangedEvent;

    private bool open = false;

    private void Awake()
    {
        destructionObjective.LegsDestroyed += (objective) => 
        {
            m_gameplaySequenceRequestEvent.Raise(sequenceEventAsset);

            m_gameplaySequenceChangedEvent.RegisterListener((data) =>
            {
                
                if (data.Sequence == sequenceEventAsset && data.EventType == GameplaySequenceEventType.FINISHED)
                {
                    open = true;
                    ApplyLegRailOpenState();
                }
            });
        };
        ApplyLegRailOpenState();
    }

    private void ApplyLegRailOpenState()
    {
        m_meshReferenceRegistry.GetRenderer(m_LRailBodyMeshID[0]).gameObject.SetActive(!open);
        m_meshReferenceRegistry.GetRenderer(m_LRailBodyMeshID[1]).gameObject.SetActive(!open);
        m_meshReferenceRegistry.GetRenderer(m_LRailBodyMeshID[2]).gameObject.SetActive(open);
        m_meshReferenceRegistry.GetRenderer(m_RRailBodyMeshID[0]).gameObject.SetActive(!open);
        m_meshReferenceRegistry.GetRenderer(m_RRailBodyMeshID[1]).gameObject.SetActive(!open);
        m_meshReferenceRegistry.GetRenderer(m_RRailBodyMeshID[2]).gameObject.SetActive(open);
    }

    private void Start()
    {
        
    }

    /// <summary>
    /// 脚を崩壊状態にします。
    /// </summary>
    public void CollapseLegs()
    {
        foreach (GameObject legObject in m_legObjects)
        {
            if (legObject == null)
            {
                continue;
            }

            legObject.SetActive(false);
        }
    }
}
