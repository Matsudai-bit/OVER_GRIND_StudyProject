using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// エフェクトを再生するクラス
/// </summary>
public class EffectPlayer : MonoBehaviour
{
    // EffectManager への参照
    [SerializeField]
    private EffectManager m_effectManager; 

    // エフェクトデータの配列
    [SerializeField]
    [EffectSelector("m_effectManager")]
    private EffectData[] m_selectedEffects;

    // 再生したエフェクトのハンドル
    private List<int> m_effectHandle = new List<int>();

    /// <summary>
    /// 初期化処理
    /// </summary>
    private void Start()
    {
        // エフェクトを再生し、ハンドルをリストに追加
        m_effectHandle.Add(
            m_effectManager.Play(
                m_selectedEffects[0].GetEffectName(), 
                transform.position
            )
        );
    }

    /// <summary>
    /// 更新処理
    /// </summary>
    private void Update()
    {
        // スペースキーが押されたら
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            // エフェクトの再生を停止
            EffectManager.Instance.Stop(m_effectHandle[0]);
        }
    }
}