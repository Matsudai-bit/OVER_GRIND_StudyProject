using UnityEngine;

public class ChildButton : MonoBehaviour
{
    // RectTransform
    private RectTransform m_rectTransform;

    private void Start()
    {
        // RectTransform�̎擾
        m_rectTransform = GetComponent<RectTransform>();
    }

    public RectTransform GetRectTransform()
    {
        return m_rectTransform;
    }
    public void SetRectTransform(RectTransform rectTransform)
    {
        m_rectTransform = rectTransform;
    }
}
