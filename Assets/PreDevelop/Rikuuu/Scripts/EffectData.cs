using UnityEngine; // UnityEngine の基本機能を使用するための using
using UnityEngine.VFX; // VisualEffect を使用するための using

/// <summary>
/// 1つのエフェクトを表すデータアセット
/// </summary>
[CreateAssetMenu(menuName = "Effect/EffectData")] // CreateAssetMenu により Unity メニューから作成可能にする
public class EffectData : ScriptableObject // ScriptableObject を継承してデータアセット化
{
    [SerializeField]
    private string m_effectName; // エフェクト名を保持するフィールド

    [SerializeField]
    private VisualEffect m_vfxAsset; // VisualEffect Graph のアセットを保持するフィールド

    /// <summary>
    /// エフェクト名を取得する
    /// </summary>
    /// <returns>エフェクト名の文字列</returns>
    public string GetEffectName() // エフェクト名を返すメソッド
    {
        return m_effectName; // フィールドの値を返す
    }

    /// <summary>
    /// VFX アセットを取得する
    /// </summary>
    /// <returns>VisualEffect</returns>
    public VisualEffect GetVfxAsset() // VFX アセットを返すメソッド
    {
        return m_vfxAsset; // フィールドの値を返す
    }
}
