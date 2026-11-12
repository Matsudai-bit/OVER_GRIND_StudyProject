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

    [Header("共通処理")]
    // カーソルが合わさったときに実行する関数
    [SerializeField]
    private UnityEngine.Events.UnityEvent m_onCursorAction;
    // カーソルが離れたときに実行する関数
    [SerializeField]
    private UnityEngine.Events.UnityEvent m_onCursorExitAction;
    // クリックされたときに実行する関数
    [SerializeField]
    private UnityEngine.Events.UnityEvent m_onClickAction;
    // クリックが離れたときに実行する関数
    [SerializeField]
    private UnityEngine.Events.UnityEvent m_onClickExitAction;

    [Header("関数呼び出しに関する処理")]
    [Tooltip("開始時に共通処理の関数を呼ぶかどうか")]
    [SerializeField]
    private bool m_isInvokeOnStart = false;
    [Tooltip("開始時、カーソル解除時の関数をボタンの数だけ呼ぶかどうか")]
    [SerializeField]
    private bool m_isCallCursorExitForAllButtons = false;

    [Header("カーソル")]
    // カーソルオブジェクト
    [SerializeField]
    private UnityEngine.UI.Image m_cursor;
    // カーソルが指しているボタン番号
    private int m_selectButtonNumber = 0;

    // カーソル移動させる場合に呼び出す関数
    [SerializeField]
    private UnityEngine.Events.UnityEvent<UnityEngine.UI.Image, Vector3> m_moveAction = null;
    // 停止用のイベント
    [SerializeField]
    private UnityEngine.Events.UnityEvent<UnityEngine.UI.Image> m_stopMoveAction = null;

    // カーソルが外れたときに自身を残したままにするかどうか
    [SerializeField]
    private bool m_keepSelectionWhenLocked = false;

    [Header("動作設定")]
    // ロック時にカーソルの座標を初期位置へ戻すかどうか
    [SerializeField] private bool m_resetCursorPositionOnLock = false;

    // 開始時にロック状態にするかどうか
    [SerializeField]
    private bool m_lockOnStart = false;

    // 上下移動するかどうか
    [SerializeField]
    private bool m_isVerticalNavigation = true;

    // カーソルのアニメーションのロック
    [SerializeField]
    private bool m_isLockedCursorAnimation = false;

    [Header("入力判定関連")]
    // 上キーが押される判定
    [SerializeField]
    private InputActionReference m_navigateActionRef;
    // 決定キーが押される判定
    [SerializeField]
    private InputActionReference m_enterActionRef;

    // カーソルの初期座標(Start 時に記録)
    private Vector3 m_cursorInitialPosition;
    private bool m_hasCursorInitialPosition = false;

    // 前フレームからのスティック移動距離
    private Vector2 m_previousNav = Vector2.zero;

    // カーソルがロックされているかどうか
    [SerializeField]
    private bool m_isLockCursor = false;

    // 初期化を行ったかどうか
    private bool m_initialized = false;
    // ロックが解除されたフレーム
    private int m_unlockFrame = -1;

    private void OnEnable()
    {
        // 先に null チェックを行う
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

        // 初回の有効化時だけ初期ロック状態を反映（2回目以降は状態を保持）
        if (!m_initialized)
        {
            m_isLockCursor = m_lockOnStart;
            m_initialized = true;
        }

        m_navigateActionRef.action.Enable();
        m_enterActionRef.action.Enable();

        // 押されっぱなしの入力を新規入力と誤判定しないよう、現在値で初期化
        m_previousNav = m_navigateActionRef.action.ReadValue<Vector2>();
    }

    private void OnDisable()
    {

    }

    private void Start()
    {
        // 初期座標を記録
        if (m_cursor != null)
        {
            m_cursorInitialPosition = m_cursor.transform.position;
            m_hasCursorInitialPosition = true;
        }

        if (m_buttons == null || m_buttons.Length == 0)
        {
            return;
        }

        // 共通の Exit を1回だけ呼ぶ場合
        if (m_isInvokeOnStart && !m_isCallCursorExitForAllButtons)
        {
            m_onCursorExitAction?.Invoke();
        }

        for (int i = 0; i < m_buttons.Length; i++)
        {
            // ボタンの数だけ呼ぶ場合
            if (m_isInvokeOnStart && m_isCallCursorExitForAllButtons)
            {
                m_onCursorExitAction?.Invoke();
            }

            // 選択していない状態にする
            m_buttons[i].OnCursorExit();
        }

        // 選択中のボタンだけ OnCursor を呼ぶ
        if (m_isInvokeOnStart)
        {
            m_onCursorAction?.Invoke();
        }
        m_buttons[m_selectButtonNumber].OnCursor();
    }

    private void Update()
    {
        if (m_navigateActionRef == null || m_buttons == null || m_buttons.Length == 0)
        {
            return;
        }

        // 入力方向
        Vector2 nav = m_navigateActionRef.action.ReadValue<Vector2>();

        // 現在・前フレームの「進行方向の軸」の値を取得
        float current = GetAxisValue(nav);
        float previous = GetAxisValue(m_previousNav);

        // 移動キーが押された瞬間 && カーソルが固定されていなかったら
        if (wasPressedNavigate() && !m_isLockCursor)
        {
            // 共通の Exit 関数を呼ぶ(1回の移動につき1回)
            m_onCursorExitAction?.Invoke();

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

            // 共通の Cursor 関数を呼ぶ
            m_onCursorAction?.Invoke();
            // カーソルが乗ったときの処理を呼ぶ
            m_buttons[m_selectButtonNumber].OnCursor();
        }

        // カーソルがロックされていない場合
        if (!m_isLockCursor)
        {
            // カーソルの座標更新
            if (m_cursor != null && !m_isLockedCursorAnimation)
            {
                if (m_moveAction.GetPersistentEventCount() > 0)
                {
                    m_moveAction.Invoke(
                        m_cursor,
                        GetButtonCenter(m_selectButtonNumber)
                    );
                }
                else
                {
                    m_cursor.transform.position = GetButtonCenter(m_selectButtonNumber);
                }
            }

            bool justUnlocked = (m_unlockFrame == Time.frameCount);
            // 決定ボタンが押されたら
            if (!justUnlocked && m_enterActionRef != null && m_enterActionRef.action.WasPressedThisFrame())
            {
                // 押されたときの共通処理を実行する
                m_onClickAction?.Invoke();

                // キーが押されたときの処理を実行する
                m_buttons[m_selectButtonNumber].OnClick();
            }
            // 決定ボタンが離されたら
            if (!justUnlocked && m_enterActionRef != null && m_enterActionRef.action.WasReleasedThisFrame())
            {
                // 離されたときの共通処理を実行する
                m_onClickExitAction?.Invoke();

                // キーが離されたときの処理を実行する
                m_buttons[m_selectButtonNumber].OnClickExit();
            }
        }

        // 今フレームの値を保存し、次フレームの比較に使う
        m_previousNav = nav;
    }

    // ----------------------------------------------------------------------

    // カーソルのリセット
    public void ResetCursor()
    {
        // 共通の Exit 関数を呼ぶ
        m_onCursorExitAction?.Invoke();
        // 選択していない状態にする
        m_buttons[m_selectButtonNumber].OnCursorExit();

        // 選択されているカーソル番号をリセット
        m_selectButtonNumber = 0;

        // 共通の Cursor 関数を呼ぶ
        m_onCursorAction?.Invoke();
        // 選択している状態にする
        m_buttons[m_selectButtonNumber].OnCursor();
    }

    // カーソルのロック
    public void LockCursor()
    {
        m_isLockCursor = true;

        // 見た目の更新は、有効なときだけ行う
        if (isActiveAndEnabled && !m_keepSelectionWhenLocked &&
            m_buttons != null && m_buttons.Length > 0)
        {
            if (m_isCallCursorExitForAllButtons)
            {
                // ボタンの数だけ呼ぶ
                for (int i = 0; i < m_buttons.Length; i++)
                {
                    m_onCursorExitAction?.Invoke();
                    m_buttons[i].OnCursorExit();
                }
            }
            else
            {
                // 1回だけ呼ぶ
                m_onCursorExitAction?.Invoke();
                m_buttons[m_selectButtonNumber].OnCursorExit();
            }
        }

        // カーソルが外れたので、座標を初期位置に戻す
        if (m_resetCursorPositionOnLock)
        {
            ResetCursorPosition();
        }
    }

    public bool IsLockCursor => m_isLockCursor;

    // カーソルのロックを解除する
    public void UnLockCursor()
    {
        m_isLockCursor = false;
        m_unlockFrame = Time.frameCount;   // 解除したフレームを記録

        if (isActiveAndEnabled && m_buttons != null && m_buttons.Length > 0)
        {
            m_onCursorAction?.Invoke();
            m_buttons[m_selectButtonNumber].OnCursor();
        }
    }

    public void LockCursorAnimation()
    {
        m_isLockedCursorAnimation = true;
        // 進行中の移動アニメーションを停止するイベントを発火
        if (m_cursor != null && m_stopMoveAction != null && m_stopMoveAction.GetPersistentEventCount() > 0)
        {
            m_stopMoveAction.Invoke(m_cursor);
        }
    }

    public void UnLockCursorAnimation()
    {
        m_isLockedCursorAnimation = false;
    }

    // ----------------------------------------------------------------------

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

    // カーソルの座標を初期位置に戻す
    private void ResetCursorPosition()
    {
        if (m_cursor == null || !m_hasCursorInitialPosition)
        {
            return;
        }

        // 進行中のアニメーションがあれば先に停止してから戻す
        if (m_stopMoveAction != null && m_stopMoveAction.GetPersistentEventCount() > 0)
        {
            m_stopMoveAction.Invoke(m_cursor);
        }

        m_cursor.transform.position = m_cursorInitialPosition;

        // 今選択されているボタンの見た目を解除してから番号を戻す
        m_buttons[m_selectButtonNumber].OnCursorExit();
        m_selectButtonNumber = 0;
    }

    // ボタンの見た目上の中心(ワールド座標)を返す
    private Vector3 GetButtonCenter(int index)
    {
        RectTransform rect = m_buttons[index].transform as RectTransform;
        if (rect == null)
        {
            return m_buttons[index].transform.position;
        }
        return rect.TransformPoint(rect.rect.center);
    }
}