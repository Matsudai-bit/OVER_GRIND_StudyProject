using System.Collections;
using UnityEngine;

/// <summary>
/// 爆発エフェクトの有効時間を管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class ExplosionController : MonoBehaviour
{
    // 爆発オブジェクトを無効化するまでの時間
    [SerializeField, Header("有効時間"), Min(0.0f)]
    private float m_activeDuration = 2.0f;

    // 無効化待機Coroutine
    private Coroutine m_disableCoroutine;

    /// <summary>
    /// 爆発オブジェクトが有効になったときに
    /// 無効化タイマーを開始します。
    /// </summary>
    private void OnEnable()
    {
        StartDisableTimer();
    }

    /// <summary>
    /// 無効化タイマーを開始します。
    /// </summary>
    public void StartDisableTimer()
    {
        if (m_disableCoroutine != null)
        {
            StopCoroutine(
                m_disableCoroutine);
        }

        m_disableCoroutine =
            StartCoroutine(
                DisableCoroutine());
    }

    /// <summary>
    /// 指定時間経過後に自身を無効化します。
    /// </summary>
    /// <returns>Coroutine。</returns>
    private IEnumerator DisableCoroutine()
    {
        if (m_activeDuration > 0.0f)
        {
            yield return new WaitForSeconds(
                m_activeDuration);
        }

        m_disableCoroutine =
            null;

        gameObject.SetActive(
            false);
    }

    /// <summary>
    /// 無効化時にCoroutine参照をクリアします。
    /// </summary>
    private void OnDisable()
    {
        if (m_disableCoroutine == null)
        {
            return;
        }

        StopCoroutine(
            m_disableCoroutine);

        m_disableCoroutine =
            null;
    }
}