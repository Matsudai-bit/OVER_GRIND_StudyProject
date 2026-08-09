using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// フェーズ1の足のコントローラ
/// </summary>
public class P1LegController : MonoBehaviour
{
    [SerializeField]
    List<LegPartController> m_legParts = new();

    private bool m_isBreakingAllPart;

    public bool AreBothLegsBroken()
    {
        return m_legParts.All((legPart) => { return legPart.CurrentState == P1BossLegPartState.BROKEN; });
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (var item in m_legParts)
        {
            item.OnBroken += (legPart) => { OnBroken(legPart); };
        }
        m_isBreakingAllPart = false;
    }

    // Update is called once per frame
    void Update()
    {

    }


    public void OnBroken(LegPartController legPart)
    {
        Debug.Log("ダメージ通知");

    }

    public void DestroyLegParts()
    {
        foreach (var item in m_legParts)
        {
            item.LegModelObject.SetActive(false);
        }
    }

}
