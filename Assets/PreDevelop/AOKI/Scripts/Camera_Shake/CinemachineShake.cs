using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Cinemachine Impulse 機能を利用してカメラの振動（シェイク）を制御するクラス。
/// </summary>
public class CinemachineShake : MonoBehaviour
{
    /// <summary>
    /// 振動の発生源となる Cinemachine Impulse Source コンポーネント。
    /// </summary>
    [Header("インパルス送信元")]
    [SerializeField]
    private CinemachineImpulseSource m_impulseSource;

    /// <summary>
    /// コンポーネントの初期化処理を行います。
    /// </summary>
    private void Awake()
    {
        // Inspector で未設定の場合、同一 GameObject から自動取得
        if (m_impulseSource == null)
        {
            m_impulseSource = GetComponent<CinemachineImpulseSource>();
        }
    }
    /// <summary>
    /// 指定された条件でカメラの振動（シェイク）を開始します。
    /// </summary>
    /// <param name="duration">振動の継続時間（秒）。</param>
    /// <param name="magnitude">振動の強さ（振れ幅）。</param>
    /// <param name="isOverwrite">
    /// true：既存の振動をクリアして上書きします。
    /// false：既存の振動に新しい振動を加算します。
    /// </param>
    public void Shake(float duration, float magnitude, bool isOverwrite = false)
    {
        // 安全のための null チェック
        if (m_impulseSource == null)
        {
            Debug.LogWarning("【CinemachineShake】CinemachineImpulseSource が設定されていません。");
            return;
        }

        // 上書き指定の場合、現在発生中のインパルスをクリア
        if (isOverwrite && CinemachineImpulseManager.Instance != null)
        {
            CinemachineImpulseManager.Instance.Clear();
        }

        // 振動の継続時間を設定
        m_impulseSource.ImpulseDefinition.ImpulseDuration = duration;

        // 指定した強さの倍率で衝撃を発生
        m_impulseSource.GenerateImpulseWithForce(magnitude);
    }

    /// <summary>
    /// 現在発生中の振動を即座に強制停止します。
    /// </summary>
    public void StopShake()
    {
        // 全てのインパルス振動を即座にクリア
        if (CinemachineImpulseManager.Instance != null)
        {
            CinemachineImpulseManager.Instance.Clear();
        }
    }
}