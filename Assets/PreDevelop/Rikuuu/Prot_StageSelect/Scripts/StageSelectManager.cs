using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ステージ情報
/// </summary>
[System.Serializable]
public struct StageInfomation
{
    // ステージアイコン
    public UnityEngine.UI.Image stagePoint;

    // ステージ番号
    public int stageNumber;

    // セクター番号テクスチャ
    public Sprite stageSectorNumber;

    // ハイドイメージコンポーネント
    public UnityEngine.UI.Image hideImage;

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
/// ステージ選択画面を管理します。
/// カーソルとステージポイントの位置関係から、
/// 現在選択されているステージを判定します。
/// </summary>
public class StageSelectManager : MonoBehaviour
{
    // 乗っているステージがないことを表すインデックス
    private const int NO_HOVER = -1;

    [Header("カーソル判定")]

    // カーソルの衝突検知範囲
    [SerializeField]
    private float HIT_RADIUS = 40.0f;

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

    [Header("カーソル")]

    // マップ中央に配置されたカーソル座標
    [SerializeField]
    private RectTransform m_cursor;

    [Header("ステージの情報")]

    // ステージの情報
    [SerializeField]
    private StageInfomation[] m_stagePoints;

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

    [Header("カーソル表示関連")]

    // カーソルが乗っているときのイメージコンポーネント
    [SerializeField]
    private UnityEngine.UI.Image m_onCursorImage;

    // 矢印線のイメージコンポーネント
    [SerializeField]
    private UnityEngine.UI.Image m_arrowLineImage;

    [Header("デフォルト値関連")]

    // セクター番号の通常時テクスチャ
    [SerializeField]
    private Sprite m_defaultSectorTexture;

    // ステージ番号のデフォルト値
    [SerializeField]
    private string m_defaultStageNumber = "STAGE-";

    // ステージ名（日本語）のデフォルト値
    [SerializeField]
    private string m_defaultStageNameJa = "--------";

    // ステージ名（英語）のデフォルト値
    [SerializeField]
    private string m_defaultStageNameEn = "-------------";

    // ベストタイムのデフォルト値
    [SerializeField]
    private string m_defaultBestTime = "-:-.-";

    // 選択可能なボタンテクスチャ
    [SerializeField]
    private Sprite m_selectableButtonexture;

    // 選択不可なボタンテクスチャ
    [SerializeField]
    private Sprite m_unselectableButtonTexture;

    [Header("READYボタン")]

    // 準備完了ボタン
    [SerializeField]
    private StageSelectButton m_readyButton;

    [Header("入力判定関連")]

    // 移動キーが押される判定
    [SerializeField]
    private InputActionReference m_navigateActionRef;

    // 決定キーが押される判定
    [SerializeField]
    private InputActionReference m_enterActionRef;

    [Header("マップコントローラー")]

    // マップコントローラー
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
        // 表示を初期化する
        ResetComponent();

        if (m_stagePoints == null)
        {
            return;
        }

        // 選択可否による表示の切り替え
        for (int i = 0; i < m_stagePoints.Length; i++)
        {
            if (m_stagePoints[i].stagePoint == null)
            {
                continue;
            }

            // 選択できる場合
            if (m_stagePoints[i].canPlay)
            {
                m_stagePoints[i].stagePoint.sprite = m_selectableButtonexture;

                // ハイドイメージを非表示にする
                if (m_stagePoints[i].hideImage != null)
                {
                    m_stagePoints[i].hideImage.enabled = false;
                }
            }
            // 選択できない場合
            else
            {
                m_stagePoints[i].stagePoint.sprite = m_unselectableButtonTexture;

                // ハイドイメージを表示する
                if (m_stagePoints[i].hideImage != null)
                {
                    m_stagePoints[i].hideImage.enabled = true;
                }
            }
        }
    }

    private void LateUpdate()
    {
        // 判定の前にカーソルを吸着させる
        UpdateCursorSnap();

        // マップの移動・拡大縮小後に判定する
        int hoveredIndex = FindHoveredIndex();

        // カーソルが乗っている対象が変化した場合
        if (hoveredIndex != m_hoveredIndex)
        {
            UpdateHoveredStage(hoveredIndex);
        }

        // READYボタンへの入力は毎フレーム確認する
        UpdateReadyButtonInput();
    }

    /// <summary>
    /// カーソルが乗っているステージの変更を反映します。
    /// </summary>
    private void UpdateHoveredStage(int hoveredIndex)
    {
        m_hoveredIndex = hoveredIndex;

        // ステージに乗っていない場合
        if (!IsHovering)
        {
            ResetComponent();
            return;
        }

        StageInfomation stageInformation = m_stagePoints[m_hoveredIndex];

        // ステージ情報を表示する
        UpdateStageInformation(stageInformation);

        // プレイ可能なステージの場合
        if (stageInformation.canPlay)
        {
            m_readyButton.OnCursor();
        }
        // プレイ不可のステージの場合
        else
        {
            m_readyButton.OnCursorExit();
        }
    }

    /// <summary>
    /// 選択中のステージ情報をUIへ反映します。
    /// </summary>
    private void UpdateStageInformation(StageInfomation stageInformation)
    {
        // ステージ番号
        m_stageNumber.text = "STAGE-" + stageInformation.stageNumber;

        // セクター番号
        m_sectorNumber.sprite = stageInformation.stageSectorNumber;

        // ステージ名（日本語）
        m_stageNameJa.text = stageInformation.stageNameJa;

        // ステージ名（英語）
        foreach (TextMeshProUGUI stageName in m_stageNameEng)
        {
            if (stageName == null)
            {
                continue;
            }

            stageName.text = stageInformation.stageNameEng;
        }

        // ベストタイム
        int bestTime = stageInformation.bestTime;
        int minutes = bestTime / 60;
        float seconds = bestTime - (minutes * 60);

        m_bestTime.text =
            minutes.ToString("00") + ":" +
            seconds.ToString("00.00");

        // カーソル関連のイメージを表示
        m_onCursorImage.enabled = true;
        m_arrowLineImage.enabled = true;
    }

    /// <summary>
    /// READYボタンへの決定入力を処理します。
    /// </summary>
    private void UpdateReadyButtonInput()
    {
        if (m_readyButton == null)
        {
            return;
        }

        // READYボタンにカーソルが合っていない場合
        if (!m_readyButton.m_isOnCursor)
        {
            return;
        }

        if (m_enterActionRef == null)
        {
            return;
        }

        // 決定ボタンが押された場合
        if (m_enterActionRef.action.WasPressedThisFrame())
        {
            m_readyButton.OnClick();
        }

        // 決定ボタンが離された場合
        if (m_enterActionRef.action.WasReleasedThisFrame())
        {
            m_readyButton.OnClickExit();
        }
    }

    /// <summary>
    /// カーソルに最も近いステージのインデックスを返します。
    /// 衝突検知範囲内になければ-1を返します。
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
            if (m_stagePoints[i].stagePoint == null)
            {
                continue;
            }

            // ステージの座標をカーソルのローカル座標に変換する
            Vector2 localPosition =
                m_cursor.InverseTransformPoint(
                    m_stagePoints[i].stagePoint.rectTransform.position);

            float distance = localPosition.magnitude;

            // 最も近いステージを取得する
            if (distance <= nearestDistance)
            {
                nearestDistance = distance;
                hoveredIndex = i;
            }
        }

        return hoveredIndex;
    }

    /// <summary>
    /// カーソルのステージポイントへの吸着を更新します。
    /// </summary>
    private void UpdateCursorSnap()
    {
        if (m_cursor == null ||
            m_stagePoints == null ||
            m_mapController == null)
        {
            return;
        }

        // プレイヤーが移動操作をしている間は
        // 吸着による引き寄せを行わない
        Vector2 moveInput =
            m_navigateActionRef?.action.ReadValue<Vector2>() ??
            Vector2.zero;

        if (moveInput != Vector2.zero)
        {
            m_snappedIndex = NO_HOVER;
            m_isSnapCompleted = false;
            return;
        }

        // 既に何かへ吸着している場合
        if (m_snappedIndex != NO_HOVER)
        {
            if (m_stagePoints[m_snappedIndex].stagePoint == null)
            {
                // 参照が失われていたら吸着を解除する
                m_snappedIndex = NO_HOVER;
                m_isSnapCompleted = false;
            }
            else
            {
                Vector2 offset =
                    (Vector2)m_cursor.InverseTransformPoint(
                        m_stagePoints[m_snappedIndex]
                            .stagePoint
                            .rectTransform
                            .position)
                    + m_mapController.CursorTilt;

                float distance = offset.magnitude;

                // 解除距離を超えた場合
                if (distance > RELEASE_RADIUS)
                {
                    m_snappedIndex = NO_HOVER;
                    m_isSnapCompleted = false;
                }
                // まだ中心に到達していない場合
                else if (!m_isSnapCompleted)
                {
                    // 中心へ到達した場合
                    if (distance <= SNAP_COMPLETE_THRESHOLD)
                    {
                        m_isSnapCompleted = true;
                    }
                    else
                    {
                        // ステージポイントへ引き寄せる
                        Vector2 moveDelta =
                            -offset * SNAP_SPEED * Time.deltaTime;

                        m_mapController.PanMapBy(moveDelta);
                    }

                    return;
                }
                else
                {
                    // 到達済みの場合は解除判定のみ行う
                    return;
                }
            }
        }

        // 吸着していない場合、
        // 範囲内にある最も近いステージを探す
        int nearestIndex = NO_HOVER;
        float nearestDistance = SNAP_RADIUS;
        Vector2 nearestOffset = Vector2.zero;

        for (int i = 0; i < m_stagePoints.Length; i++)
        {
            if (m_stagePoints[i].stagePoint == null)
            {
                continue;
            }

            Vector2 offset =
                (Vector2)m_cursor.InverseTransformPoint(
                    m_stagePoints[i]
                        .stagePoint
                        .rectTransform
                        .position)
                + m_mapController.CursorTilt;

            float distance = offset.magnitude;

            if (distance <= nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
                nearestOffset = offset;
            }
        }

        // 新たに範囲内へ入ったステージがある場合
        if (nearestIndex != NO_HOVER)
        {
            m_snappedIndex = nearestIndex;
            m_isSnapCompleted = false;

            Vector2 moveDelta =
                -nearestOffset * SNAP_SPEED * Time.deltaTime;

            m_mapController.PanMapBy(moveDelta);
        }
    }

    /// <summary>
    /// ステージ情報表示とREADYボタンを初期状態へ戻します。
    /// </summary>
    private void ResetComponent()
    {
        m_enterActionRef.action.Enable();
        // ステージ番号
        m_stageNumber.text = m_defaultStageNumber;

        // セクター番号
        m_sectorNumber.sprite = m_defaultSectorTexture;

        // ステージ名（日本語）
        m_stageNameJa.text = m_defaultStageNameJa;

        // ステージ名（英語）
        foreach (TextMeshProUGUI stageName in m_stageNameEng)
        {
            if (stageName == null)
            {
                continue;
            }

            stageName.text = m_defaultStageNameEn;
        }

        // ベストタイム
        m_bestTime.text = m_defaultBestTime;

        // イメージコンポーネントを非表示にする
        m_onCursorImage.enabled = false;
        m_arrowLineImage.enabled = false;

        // READYボタンの選択状態を解除する
        if (m_readyButton != null)
        {
            m_readyButton.OnCursorExit();
        }
    }
}