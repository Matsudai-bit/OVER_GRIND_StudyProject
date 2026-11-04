using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// マップの移動・拡大縮小を管理する
/// カーソルは画像内を自由に移動し、フレームはカーソルに追従する
/// 画像が端に到達した場合は、それ以上フレームを追従させない
/// </summary>
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

    [Header("カーソルの傾き挙動")]
    // スティックを倒したときにカーソルが寄る最大距離
    [SerializeField]
    private float CURSOR_TILT_DISTANCE = 20.0f;
    // 傾きオフセットが目標へ追従する速さ(大きいほど素早い)
    [SerializeField]
    private float CURSOR_TILT_SPEED = 12.0f;

    // マップイメージコンポーネント
    [SerializeField]
    private Image m_mapImage;
    // 倍率を表示するテキストコンポーネント
    [SerializeField]
    private TextMeshProUGUI m_magnificationText;

    // 枠 Width/Height固定
    [SerializeField]
    private RectTransform m_frameRect;

    // マップ中央に配置されたカーソル座標
    [Header("カーソル")]
    [SerializeField]
    private RectTransform m_cursorRect;

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
    // 画像の初期Scale（Inspectorで設定された値）
    private Vector3 m_baseScale = Vector3.one;

    // カーソルの基準位置（地図座標が(0,0)のときの表示位置）
    private Vector2 m_cursorHomePosition;
    // カーソルの地図上の座標（画像の中心からのオフセット、画像の等倍(Scale=1)でのローカル座標）
    private Vector2 m_cursorMapPosition = Vector2.zero;

    // スティック入力によるカーソルの見た目上のずれ
    private Vector2 m_cursorTiltOffset = Vector2.zero;
    // 外部(吸着処理)から参照するための公開プロパティ
    public Vector2 CursorTilt => m_cursorTiltOffset;

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

    private void Start()
    {
        // 画像の初期Scaleを記録する
        if (m_mapImage != null)
        {
            m_baseScale = m_mapImage.rectTransform.localScale;
        }

        // カーソルの基準位置を記録する
        if (m_cursorRect != null)
        {
            m_cursorHomePosition = m_cursorRect.anchoredPosition;
        }

        // 初期状態の表示を反映する
        ApplyCursorAndImagePosition();
    }

    private void Update()
    {
        Vector2 move = m_navigateActionRef?.action.ReadValue<Vector2>() ?? Vector2.zero;

        // 傾きオフセットを更新する
        bool tiltChanged = UpdateCursorTilt(move);

        if (move != Vector2.zero)
        {
            // 内部で ApplyCursorAndImagePosition() が呼ばれる
            MoveCursorOnMap(move);
        }
        else if (tiltChanged)
        {
            // 移動はないが、傾きが戻っている最中は表示を更新する
            ApplyCursorAndImagePosition();
        }

        // (ズーム処理は今まで通り)
        if (m_zoomInRef != null && m_zoomInRef.action.IsPressed())
        {
            Zoom(ZOOM_SPEED);
        }
        if (m_zoomOutRef != null && m_zoomOutRef.action.IsPressed())
        {
            Zoom(-ZOOM_SPEED);
        }
    }

    /// <summary>
    /// スティックの倒した方向へカーソルを少し寄せ、離すと中央へ戻す
    /// </summary>
    /// <returns>オフセットが変化したかどうか</returns>
    private bool UpdateCursorTilt(Vector2 move)
    {
        // 斜め入力でも最大距離を超えないようにする
        Vector2 target = Vector2.ClampMagnitude(move, 1.0f) * CURSOR_TILT_DISTANCE;

        // フレームレートに依存しない補間
        float t = 1.0f - Mathf.Exp(-CURSOR_TILT_SPEED * Time.deltaTime);
        Vector2 next = Vector2.Lerp(m_cursorTiltOffset, target, t);

        // 十分小さくなったら完全に0へ戻す(無駄な更新を止める)
        if (target == Vector2.zero && next.sqrMagnitude < 0.01f)
        {
            next = Vector2.zero;
        }

        bool changed = next != m_cursorTiltOffset;
        m_cursorTiltOffset = next;
        return changed;
    }

    /// <summary>
    /// マップを拡大縮小する（枠の中心を基準にする）
    /// </summary>
    /// <param name="zoomSpeed">倍率の変化速度（縮小は負の値）</param>
    private void Zoom(float zoomSpeed)
    {
        m_currentZoom = Mathf.Clamp(m_currentZoom + zoomSpeed * Time.deltaTime, MIN_ZOOM, MAX_ZOOM);

        // 初期Scaleを基準に拡大縮小する
        m_mapImage.rectTransform.localScale = m_baseScale * m_currentZoom;

        // 倍率を表示する
        m_magnificationText.text = "×" + m_currentZoom.ToString("F1");

        // カーソルの地図上座標を基準に、画像とカーソルの表示位置を再計算する
        // （effectiveScaleが変わるため、自動的にカーソルの位置を中心とした拡大縮小になる）
        ApplyCursorAndImagePosition();
    }

    /// <summary>
    /// 入力分だけカーソルの地図上の座標を動かし、表示を更新する
    /// </summary>
    private void MoveCursorOnMap(Vector2 move)
    {
        if (m_mapImage == null)
        {
            return;
        }

        // 入力量を、画像の等倍(Scale=1)でのローカル座標に変換する
        Vector2 delta = move * MOVE_SPEED * Time.deltaTime;
        Vector2 effectiveScale = GetEffectiveScale();
        m_cursorMapPosition += new Vector2(delta.x / effectiveScale.x, delta.y / effectiveScale.y);

        // 画像の範囲内にカーソル位置を収める
        m_cursorMapPosition = ClampCursorMapPosition(m_cursorMapPosition);

        ApplyCursorAndImagePosition();
    }

    /// <summary>
    /// カーソルの地図上の座標を、画像の範囲内に収める
    /// </summary>
    private Vector2 ClampCursorMapPosition(Vector2 mapPosition)
    {
        if (m_mapImage == null)
        {
            return mapPosition;
        }

        // 画像の等倍(Scale=1)でのサイズの半分が、カーソルの動ける範囲
        Vector2 halfImageSize = m_mapImage.rectTransform.rect.size * 0.5f;

        float clampedX = Mathf.Clamp(mapPosition.x, -halfImageSize.x, halfImageSize.x);
        float clampedY = Mathf.Clamp(mapPosition.y, -halfImageSize.y, halfImageSize.y);

        return new Vector2(clampedX, clampedY);
    }

    /// <summary>
    /// カーソルの地図上の座標から、画像位置とカーソルの表示位置を計算して反映する
    /// 画像は、カーソルが枠の中心に来るように追従する
    /// 画像が端に到達した場合は追従できないため、その分カーソルが中心からずれて表示される
    /// </summary>
    private void ApplyCursorAndImagePosition()
    {
        if (m_mapImage == null || m_frameRect == null || m_cursorRect == null)
        {
            return;
        }

        Vector2 effectiveScale = GetEffectiveScale();

        // カーソルを枠の中心に表示するために必要な画像位置
        Vector2 desiredImagePos = -Vector2.Scale(m_cursorMapPosition, effectiveScale);

        // 画像がフレームをはみ出せる範囲でクランプする（端に到達したらそれ以上動かさない）
        RectTransform imgRect = m_mapImage.rectTransform;
        imgRect.anchoredPosition = ClampToImageBounds(desiredImagePos);

        // 画像が追従しきれなかった分だけ、カーソルが枠の中心からずれる
        m_cursorRect.anchoredPosition =
            m_cursorHomePosition + 
            Vector2.Scale(m_cursorMapPosition, effectiveScale) + 
            imgRect.anchoredPosition + 
            m_cursorTiltOffset;
    }

    /// <summary>
    /// 画像の位置を、枠がフチからはみ出ない範囲に収める
    /// </summary>
    private Vector2 ClampToImageBounds(Vector2 position)
    {
        if (m_mapImage == null || m_frameRect == null)
        {
            return position;
        }

        RectTransform imgRect = m_mapImage.rectTransform;

        // 画像の4隅のワールド座標から、実際の見た目のサイズを求める
        Vector3[] corners = new Vector3[4];
        imgRect.GetWorldCorners(corners);

        Vector2 bottomLeft = m_frameRect.InverseTransformPoint(corners[0]);
        Vector2 topRight = m_frameRect.InverseTransformPoint(corners[2]);
        Vector2 imageSize = topRight - bottomLeft;

        Vector2 frameSize = m_frameRect.rect.size;

        // 画像がフレームよりはみ出せる範囲の半分（はみ出し量の上限）
        Vector2 maxOffset = Vector2.Max((imageSize - frameSize) * 0.5f, Vector2.zero);

        float clampedX = Mathf.Clamp(position.x, -maxOffset.x, maxOffset.x);
        float clampedY = Mathf.Clamp(position.y, -maxOffset.y, maxOffset.y);

        return new Vector2(clampedX, clampedY);
    }

    /// <summary>
    /// 現在の実際のScale（初期Scale × 拡大縮小倍率）を返す
    /// </summary>
    private Vector2 GetEffectiveScale()
    {
        return (Vector2)m_baseScale * m_currentZoom;
    }

    /// <summary>
    /// 外部（吸着処理など）からカーソルを直接移動させる
    /// 移動後は、画像の範囲内に収める
    /// </summary>
    /// <param name="delta">移動量（フレーム座標系）</param>
    public void PanMapBy(Vector2 delta)
    {
        if (m_mapImage == null)
        {
            return;
        }

        Vector2 effectiveScale = GetEffectiveScale();
        m_cursorMapPosition -= new Vector2(delta.x / effectiveScale.x, delta.y / effectiveScale.y);
        m_cursorMapPosition = ClampCursorMapPosition(m_cursorMapPosition);

        ApplyCursorAndImagePosition();
    }
}