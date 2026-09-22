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


    // マップ中央に配置されたカーソル座標
    [Header("カーソル")]
    [SerializeField]
    private RectTransform m_cursor;


    // ステージのある座標
    [Header("ステージの情報")]
    [SerializeField]
    private StageInfomation[] m_stagePoint;

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

    // 現在カーソルが乗っているステージのインデックス
    private int m_hoveredIndex = NO_HOVER;
    // カーソルがいずれかのステージに乗っているかどうか
    public bool IsHovering => m_hoveredIndex != NO_HOVER;
    // カーソルが乗っているステージのインデックス（乗っていない場合は-1）
    public int HoveredIndex => m_hoveredIndex;

    private void Start()
    {
        ResetConmornent();
    }

    private void LateUpdate()
    {
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
            Debug.Log("カーソルが乗った: " + m_stagePoint[m_hoveredIndex].rectTransform.name);

            // ステージ番号の表示
            m_stageNumber.text = "STAGE-" + m_stagePoint[m_hoveredIndex].stageNumber.ToString();
            // セクター番号の表示
            m_sectorNumber.sprite = m_stagePoint[m_hoveredIndex].stageSectorNumber;

            // ステージ名の表示
            m_stageNameJa.text = m_stagePoint[m_hoveredIndex].stageNameJa;
            foreach (var stageName in m_stageNameEng)
            {
                stageName.text = m_stagePoint[m_hoveredIndex].stageNameEng;
            }

            // ベストタイムの表示
            int bestTime = m_stagePoint[m_hoveredIndex].bestTime;
            int minutes = bestTime / 60;
            float seconds = bestTime - (minutes * 60);

            string timeText = minutes.ToString("00") + ":" + seconds.ToString("00.00");
            m_bestTime.text = timeText;

            // イメージコンポーネントの表示
            m_onCursorImage.enabled = true;
            m_arrowLineImage.enabled = true;

            // プレイできる場合
            if (m_stagePoint[m_hoveredIndex].canPlay)
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
        if (m_cursor == null || m_stagePoint == null)
        {
            return NO_HOVER;
        }

        int hoveredIndex = NO_HOVER;
        float nearestDistance = HIT_RADIUS;

        for (int i = 0; i < m_stagePoint.Length; i++)
        {
            if (m_stagePoint[i].rectTransform == null)
            {
                continue;
            }

            // ステージの座標をカーソルのローカル座標に変換して距離を測る
            // （Canvasの拡大率やマップの拡大縮小の影響を受けない）
            Vector2 localPos = m_cursor.InverseTransformPoint(m_stagePoint[i].rectTransform.position);
            float distance = localPos.magnitude;

            if (distance <= nearestDistance)
            {
                nearestDistance = distance;
                hoveredIndex = i;
            }
        }

        return hoveredIndex;
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