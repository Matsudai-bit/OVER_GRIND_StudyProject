using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class SwitchParamater : MonoBehaviour
{
    // 何の値かを格納する
    [SerializeField]
    private string m_paramaterName = "null";
    // テキストコンポーネント
    [SerializeField]
    private TextMeshProUGUI m_paramaterText;

    // 初期の値
    [SerializeField]
    private bool m_defaultValue = false;

    [Header("テクスチャ関連")]
    // ONになったときのテクスチャ
    [SerializeField]
    private Sprite m_trueTexture;
    // OFFになったときのテクスチャ
    [SerializeField]
    private Sprite m_falseTexture;
    // スイッチテクスチャイメージコンポーネント
    [SerializeField]
    private UnityEngine.UI.Image m_switchImage;

    // 値の変更状態を固定・解除の有無
    private bool m_isLocked = true;

    private void Start()
    {
        // 文字の置き換え
        m_paramaterText.text = m_paramaterName;
        
        // 初期値に応じて画像を変更する
        SwitchTexture();
    }

    private void Update()
    {
        if(!m_isLocked)
        {
            // スペースキーが押されたら
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                // 値を変更する
                SwitchValue();
            }
        }
    }

    public void SwitchValue()
    {
        // 値を入れ替える
        m_defaultValue = !m_defaultValue;
        // 画像を変更する
        SwitchTexture();
    }

    private void SwitchTexture()
    {
        // 値がONの場合
        if (m_defaultValue)
        {
            // ONの画像を表示する
            m_switchImage.sprite = m_trueTexture;
        }
        // 値がOFFの場合
        else
        {
            // OFFの画像を表示する
            m_switchImage.sprite= m_falseTexture;
        }
    }

    public void OnCursor()
    {
        m_isLocked = false;
    }

    public void OnCursorExit()
    {
        m_isLocked = true;
    }
}
