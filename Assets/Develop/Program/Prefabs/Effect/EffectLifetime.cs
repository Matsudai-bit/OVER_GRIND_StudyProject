using UnityEngine;

/// <summary>
/// 一時的なエフェクトの寿命を管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class EffectLifetime : MonoBehaviour
{
    [SerializeField, Header("寿命")]
    [Min(0.0f)]
    private float m_lifetime = 3.0f;

    // 経過時間
    private float m_elapsedTime;

    private void OnEnable()
    {
        m_elapsedTime = 0.0f;
    }

    private void Update()
    {
        m_elapsedTime += Time.deltaTime;

        if (m_elapsedTime < m_lifetime)
        {
            return;
        }

        DestroyEffect();
    }

    /// <summary>
    /// エフェクトを破棄します。
    /// </summary>
    private void DestroyEffect()
    {
        Destroy(gameObject);
    }
}