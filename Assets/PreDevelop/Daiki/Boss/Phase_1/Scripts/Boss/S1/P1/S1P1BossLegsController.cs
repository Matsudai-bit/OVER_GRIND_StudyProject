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
