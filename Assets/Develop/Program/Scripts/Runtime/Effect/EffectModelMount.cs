using UnityEngine;

/// <summary>モデル格納用Transformの直下にモデルが1つある場合、交換を検出して再バインドします。</summary>
[DisallowMultipleComponent]
public sealed class EffectModelMount : MonoBehaviour
{
    [SerializeField, Header("エフェクト発生元")]
    private EffectEmitter m_emitter;
    private Transform m_lastModel;

    /// <summary>起動時にモデルを登録します。</summary>
    private void Start()
    {
        DetectModelChange();
    }

    /// <summary>子モデルの追加・除去時に再バインドします。</summary>
    private void OnTransformChildrenChanged()
    {
        if (Application.isPlaying)
        {
            DetectModelChange();
        }
    }

    /// <summary>子オブジェクトが唯一の場合にモデルルートを更新します。</summary>
    private void DetectModelChange()
    {
        if (m_emitter == null || transform.childCount != 1)
        {
            return;
        }
        Transform model = transform.GetChild(0);
        if (model == m_lastModel)
        {
            return;
        }
        m_lastModel = model;
        m_emitter.SetModelRoot(model);
    }
}
