using UnityEngine;

public class ParentButton : MonoBehaviour
{
    [Header("親ボタン")]
    [SerializeField]
    private RectTransform m_parent;

    [Header("子ボタン")]
    [SerializeField]
    private ChildButton[] m_childButtons;

    [Header("子ボタンセレクター")]
    [SerializeField]
    private ButtonSelector m_childButtonSelector;
    
    [Header("自分より下に配置された親ボタン")]
    [SerializeField]
    private ParentButton[] m_underParentButtons;

    [Header("親ボタンセレクター")]
    [SerializeField]
    private ButtonSelector m_parentButtonSelector;

    [Header("子ボタンの配置に関するデータ")]
    [SerializeField]
    private float m_paddingTop = 0.0f;

    // 展開前の位置（ローカル座標）
    private Vector3[] m_childOriginalPositions;
    private Vector3[] m_underOriginalPositions;

    // 展開中かどうか
    private bool m_isExpanded;

    private void Awake()
    {
        // 子ボタンを非表示にする
        SetChildActive(false);
    }

    public void OnClick()
    {
        if (m_isExpanded)
        {
            return;
        }

        // 子ボタンを表示する
        SetChildActive(true);

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
            Vector3 bottomWorld = m_parent.TransformPoint(
                new Vector3(0.0f, m_parent.rect.yMin, 0.0f));

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

        // 子ボタンセレクターのロックを解除する
        m_childButtonSelector.UnLockCursor();
        // 親ボタンセレクターをロックする
        m_parentButtonSelector.LockCursor();

        m_isExpanded = true;
    }

    public void OnCursorExit()
    {
        if (!m_isExpanded)
        {
            return;
        }

        // 子ボタンを非表示にする
        SetChildActive(false);
        // 子ボタンセレクターをロックする
        m_childButtonSelector.LockCursor();
        // 親ボタンセレクターのロックを解除する
        m_parentButtonSelector.UnLockCursor();

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

    // 子ボタンと子ボタンセレクターの表示・非表示を切り替える
    private void SetChildActive(bool isActive)
    {
        for (int i = 0; i < m_childButtons.Length; i++)
        {
            if (m_childButtons[i] != null)
            {
                SetVisible(m_childButtons[i].gameObject, isActive);
            }
        }

        // 子ボタンセレクター：SetActiveで切り替える
        if (m_childButtonSelector != null)
        {
            m_childButtonSelector.gameObject.SetActive(isActive);
        }
    }

    private static void SetVisible(GameObject target, bool isVisible)
    {
        CanvasGroup group = target.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = target.AddComponent<CanvasGroup>();
        }

        group.alpha = isVisible ? 1.0f : 0.0f;
        group.interactable = isVisible;
        group.blocksRaycasts = isVisible;
    }

    public RectTransform GetRectTransform()
    {
        return m_parent;
    }
}