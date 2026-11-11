using UnityEngine;

/// <summary>各プレイヤー状態に対応するチェーンソーの表示速度を保持します。</summary>
[CreateAssetMenu(fileName = "PlayerChainsawSpeedData", menuName = "Game/Parameters/Player/Chainsaw Speed")]
public sealed class PlayerChainsawSpeedData : ScriptableObject
{
    [SerializeField, Tooltip("初期化前、または未対応のStateで使用する速度です。")]
    private float m_defaultSpeed;
    [SerializeField, Tooltip("PlayerIdlingStateで使用する速度です。")]
    private float m_idlingSpeed;
    [SerializeField, Tooltip("PlayerWalkingStateで使用する速度です。")]
    private float m_walkingSpeed;
    [SerializeField, Tooltip("PlayerJumpingStateで使用する速度です。")]
    private float m_jumpingSpeed;
    [SerializeField, Tooltip("PlayerVRunningStateで使用する速度です。")]
    private float m_vRunningSpeed;
    [SerializeField, Tooltip("PlayerBoostChargingStateで使用する速度です。")]
    private float m_boostChargingSpeed;
    [SerializeField, Tooltip("PlayerAttackingStateで使用する速度です。")]
    private float m_attackingSpeed;
    [SerializeField, Tooltip("PlayerGrindingStateで使用する速度です。")]
    private float m_grindingSpeed;
    [SerializeField, Tooltip("PlayerRailJumpingStateで使用する速度です。")]
    private float m_railJumpingSpeed;
    [SerializeField, Tooltip("PlayerRailExitingStateで使用する速度です。")]
    private float m_railExitingSpeed;
    [SerializeField, Tooltip("PlayerHitStateで使用する速度です。")]
    private float m_hitSpeed;

    /// <summary>指定した状態の表示速度を取得します。</summary>
    /// <param name="state">現在の表示状態。</param>
    /// <returns>ShaderのSpeedへ設定する値。負数も設定できます。</returns>
    public float GetSpeed(PlayerChainsawState state)
    {
        return state switch
        {
            PlayerChainsawState.IDLING => m_idlingSpeed,
            PlayerChainsawState.WALKING => m_walkingSpeed,
            PlayerChainsawState.JUMPING => m_jumpingSpeed,
            PlayerChainsawState.VRUNNING => m_vRunningSpeed,
            PlayerChainsawState.BOOST_CHARGING => m_boostChargingSpeed,
            PlayerChainsawState.ATTACKING => m_attackingSpeed,
            PlayerChainsawState.GRINDING => m_grindingSpeed,
            PlayerChainsawState.RAIL_JUMPING => m_railJumpingSpeed,
            PlayerChainsawState.RAIL_EXITING => m_railExitingSpeed,
            PlayerChainsawState.HIT => m_hitSpeed,
            _ => m_defaultSpeed,
        };
    }
}
