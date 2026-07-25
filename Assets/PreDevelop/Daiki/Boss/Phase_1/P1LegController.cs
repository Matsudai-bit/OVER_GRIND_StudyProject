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

    private bool isBreakingAllPart;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (var item in m_legParts)
        {
            item.OnBreak += (legPart)=> { OnBreak(legPart); };
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }

  
    public void OnBreak(LegPartController legPart)
    {
        Debug.Log("ダメージ通知");
        if (legPart.IsBreaking)
        {
            if (m_legParts.All((legPart) => { return legPart.IsBreaking; }))
            {
                foreach (var item in m_legParts)
                {
                    item.LegModelObject.SetActive(false);
                }
            }
        }
    }
}
