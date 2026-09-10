using UnityEngine;
using UnityEngine.InputSystem;

public class OptionPageManager : MonoBehaviour
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
    // 矢印イメージ
    [SerializeField]
    private UnityEngine.UI.Image m_arrowImage;
    // Left矢印イメージ
    [SerializeField]
    private UnityEngine.UI.Image m_arrowLImage;
    // Right矢印イメージ
    [SerializeField]
    private UnityEngine.UI.Image m_arrowRImage;

    [Header("マニュアルテクスチャ")]
    // 通常時マニュアルテクスチャ
    [SerializeField]
    private Sprite m_defaultManualTexture;
    // 選択時マニュアルテクスチャ
    [SerializeField]
    private Sprite m_OnCursorManualTexture;
    // マニュアルイメージコンポーネント
    [SerializeField]
    private UnityEngine.UI.Image m_manualImage;

    [Header("オプションテクスチャ")]
    // 通常時マニュアルテクスチャ
    [SerializeField]
    private Sprite m_defaultOptionTexture;
    // 選択時マニュアルテクスチャ
    [SerializeField]
    private Sprite m_OnCursorOptionTexture;
    // マニュアルイメージコンポーネント
    [SerializeField]
    private UnityEngine.UI.Image m_optionImage;

    [Space(20)]
    [Header("メイン関連")]
    // 通常時左コンテナテクスチャ
    [SerializeField]
    private Sprite m_defaultContainerLTexture;
    // 選択時左コンテナテクスチャ
    [SerializeField]
    private Sprite m_OnCursorContainerLTexture;
    // 左コンテナテクスチャイメージコンポーネント
    [SerializeField]
    private UnityEngine.UI.Image m_containerLImage;

    private void Update()
    {
        // ページの切り替え
        if(Keyboard.current.qKey.wasPressedThisFrame)
        {
            // マニュアルページにする
            m_choseState = ChoseState.MANUAL;
        }
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            // オプションページにする
            m_choseState = ChoseState.OPTION;
        }
    }
}
