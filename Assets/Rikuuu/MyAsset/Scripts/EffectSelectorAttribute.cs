using UnityEngine; // UnityEngine の基本機能を使用するための using

/// <summary>
/// Inspector でエフェクト選択用ドロップダウンを表示するための属性
/// </summary>
public class EffectSelectorAttribute : PropertyAttribute // PropertyAttribute を継承してカスタム属性を作成
{
    private readonly string m_managerFieldName; // EffectManager を参照するフィールド名を保持する変数

    /// <summary>
    /// エフェクト一覧を取得するためのフィールド名を指定するコンストラクタ
    /// </summary>
    /// <param name="managerFieldName">EffectManager を参照するフィールド名</param>
    public EffectSelectorAttribute(string managerFieldName) // コンストラクタ
    {
        m_managerFieldName = managerFieldName; // 渡されたフィールド名を保存
    }

    /// <summary>
    /// EffectManager を参照するフィールド名を取得する
    /// </summary>
    /// <returns>フィールド名の文字列</returns>
    public string GetManagerFieldName() // フィールド名を返すメソッド
    {
        return m_managerFieldName; // 保存していたフィールド名を返す
    }
}
