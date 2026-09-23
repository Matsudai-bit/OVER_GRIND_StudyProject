using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public struct StageInfomation
{
    // ステージ座標
    public RectTransform rectTransform;

    // ステージ番号
    public int stageNumber;
    // セクター番号テクスチャ
    public Sprite stageSectorNumber;

    // ステージ名（日本語）
    public string stageNameJa;
    // ステージ名（英語）
    public string stageNameEng;

    // ベストタイム
    public int bestTime;

    // プレイ可能かどうか
    public bool canPlay;
}

/// <summary>
/// ステージ選択画面の管理
/// カーソルがステージの座標に乗っているかを判定する
/// </summary>
public class StageSelectManager : MonoBehaviour
{
    // 乗っているステージがないことを表すインデックス
    private const int NO_HOVER = -1;

    // カーソルの衝突検知範囲
    [SerializeField]
    private float HIT_RADIUS = 40.0f;

    // ステージポイントへの吸着
    [Header("ステージポイントへの吸着")]
    // 吸着が始まる距離
    [SerializeField]
    private float SNAP_RADIUS = 80.0f;
    // 吸着を解除する距離
    [SerializeField]
    private float RELEASE_RADIUS = 150.0f;
    // 吸着が完了したとみなす距離
    [SerializeField]
    private float SNAP_COMPLETE_THRESHOLD = 2.0f;
    // 吸着の滑らかさ
    [SerializeField]
    private float SNAP_SPEED = 10.0f;

    // マップ中央に配置されたカーソル座標
    [Header("カーソル")]
    [SerializeField]
    private RectTransform m_cursor;


    // ステージのある座標
    [Header("ステージの情報")]
    [SerializeField]
    private StageInfomation[] m_stagePoints;

    [Header("デフォルト値")]
    // セクター番号の通常時テクスチャ
    [SerializeField]
    private Sprite m_sectorDefaultTexture;

    // ステージ情報を表示するコンポーネント
    [Header("ステージ情報を表示するコンポーネント")]
    // ステージ番号
    [SerializeField]
    private TextMeshProUGUI m_stageNumber;
    // セクター番号
    [SerializeField]
    private UnityEngine.UI.Image m_sectorNumber;

    // ステージ名（日本語）
    [SerializeField]
    private TextMeshProUGUI m_stageNameJa;
    // ステージ名（英語）
    [SerializeField]
    private TextMeshProUGUI[] m_stageNameEng;

    // ベストタイム
    [SerializeField]
    private TextMeshProUGUI m_bestTime;


    // カーソルの衝突の有無で表示非表示を切り替えるオブジェクト
    [Header("ステージ情報を表示するコンポーネント")]
    // カーソルの乗っているときのイメージコンポーネント
    [SerializeField]
    private UnityEngine.UI.Image m_onCursorImage;
    // 矢印線のイメージコンポーネント
    [SerializeField]
    private UnityEngine.UI.Image m_arrowLineImage;


    [Header("READYボタン")]
    [SerializeField]
    private StageSelectButton m_readyButton;


    [Header("入力判定関連")]
    // 決定キーが押される判定
    [SerializeField]
    private InputActionReference m_enterActionRef;


    [Header("マップ")]
    [SerializeField]
    private MapController m_mapController;

    // 現在カーソルが乗っているステージのインデックス
    private int m_hoveredIndex = NO_HOVER;
    // カーソルがいずれかのステージに乗っているかどうか
    public bool IsHovering => m_hoveredIndex != NO_HOVER;
    // 現在吸着中のステージのインデックス
    private int m_snappedIndex = NO_HOVER;
    // 吸着が完了し、既に中心へ到達済みかどうか
    private bool m_isSnapCompleted = false;

    private void Start()
    {
        ResetConmornent();
    }

    private void LateUpdate()
    {
        // 判定の前にカーソルを吸着させる
        UpdateCursorSnap();

        // マップの移動・拡大縮小（Update）の後で判定する
        int hoveredIndex = FindHoveredIndex();

        // 変化がなければ何もしない
        if (hoveredIndex == m_hoveredIndex)
        {
            return;
        }

        m_hoveredIndex = hoveredIndex;

        // 変化があったときのみ表示する
        if (IsHovering)
        {
            Debug.Log("カーソルが乗った: " + m_stagePoints[m_hoveredIndex].rectTransform.name);

            // ステージ番号の表示
            m_stageNumber.text = "STAGE-" + m_stagePoints[m_hoveredIndex].stageNumber.ToString();
            // セクター番号の表示
            m_sectorNumber.sprite = m_stagePoints[m_hoveredIndex].stageSectorNumber;

            // ステージ名の表示
            m_stageNameJa.text = m_stagePoints[m_hoveredIndex].stageNameJa;
            foreach (var stageName in m_stageNameEng)
            {
                stageName.text = m_stagePoints[m_hoveredIndex].stageNameEng;
            }

            // ベストタイムの表示
            int bestTime = m_stagePoints[m_hoveredIndex].bestTime;
            int minutes = bestTime / 60;
            float seconds = bestTime - (minutes * 60);

            string timeText = minutes.ToString("00") + ":" + seconds.ToString("00.00");
            m_bestTime.text = timeText;

            // イメージコンポーネントの表示
            m_onCursorImage.enabled = true;
            m_arrowLineImage.enabled = true;

            // プレイできる場合
            if (m_stagePoints[m_hoveredIndex].canPlay) 
            {
                // ボタンにカーソルが合っている状態にする
                m_readyButton.OnCursor();
            }
        }
        else
        {
            Debug.Log("カーソルが離れた");

            ResetConmornent();
        }

        // カーソルが合わさっていたら
        if(m_readyButton.m_isOnCursor)
        {
            // 決定キーが押されたら
            if (m_enterActionRef != null && m_enterActionRef.action.WasPressedThisFrame())
            {
                // キーが押されたときの処理を実行する
                m_readyButton.OnClick();
            }
            // 決定ボタンが離されたら
            if (m_enterActionRef != null && m_enterActionRef.action.WasReleasedThisFrame())
            {
                // キーが離されたときの処理を実行する
                m_readyButton.OnClickExit();
            }
        }
    }

    /// <summary>
    /// カーソルに最も近いステージのインデックスを返す
    /// 衝突検知範囲内になければ-1を返す
    /// </summary>
    private int FindHoveredIndex()
    {
        // カーソル、またはステージ情報が設定されていない場合
        if (m_cursor == null || m_stagePoints == null)
        {
            return NO_HOVER;
        }

        int hoveredIndex = NO_HOVER;
        float nearestDistance = HIT_RADIUS;

        for (int i = 0; i < m_stagePoints.Length; i++)
        {
            if (m_stagePoints[i].rectTransform == null)
            {
                continue;
            }

            // ステージの座標をカーソルのローカル座標に変換して距離を測る
            // （Canvasの拡大率やマップの拡大縮小の影響を受けない）
            Vector2 localPos = m_cursor.InverseTransformPoint(m_stagePoints[i].rectTransform.position);
            float distance = localPos.magnitude;

            if (distance <= nearestDistance)
            {
                nearestDistance = distance;
                hoveredIndex = i;
            }
        }

        return hoveredIndex;
    }

    private void UpdateCursorSnap()
    {
        if (m_cursor == null || m_stagePoints == null || m_mapController == null)
        {
            return;
        }

        // 既に何かへ吸着している場合
        if (m_snappedIndex != NO_HOVER)
        {
            if (m_stagePoints[m_snappedIndex].rectTransform == null)
            {
                // 参照が失われていたら吸着を解除する
                m_snappedIndex = NO_HOVER;
                m_isSnapCompleted = false;
            }
            else
            {
                Vector2 offset = m_cursor.InverseTransformPoint(m_stagePoints[m_snappedIndex].rectTransform.position);
                float distance = offset.magnitude;

                // 解除距離を超えて離れたら吸着を解除し、新規探索へ進む
                if (distance > RELEASE_RADIUS)
                {
                    m_snappedIndex = NO_HOVER;
                    m_isSnapCompleted = false;
                }
                // まだ中心に到達していない間だけ、引き寄せる力を加える
                else if (!m_isSnapCompleted)
                {
                    if (distance <= SNAP_COMPLETE_THRESHOLD)
                    {
                        // 中心へ到達したので、以降は力を加えるのをやめる
                        m_isSnapCompleted = true;
                    }
                    else
                    {
                        Vector2 moveDelta = -offset * SNAP_SPEED * Time.deltaTime;
                        m_mapController.PanMapBy(moveDelta);
                    }
                    return;
                }
                else
                {
                    // 到達済みの間はプレイヤーの移動を一切妨げない（解除判定のみ）
                    return;
                }
            }
        }

        // 吸着していない場合のみ、範囲内の最も近いステージを新たに探す
        int nearestIndex = NO_HOVER;
        float nearestDistance = SNAP_RADIUS;
        Vector2 nearestOffset = Vector2.zero;

        for (int i = 0; i < m_stagePoints.Length; i++)
        {
            if (m_stagePoints[i].rectTransform == null)
            {
                continue;
            }

            Vector2 offset = m_cursor.InverseTransformPoint(m_stagePoints[i].rectTransform.position);
            float distance = offset.magnitude;

            if (distance <= nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
                nearestOffset = offset;
            }
        }

        // 新たに範囲内に入ったステージがあれば吸着を開始する
        if (nearestIndex != NO_HOVER)
        {
            m_snappedIndex = nearestIndex;
            m_isSnapCompleted = false;

            Vector2 moveDelta = -nearestOffset * SNAP_SPEED * Time.deltaTime;
            m_mapController.PanMapBy(moveDelta);
        }
    }

    private void ResetConmornent()
    {
        // ステージ番号の初期化
        m_stageNumber.text = "";
        // セクター番号の初期化
        m_sectorNumber.sprite = m_sectorDefaultTexture;

        // ステージ名の初期化
        m_stageNameJa.text = "";
        foreach (var stageName in m_stageNameEng)
        {
            stageName.text = "";
        }

        // ベストタイムの初期化
        m_bestTime.text = "00:00.00";

        // イメージコンポーネントの非表示
        m_onCursorImage.enabled = false;
        m_arrowLineImage.enabled = false;

        // ボタンにカーソルが合っていない状態にする
        m_readyButton.OnCursorExit();
    }
}