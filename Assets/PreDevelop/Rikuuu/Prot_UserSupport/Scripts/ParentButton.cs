using UnityEngine;

public class ParentButton : MonoBehaviour
{
    [Header("親ボタン")]
    [SerializeField]
    private RectTransform m_parentButton;

    [Header("子ボタン")]
    [SerializeField]
    private ChildButton[] m_childButtons;

    [Header("自分より下に配置された親ボタン")]
    [SerializeField]
    private ParentButton[] m_underParentButtons;

    [Header("子ボタンの配置に関するデータ")]
    [SerializeField]
    private float m_paddingTop = 0.0f;

    // RectTransform
    private RectTransform m_rectTransform => m_parentButton;

    // 展開前の位置（ローカル座標）
    private Vector3[] m_childOriginalPositions;
    private Vector3[] m_underOriginalPositions;

    // 展開中かどうか
    private bool m_isExpanded;

    public void OnClick()
    {
        Debug.Log($"[ParentButton] OnClick expanded={m_isExpanded} self={name}", this);
        if (m_isExpanded)
        {
            return;
        }

        // 展開前の位置を保存（ここは今のまま残す）
        m_childOriginalPositions = new Vector3[m_childButtons.Length];
        for (int i = 0; i < m_childButtons.Length; i++)
        {
            m_childOriginalPositions[i] =
                m_childButtons[i].GetRectTransform().localPosition;
        }

        m_underOriginalPositions = new Vector3[m_underParentButtons.Length];
        for (int i = 0; i < m_underParentButtons.Length; i++)
        {
            m_underOriginalPositions[i] =
                m_underParentButtons[i].GetRectTransform().localPosition;
        }

        // ↓ここから貼り付けたコード
        if (m_childButtons.Length > 0)
        {
            // 親ボタンの下端をワールド座標で取得
            Vector3 bottomWorld = m_rectTransform.TransformPoint(
                new Vector3(0.0f, m_rectTransform.rect.yMin, 0.0f));

            // 子ボタンの親（Move）から見た座標に変換
            Transform childSpace = m_childButtons[0].GetRectTransform().parent;
            float nextTop = childSpace.InverseTransformPoint(bottomWorld).y - m_paddingTop;

            // 子ボタンが占める高さの合計
            float totalHeight = 0.0f;

            // 子ボタンを親の下端から順に並べる
            for (int i = 0; i < m_childButtons.Length; i++)
            {
                RectTransform child = m_childButtons[i].GetRectTransform();

                Vector3 pos = child.localPosition;
                pos.y = nextTop - child.rect.height * (1.0f - child.pivot.y);
                child.localPosition = pos;

                float step = child.rect.height + m_paddingTop;
                nextTop -= step;
                totalHeight += step;
            }

            // 自分より下のボタンを下げる
            for (int i = 0; i < m_underParentButtons.Length; i++)
            {
                RectTransform under = m_underParentButtons[i].GetRectTransform();

                Vector3 pos = under.localPosition;
                pos.y -= totalHeight;
                under.localPosition = pos;
            }
        }

        m_isExpanded = true;
    }

    public void OnCursorExit()
    {
        Debug.Log($"[ParentButton] OnCursorExit expanded={m_isExpanded} self={name}", this);
        if (!m_isExpanded)
        {
            return;
        }

        // 子ボタンを元の位置へ
        for (int i = 0; i < m_childButtons.Length; i++)
        {
            m_childButtons[i].GetRectTransform().localPosition =
                m_childOriginalPositions[i];
        }

        // 下のボタンを元の位置へ
        for (int i = 0; i < m_underParentButtons.Length; i++)
        {
            m_underParentButtons[i].GetRectTransform().localPosition =
                m_underOriginalPositions[i];
        }

        m_isExpanded = false;
    }

    // -------------------------------------------------------

    public RectTransform GetRectTransform()
    {
        return m_rectTransform;
    }
}