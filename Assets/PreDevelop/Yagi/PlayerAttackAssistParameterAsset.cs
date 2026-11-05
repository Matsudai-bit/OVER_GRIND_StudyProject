using UnityEngine;

/// <summary>攻撃開始時の対象検索と向き補正の設定を保持します。</summary>
[CreateAssetMenu(fileName = "PlayerAttackAssistParameter", menuName = "Game/Parameters/Player/Attack Assist")]
public sealed class PlayerAttackAssistParameterAsset : ScriptableObject
{
    [Header("地上攻撃")]
    [SerializeField, InspectorName("検索半径"), Min(0.0f), Tooltip("対象までの検索半径（m）。Collider距離を使用する場合は表面まで、それ以外はTransform位置までの距離です。")]
    private float m_searchRadius = 2.0f;

    [SerializeField, InspectorName("検索角度"), Range(0.0f, 360.0f), Tooltip("正面を中心とする検索角度。360で全方向。")]
    private float m_searchAngle = 360.0f;

    [SerializeField, InspectorName("Collider距離を使用"), Tooltip("登録オブジェクト自身にColliderがある場合、表面までの距離で検索します。無効なColliderは対象外です。")]
    private bool m_useColliderDistance = true;

    [Header("空中攻撃")]
    [SerializeField, InspectorName("検索半径"), Min(0.0f), Tooltip("空中攻撃で対象を検索する距離（m）。")]
    private float m_airSearchRadius = 2.0f;

    [SerializeField, InspectorName("検索角度"), Range(0.0f, 360.0f), Tooltip("正面を中心とする検索角度。360で全方向。")]
    private float m_airSearchAngle = 360.0f;

    [SerializeField, InspectorName("Collider距離を使用"), Tooltip("対象自身の有効なCollider表面までの距離で検索します。")]
    private bool m_airUseColliderDistance = true;

    /// <summary>攻撃種別に対応する検索半径を取得します。</summary>
    /// <param name="isAirAttack">true：空中攻撃。false：地上攻撃。</param>
    /// <returns>対象を検索する半径（m）。</returns>
    public float GetSearchRadius(bool isAirAttack) =>
        isAirAttack ? Mathf.Max(0.0f, m_airSearchRadius) : SearchRadius;

    /// <summary>攻撃種別に対応する検索角度を取得します。</summary>
    /// <param name="isAirAttack">true：空中攻撃。false：地上攻撃。</param>
    /// <returns>正面を中心とする検索角度（度）。</returns>
    public float GetSearchAngle(bool isAirAttack) =>
        isAirAttack ? Mathf.Clamp(m_airSearchAngle, 0.0f, 360.0f) : SearchAngle;

    /// <summary>攻撃種別に対応する距離の判定方法を取得します。</summary>
    /// <param name="isAirAttack">true：空中攻撃。false：地上攻撃。</param>
    /// <returns>true：Collider表面までの距離。false：Transform位置までの距離。</returns>
    public bool GetUseColliderDistance(bool isAirAttack) =>
        isAirAttack ? m_airUseColliderDistance : UseColliderDistance;

    /// <summary>対象自身のCollider表面までの距離を使用するかを取得します。</summary>
    public bool UseColliderDistance => m_useColliderDistance;

    /// <summary>対象を検索する半径を取得します。</summary>
    public float SearchRadius => Mathf.Max(0.0f, m_searchRadius);

    /// <summary>正面を中心とする検索角度を取得します。</summary>
    public float SearchAngle => Mathf.Clamp(m_searchAngle, 0.0f, 360.0f);

}
