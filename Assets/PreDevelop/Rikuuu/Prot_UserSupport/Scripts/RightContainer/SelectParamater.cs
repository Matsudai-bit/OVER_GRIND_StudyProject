using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class SelectParamater : MonoBehaviour
{
    // 何の値かを格納する
    [SerializeField]
    private string m_paramaterName = "null";
    // テキストコンポーネント
    [SerializeField]
    private TextMeshProUGUI m_paramaterText;

    // 何の値かを格納する
    [SerializeField]
    private string[] m_selectName;
    // テキストコンポーネント
    [SerializeField]
    private TextMeshProUGUI m_selectText;

    // 初期の値
    [SerializeField]
    private int m_defaultValue = 0;

    // 値の変更状態を固定・解除の有無
    private bool m_isLocked = true;

    private void Start()
    {
        // 配列が空、または初期値が範囲外の場合に備えてクランプする
        if (m_selectText == null || m_selectName.Length == 0)
        {
            Debug.LogError($"{name}: m_selectTexture が未設定です", this);
            return;
        }

        // 表示するテクスチャの初期化
        m_paramaterText.text = m_paramaterName;
        m_selectText.text = m_selectName[m_defaultValue];
    }

    private void Update()
    {
        // 固定されていなければ
        if (!m_isLocked)
        {
            // 左キーが押されたら
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
            {
                // 値を減少させる
                m_defaultValue--;

                // 値が0以下になった場合
                if (m_defaultValue < 0)
                {
                    // 最後尾に戻す
                    m_defaultValue = m_selectName.Length - 1;
                }
            }
            // 右キーが押されたら
            if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
            {
                // 値を増加させる
                m_defaultValue++;

                // 値が配列の長さを超過した場合
                if(m_defaultValue >= m_selectName.Length)
                {
                    // 最初に戻す
                    m_defaultValue = 0;
                }
            }
        }

        // 値に合わせたテクスチャを表示する
        m_selectText.text = m_selectName[m_defaultValue];
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
