using UnityEngine;

/// <summary>PlayerRootControllerが使用するPlayer設定Asset群を保持します。</summary>
[CreateAssetMenu(fileName = "PlayerRootControllerParameter", menuName = "Game/Parameters/Player/Root Controller")]
public sealed class PlayerRootControllerParameterAsset : ScriptableObject
{
    [SerializeField] private PlayerMovementParameterAsset m_movementParameterAsset;
    [SerializeField] private PlayerVBoostMovementParameterAsset m_vBoostMovementParameterAsset;
    [SerializeField] private PlayerBoostChargingParameterAsset m_boostChargingParameterAsset;

    public PlayerMovementParameterAsset MovementParameterAsset => m_movementParameterAsset;
    public PlayerVBoostMovementParameterAsset VBoostMovementParameterAsset => m_vBoostMovementParameterAsset;
    public PlayerBoostChargingParameterAsset BoostChargingParameterAsset => m_boostChargingParameterAsset;
}
