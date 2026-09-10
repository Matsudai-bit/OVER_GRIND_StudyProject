using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PauseButtonSelector : MonoBehaviour
{
    // ボタン
    [SerializeField]
    private PauseSelectButton[] m_buttons;

    // カーソルオブジェクト
    [SerializeField]
    private UnityEngine.UI.Image m_cursor;
    // カーソルが指しているボタン番号
    private int m_selectButtonNumber = 0;

    // カーソル移動させる場合に呼び出す関数
    [SerializeField]
    private UnityEngine.Events.UnityEvent<UnityEngine.UI.Image, Vector3> m_moveAction = null;

    private void Start()
    {
        // カーソルの座標初期化
        if(m_cursor != null)
        {
            m_cursor.transform.position = m_buttons[m_selectButtonNumber].transform.position;
        }

        // 選択している状態にする
        m_buttons[m_selectButtonNumber].OnCursor();
    }

    private void Update()
    {
        // 上下キーどちらかが押されていたら
        if (isPressedUpOrDown())
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

    // ----------------------------------------------------------------------

    // 上キーまたは下キーが押されているか
    private bool isPressedUpOrDown()
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
