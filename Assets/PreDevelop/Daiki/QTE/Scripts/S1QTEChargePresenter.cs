using UnityEngine;

/// <summary>
/// QTEの進行状態を複数のQTE UIへ反映します。
/// </summary>
[DisallowMultipleComponent]
public sealed class S1QTEChargePresenter : MonoBehaviour
{
    [SerializeField, Header("参照")]
    private S1QTEController m_qteController;

    [SerializeField]
    private S1QTEChargeView[] m_views;

    private void OnEnable()
    {
        UpdateViews();
    }

    private void Update()
    {
        UpdateViews();
    }

    /// <summary>
    /// QTEの現在状態をすべてのViewへ反映します。
    /// </summary>
    private void UpdateViews()
    {
        if (m_qteController == null ||
            m_views == null)
        {
            return;
        }

        float progress =
            m_qteController.HoldProgress;

        bool isHolding =
            m_qteController.IsHolding;

        foreach (S1QTEChargeView view in m_views)
        {
            if (view == null)
            {
                continue;
            }

            view.SetProgress(
                progress,
                isHolding);
        }
    }
}