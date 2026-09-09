using System.Security.Claims;
using UnityEngine;
using UnityEngine.InputSystem;

public class SelectionItemManager : MonoBehaviour
{
    // ボタン
    [SerializeField]
    private SelectionItem[] m_selectionItems;

    // カーソルテクスチャ
    [SerializeField]
    private GameObject m_cursorImage;

    // 選択されているボタン番号
    private int m_selectedButton = 0;

    private void Start()
    {
        // カーソルが乗ったときの処理を呼ぶ
        m_selectionItems[m_selectedButton].OnCursor();
    }

    private void Update()
    {
        // 上ボタンが押されたら
        if(Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            // カーソルが離れたときの処理を呼ぶ
            m_selectionItems[m_selectedButton].OnExit();

            m_selectedButton--;

            // 範囲内に収める
            Clamp();

            // カーソルが乗ったときの処理を呼ぶ
            m_selectionItems[m_selectedButton].OnCursor();
        }
        
        // 下ボタンが押されたら
        if (Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            // カーソルが離れたときの処理を呼ぶ
            m_selectionItems[m_selectedButton].OnExit();
            
            // カーソルを下に動かす
            m_selectedButton++;

            // 範囲内に収める
            Clamp();

            // カーソルが乗ったときの処理を呼ぶ
            m_selectionItems[m_selectedButton].OnCursor();
        }

        // カーソル画像を移動させる
        m_cursorImage.transform.position = m_selectionItems[m_selectedButton].transform.position;

        // 決定ボタンを押された場合

    }

    private void Clamp()
    {
        // 範囲内に収める
        if (m_selectedButton < 0)
        {
            m_selectedButton += m_selectionItems.Length;
        }
        m_selectedButton %= m_selectionItems.Length;
    }
}
