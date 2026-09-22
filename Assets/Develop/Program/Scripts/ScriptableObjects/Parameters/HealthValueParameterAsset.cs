using UnityEngine;

/// <summary>
/// 体力調整用パラメータ
/// </summary>
[CreateAssetMenu(
    fileName = "S1P1BossMissileParameter",
    menuName = "Game/Parameters/Health")]
public class HealthValueParameterAsset : ScriptableObject
{
    [SerializeField, Header("HP設定")]
    private int m_hp;

    /// <summary>
    /// 体力量の取得
    /// </summary>
    public int HpValue { get { return m_hp; } }

}
