using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MapController : MonoBehaviour
{
    // 移動速度
    [SerializeField]
    private float MOVE_SPEED = 300.0f;

    // 倍率変化速度
    [SerializeField]
    private float ZOOM_SPEED = 1.0f;
    // 最小縮小倍率
    [SerializeField]
    private float MIN_ZOOM = 0.5f;
    // 最大拡大倍率
    [SerializeField]
    private float MAX_ZOOM = 3.0f;

    // マップイメージコンポーネント
    [SerializeField] 
    private UnityEngine.UI.Image m_mapImage;
    // 倍率を表示するテキストコンポーネント
    [SerializeField]
    private TextMeshProUGUI m_magnificationText;

    // 枠 Width/Height固定
    [SerializeField]
    private RectTransform m_frameRect;

    [Header("入力判定関連")]
    // 移動キーが押される判定
    [SerializeField]
    private InputActionReference m_navigateActionRef;
    // ズームインキーが押される判定
    [SerializeField]
    private InputActionReference m_zoomInRef;
    // ズームアウトキーが押される判定
    [SerializeField]
    private InputActionReference m_zoomOutRef;

    // 現在の倍率
    private float m_currentZoom = 1.0f;  

    private void OnEnable()
    {
        // 有効にする
        m_navigateActionRef?.action.Enable();
    }

    private void OnDisable()
    {
        // 無効にする
        m_navigateActionRef?.action.Disable();
    }

    void Update()
    {
        // 移動
        Vector2 move = m_navigateActionRef?.action.ReadValue<Vector2>() ?? Vector2.zero;
        if (move != Vector2.zero)
        {
            MoveMap(move);
        }

        // 拡大する
        if (m_zoomInRef != null &&
           m_zoomInRef.action.IsPressed())
        {
            Zoom(ZOOM_SPEED);
        }
        // 縮小する
        if (m_zoomOutRef != null &&
            m_zoomOutRef.action.IsPressed())
        {
            Zoom(-ZOOM_SPEED);
        }
    }

    private void Zoom(float zoomSpeed)
    {
        // 必要な参照がなければ何もしない
        if (m_mapImage == null || m_frameRect == null)
        {
            return;
        }

        float oldZoom = m_currentZoom;
        m_currentZoom = Mathf.Clamp(m_currentZoom + zoomSpeed * Time.deltaTime, MIN_ZOOM, MAX_ZOOM);

        // 上限・下限で倍率が変わらなかった場合は何もしない
        if (Mathf.Approximately(oldZoom, m_currentZoom))
        {
            return;
        }

        ApplyZoomAroundFrameCenter(m_currentZoom / oldZoom);

        // 倍率を表示する
        if (m_magnificationText != null)
        {
            m_magnificationText.text = "×" + m_currentZoom.ToString("F1");
        }
    }

    private void ApplyZoomAroundFrameCenter(float ratio)
    {
        RectTransform imgRect = m_mapImage.rectTransform;
        RectTransform parentRect = imgRect.parent as RectTransform;
        if (parentRect == null)
        {
            return;
        }

        // 枠の中心を、画像の親のローカル座標に変換する
        Vector3 frameCenterWorld = m_frameRect.TransformPoint(m_frameRect.rect.center);
        Vector2 frameCenter = parentRect.InverseTransformPoint(frameCenterWorld);

        // 枠の中心から見た画像pivotの位置を、倍率の比率分だけ伸縮する
        Vector2 pivotPos = imgRect.localPosition;
        Vector2 newPos = frameCenter + (pivotPos - frameCenter) * ratio;

        imgRect.localPosition = new Vector3(newPos.x, newPos.y, imgRect.localPosition.z);
        imgRect.localScale = new Vector3(m_currentZoom, m_currentZoom, 1f);
    }

    private void MoveMap(Vector2 move)
    {
        RectTransform imgRect = m_mapImage.rectTransform;

        // 移動量（Time.deltaTime を使って滑らかに）
        Vector2 delta = move * MOVE_SPEED * Time.deltaTime;

        imgRect.anchoredPosition -= delta;
    }
}
