using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class GaugeParamater : MonoBehaviour
{
    // 何の値かを格納する
    [SerializeField]
    private string m_paramaterName = "null";
    // テキストコンポーネント
    [SerializeField]
    private TextMeshProUGUI m_paramaterText;

    // 最大値
    [SerializeField]
    private float MAX_VALUE = 1.0f;
    // 最小値
    [SerializeField]
    private float MIN_VALUE = 0.0f;
    // 値の変化量
    [SerializeField]
    private float VALUE_AMOUNT = 0.1f;
    // 初期の値
    [SerializeField]
    private float m_defaultValue = 0.5f;
    // 現在の値を表示するテキストコンポーネント
    [SerializeField]
    private TextMeshProUGUI m_valueText;

    [Header("テクスチャ関連")]
    // ゲージテクスチャイメージコンポーネント
    [SerializeField]
    private UnityEngine.UI.Image m_gaugeImage;

    // 値の変更状態を固定・解除の有無
    private bool m_isLocked = true;

    private void Start()
    {
        // 文字の置き換え
        m_valueText.text = m_defaultValue.ToString();
        m_paramaterText.text = m_paramaterName;
    }

    private void Update()
    {
        // 固定されていなければ
        if(!m_isLocked)
        {
            // 左キーが押されたら
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
            {
                // 値を減少させる
                m_defaultValue -= VALUE_AMOUNT;
            }
            // 右キーが押されたら
            if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
            {
                // 値を増加させる
                m_defaultValue += VALUE_AMOUNT;
            }
        }

        // 値の制限
        m_defaultValue = Mathf.Clamp(m_defaultValue, MIN_VALUE, MAX_VALUE);

        // 文字の置き換え
        m_valueText.text = m_defaultValue.ToString("F1");

        // イメージの割合表示
        m_gaugeImage.fillAmount = m_defaultValue;
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
