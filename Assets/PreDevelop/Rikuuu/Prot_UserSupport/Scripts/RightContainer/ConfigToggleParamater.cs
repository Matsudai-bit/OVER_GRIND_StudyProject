using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ON/OFF(トグル)式の設定項目UIを管理するクラス
/// </summary>
public class ConfigToggleParamater : MonoBehaviour
{
    [Header("UI参照")]
    /// <summary>設定を切り替えるトグルUI</summary>
    [SerializeField]
    private Toggle m_toggle;

    /// <summary>
    /// セーブデータからの初期値をUIに反映します。
    /// このメソッド経由で値を変更した際は、OnValueChangedイベントは発火しません。
    /// </summary>
    /// <param name="isOn">反映させるON/OFFの状態。</param>
    public void SetDefaultValue(bool isOn)
    {
        if (m_toggle != null)
        {
            // 見た目だけを更新し、余計なセーブ処理などを誘発させない
            m_toggle.SetIsOnWithoutNotify(isOn);
        }
    }
}