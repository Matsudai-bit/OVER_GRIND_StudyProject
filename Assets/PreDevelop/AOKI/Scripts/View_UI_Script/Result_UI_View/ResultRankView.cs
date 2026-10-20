using DG.Tweening;
using UnityEngine;

/// <summary>
/// クリアランク（CLEAR RANK）の GameObject 切替およびポップアニメーション演出を担当する View です。
/// </summary>
public class ResultRankView : MonoBehaviour
{
    [Header("表示UI設定")]
    [Tooltip("ランク別に切り替える GameObject の配列（[0]=S, [1]=A, [2]=B, [3]=C の順で登録）")]
    [SerializeField] private GameObject[] m_rankObjects;

    [Header("演出設定")]
    [Tooltip("ランク決定時にぽんと拡大表示するターゲット Transform（未指定時は自身）")]
    [SerializeField] private Transform m_popTarget;

    [Tooltip("ポップアニメーションの演出時間（秒）")]
    [SerializeField] private float m_popDuration = 0.4f;

    private Vector3 m_originalScale = Vector3.one;
    private Tween m_popTween;

    private void Awake()
    {
        if (m_popTarget == null)
        {
            m_popTarget = transform;
        }

        if (m_popTarget != null)
        {
            m_originalScale = m_popTarget.localScale;
        }
    }

    /// <summary>
    /// 指定されたランクの GameObject のみ表示（SetActive(true)）し、それ以外を非表示にします。
    /// </summary>
    public void DisplayRank(RankType rank)
    {
        int rankIndex = (int)rank;

        if (m_rankObjects == null || m_rankObjects.Length == 0) return;

        for (int i = 0; i < m_rankObjects.Length; i++)
        {
            if (m_rankObjects[i] != null)
            {
                // 該当するランクのインデックスだけ true にする
                m_rankObjects[i].SetActive(i == rankIndex);
            }
        }
    }

    /// <summary>
    /// ランク表示時のぽんっと浮き出るポップアニメーションを再生します。
    /// </summary>
    public void PlayPopAnimation()
    {
        if (m_popTarget == null) return;

        m_popTween?.Kill();

        // 一時的に縮小させてから、バウンド付きで元の大きさに拡大
        m_popTarget.localScale = m_originalScale * 0.2f;

        m_popTween = m_popTarget.DOScale(m_originalScale, m_popDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true); // timeScale = 0（ポーズ中）でも動作
    }

    private void OnDestroy()
    {
        m_popTween?.Kill();
    }
}