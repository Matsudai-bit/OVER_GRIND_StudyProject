using UnityEngine;
using DG.Tweening;

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

    [SerializeField] 
    private float m_expandDuration = 0.25f;
    [SerializeField] 
    private Ease m_expandEase = Ease.OutCubic;

    // 展開前の位置（ローカル座標）
    private Vector3[] m_childOriginalPositions;
    private Vector3[] m_underOriginalPositions;

    // 展開中かどうか
    private bool m_isExpanded;
    // 収縮アニメーション中かどうか
    private bool m_isCollapsing;

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

        // 収縮中なら、非表示にするDelayedCallを止める
        if (m_isCollapsing)
        {
            DOTween.Kill(this);
            m_isCollapsing = false;
        }
        else
        {
            // 展開前の位置を保存（収縮中は保存済みの値を使うので上書きしない）
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
        }

        // 子ボタンを表示する
        SetChildActive(true);

        if (m_childButtons.Length > 0)
        {
            Vector3 bottomWorld = m_parent.TransformPoint(
                new Vector3(0.0f, m_parent.rect.yMin, 0.0f));

            Transform childSpace = m_childButtons[0].GetRectTransform().parent;
            float nextTop = childSpace.InverseTransformPoint(bottomWorld).y - m_paddingTop;

            float totalHeight = 0.0f;

            // 子ボタン：目標位置を計算して、そこへ移動させる
            for (int i = 0; i < m_childButtons.Length; i++)
            {
                RectTransform child = m_childButtons[i].GetRectTransform();

                // 前回のTweenが残っていたら止める
                child.DOKill();

                Vector3 target = m_childOriginalPositions[i];
                target.y = nextTop - child.rect.height * (1.0f - child.pivot.y);

                // 親ボタンの位置から滑り出すようにしたい場合は、先に開始位置をセットする
                // Vector3 start = target;
                // start.y = <親ボタン下端のy>;
                // child.localPosition = start;

                child.DOLocalMove(target, m_expandDuration).SetEase(m_expandEase);

                float step = child.rect.height + m_paddingTop;
                nextTop -= step;
                totalHeight += step;
            }

            // 下のボタン：下げる
            for (int i = 0; i < m_underParentButtons.Length; i++)
            {
                RectTransform under = m_underParentButtons[i].GetRectTransform();
                under.DOKill();

                Vector3 target = m_underOriginalPositions[i];
                target.y -= totalHeight;

                under.DOLocalMove(target, m_expandDuration).SetEase(m_expandEase);
            }
        }

        m_childButtonSelector.UnLockCursor();
        m_parentButtonSelector.LockCursor();

        m_isExpanded = true;
    }

    public void OnCursorExit()
    {
        if (!m_isExpanded)
        {
            return;
        }

        m_isExpanded = false;
        m_isCollapsing = true;

        // セレクターの切り替えは先に行う
        m_childButtonSelector.LockCursor();
        m_parentButtonSelector.UnLockCursor();

        // 子ボタンを元の位置へ
        for (int i = 0; i < m_childButtons.Length; i++)
        {
            RectTransform child = m_childButtons[i].GetRectTransform();
            child.DOKill();
            child.DOLocalMove(m_childOriginalPositions[i], m_expandDuration)
                .SetEase(m_expandEase);
        }

        // 下のボタンを元の位置へ
        for (int i = 0; i < m_underParentButtons.Length; i++)
        {
            RectTransform under = m_underParentButtons[i].GetRectTransform();
            under.DOKill();
            under.DOLocalMove(m_underOriginalPositions[i], m_expandDuration)
                .SetEase(m_expandEase);
        }

        // 戻り終わったら子ボタンを非表示にする
        DOVirtual.DelayedCall(m_expandDuration, () =>
        {
            if (!m_isCollapsing)
            {
                return;
            }

            SetChildActive(false);
            m_isCollapsing = false;
        }).SetId(this);
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