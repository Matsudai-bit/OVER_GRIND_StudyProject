/// @ using :: 使用エンジン
using UnityEngine;
using TMPro;

/// @ className :: テキスト値表示UIクラス
/// @ name :: Aoki Hayate
/// @ date :: 2026/08/28
public class ValueTextView : MonoBehaviour
{
    /// <summary> パラメータ名を表示するテキスト </summary>
    [SerializeField] private TextMeshProUGUI m_paramNameText;

    /// <summary> 値を表示するテキスト </summary>
    [SerializeField] private TextMeshProUGUI m_valueText;

    /// <summary>
    /// パラメータ名と値をテキストUIに反映する
    /// </summary>
    /// <param name="paramName">表示する項目名</param>
    /// <param name="valueString">表示する値の文字列</param>
    public void Render(string paramName, string valueString)
    {
        if (m_paramNameText != null)
        {
            m_paramNameText.text = paramName;
        }

        if (m_valueText != null)
        {
            m_valueText.text = valueString;
        }
    }
}