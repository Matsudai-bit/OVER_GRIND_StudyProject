using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// リザルト画面におけるステージ情報（ステージ番号およびステージ名）の UI 表示・スクランブル演出を担当する View クラスです。
/// 文字が左から順番に解読・確定していく演出に対応しています。
/// </summary>
public class ResultStageView : MonoBehaviour
{
    private const float DEFAULT_START_DELAY = 0.5f;
    private const float DEFAULT_CHAR_REVEAL_DELAY = 0.05f;

    [Header("単一オブジェクト指定（ステージ番号 UI）")]
    /// <summary>ステージ番号表示用の GameObject。</summary>
    [Tooltip("ステージ番号表示用の GameObject（ドラッグ＆ドロップ時、内部の TMP/Text/ScrambleText を自動取得します）")]
    [SerializeField] private GameObject m_stageNumberObject;

    [Header("複数オブジェクト切替（ステージ番号 UI）")]
    /// <summary>ステージごとに個別の GameObject を用意して切り替える場合の配列。</summary>
    [Tooltip("ステージごとに個別の GameObject を用意して切り替える場合の配列")]
    [SerializeField] private GameObject[] m_stageNumberObjects;

    [Header("単一オブジェクト指定（ステージ名 UI）")]
    /// <summary>ステージ名表示用の GameObject。</summary>
    [Tooltip("ステージ名表示用の GameObject（ドラッグ＆ドロップ時、内部の TMP/Text/ScrambleText を自動取得します）")]
    [SerializeField] private GameObject m_stageNameObject;

    [Header("複数オブジェクト切替（ステージ名 UI）")]
    /// <summary>ステージごとに個別の GameObject を用意して切り替える場合の配列。</summary>
    [Tooltip("ステージごとに個別の GameObject を用意して切り替える場合の配列")]
    [SerializeField] private GameObject[] m_stageNameObjects;

    [Header("演出タイミング設定")]
    /// <summary>表示・スクランブル演出を開始するまでの遅延時間（秒）。</summary>
    [Tooltip("表示・スクランブル演出を開始するまでの遅延時間（秒）")]
    [SerializeField] private float m_startDelay = DEFAULT_START_DELAY;

    /// <summary>遅延時間中（Delay中）に UI オブジェクトを非表示にしておくかどうか。</summary>
    [Tooltip("遅延時間中（Delay中）は UI オブジェクトを非表示にしておくか")]
    [SerializeField] private bool m_hideDuringDelay = true;

    [Header("順次解読（復元）演出設定")]
    /// <summary>左から右へ1文字ずつ確定させる際の、1文字あたりのずらし時間（秒）。</summary>
    [Tooltip("左から1文字ずつ順番に本来の文字に戻る際の時間差（秒）")]
    [SerializeField] private float m_charRevealDelay = DEFAULT_CHAR_REVEAL_DELAY;

    /// <summary>現在アクティブなステージ番号の TMP_Text キャッシュ。</summary>
    private TMP_Text m_activeStageNumberTmp;

    /// <summary>現在アクティブなステージ番号の Text キャッシュ。</summary>
    private Text m_activeStageNumberText;

    /// <summary>現在アクティブなステージ番号の ScrambleText キャッシュ。</summary>
    private ScrambleText m_activeStageNumberScramble;

    /// <summary>現在アクティブなステージ名の TMP_Text キャッシュ。</summary>
    private TMP_Text m_activeStageNameTmp;

    /// <summary>現在アクティブなステージ名の Text キャッシュ。</summary>
    private Text m_activeStageNameText;

    /// <summary>現在アクティブなステージ名の ScrambleText キャッシュ。</summary>
    private ScrambleText m_activeStageNameScramble;

    /// <summary>現在選択されているステージ番号の GameObject。</summary>
    private GameObject m_currentNumberObj;

    /// <summary>現在選択されているステージ名の GameObject。</summary>
    private GameObject m_currentNameObj;

    /// <summary>演出実行中のコルーチン参照。</summary>
    private Coroutine m_playCoroutine;

    private void Awake()
    {
        SetupStageComponents(0);
    }

    private void OnDisable()
    {
        StopPlayCoroutine();
    }

    /// <summary>
    /// 指定された stageIndex に基づいてアクティブな GameObject を切り替え、UI コンポーネントを最新の状態で自動取得します。
    /// </summary>
    /// <param name="stageIndex">対象のステージインデックス。</param>
    private void SetupStageComponents(int stageIndex)
    {
        m_currentNumberObj = GetActiveObject(m_stageNumberObject, m_stageNumberObjects, stageIndex);
        CacheNumberComponents(m_currentNumberObj);

        m_currentNameObj = GetActiveObject(m_stageNameObject, m_stageNameObjects, stageIndex);
        CacheNameComponents(m_currentNameObj);
    }

    /// <summary>
    /// 単一指定または配列から、対象インデックスに該当するアクティブな GameObject を取得します。
    /// </summary>
    /// <param name="singleObj">単一指定の GameObject。</param>
    /// <param name="arrayObjs">複数切替用の GameObject 配列。</param>
    /// <param name="targetIndex">対象のインデックス。</param>
    /// <returns>アクティブに設定された GameObject。</returns>
    private GameObject GetActiveObject(GameObject singleObj, GameObject[] arrayObjs, int targetIndex)
    {
        if (arrayObjs != null && arrayObjs.Length > 0)
        {
            GameObject selectedObj = null;

            for (int i = 0; i < arrayObjs.Length; i++)
            {
                if (arrayObjs[i] == null) continue;

                bool isActive = (i == targetIndex);
                arrayObjs[i].SetActive(isActive);

                if (isActive)
                {
                    selectedObj = arrayObjs[i];
                }
            }

            if (selectedObj != null) return selectedObj;
        }

        if (singleObj != null)
        {
            singleObj.SetActive(true);
            return singleObj;
        }

        return null;
    }

    /// <summary>
    /// ステージ番号オブジェクトから必要な UI コンポーネントをキャッシュします。
    /// </summary>
    /// <param name="targetObj">検索対象の GameObject。</param>
    private void CacheNumberComponents(GameObject targetObj)
    {
        if (targetObj == null)
        {
            m_activeStageNumberScramble = null;
            m_activeStageNumberTmp = null;
            m_activeStageNumberText = null;
            return;
        }

        m_activeStageNumberScramble = targetObj.GetComponentInChildren<ScrambleText>(true);
        m_activeStageNumberTmp = targetObj.GetComponentInChildren<TMP_Text>(true);
        m_activeStageNumberText = targetObj.GetComponentInChildren<Text>(true);
    }

    /// <summary>
    /// ステージ名オブジェクトから必要な UI コンポーネントをキャッシュします。
    /// </summary>
    /// <param name="targetObj">検索対象の GameObject。</param>
    private void CacheNameComponents(GameObject targetObj)
    {
        if (targetObj == null)
        {
            m_activeStageNameScramble = null;
            m_activeStageNameTmp = null;
            m_activeStageNameText = null;
            return;
        }

        m_activeStageNameScramble = targetObj.GetComponentInChildren<ScrambleText>(true);
        m_activeStageNameTmp = targetObj.GetComponentInChildren<TMP_Text>(true);
        m_activeStageNameText = targetObj.GetComponentInChildren<Text>(true);
    }

    /// <summary>
    /// 指定された Delay 時間経過後に、ステージ番号およびステージ名をスクランブル再生・表示します。
    /// </summary>
    /// <param name="stageNumberText">表示するステージ番号文字列（例: "Stage00"）。</param>
    /// <param name="stageNameText">表示するステージ名文字列（例: "Tutorial"）。</param>
    /// <param name="stageIndex">ステージインデックス（0始まり）。</param>
    /// <param name="customDelay">個別に指定したい遅延時間（負の数値の場合は Inspector の Start Delay を使用）。</param>
    public void PlayStageInfoScramble(string stageNumberText, string stageNameText, int stageIndex = 0, float customDelay = -1f)
    {
        StopPlayCoroutine();

        float delay = (customDelay >= 0f) ? customDelay : m_startDelay;
        m_playCoroutine = StartCoroutine(CoPlayWithDelay(stageNumberText, stageNameText, stageIndex, delay));
    }

    /// <summary>
    /// 遅延時間を待機してからスクランブル再生を実行するコルーチンです。
    /// </summary>
    /// <param name="stageNumberText">表示するステージ番号文字列。</param>
    /// <param name="stageNameText">表示するステージ名文字列。</param>
    /// <param name="stageIndex">ステージインデックス。</param>
    /// <param name="delay">遅延時間（秒）。</param>
    private IEnumerator CoPlayWithDelay(string stageNumberText, string stageNameText, int stageIndex, float delay)
    {
        SetupStageComponents(stageIndex);

        // 遅延時間中の非表示制御および待機
        if (m_hideDuringDelay && delay > 0f)
        {
            SetObjectsVisibility(false);
            yield return new WaitForSecondsRealtime(delay);
            SetObjectsVisibility(true);
        }
        else if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        ExecutePlay(stageNumberText, stageNameText);
        m_playCoroutine = null;
    }

    /// <summary>
    /// ステージ番号およびステージ名のスクランブル表示処理を実行します。
    /// </summary>
    /// <param name="stageNumberText">ステージ番号文字列。</param>
    /// <param name="stageNameText">ステージ名文字列。</param>
    private void ExecutePlay(string stageNumberText, string stageNameText)
    {
        // ステージ番号のスクランブル再生
        if (m_activeStageNumberScramble != null)
        {
            m_activeStageNumberScramble.Play(stageNumberText);
        }
        else if (m_activeStageNumberTmp != null)
        {
            m_activeStageNumberTmp.text = stageNumberText;
        }
        else if (m_activeStageNumberText != null)
        {
            m_activeStageNumberText.text = stageNumberText;
        }

        // ステージ名のスクランブル再生
        if (m_activeStageNameScramble != null)
        {
            m_activeStageNameScramble.Play(stageNameText);
        }
        else if (m_activeStageNameTmp != null)
        {
            m_activeStageNameTmp.text = stageNameText;
        }
        else if (m_activeStageNameText != null)
        {
            m_activeStageNameText.text = stageNameText;
        }
    }

    /// <summary>
    /// 即座にステージ番号およびステージ名の表示を更新します（Delay無視・アニメーションなし）。
    /// </summary>
    /// <param name="stageNumberText">表示するステージ番号文字列。</param>
    /// <param name="stageNameText">表示するステージ名文字列。</param>
    /// <param name="stageIndex">ステージインデックス（0始まり）。</param>
    public void SetStageInfo(string stageNumberText, string stageNameText, int stageIndex = 0)
    {
        StopPlayCoroutine();
        SetupStageComponents(stageIndex);
        SetObjectsVisibility(true);

        if (m_activeStageNumberScramble != null)
        {
            m_activeStageNumberScramble.SetImmediate(stageNumberText);
        }
        else if (m_activeStageNumberTmp != null)
        {
            m_activeStageNumberTmp.text = stageNumberText;
        }
        else if (m_activeStageNumberText != null)
        {
            m_activeStageNumberText.text = stageNumberText;
        }

        if (m_activeStageNameScramble != null)
        {
            m_activeStageNameScramble.SetImmediate(stageNameText);
        }
        else if (m_activeStageNameTmp != null)
        {
            m_activeStageNameTmp.text = stageNameText;
        }
        else if (m_activeStageNameText != null)
        {
            m_activeStageNameText.text = stageNameText;
        }
    }

    /// <summary>
    /// 表示中の UI オブジェクトを非表示にします。
    /// </summary>
    public void Hide()
    {
        StopPlayCoroutine();
        SetObjectsVisibility(false);
    }

    /// <summary>
    /// 現在ターゲットとなっている UI オブジェクトの表示/非表示を設定します。
    /// </summary>
    /// <param name="isVisible">
    /// true：表示します。
    /// false：非表示にします。
    /// </param>
    private void SetObjectsVisibility(bool isVisible)
    {
        if (m_currentNumberObj != null) m_currentNumberObj.SetActive(isVisible);
        if (m_currentNameObj != null) m_currentNameObj.SetActive(isVisible);
    }

    /// <summary>
    /// 実行中の再生用コルーチンを停止・破棄します。
    /// </summary>
    private void StopPlayCoroutine()
    {
        if (m_playCoroutine != null)
        {
            StopCoroutine(m_playCoroutine);
            m_playCoroutine = null;
        }
    }
}