using UnityEngine; // UnityEngine の基本機能を使用するための using

/// <summary>
/// エフェクト一覧を表示するためのビュークラス
/// </summary>
public class EffectViewer : MonoBehaviour // MonoBehaviour を継承した通常のコンポーネント
{
    [SerializeField]
    private EffectManager m_effectManager; // EffectManager を参照するフィールド（インスペクタで設定）

    /// <summary>
    /// このビューが参照している EffectManager を取得する
    /// </summary>
    /// <returns>EffectManager のインスタンス</returns>
    public EffectManager GetEffectManager() // EffectManager を返すメソッド
    {
        return m_effectManager; // フィールドの値をそのまま返す
    }
}
