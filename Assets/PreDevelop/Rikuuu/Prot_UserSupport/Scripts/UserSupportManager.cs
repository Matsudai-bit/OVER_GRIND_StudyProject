using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class UserSupportManager : MonoBehaviour
{
    // ページの選択状況
    enum ChoseState
    { 
        NONE = 0,   // なし

        MANUAL,     // チュートリアル
        OPTION      // 設定
    }
    // 開いているページ
    ChoseState m_choseState = ChoseState.NONE;

    [Header("ヘッダー関連")]
    // マニュアルボタン
    [SerializeField]
    private SelectButton m_manualButton;
    // オプションボタン
    [SerializeField]
    private SelectButton m_optionButton;

    [Space(20)]
    [Header("メイン関連")]
    [Header("コンテナコントローラ")]
    [SerializeField]
    private ContainerController m_containerController;

    [Header("マニュアル関連")]
    [SerializeField]
    private GameObject[] m_manualItems;

    [Header("オプション関連")]
    [SerializeField]
    private GameObject[] m_optionItems;

    // 選択されているボタン番号
    private int m_selectedIndex = 0;

    private void Start()
    {
        // オプションページを表示する
        m_choseState = ChoseState.OPTION;
        // 切り替え関数を実行する
        ChangeToOptionPage();
    }

    private void Update()
    {
        // ページの切り替え
        if(WasPressedChangeKey())
        {
            if(m_choseState != ChoseState.MANUAL)
            {
                // マニュアルページにする
                m_choseState = ChoseState.MANUAL;

                // 切り替え関数を実行する
                ChangeToManualPage();
            }
            else if (m_choseState != ChoseState.OPTION)
            {
                // オプションページにする
                m_choseState = ChoseState.OPTION;

                // 切り替え関数を実行する
                ChangeToOptionPage();
            }
        }
    }

    // 切り替え ----------------------------------------------

    // マニュアルページへの切り替え
    void ChangeToManualPage()
    {
        // 選択しているコンテナを左にする
        m_containerController.ChangeToLeft();

        // マニュアルテクスチャを表示する
        m_manualButton.OnCursor();
        // オプションテクスチャを薄くする
        m_optionButton.OnCursorExit();

        // アイテムの表示切替
        foreach (var item in m_manualItems)
        {
            item.SetActive(true);
        }
        foreach (var item in m_optionItems)
        {
            item.SetActive(false);
        }
    }

    // オプションページへの切り替え
    void ChangeToOptionPage()
    {
        // 選択しているコンテナを左にする
        m_containerController.ChangeToLeft();

        // オプションテクスチャを表示する
        m_optionButton.OnCursor();
        // マニュアルテクスチャを薄くする
        m_manualButton.OnCursorExit();

        // アイテムの表示切替
        foreach (var item in m_manualItems)
        {
            item.SetActive(false);
        }
        foreach (var item in m_optionItems)
        {
            item.SetActive(true);
        }
    }

    // ------------------------------------------------------

    private bool WasPressedChangeKey()
    {
        return (Keyboard.current.qKey.wasPressedThisFrame ||
                Keyboard.current.eKey.wasPressedThisFrame);
    }
}
