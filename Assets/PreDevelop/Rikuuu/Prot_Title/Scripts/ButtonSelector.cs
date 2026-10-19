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

    // カーソルが外れたときに自身を残したままにするかどうか
    [SerializeField]
    private bool m_keepSelectionWhenLocked = false;

    // 開始時にロック状態にするかどうか
    [SerializeField]
    private bool m_lockOnStart = false;

    // 上下移動するかどうか
    [SerializeField]
    private bool m_isVerticalNavigation = true;

    // カーソルがロックされているかどうか
    private bool m_isLockCursor = false;
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

    // 初期化を行ったかどうか
    private bool m_initialized = false;
    // ロックが解除されたフレーム
    private int m_unlockFrame = -1;

    private void OnEnable()
    {
        // 初回の有効化時だけ初期ロック状態を反映（2回目以降は状態を保持）
        if (!m_initialized)
        {
            m_isLockCursor = m_lockOnStart;
            m_initialized = true;
        }

        // 押されっぱなしの入力を新規入力と誤判定しないよう、現在値で初期化
        m_previousNav = (m_navigateActionRef != null)
            ? m_navigateActionRef.action.ReadValue<Vector2>()
            : Vector2.zero;

        if (m_navigateActionRef == null)
        {
            Debug.LogWarning($"{gameObject.name}: m_navigateActionRef が設定されていません。", this);
            return;
        }
        if (m_enterActionRef == null)
        {
            Debug.LogWarning($"{gameObject.name}: m_enterActionRef が設定されていません。", this);
            return;
        }
    }

    private void OnDisable()
    {
        // 無効にする
        //m_navigateActionRef?.action.Disable();
        //m_enterActionRef?.action.Disable();
    }

    private void Start()
    {
        if (m_buttons != null)
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

        // 現在・前フレームの「進行方向の軸」の値を取得
        float current = GetAxisValue(nav);
        float previous = GetAxisValue(m_previousNav);

        // 移動キーが押された瞬間 && カーソルが固定されていなかったら
        if (wasPressedNavigate() && !m_isLockCursor)
        {
            // カーソルを離れるときの関数を実行する
            m_buttons[m_selectButtonNumber].OnCursorExit();

            // 上(縦) / 左(横) 方向に押されたら → 前のボタンへ
            // 縦: 上キーが +、横: 左キーが -  なので符号を揃える
            float forward = m_isVerticalNavigation ? current : -current;
            float forwardPrev = m_isVerticalNavigation ? previous : -previous;

            if (forward > NAVIGATE_THRESHOLD && forwardPrev <= NAVIGATE_THRESHOLD)
            {
                m_selectButtonNumber--;
                Debug.Log(m_isVerticalNavigation ? "MoveUP" : "MoveLEFT");
            }
            // 下(縦) / 右(横) 方向に押されたら → 次のボタンへ
            if (forward < -NAVIGATE_THRESHOLD && forwardPrev >= -NAVIGATE_THRESHOLD)
            {
                m_selectButtonNumber++;
                Debug.Log(m_isVerticalNavigation ? "MoveDOWN" : "MoveRIGHT");
            }

            // 範囲内に収める
            Clamp();
            // カーソルが乗ったときの処理を呼ぶ
            m_buttons[m_selectButtonNumber].OnCursor();
        }

        // カーソルがロックされていない場合
        if (!m_isLockCursor)
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

            bool justUnlocked = (m_unlockFrame == Time.frameCount);
            // 決定ボタンが押されたら
            if (!justUnlocked && m_enterActionRef != null && m_enterActionRef.action.WasPressedThisFrame())
            {
                // キーが押されたときの処理を実行する
                m_buttons[m_selectButtonNumber].OnClick();
            }
            // 決定ボタンが離されたら
            if (!justUnlocked && m_enterActionRef != null && m_enterActionRef.action.WasReleasedThisFrame())
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
        if (m_cursorLockRequested)
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
        if (m_cursorUnlockRequested)
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
        m_isLockCursor = true;

        // 見た目の更新は、有効なときだけ行う
        if (isActiveAndEnabled && !m_keepSelectionWhenLocked &&
            m_buttons != null && m_buttons.Length > 0)
        {
            m_buttons[m_selectButtonNumber].OnCursorExit();
        }
    }

    // カーソルのロックを解除する
    public void UnLockCursor()
    {
        m_isLockCursor = false;
        m_unlockFrame = Time.frameCount;   // 解除したフレームを記録

        if (isActiveAndEnabled && m_buttons != null && m_buttons.Length > 0)
        {
            m_buttons[m_selectButtonNumber].OnCursor();
        }
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

    // ナビゲーションキー(縦なら上下、横なら左右)が押された瞬間か
    private bool wasPressedNavigate()
    {
        Vector2 nav = m_navigateActionRef.action.ReadValue<Vector2>();

        float current = GetAxisValue(nav);
        float previous = GetAxisValue(m_previousNav);

        return (current > NAVIGATE_THRESHOLD && previous <= NAVIGATE_THRESHOLD) ||
               (current < -NAVIGATE_THRESHOLD && previous >= -NAVIGATE_THRESHOLD);
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

    // 現在の方向設定に応じた軸の値を返す(縦: y / 横: x)
    private float GetAxisValue(Vector2 v)
    {
        return m_isVerticalNavigation ? v.y : v.x;
    }
}