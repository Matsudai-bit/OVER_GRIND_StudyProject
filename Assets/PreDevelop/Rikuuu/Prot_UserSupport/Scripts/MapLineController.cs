using UnityEngine;

public class MapLineController : MonoBehaviour
{
    // マップライン本体
    [SerializeField]
    private UnityEngine.UI.Image m_mapLineImage;

    // カーソル座標
    [SerializeField]
    private RectTransform m_cursorTransform;
    // マップライン基準座標
    [SerializeField]
    private RectTransform m_lineAncher;

    // 画像の元サイズ
    private Vector2 m_baseSize;
    private RectTransform m_lineRect;

    private void Awake()
    {
        m_lineRect = m_mapLineImage.rectTransform;
        m_baseSize = m_lineRect.rect.size;

        // 2点の中心に置いて、中心基準で拡縮・反転する
        m_lineRect.anchorMin = new Vector2(0.5f, 0.5f);
        m_lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        m_lineRect.pivot = new Vector2(0.5f, 0.5f);
    }

    private void LateUpdate()
    {
        Transform parent = m_lineRect.parent;

        // ワールド座標からラインの親のローカル座標に変換する
        Vector2 anchorPos = parent.InverseTransformPoint(m_lineAncher.position);
        Vector2 cursorPos = parent.InverseTransformPoint(m_cursorTransform.position);

        Vector2 diff = anchorPos - cursorPos;

        // 拡大率を求める
        float scaleX = Mathf.Abs(diff.x) / m_baseSize.x;
        float scaleY = Mathf.Abs(diff.y) / m_baseSize.y;

        // カーソルがAnchorより右/上に来たら反転する
        float flipX = diff.x >= 0.0f ? 1.0f : -1.0f;
        float flipY = diff.y >= 0.0f ? 1.0f : -1.0f;

        // 2点の中間に配置する
        m_lineRect.localPosition = (anchorPos + cursorPos) * 0.5f;
        m_lineRect.localScale = new Vector3(scaleX * flipX, scaleY * flipY, 1.0f);
    }
}