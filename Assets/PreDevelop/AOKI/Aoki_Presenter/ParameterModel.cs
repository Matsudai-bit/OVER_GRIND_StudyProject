/// @ using :: 使用ネームスペース
using System;
using UnityEngine;

/// @ className :: パラメータモデルクラス
/// @ name :: Aoki Hayate
/// @ date :: 2026/08/28
public class ParameterModel
{
    private string m_name;
    private int m_currentValue;
    private int m_maxValue;

    /// <summary> パラメータ名 </summary>
    public string Name => m_name;

    /// <summary> 現在の値 </summary>
    public int CurrentValue => m_currentValue;

    /// <summary> 最大値 </summary>
    public int MaxValue => m_maxValue;

    /// <summary> 値変更時に発行されるイベント </summary>
    public event Action OnValueChanged;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="name">パラメータ名</param>
    /// <param name="initialValue">初期値</param>
    /// <param name="maxValue">最大値</param>
    public ParameterModel(string name, int initialValue, int maxValue)
    {
        m_name = name;
        m_maxValue = maxValue;
        m_currentValue = Mathf.Clamp(initialValue, 0, maxValue);
    }

    /// <summary>
    /// 値を設定する（範囲外の値は 0 ? MaxValue に制限される）
    /// </summary>
    /// <param name="value">設定する値</param>
    public void SetValue(int value)
    {
        int clamped = Mathf.Clamp(value, 0, m_maxValue);
        if (m_currentValue == clamped) return;

        m_currentValue = clamped;
        OnValueChanged?.Invoke();
    }

    /// <summary>
    /// 表示用文字列（"現在値 / 最大値"）を取得する
    /// </summary>
    /// <returns>フォーマット済み文字列</returns>
    public string GetValueString() => $"{m_currentValue} / {m_maxValue}";
}