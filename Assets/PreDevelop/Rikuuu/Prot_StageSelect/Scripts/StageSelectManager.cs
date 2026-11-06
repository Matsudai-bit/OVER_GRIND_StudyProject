using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// ステージの種類です。
/// SaveDataで使用するステージ番号と値を一致させます。
/// </summary>
public enum StageType
{
    TUTORIAL = 0,
    STAGE_1 = 1,
    STAGE_2 = 2,
    STAGE_3 = 3
}

/// <summary>
/// ステージ選択画面で使用するステージ情報です。
/// </summary>
[System.Serializable]
public struct StageInfomation
{
    // ステージアイコン
    public Image stagePoint;

    // ステージ番号
    public string stageNumber;

    // セクター番号テクスチャ
    public Sprite stageSectorNumber;

    // 未解放時に表示する画像
    public Image hideImage;

    // ステージ名（日本語）
    public string stageNameJa;

    // ステージ名（英語）
    public string stageNameEng;

    // ランク
    public string rank;

    // ベストタイム
    public int bestTime;

    // プレイ可能かどうか
    public bool canPlay;

    // 一度クリアしたかどうか
    public bool isCleared;

    // 押下されたときに呼ぶイベント
    public UnityEvent onClickAction;

    // 半分のサイズで表示するか
    public bool isHalfSize;
}

/// <summary>
/// ステージ選択画面を管理します。
/// ステージ進捗の反映、カーソル判定、
/// ステージ情報表示、ステージポイントへの吸着を管理します。
/// </summary>
public sealed class StageSelectManager : MonoBehaviour
{
    // ステージにカーソルが乗っていないことを表すインデックス
    private const int NO_HOVER = -1;

    [SerializeField, Header("セーブデータ")]
    private SaveDataController m_saveDataController;

    [SerializeField, Header("ステージマッピング")]
    private SerializeDictionary<StageType, int>
        m_stageIndexMap = new();

    [SerializeField, Header("カーソル判定")]
    [FormerlySerializedAs("HIT_RADIUS")]
    [Min(0.0f)]
    private float m_hitRadius = 40.0f;

    [SerializeField, Header("ステージポイントへの吸着")]
    [FormerlySerializedAs("SNAP_RADIUS")]
    [Min(0.0f)]
    [Tooltip("吸着を開始する距離。")]
    private float m_snapRadius = 80.0f;

    [SerializeField]
    [FormerlySerializedAs("RELEASE_RADIUS")]
    [Min(0.0f)]
    [Tooltip("吸着を解除する距離。")]
    private float m_releaseRadius = 150.0f;

    [SerializeField]
    [FormerlySerializedAs("SNAP_COMPLETE_THRESHOLD")]
    [Min(0.0f)]
    [Tooltip("吸着完了と判定する距離。")]
    private float m_snapCompleteThreshold = 2.0f;

    [SerializeField]
    [FormerlySerializedAs("SNAP_SPEED")]
    [Min(0.0f)]
    [Tooltip("ステージポイントへ吸着する速度。")]
    private float m_snapSpeed = 10.0f;

    [SerializeField, Header("カーソル")]
    private RectTransform m_cursor;

    [SerializeField, Header("ステージ情報")]
    private StageInfomation[] m_stagePoints;

    [SerializeField, Header("ステージ情報表示")]
    private TextMeshProUGUI m_stageNumber;

    [SerializeField]
    private Image m_sectorNumber;

    [SerializeField]
    private ScrambleText m_stageNameJa;

    [SerializeField]
    private ScrambleText[] m_stageNameEng;

    [SerializeField]
    private ScrambleText m_rank;

    [SerializeField]
    private ScrambleText m_bestTime;

    [SerializeField, Header("カーソル選択表示")]
    private Image m_onCursorImage;

    [SerializeField]
    private Image m_arrowLineImage;

    [SerializeField, Header("デフォルト表示")]
    private Sprite m_defaultSectorTexture;

    [SerializeField]
    private string m_defaultStageNumber = "STAGE-";

    [SerializeField]
    private string m_defaultStageNameJa = "--------";

    [SerializeField]
    private string m_defaultStageNameEn = "-------------";

    [SerializeField]
    private string m_defaultRank = "-";

    [SerializeField]
    private string m_defaultBestTime = "-:-.-";

    [SerializeField, Header("ステージポイント")]
    [FormerlySerializedAs("m_selectableButtonexture")]
    private Sprite m_selectableButtonTexture;

    [SerializeField]
    private Sprite m_unselectableButtonTexture;

    [SerializeField]
    private Sprite m_clearedButtonTexture;

    [SerializeField, Header("READYボタン")]
    private StageSelectButton m_readyButton;

    [SerializeField, Header("入力判定")]
    private InputActionReference m_navigateActionRef;

    [SerializeField]
    private InputActionReference m_enterActionRef;

    [SerializeField, Header("マップコントローラー")]
    private MapController m_mapController;

    [SerializeField, Header("ステージ決定")]
    [Min(0.0f)]
    private float m_invokeDelay = 1.0f;

    // 現在カーソルが乗っているステージ
    private int m_hoveredIndex = NO_HOVER;

    // 現在吸着しているステージ
    private int m_snappedIndex = NO_HOVER;

    // ステージポイントへの吸着が完了したか
    private bool m_isSnapCompleted = false;

    // ステージ決定処理の遅延中か
    private bool m_isWaiting = false;

    /// <summary>
    /// 現在カーソルがステージに乗っているか取得します。
    /// </summary>
    public bool IsHovering =>
        m_hoveredIndex != NO_HOVER;

    private void Start()
    {
        // 現在のSaveDataからステージ進捗を反映
        ApplyStageProgress();

        // 解放・クリア状態に応じて
        // ステージポイントの見た目を更新
        RefreshStagePointVisuals();

        // ステージ情報表示を初期化
        ResetComponents();
    }

    private void LateUpdate()
    {
        // 判定前にステージポイントへの吸着処理を行う
        UpdateCursorSnap();

        // MapControllerの移動後に
        // カーソルとステージポイントの位置を判定
        int hoveredIndex =
            FindHoveredIndex();

        // ホバー対象が変化した場合のみ表示を更新
        if (hoveredIndex != m_hoveredIndex)
        {
            m_hoveredIndex =
                hoveredIndex;

            OnHoverChanged();
        }

        // 決定入力を確認
        UpdateReadyButtonInput();
    }

    /// <summary>
    /// SaveDataの進捗をすべてのステージへ反映します。
    /// </summary>
    private void ApplyStageProgress()
    {
        if (m_saveDataController == null)
        {
            Debug.LogWarning(
                $"{nameof(SaveDataController)} "
                + "が設定されていません。",
                this);

            return;
        }

        GameProgressSaveData progress =
            m_saveDataController.Progress;

        if (progress == null)
        {
            Debug.LogWarning(
                $"{nameof(GameProgressSaveData)} "
                + "を取得できませんでした。",
                this);

            return;
        }

        ApplyStageProgress(
            StageType.TUTORIAL,
            progress);

        ApplyStageProgress(
            StageType.STAGE_1,
            progress);

        ApplyStageProgress(
            StageType.STAGE_2,
            progress);

        ApplyStageProgress(
            StageType.STAGE_3,
            progress);
    }

    /// <summary>
    /// 指定されたステージへ進行状況を反映します。
    /// </summary>
    /// <param name="stageType">
    /// 反映するステージ。
    /// </param>
    /// <param name="progress">
    /// 現在のゲーム進行データ。
    /// </param>
    private void ApplyStageProgress(
        StageType stageType,
        GameProgressSaveData progress)
    {
        if (!m_stageIndexMap.TryGetValue(
            stageType,
            out int stageIndex))
        {
            Debug.LogWarning(
                $"{stageType} のステージIndexが"
                + "設定されていません。",
                this);

            return;
        }

        if (!IsValidStageIndex(stageIndex))
        {
            Debug.LogWarning(
                $"{stageType} のステージIndexが不正です。"
                + $" Index: {stageIndex}",
                this);

            return;
        }

        int stageNumber =
            (int)stageType;

        /*
         * Tutorialは最初からプレイ可能。
         *
         * Stage1以降は、
         * 直前のステージをクリアすると解放される。
         *
         * Tutorial : 常時解放
         * Stage1   : ClearedStage >= 0
         * Stage2   : ClearedStage >= 1
         * Stage3   : ClearedStage >= 2
         */
        bool canPlay =
            stageNumber == 0
            || progress.ClearedStage
            >= stageNumber - 1;

        // 自身のステージまで進行していればクリア済み
        bool isCleared =
            progress.ClearedStage
            >= stageNumber;

        m_stagePoints[stageIndex].canPlay =
            canPlay;

        m_stagePoints[stageIndex].isCleared =
            isCleared;

        // 記録が存在しない場合のデフォルト値
        m_stagePoints[stageIndex].rank =
            m_defaultRank;

        m_stagePoints[stageIndex].bestTime =
            -1;

        // 未クリアの場合はクリア記録を表示しない
        if (!isCleared)
        {
            return;
        }

        if (!progress.TryGetStageData(
            stageNumber,
            out GameProgressSaveData.StageData stageData))
        {
            return;
        }

        m_stagePoints[stageIndex].rank =
            GetRank(stageData.Rank);

        m_stagePoints[stageIndex].bestTime =
            stageData.BestTime;
    }

    /// <summary>
    /// ステージの解放状態とクリア状態に応じて
    /// ステージポイントの表示を更新します。
    /// </summary>
    private void RefreshStagePointVisuals()
    {
        if (m_stagePoints == null)
        {
            return;
        }

        for (int i = 0;
             i < m_stagePoints.Length;
             i++)
        {
            Image stagePoint =
                m_stagePoints[i].stagePoint;

            if (stagePoint == null)
            {
                continue;
            }

            if (m_stagePoints[i].canPlay)
            {
                ApplyUnlockedStageVisual(i);
            }
            else
            {
                ApplyLockedStageVisual(i);
            }
        }
    }

    /// <summary>
    /// 解放済みステージの表示を設定します。
    /// </summary>
    /// <param name="index">
    /// ステージ情報のインデックス。
    /// </param>
    private void ApplyUnlockedStageVisual(int index)
    {
        StageInfomation stageInfo =
            m_stagePoints[index];

        stageInfo.stagePoint.sprite =
            stageInfo.canPlay
                ? m_clearedButtonTexture
                : m_selectableButtonTexture;

        ApplyStagePointSize(
            stageInfo.stagePoint,
            stageInfo.isHalfSize);

        if (stageInfo.hideImage != null)
        {
            stageInfo.hideImage.enabled =
                false;
        }
    }

    /// <summary>
    /// 未解放ステージの表示を設定します。
    /// </summary>
    /// <param name="index">
    /// ステージ情報のインデックス。
    /// </param>
    private void ApplyLockedStageVisual(int index)
    {
        StageInfomation stageInfo =
            m_stagePoints[index];

        stageInfo.stagePoint.sprite =
            m_unselectableButtonTexture;

        ApplyStagePointSize(
            stageInfo.stagePoint,
            stageInfo.isHalfSize);

        if (stageInfo.hideImage != null)
        {
            stageInfo.hideImage.enabled =
                true;
        }
    }

    /// <summary>
    /// ステージポイントのサイズを設定します。
    /// </summary>
    /// <param name="stagePoint">
    /// サイズを変更する画像。
    /// </param>
    /// <param name="isHalfSize">
    /// 半分のサイズにするか。
    /// </param>
    private void ApplyStagePointSize(
        Image stagePoint,
        bool isHalfSize)
    {
        if (stagePoint == null)
        {
            return;
        }

        /*
         * 先にNativeSizeへ戻すことで、
         * RefreshStagePointVisualsを複数回呼んでも
         * 50% → 25% → 12.5% と縮み続けないようにする。
         */
        stagePoint.SetNativeSize();

        if (!isHalfSize)
        {
            return;
        }

        RectTransform rectTransform =
            stagePoint.rectTransform;

        Vector2 nativeSize =
            rectTransform.rect.size;

        rectTransform.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            nativeSize.x * 0.5f);

        rectTransform.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            nativeSize.y * 0.5f);
    }

    /// <summary>
    /// ホバーしているステージが変更された際に
    /// ステージ情報表示を更新します。
    /// </summary>
    private void OnHoverChanged()
    {
        if (!IsHovering)
        {
            ResetComponents();

            return;
        }

        if (!IsValidStageIndex(
            m_hoveredIndex))
        {
            ResetComponents();

            return;
        }

        StageInfomation stageInfo =
            m_stagePoints[m_hoveredIndex];

        // ステージ番号
        if (m_stageNumber != null)
        {
            m_stageNumber.text =
                "STAGE-"
                + stageInfo.stageNumber;
        }

        // セクター番号
        if (m_sectorNumber != null)
        {
            m_sectorNumber.sprite =
                stageInfo.stageSectorNumber;
        }

        // 日本語ステージ名
        m_stageNameJa?.Play(
            stageInfo.stageNameJa);

        // 英語ステージ名
        if (m_stageNameEng != null)
        {
            foreach (
                ScrambleText stageName
                in m_stageNameEng)
            {
                stageName?.Play(
                    stageInfo.stageNameEng);
            }
        }

        // ランク
        if (m_rank != null)
        {
            string rank =
                string.IsNullOrEmpty(stageInfo.rank)
                    ? m_defaultRank
                    : stageInfo.rank;

            m_rank.Play(rank);
        }

        // ベストタイム
        ApplyBestTimeText(
            stageInfo.bestTime);

        // カーソル選択表示
        if (m_onCursorImage != null)
        {
            m_onCursorImage.enabled =
                true;
        }

        if (m_arrowLineImage != null)
        {
            m_arrowLineImage.enabled =
                true;
        }

        if (m_readyButton == null)
        {
            return;
        }

        // プレイ可能ならREADYボタンを選択状態にする
        if (stageInfo.canPlay)
        {
            m_readyButton.OnCursor();
        }
        else
        {
            // 未解放ステージへ移動した場合、
            // 前ステージの選択状態を残さない
            m_readyButton.OnCursorExit();
        }
    }

    /// <summary>
    /// ベストタイム表示を更新します。
    /// </summary>
    /// <param name="bestTime">
    /// 表示するベストタイム。
    /// 未記録の場合は負数。
    /// </param>
    private void ApplyBestTimeText(
        int bestTime)
    {
        if (m_bestTime == null)
        {
            return;
        }

        if (bestTime < 0)
        {
            m_bestTime.Play(
                m_defaultBestTime);

            return;
        }

        int minutes =
            bestTime / 60;

        int seconds =
            bestTime % 60;

        string timeText =
            $"{minutes:00}:{seconds:00}";

        m_bestTime.Play(
            timeText);
    }

    /// <summary>
    /// READYボタンへの決定入力を処理します。
    /// </summary>
    private void UpdateReadyButtonInput()
    {
        if (m_readyButton == null
            || !m_readyButton.m_isOnCursor
            || m_enterActionRef == null
            || !IsHovering
            || !IsValidStageIndex(m_hoveredIndex))
        {
            return;
        }

        StageInfomation stageInfo =
            m_stagePoints[m_hoveredIndex];

        // 未解放ステージでは決定処理を行わない
        if (!stageInfo.canPlay)
        {
            return;
        }

        // 決定キーを押した瞬間
        if (m_enterActionRef.action.WasPressedThisFrame())
        {
            if (m_isWaiting)
            {
                return;
            }

            int index =
                m_hoveredIndex;

            if (m_stagePoints[index].onClickAction != null
                && m_stagePoints[index]
                    .onClickAction
                    .GetPersistentEventCount() > 0)
            {
                m_readyButton.OnClick();

                StartCoroutine(
                    InvokeAfterDelay(
                        index,
                        m_invokeDelay));
            }
        }

        // 決定キーを離した瞬間
        if (m_enterActionRef.action.WasReleasedThisFrame())
        {
            int index =
                m_hoveredIndex;

            if (m_stagePoints[index].onClickAction != null
                && m_stagePoints[index]
                    .onClickAction
                    .GetPersistentEventCount() > 0)
            {
                m_readyButton.OnClickExit();
            }
        }
    }

    /// <summary>
    /// カーソルに最も近いステージのインデックスを取得します。
    /// 判定範囲内に存在しない場合はNO_HOVERを返します。
    /// </summary>
    /// <returns>
    /// 最も近いステージのインデックス。
    /// </returns>
    private int FindHoveredIndex()
    {
        if (m_cursor == null
            || m_stagePoints == null)
        {
            return NO_HOVER;
        }

        int hoveredIndex =
            NO_HOVER;

        float nearestDistance =
            m_hitRadius;

        for (int i = 0;
             i < m_stagePoints.Length;
             i++)
        {
            Image stagePoint =
                m_stagePoints[i].stagePoint;

            if (stagePoint == null)
            {
                continue;
            }

            Vector2 localPosition =
                m_cursor.InverseTransformPoint(
                    stagePoint
                        .rectTransform
                        .position);

            float distance =
                localPosition.magnitude;

            if (distance > nearestDistance)
            {
                continue;
            }

            nearestDistance =
                distance;

            hoveredIndex =
                i;
        }

        return hoveredIndex;
    }

    /// <summary>
    /// カーソルを近くのステージポイントへ吸着させます。
    /// </summary>
    private void UpdateCursorSnap()
    {
        if (m_cursor == null
            || m_stagePoints == null
            || m_mapController == null)
        {
            return;
        }

        Vector2 moveInput =
            m_navigateActionRef?.action
                .ReadValue<Vector2>()
            ?? Vector2.zero;

        /*
         * プレイヤーがマップを操作している間は
         * カーソル吸着による引き寄せを行わない。
         */
        if (moveInput != Vector2.zero)
        {
            ResetSnapState();

            return;
        }

        /*
         * 既にステージポイントへ
         * 吸着している場合の処理。
         */
        if (m_snappedIndex != NO_HOVER)
        {
            if (!IsValidStageIndex(
                m_snappedIndex))
            {
                ResetSnapState();
            }
            else if (
                m_stagePoints[m_snappedIndex]
                    .stagePoint == null)
            {
                ResetSnapState();
            }
            else
            {
                Vector2 offset =
                    GetStagePointOffset(
                        m_snappedIndex);

                float distance =
                    offset.magnitude;

                // 解除距離を超えた場合
                if (distance >
                    m_releaseRadius)
                {
                    ResetSnapState();
                }
                // 中心へ到達するまで吸着
                else if (!m_isSnapCompleted)
                {
                    if (distance
                        <= m_snapCompleteThreshold)
                    {
                        m_isSnapCompleted =
                            true;
                    }
                    else
                    {
                        MoveMapToStagePoint(
                            offset);
                    }

                    return;
                }
                else
                {
                    /*
                     * 吸着完了後は、
                     * プレイヤーの操作を妨げない。
                     * 解除距離判定のみ継続する。
                     */
                    return;
                }
            }
        }

        FindAndStartSnap();
    }

    /// <summary>
    /// 吸着対象となる最も近いステージを検索します。
    /// </summary>
    private void FindAndStartSnap()
    {
        int nearestIndex =
            NO_HOVER;

        float nearestDistance =
            m_snapRadius;

        Vector2 nearestOffset =
            Vector2.zero;

        for (int i = 0;
             i < m_stagePoints.Length;
             i++)
        {
            if (m_stagePoints[i].stagePoint
                == null)
            {
                continue;
            }

            Vector2 offset =
                GetStagePointOffset(i);

            float distance =
                offset.magnitude;

            if (distance >
                nearestDistance)
            {
                continue;
            }

            nearestDistance =
                distance;

            nearestIndex =
                i;

            nearestOffset =
                offset;
        }

        if (nearestIndex == NO_HOVER)
        {
            return;
        }

        m_snappedIndex =
            nearestIndex;

        m_isSnapCompleted =
            false;

        MoveMapToStagePoint(
            nearestOffset);
    }

    /// <summary>
    /// 指定されたステージポイントまでの
    /// カーソル基準オフセットを取得します。
    /// </summary>
    /// <param name="index">
    /// ステージ情報のインデックス。
    /// </param>
    /// <returns>
    /// ステージポイントまでのオフセット。
    /// </returns>
    private Vector2 GetStagePointOffset(
        int index)
    {
        Vector2 offset =
            m_cursor.InverseTransformPoint(
                m_stagePoints[index]
                    .stagePoint
                    .rectTransform
                    .position);

        return offset
            + m_mapController.CursorTilt;
    }

    /// <summary>
    /// 指定されたオフセット分だけ
    /// ステージポイント方向へマップを移動します。
    /// </summary>
    /// <param name="offset">
    /// ステージポイントまでのオフセット。
    /// </param>
    private void MoveMapToStagePoint(
        Vector2 offset)
    {
        Vector2 moveDelta =
            -offset
            * m_snapSpeed
            * Time.deltaTime;

        m_mapController.PanMapBy(
            moveDelta);
    }

    /// <summary>
    /// ステージポイントへの吸着状態を解除します。
    /// </summary>
    private void ResetSnapState()
    {
        m_snappedIndex =
            NO_HOVER;

        m_isSnapCompleted =
            false;
    }

    /// <summary>
    /// ステージ情報表示をデフォルト状態へ戻します。
    /// </summary>
    private void ResetComponents()
    {
        if (m_stageNumber != null)
        {
            m_stageNumber.text =
                m_defaultStageNumber;
        }

        if (m_sectorNumber != null)
        {
            m_sectorNumber.sprite =
                m_defaultSectorTexture;
        }

        m_stageNameJa?.Play(
            m_defaultStageNameJa);

        if (m_stageNameEng != null)
        {
            foreach (
                ScrambleText stageName
                in m_stageNameEng)
            {
                stageName?.Play(
                    m_defaultStageNameEn);
            }
        }

        m_rank?.Play(
            m_defaultRank);

        m_bestTime?.Play(
            m_defaultBestTime);

        if (m_onCursorImage != null)
        {
            m_onCursorImage.enabled =
                false;
        }

        if (m_arrowLineImage != null)
        {
            m_arrowLineImage.enabled =
                false;
        }

        m_readyButton?.OnCursorExit();
    }

    /// <summary>
    /// 指定されたステージインデックスが有効か確認します。
    /// </summary>
    /// <param name="index">
    /// 確認するインデックス。
    /// </param>
    /// <returns>
    /// true：有効なインデックスです。
    /// false：無効なインデックスです。
    /// </returns>
    private bool IsValidStageIndex(
        int index)
    {
        if (m_stagePoints == null)
        {
            return false;
        }

        return index >= 0
            && index < m_stagePoints.Length;
    }

    /// <summary>
    /// 数値ランクを表示用文字列へ変換します。
    /// </summary>
    /// <param name="rank">
    /// 数値ランク。
    /// </param>
    /// <returns>
    /// 表示するランク文字列。
    /// </returns>
    private string GetRank(int rank)
    {
        switch (rank)
        {
            case 0:
                return "C";

            case 1:
                return "B";

            case 2:
                return "A";

            case 3:
                return "S";

            case 4:
                return "S+";

            default:
                return m_defaultRank;
        }
    }

    /// <summary>
    /// 指定時間経過後にステージ決定イベントを実行します。
    /// </summary>
    /// <param name="index">
    /// 決定したステージのインデックス。
    /// </param>
    /// <param name="delay">
    /// イベント実行までの待機時間。
    /// </param>
    private IEnumerator InvokeAfterDelay(
        int index,
        float delay)
    {
        m_isWaiting =
            true;

        yield return new WaitForSeconds(
            delay);

        if (IsValidStageIndex(index)
            && m_stagePoints[index]
                .onClickAction != null
            && m_stagePoints[index]
                .onClickAction
                .GetPersistentEventCount() > 0)
        {
            m_stagePoints[index]
                .onClickAction
                .Invoke();
        }

        m_isWaiting =
            false;
    }
}