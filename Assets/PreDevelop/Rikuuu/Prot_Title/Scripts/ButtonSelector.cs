using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class ButtonSelector : MonoBehaviour
{
    // 上下判定のしきい値
    private const float NAVIGATE_THRESHOLD = 0.75f;

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

    // カーソルが外れたときに自信を残したままにするかどうか
    [SerializeField]
    private bool m_keepSelectionWhenLocked = false;

    // カーソルがロックされているかどうか
    private bool m_isLockCursor = true;
    // カーソルの固定・解除の予約
    private bool m_cursorLockRequested;
    private bool m_cursorUnlockRequested;

    [Header("入力判定関連")]
    // 上キーが押される判定
    [SerializeField] 
    private InputActionReference m_navigateActionRef;
    // 決定キーが押される判定
    [SerializeField]
    private InputActionReference m_enterActionRef;

    // 前フレームからのスティック移動距離
    private Vector2 m_previousNav = Vector2.zero;

    private void OnEnable()
    {
        if(m_navigateActionRef == null)
        {
            Debug.LogWarning($"{gameObject.name}: {m_navigateActionRef} が設定されていません。Inspectorで割り当ててください。", this);
            return;
        }
        if(m_enterActionRef == null)
        {
            Debug.LogWarning($"{gameObject.name}: {m_enterActionRef} が設定されていません。Inspectorで割り当ててください。", this);
            return;
        }

        // 有効にする
        m_navigateActionRef?.action.Enable();
        m_enterActionRef?.action.Enable();
    }

    private void OnDisable()
    {
        // 無効にする
        m_navigateActionRef?.action.Disable();
        m_enterActionRef?.action.Disable();
    }

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
        // 入力方向
        Vector2 nav = m_navigateActionRef.action.ReadValue<Vector2>();

        // 上下キーどちらかが押されていたら || カーソルが固定されていなかったら
        if (wasPressedUpOrDown() && !m_isLockCursor)
        {
            // カーソルを離れるときの関数を実行する
            m_buttons[m_selectButtonNumber].OnCursorExit();
            
            // 上キーが押されたら
            if (nav.y > NAVIGATE_THRESHOLD && 
                m_previousNav.y <= NAVIGATE_THRESHOLD)
            {
                // カーソルを一つ上に移動させる
                m_selectButtonNumber--;
                //Debug.Log("MoveUP");
            }
            // 下キーが押されたら
            if (nav.y < -NAVIGATE_THRESHOLD && 
                m_previousNav.y >= -NAVIGATE_THRESHOLD)
            {
                // カーソルを一つ下に移動させる
                m_selectButtonNumber++;
                //Debug.Log("MoveDOWN");
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
            if (m_enterActionRef != null && m_enterActionRef.action.WasPressedThisFrame())
            {
                // キーが押されたときの処理を実行する
                m_buttons[m_selectButtonNumber].OnClick();
            }
            // 決定ボタンが離されたら
            if (m_enterActionRef != null && m_enterActionRef.action.WasReleasedThisFrame())
            {
                // キーが押されたときの処理を実行する
                m_buttons[m_selectButtonNumber].OnClickExit();
            }
        }

        // 今フレームの値を保存し、次フレームの比較に使う
        m_previousNav = nav;
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
        // 入力方向
        Vector2 nav = m_navigateActionRef.action.ReadValue<Vector2>();

        return (nav.y > NAVIGATE_THRESHOLD && m_previousNav.y <= NAVIGATE_THRESHOLD ||
                nav.y < -NAVIGATE_THRESHOLD && m_previousNav.y >= -NAVIGATE_THRESHOLD);
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
