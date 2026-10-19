using UnityEngine;

/// <summary>PlayerAnimationPresenterの表示設定を保持します。</summary>
[CreateAssetMenu(fileName = "PlayerAnimationPresenterParameter", menuName = "Game/Parameters/Player/Animation Presenter")]
public sealed class PlayerAnimationPresenterParameterAsset : ScriptableObject
{
    [SerializeField, Min(0.0f)] private float m_moveSpeedDampTime = 0.1f;
    public float MoveSpeedDampTime => Mathf.Max(0.0f, m_moveSpeedDampTime);
}
