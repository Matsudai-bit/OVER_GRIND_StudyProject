using UnityEngine;

/// <summary>プレイヤーの状態に応じて、対象マテリアルのチェーンソー表示速度を変更します。</summary>
[DisallowMultipleComponent]
public sealed class PlayerChainsawPresenter : MonoBehaviour
{
    [SerializeField] private PlayerStateMachineComponent m_stateMachine;
    [SerializeField] private PlayerChainsawSpeedData m_speedData;
    [SerializeField, Tooltip("左のChainsawShoesのRendererです。")]
    private Renderer m_targetRenderer;
    [SerializeField, Tooltip("右のChainsawShoesのRendererです。")]
    private Renderer m_rightRenderer;
    [SerializeField, Min(0), Tooltip("左のChainsawマテリアルのスロット番号（0始まり）。")]
    private int m_materialIndex;
    [SerializeField, Min(0), Tooltip("右のChainsawマテリアルのスロット番号（0始まり）。")]
    private int m_rightMaterialIndex;

    private static readonly int SPEED_PROPERTY_ID = Shader.PropertyToID("_Speed");
    private MaterialPropertyBlock m_propertyBlock;
    private MaterialPropertyBlock m_originalPropertyBlock;
    private MaterialPropertyBlock m_rightPropertyBlock;
    private MaterialPropertyBlock m_originalRightPropertyBlock;
    private bool m_hadOriginalPropertyBlock;
    private bool m_hadOriginalRightPropertyBlock;
    private bool m_isInitialized;
    private float m_appliedSpeed;
    private bool m_hasAppliedSpeed;
    private bool m_isManualControl;
    private float m_manualSpeed;

    /// <summary>State設定より優先して、左右のチェーンソーを指定速度で回します。</summary>
    /// <param name="speed">Shaderへ設定する回転速度。</param>
    public void StartChainsaw(float speed)
    {
        m_isManualControl = true;
        m_manualSpeed = speed;
        if (m_isInitialized)
        {
            RefreshSpeed();
        }
    }

    /// <summary>左右のチェーンソーを停止し、停止状態を維持します。</summary>
    public void Stop()
    {
        m_isManualControl = true;
        m_manualSpeed = 0.0f;
        if (m_isInitialized)
        {
            RefreshSpeed();
        }
    }

    /// <summary>手動制御を解除し、現在のStateに応じた速度へ戻します。</summary>
    public void ResumeStateSpeed()
    {
        m_isManualControl = false;
        if (m_isInitialized)
        {
            RefreshSpeed();
        }
    }

    /// <summary>参照とShaderを検証し、有効化前の描画設定を保存します。</summary>
    private void OnEnable()
    {
        if (m_stateMachine == null || m_speedData == null ||
            m_targetRenderer == null || m_rightRenderer == null)
        {
            Debug.LogError("Chainsaw PresenterのState Machine、Speed Data、左右のRendererを設定してください。", this);
            enabled = false;
            return;
        }

        Material[] materials = m_targetRenderer.sharedMaterials;
        if (m_materialIndex < 0 || m_materialIndex >= materials.Length ||
            materials[m_materialIndex] == null ||
            !materials[m_materialIndex].HasProperty(SPEED_PROPERTY_ID) ||
            !HasSpeedMaterial(m_rightRenderer, m_rightMaterialIndex))
        {
            Debug.LogError("指定スロットに_Speedを持つマテリアルを設定してください。", this);
            enabled = false;
            return;
        }

        m_propertyBlock ??= new MaterialPropertyBlock();
        m_originalPropertyBlock ??= new MaterialPropertyBlock();
        m_rightPropertyBlock ??= new MaterialPropertyBlock();
        m_originalRightPropertyBlock ??= new MaterialPropertyBlock();
        m_targetRenderer.GetPropertyBlock(m_originalPropertyBlock, m_materialIndex);
        m_rightRenderer.GetPropertyBlock(m_originalRightPropertyBlock, m_rightMaterialIndex);
        m_hadOriginalPropertyBlock = !m_originalPropertyBlock.isEmpty;
        m_hadOriginalRightPropertyBlock = !m_originalRightPropertyBlock.isEmpty;
        m_isInitialized = true;
        m_hasAppliedSpeed = false;
        RefreshSpeed();
    }

    /// <summary>State更新後に表示速度を確認します。値が同じ場合は描画設定を更新しません。</summary>
    private void LateUpdate()
    {
        if (m_isInitialized && m_stateMachine != null &&
            m_speedData != null && m_targetRenderer != null && m_rightRenderer != null)
        {
            RefreshSpeed();
        }
    }

    /// <summary>現在の状態と設定データから速度を反映します。</summary>
    private void RefreshSpeed()
    {
        float speed = m_isManualControl
            ? m_manualSpeed
            : m_speedData.GetSpeed(GetCurrentState());
        if (m_hasAppliedSpeed && m_appliedSpeed == speed)
        {
            return;
        }

        // 他の設定値を保持したうえで、対象スロットのSpeedだけを変更します。
        m_targetRenderer.GetPropertyBlock(m_propertyBlock, m_materialIndex);
        if (m_propertyBlock.isEmpty)
        {
            m_targetRenderer.GetPropertyBlock(m_propertyBlock);
        }
        m_propertyBlock.SetFloat(SPEED_PROPERTY_ID, speed);
        m_targetRenderer.SetPropertyBlock(m_propertyBlock, m_materialIndex);
        m_rightRenderer.GetPropertyBlock(m_rightPropertyBlock, m_rightMaterialIndex);
        if (m_rightPropertyBlock.isEmpty)
        {
            m_rightRenderer.GetPropertyBlock(m_rightPropertyBlock);
        }
        m_rightPropertyBlock.SetFloat(SPEED_PROPERTY_ID, speed);
        m_rightRenderer.SetPropertyBlock(m_rightPropertyBlock, m_rightMaterialIndex);
        m_appliedSpeed = speed;
        m_hasAppliedSpeed = true;
    }

    /// <summary>指定Rendererのスロットに_Speedを持つマテリアルがあるか確認します。</summary>
    /// <param name="renderer">確認するRenderer。</param>
    /// <param name="materialIndex">確認するスロット番号。</param>
    /// <returns>対象が有効ならtrue。無効ならfalse。</returns>
    private static bool HasSpeedMaterial(Renderer renderer, int materialIndex)
    {
        Material[] materials = renderer.sharedMaterials;
        return materialIndex >= 0 && materialIndex < materials.Length &&
               materials[materialIndex] != null &&
               materials[materialIndex].HasProperty(SPEED_PROPERTY_ID);
    }

    /// <summary>現在のゲームプレイStateを表示用の状態へ変換します。</summary>
    /// <returns>対応する状態。初期化前または未対応のStateではDEFAULT。</returns>
    private PlayerChainsawState GetCurrentState()
    {
        if (m_stateMachine.IsCurrentState<PlayerIdlingState>()) return PlayerChainsawState.IDLING;
        if (m_stateMachine.IsCurrentState<PlayerWalkingState>()) return PlayerChainsawState.WALKING;
        if (m_stateMachine.IsCurrentState<PlayerJumpingState>()) return PlayerChainsawState.JUMPING;
        if (m_stateMachine.IsCurrentState<PlayerVRunningState>()) return PlayerChainsawState.VRUNNING;
        if (m_stateMachine.IsCurrentState<PlayerBoostChargingState>()) return PlayerChainsawState.BOOST_CHARGING;
        if (m_stateMachine.IsCurrentState<PlayerAttackingState>()) return PlayerChainsawState.ATTACKING;
        if (m_stateMachine.IsCurrentState<PlayerGrindingState>()) return PlayerChainsawState.GRINDING;
        if (m_stateMachine.IsCurrentState<PlayerRailJumpingState>()) return PlayerChainsawState.RAIL_JUMPING;
        if (m_stateMachine.IsCurrentState<PlayerRailExitingState>()) return PlayerChainsawState.RAIL_EXITING;
        if (m_stateMachine.IsCurrentState<PlayerHitState>()) return PlayerChainsawState.HIT;
        return PlayerChainsawState.DEFAULT;
    }

    /// <summary>無効化時に、このコンポーネントが変更したスロットの描画設定を復元します。</summary>
    private void OnDisable()
    {
        if (m_isInitialized && m_targetRenderer != null)
        {
            m_targetRenderer.SetPropertyBlock(
                m_hadOriginalPropertyBlock ? m_originalPropertyBlock : null, m_materialIndex);
        }
        if (m_isInitialized && m_rightRenderer != null)
        {
            m_rightRenderer.SetPropertyBlock(
                m_hadOriginalRightPropertyBlock ? m_originalRightPropertyBlock : null,
                m_rightMaterialIndex);
        }
        m_isInitialized = false;
        m_hasAppliedSpeed = false;
    }
}
