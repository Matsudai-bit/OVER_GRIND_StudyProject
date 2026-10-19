using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// リザルト画面におけるステージ情報（ステージ番号およびステージ名）の UI 表示を担当する View クラスです。
/// ヒエラルキー上の Result_Scene_UI_Stage や Result_Scene_UI_Tutorial などの GameObject を直接ドラッグ＆ドロップして使用できます。
/// </summary>
public class ResultStageView : MonoBehaviour
{
    [Header("ステージ番号 UI（Result_Scene_UI_Stage 等の GameObject）")]
    [Tooltip("ステージ番号表示用の GameObject（ドラッグ＆ドロップ時、内部の TMP/Text を自動取得します）")]
    [SerializeField] private GameObject m_stageNumberObject;

    [Tooltip("ステージごとに個別の GameObject を用意して切り替える場合の配列")]
    [SerializeField] private GameObject[] m_stageNumberObjects;

    [Header("ステージ名 UI（Result_Scene_UI_Tutorial 等の GameObject）")]
    [Tooltip("ステージ名表示用の GameObject（ドラッグ＆ドロップ時、内部の TMP/Text を自動取得します）")]
    [SerializeField] private GameObject m_stageNameObject;

    [Tooltip("ステージごとに個別の GameObject を用意して切り替える場合の配列")]
    [SerializeField] private GameObject[] m_stageNameObjects;

    // 内部で自動取得するコンポーネント参照
    private TMP_Text m_stageNumberTmp;
    private Text m_stageNumberText;

    private TMP_Text m_stageNameTmp;
    private Text m_stageNameText;

    private void Awake()
    {
        InitializeComponents();
    }

    /// <summary>
    /// アタッチされた GameObject から UI テキストコンポーネントを自動取得します。
    /// </summary>
    private void InitializeComponents()
    {
        // 1. ステージ番号オブジェクトから自動取得
        if (m_stageNumberObject != null)
        {
            m_stageNumberTmp = m_stageNumberObject.GetComponentInChildren<TMP_Text>();
            m_stageNumberText = m_stageNumberObject.GetComponentInChildren<Text>();
        }

        // 2. ステージ名オブジェクトから自動取得
        if (m_stageNameObject != null)
        {
            m_stageNameTmp = m_stageNameObject.GetComponentInChildren<TMP_Text>();
            m_stageNameText = m_stageNameObject.GetComponentInChildren<Text>();
        }
    }

    /// <summary>
    /// ステージ番号およびステージ名の表示を更新します。
    /// </summary>
    /// <param name="stageNumberText">表示するステージ番号文字列（例: \"Stage00\"）。</param>
    /// <param name="stageNameText">表示するステージ名文字列（例: \"Tutorial\"）。</param>
    /// <param name="stageIndex">ステージインデックス（0始まり）。</param>
    public void SetStageInfo(string stageNumberText, string stageNameText, int stageIndex = 0)
    {
        InitializeComponents();

        // --------------------------------------------------
        // 1. ステージ番号の描画・切替
        // --------------------------------------------------
        if (m_stageNumberTmp != null)
        {
            m_stageNumberTmp.text = stageNumberText;
        }
        else if (m_stageNumberText != null)
        {
            m_stageNumberText.text = stageNumberText;
        }

        // ステージ番号オブジェクトの切り替え表示（複数登録されている場合）
        if (m_stageNumberObjects != null && m_stageNumberObjects.Length > 0)
        {
            for (int i = 0; i < m_stageNumberObjects.Length; i++)
            {
                if (m_stageNumberObjects[i] != null)
                {
                    m_stageNumberObjects[i].SetActive(i == stageIndex);
                }
            }
        }

        // --------------------------------------------------
        // 2. ステージ名の描画・切替
        // --------------------------------------------------
        if (m_stageNameTmp != null)
        {
            m_stageNameTmp.text = stageNameText;
        }
        else if (m_stageNameText != null)
        {
            m_stageNameText.text = stageNameText;
        }

        // ステージ名オブジェクトの切り替え表示（複数登録されている場合）
        if (m_stageNameObjects != null && m_stageNameObjects.Length > 0)
        {
            for (int i = 0; i < m_stageNameObjects.Length; i++)
            {
                if (m_stageNameObjects[i] != null)
                {
                    m_stageNameObjects[i].SetActive(i == stageIndex);
                }
            }
        }
    }
}

