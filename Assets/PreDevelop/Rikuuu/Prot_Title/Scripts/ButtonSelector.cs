using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class ButtonSelector : MonoBehaviour
{
    // ボタン
    [SerializeField]
    private SelectButton[] m_buttons;

    // カーソルオブジェクト
    [SerializeField]
    private UnityEngine.UI.Image m_cursor;
    // カーソルが指しているボタン番号
    private int m_selectButtonNumber = 0;

    // カーソル移動させる場合に呼び出す関数
    [SerializeField]
    private UnityEngine.Events.UnityEvent<UnityEngine.UI.Image, Vector3> m_moveAction = null;

    [SerializeField]
    private bool m_keepSelectionWhenLocked = false;

    // カーソルがロックされているかどうか
    private bool m_isLockCursor = true;
    // カーソルの固定・解除の予約
    private bool m_cursorLockRequested;
    private bool m_cursorUnlockRequested;

    private void Start()
    {
        if(m_buttons != null)
        {
            for (int i = 0; i < m_buttons.Length; i++)
            {
                // 選択していない状態にする
                m_buttons[i].OnCursorExit();

                // もし選択されている番号と一致した場合
                if (m_selectButtonNumber == i)
                {
                    // 選択している状態にする
                    m_buttons[m_selectButtonNumber].OnCursor();
                }
            }
        }
    }

    private void Update()
    {
        // 上下キーどちらかが押されていたら || カーソルが固定されていなかったら
        if (wasPressedUpOrDown() && !m_isLockCursor)
        {
            // カーソルを離れるときの関数を実行する
            m_buttons[m_selectButtonNumber].OnCursorExit();

            // 上キーが押されたら
            if (Keyboard.current.upArrowKey.wasPressedThisFrame)
            {
                // カーソルを一つ上に移動させる
                m_selectButtonNumber--;

                Debug.Log("MoveUP");
            }
            // 下キーが押されたら
            if (Keyboard.current.downArrowKey.wasPressedThisFrame)
            {
                // カーソルを一つ下に移動させる
                m_selectButtonNumber++;

                Debug.Log("MoveDOWN");
            }

            // 範囲内に収める
            Clamp();
            // カーソルが乗ったときの処理を呼ぶ
            m_buttons[m_selectButtonNumber].OnCursor();
        }

        // カーソルがロックされていない場合
        if(!m_isLockCursor)
        {
            // カーソルの座標更新
            if (m_cursor != null)
            {
                if (m_moveAction.GetPersistentEventCount() > 0)
                {
                    m_moveAction.Invoke(
                        m_cursor,
                        m_buttons[m_selectButtonNumber].transform.position
                    );
                }
                else
                {
                    m_cursor.transform.position = m_buttons[m_selectButtonNumber].transform.position;
                }
            }

            // 決定ボタンが押されたら
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                // キーが押されたときの処理を実行する
                m_buttons[m_selectButtonNumber].OnClick();
            }
            // 決定ボタンが離されたら
            if (Keyboard.current.spaceKey.wasReleasedThisFrame)
            {
                // キーが押されたときの処理を実行する
                m_buttons[m_selectButtonNumber].OnClickExit();
            }
        }
    }

    private void LateUpdate()
    {
        // 固定するように言われている場合
        if(m_cursorLockRequested)
        {
            m_isLockCursor = true;

            m_cursorLockRequested = false;

            // 選択状態を維持しない設定の場合のみ、ロック時に非選択状態へ戻す
            if (!m_keepSelectionWhenLocked && m_buttons != null && m_buttons.Length > 0)
            {
                m_buttons[m_selectButtonNumber].OnCursorExit();
            }
        }
        // 解除するように言われている場合
        if(m_cursorUnlockRequested)
        {
            m_isLockCursor = false;

            m_cursorUnlockRequested = false;

            // ロック解除時は選択中ボタンを即座に有効化（クリック不要）
            if (m_buttons != null && m_buttons.Length > 0)
            {
                m_buttons[m_selectButtonNumber].OnCursor();
            }
        }
    }

    // ----------------------------------------------------------------------

    // カーソルのロック
    public void LockCursor()
    {
        m_cursorLockRequested = true;
    }

    // カーソルのロックを解除する
    public void UnLockCursor()
    {
        m_cursorUnlockRequested = true;
    }

    // ----------------------------------------------------------------------

    // 上キーまたは下キーが押されているか
    private bool wasPressedUpOrDown()
    {
        return (Keyboard.current.upArrowKey.wasPressedThisFrame ||
                Keyboard.current.downArrowKey.wasPressedThisFrame);
    }

    // カーソル番号を範囲内に収める 
    private void Clamp()
    {
        // 0以下の場合
        if (m_selectButtonNumber < 0)
        {
            m_selectButtonNumber += m_buttons.Length;
        }

        // 0～ボタン配列の最大値の間の値にする
        m_selectButtonNumber %= m_buttons.Length;
    }
}
