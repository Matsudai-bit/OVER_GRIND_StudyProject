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

    void Zoom(float zoomSpeed)
    {
        m_currentZoom += zoomSpeed * Time.deltaTime;
        m_currentZoom = Mathf.Clamp(m_currentZoom, MIN_ZOOM, MAX_ZOOM);

        m_mapImage.rectTransform.localScale = new Vector3(m_currentZoom, m_currentZoom, 1f);

        // 倍率を表示する
        m_magnificationText.text = "×" + m_currentZoom.ToString("F1");
    }

    private void MoveMap(Vector2 move)
    {
        RectTransform imgRect = m_mapImage.rectTransform;

        // 移動量（Time.deltaTime を使って滑らかに）
        Vector2 delta = move * MOVE_SPEED * Time.deltaTime;

        imgRect.anchoredPosition += delta;
    }
}
