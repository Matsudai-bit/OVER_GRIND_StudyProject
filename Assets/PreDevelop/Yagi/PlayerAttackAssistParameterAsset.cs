using UnityEngine;

/// <summary>攻撃開始時の対象検索と向き補正の設定を保持します。</summary>
[CreateAssetMenu(fileName = "PlayerAttackAssistParameter", menuName = "Game/Parameters/Player/Attack Assist")]
public sealed class PlayerAttackAssistParameterAsset : ScriptableObject
{
    [SerializeField, Min(0.0f), Tooltip("対象までの検索半径（m）。Collider距離を使用する場合は表面まで、それ以外はTransform位置までの距離です。")]
    private float m_searchRadius = 2.0f;

    [SerializeField, Range(0.0f, 360.0f), Tooltip("正面を中心とする検索角度。360で全方向。")]
    private float m_searchAngle = 360.0f;

    [SerializeField, Tooltip("登録オブジェクト自身にColliderがある場合、表面までの距離で検索します。無効なColliderは対象外です。")]
    private bool m_useColliderDistance = true;

    /// <summary>対象自身のCollider表面までの距離を使用するかを取得します。</summary>
    public bool UseColliderDistance => m_useColliderDistance;

    /// <summary>対象を検索する半径を取得します。</summary>
    public float SearchRadius => Mathf.Max(0.0f, m_searchRadius);

    /// <summary>正面を中心とする検索角度を取得します。</summary>
    public float SearchAngle => Mathf.Clamp(m_searchAngle, 0.0f, 360.0f);

}
