using UnityEngine;
using UnityEngine.InputSystem;

public class ContainerController : MonoBehaviour
{
    // ページの選択状況
    enum ChoseState
    {
        NONE = 0,   // なし

        LEFT,       // 左コンテナ
        RIGHT       // 右コンテナ
    }
    // 開いているページ
    ChoseState m_choseState = ChoseState.NONE;

    [Header("左コンテナ関連")]
    // 通常時左コンテナテクスチャ
    [SerializeField]
    private Sprite m_defaultContainerLTexture;
    // 選択時左コンテナテクスチャ
    [SerializeField]
    private Sprite m_OnCursorContainerLTexture;
    // 左コンテナテクスチャイメージコンポーネント
    [SerializeField]
    private UnityEngine.UI.Image m_containerLImage;

    [Header("右コンテナ関連")]
    // 通常時左コンテナテクスチャ
    [SerializeField]
    private Sprite m_defaultContainerRTexture;
    // 選択時左コンテナテクスチャ
    [SerializeField]
    private Sprite m_OnCursorContainerRTexture;
    // 左コンテナテクスチャイメージコンポーネント
    [SerializeField]
    private UnityEngine.UI.Image m_containerRImage;

    // 左コンテナにあるボタンセレクター
    [SerializeField]
    private ButtonSelector[] m_buttonSelectorL;
    // 右コンテナにあるボタンセレクター
    [SerializeField]
    private ButtonSelector[] m_buttonSelectorR;

    [Header("入力判定関連")]
    // 上キーが押される判定
    [SerializeField]
    private InputActionReference m_backActionRef;

    private void OnEnable()
    {
        if (m_backActionRef == null)
        {
            Debug.LogWarning($"{gameObject.name}: {m_backActionRef} が設定されていません。Inspectorで割り当ててください。", this);
            return;
        }

        // 有効にする
        m_backActionRef?.action.Enable();
    }

    private void OnDisable()
    {
        // 無効にする
        m_backActionRef?.action.Disable();
    }

    private void Start()
    {
        // 選択状態の初期化
        m_choseState = ChoseState.LEFT;
        // 左を選択している状態に切り替える
        ChangeToLeft();
    }

    private void Update()
    {
        // escキーが押されたら
        if (m_choseState == ChoseState.RIGHT &&
            m_backActionRef != null && 
            m_backActionRef.action.WasPressedThisFrame())
        {
            // 左にカーソルを合わせる
            ChangeToLeft();
        }
    }

    public void ChangeToLeft()
    {
        // 選択状態の更新
        m_choseState = ChoseState.LEFT;
        // 表示するテクスチャを入れ替える
        m_containerLImage.sprite = m_OnCursorContainerLTexture;
        m_containerRImage.sprite = m_defaultContainerRTexture;

        // カーソルを固定・解除する
        foreach (var selector in m_buttonSelectorL)
        {
            if (selector.isActiveAndEnabled)
            {
                selector.UnLockCursor();
            }
        }
        foreach (var selector in m_buttonSelectorR)
        {
            if (selector.isActiveAndEnabled)
            {
                selector.LockCursor();
            }
        }
    }

    public void ChangeToRight()
    {
        // 選択状態の更新
        m_choseState = ChoseState.RIGHT;
        // 表示するテクスチャを入れ替える
        m_containerLImage.sprite = m_defaultContainerLTexture;
        m_containerRImage.sprite = m_OnCursorContainerRTexture;

        // カーソルを固定・解除する
        foreach (var selector in m_buttonSelectorL)
        {
            if(selector.isActiveAndEnabled)
            {
                selector.LockCursor();
            }
        }
        foreach (var selector in m_buttonSelectorR)
        {
            if (selector.isActiveAndEnabled)
            {
                selector.UnLockCursor();
            }
        }
    }
}
